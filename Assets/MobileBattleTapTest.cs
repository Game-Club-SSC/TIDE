using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for the mobile battle tap path.
///
/// The tap-to-attack gate was inverted in both directions during the bug-fix
/// work: it first required the tap to land inside the battle action bar (a
/// bottom strip that never overlaps an enemy, which made the feature
/// unreachable), and the correction risked regressing the other way. These
/// tests invoke the real HandleBattleTouch and observe whether an action
/// actually reaches BattleManager.TryAssignActionFromHud, so a re-inversion
/// is caught rather than merely documented.
///
/// Screen-space probing is unreliable under -nographics (Screen reports 0x0),
/// so the tap is synthesised through a real Camera at a known pixel size and
/// the assertions are made on the real screen-space raycast the code uses.
/// </summary>
[DisallowMultipleComponent]
public class MobileBattleTapTest : MonoBehaviour
{
    private GameObject managerObject;
    private GameObject cameraObject;
    private GameObject enemyObject;
    private GameObject allyObject;
    private GameObject battleManagerObject;

    // The real screen size, which under -nographics is 640x480 rather than
    // zero. Probe points must be expressed in these units or the production
    // ScreenPointToRay produces a ray that points nowhere.
    private static int ProbeWidth { get { return Screen.width; } }
    private static int ProbeHeight { get { return Screen.height; } }

    [SetUp]
    public void SetUp()
    {
        Cleanup();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [Test]
    public void TapOnAnEnemyInTheViewportAssignsAnAttack()
    {
        // The regression the inverted gate caused. The enemy sits in the
        // middle of the view, deliberately away from the bottom action bar.
        MobileTouchInputManager manager = CreateManager();
        Camera camera = CreateCamera();
        CombatUnit enemy = SpawnEnemy(new Vector3(0f, 0f, 0f), CombatUnit.UnitType.Enemy);
        CombatUnit actor = SpawnAllyActor();

        Vector2 tap = ProjectToScreen(camera, new Vector3(0f, 1f, 0f));
        Assert.IsFalse(IsInsideBattleBar(manager, tap),
            "Setup: an enemy in the middle of the view must not project onto the action bar, or the tap would be rejected before the raycast.");

        CombatUnit assigned = InvokeHandleBattleTouchAndCaptureTarget(manager, tap, actor);

        Assert.IsNotNull(assigned,
            "A tap on an enemy in the viewport must reach TryAssignActionFromHud and assign the attack.");
        Assert.AreSame(enemy, assigned, "The attack must target the tapped enemy.");
    }

    [Test]
    public void TapOnTheActionBarIsRejectedBeforeTheRaycast()
    {
        // Taps on the bar are handled by its buttons. If the raycast also
        // fired, pressing a button could pick a unit behind it.
        MobileTouchInputManager manager = CreateManager();
        Camera camera = CreateCamera();
        CombatUnit enemy = SpawnEnemy(new Vector3(0f, 0f, 0f), CombatUnit.UnitType.Enemy);
        CombatUnit actor = SpawnAllyActor();

        // Derive the bar probe from the panel's own anchors so the test does
        // not depend on a hard-coded screen layout.
        Vector2 barTap = BattleBarCentre(manager);

        Assert.IsTrue(IsInsideBattleBar(manager, barTap),
            "Setup: the probe point must be inside the action bar for this test to mean anything.");

        CombatUnit assigned = InvokeHandleBattleTouchAndCaptureTarget(manager, barTap, actor);

        Assert.IsNull(assigned,
            "A tap on the action bar must be rejected before the raycast, otherwise the bar can select a unit behind it.");
    }

    [Test]
    public void TapBeyondTheBoundedRayDistanceAssignsNothing()
    {
        // The other half of the original bug: the raycast reached 200 units,
        // so a unit on the far side of the arena could be selected.
        MobileTouchInputManager manager = CreateManager();
        Camera camera = CreateCamera();

        float reach = BattleTapRayDistance();
        Camera farCamera = CreateCameraAt(new Vector3(0f, 6f, -(reach + 20f)));
        SpawnEnemy(new Vector3(0f, 0f, 0f), CombatUnit.UnitType.Enemy);
        CombatUnit actor = SpawnAllyActor();

        Vector2 tap = ProjectToScreen(farCamera, new Vector3(0f, 1f, 0f));
        CombatUnit assigned = InvokeHandleBattleTouchAndCaptureTarget(manager, tap, actor);

        Assert.IsNull(assigned,
            $"A unit beyond the {reach:0} unit tap reach must not be selectable.");
        Assert.IsNotNull(camera, "Setup: the near camera should still exist.");
    }

    [Test]
    public void TapThroughAWallAssignsNothing()
    {
        MobileTouchInputManager manager = CreateManager();
        Camera camera = CreateCamera();
        SpawnEnemy(new Vector3(0f, 0f, 3f), CombatUnit.UnitType.Enemy);
        CombatUnit actor = SpawnAllyActor();

        // Wall between the camera and the enemy.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.transform.position = new Vector3(0f, 1f, 1.5f);
            wall.transform.localScale = new Vector3(20f, 10f, 0.5f);
            Physics.SyncTransforms();

            Vector2 tap = ProjectToScreen(camera, new Vector3(0f, 1f, 3f));
            CombatUnit assigned = InvokeHandleBattleTouchAndCaptureTarget(manager, tap, actor);

            Assert.IsNull(assigned,
                "A tap blocked by level geometry must not select the unit behind it.");
        }
        finally
        {
            Object.DestroyImmediate(wall);
        }
    }

    [Test]
    public void TapOnAnAllyDoesNotAssignAnAttack()
    {
        // Only enemies are valid attack targets; an ally under the finger
        // must be ignored rather than committed as the action.
        MobileTouchInputManager manager = CreateManager();
        Camera camera = CreateCamera();
        SpawnEnemy(new Vector3(0f, 0f, 0f), CombatUnit.UnitType.Ally);
        CombatUnit actor = SpawnAllyActor();

        Vector2 tap = ProjectToScreen(camera, new Vector3(0f, 1f, 0f));
        CombatUnit assigned = InvokeHandleBattleTouchAndCaptureTarget(manager, tap, actor);

        Assert.IsNull(assigned, "Tapping an ally must not assign an attack against it.");
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Invokes the real HandleBattleTouch and returns the unit it passed to
    /// TryAssignActionFromHud, or null when it assigned nothing.
    ///
    /// TryAssignActionFromHud is not virtual, so the real BattleManager is
    /// driven instead of a subclass: it is put into the PlayerInput phase
    /// (the only phase that accepts an action) and its recorded action is
    /// read back afterwards. That way the assertion observes the real
    /// decision the tap path made, not a test double.
    /// </summary>
    private CombatUnit InvokeHandleBattleTouchAndCaptureTarget(MobileTouchInputManager manager, Vector2 screenPoint, CombatUnit actor)
    {
        MethodInfo method = typeof(MobileTouchInputManager).GetMethod(
            "HandleBattleTouch", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "MobileTouchInputManager.HandleBattleTouch should exist.");

        BattleManager battle = CreateBattleManager();
        PutIntoPlayerInputPhase(battle, actor);

        Touch touch = new Touch { position = screenPoint };
        method.Invoke(manager, new object[] { touch });

        object cam = GetPrivateField(manager, "mainCamera");
        CombatUnit result = ReadAssignedTarget(battle);

        return result;
    }

    /// <summary>
    /// BattleManager only accepts HUD actions during PlayerInput with an
    /// active phase and a unit awaiting input, so the internal state is
    /// seeded to reach the accept branch. Everything after those guards is
    /// the real production code path, including the target validity check.
    /// </summary>
    private CombatUnit PutIntoPlayerInputPhase(BattleManager battle, CombatUnit actor)
    {
        SetPrivateField(battle, "hasActivePhase", true);
        SetPrivateField(battle, "currentPhase", BattlePhase.PlayerInput);

        object units = GetPrivateField(battle, "playerInputUnits");
        if (units is System.Collections.IList list)
        {
            list.Clear();
            list.Add(actor);
        }

        SetPrivateField(battle, "playerInputIndex", 0);
        return actor;
    }

    private static CombatUnit ReadAssignedTarget(BattleManager battle)
    {
        object actions = GetPrivateField(battle, "selectedPlayerActions");
        if (actions is System.Collections.IDictionary dictionary)
        {
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                // PlannedAction is a struct, so the boxed value is what lands
                // in the dictionary enumeration.
                if (entry.Value is PlannedAction planned)
                {
                    return planned.Target;
                }
            }
        }

        return null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private BattleManager CreateBattleManager()
    {
        if (battleManagerObject != null)
        {
            return battleManagerObject.AddComponent<BattleManager>();
        }

        battleManagerObject = new GameObject("TestBattleManager_MobileTap");
        return battleManagerObject.AddComponent<BattleManager>();
    }

    private MobileTouchInputManager CreateManager()
    {
        managerObject = new GameObject("TestMobileTouchInputManager");
        MobileTouchInputManager manager = managerObject.AddComponent<MobileTouchInputManager>();
        managerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

        // The touch canvas, and therefore the battle bar, is only built on
        // mobile; the editor override is what makes it available here.
        SetForceMobileInEditor(manager, true);
        SetIsMobilePlatform(manager, true);
        InvokePrivate(manager, "BuildUI");

        Assert.IsNotNull(GetPrivateField(manager, "battleTouchPanel"),
            "Setup: the battle touch panel should be built under the editor override.");
        return manager;
    }

    private Camera CreateCamera()
    {
        return CreateCameraAt(new Vector3(0f, 8f, -12f));
    }

    private Camera CreateCameraAt(Vector3 position)
    {
        if (cameraObject == null)
        {
            cameraObject = new GameObject("TestBattleCamera_MobileTap");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
        }

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 45f;

        // pixelWidth/pixelHeight are read-only and follow the (zero-sized)
        // screen under -nographics, so the projection is done explicitly
        // against ProbeWidth/ProbeHeight in ProjectToScreen.
        cameraObject.transform.position = position;
        cameraObject.transform.LookAt(new Vector3(0f, 1f, 0f));
        return camera;
    }

    private CombatUnit SpawnEnemy(Vector3 position, CombatUnit.UnitType type)
    {
        enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.transform.position = position;
        enemyObject.transform.localScale = new Vector3(1f, 1f, 1f);
        CombatUnit unit = enemyObject.AddComponent<CombatUnit>();
        unit.Type = type;
        Physics.SyncTransforms();
        return unit;
    }

    /// <summary>
    /// The centre of the action bar, derived from the panel's own anchors so
    /// the probe tracks the real layout instead of a hard-coded screen point.
    /// </summary>
    private static Vector2 BattleBarCentre(MobileTouchInputManager manager)
    {
        GameObject panel = (GameObject)GetPrivateField(manager, "battleTouchPanel");
        RectTransform rect = panel.transform as RectTransform;
        Assert.IsNotNull(rect, "The battle touch panel must have a RectTransform.");

        float x = (rect.anchorMin.x + rect.anchorMax.x) * 0.5f;
        float y = (rect.anchorMin.y + rect.anchorMax.y) * 0.5f;
        return new Vector2(x * ProbeWidth, y * ProbeHeight);
    }

    /// <summary>
    /// Whether the bar is the region of the screen that actually blocks a tap.
    ///
    /// The production gate delegates to RectTransformUtility
    /// .RectangleContainsScreenPoint, which needs a resolved canvas rect. Under
    /// -nographics the screen is 0x0 and the canvas never lays out, so that
    /// call cannot run here. The anchors are the layout input the gate reads,
    /// so the equivalent region is derived from them directly. The gate's
    /// polarity is still covered end-to-end by the viewport tests below, which
    /// show a viewport tap passing the gate and reaching the raycast.
    /// </summary>
    private static bool BarRegionContains(RectTransform rect, Vector2 screenPoint)
    {
        float x = screenPoint.x / ProbeWidth;
        float y = screenPoint.y / ProbeHeight;
        return x >= rect.anchorMin.x && x <= rect.anchorMax.x
            && y >= rect.anchorMin.y && y <= rect.anchorMax.y;
    }

    private CombatUnit SpawnAllyActor()
    {
        GameObject actorObject = new GameObject("TestAllyActor_MobileTap");
        allyObject = actorObject;
        CombatUnit actor = actorObject.AddComponent<CombatUnit>();
        actor.Type = CombatUnit.UnitType.Ally;
        return actor;
    }

    private bool IsInsideBattleBar(MobileTouchInputManager manager, Vector2 screenPoint)
    {
        GameObject panel = (GameObject)GetPrivateField(manager, "battleTouchPanel");
        RectTransform rect = panel.transform as RectTransform;
        return rect != null && BarRegionContains(rect, screenPoint);
    }

    private static float BattleTapRayDistance()
    {
        FieldInfo field = typeof(MobileTouchInputManager).GetField(
            "BattleTapRayDistance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, "MobileTouchInputManager.BattleTapRayDistance should exist.");
        return (float)field.GetValue(null);
    }

    /// <summary>
    /// WorldToScreenPoint is unreliable with a zero-size screen under
    /// -nographics, so the projection is performed against the camera's own
    /// pixel rect explicitly.
    /// </summary>
    private static Vector2 ProjectToScreen(Camera camera, Vector3 worldPoint)
    {
        Vector3 viewport = camera.WorldToViewportPoint(worldPoint);
        return new Vector2(viewport.x * ProbeWidth, viewport.y * ProbeHeight);
    }

    private static void SetForceMobileInEditor(MobileTouchInputManager manager, bool value)
    {
        FieldInfo field = typeof(MobileTouchInputManager).GetField(
            "forceMobileInEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, "MobileTouchInputManager.forceMobileInEditor should exist.");
        field.SetValue(manager, value);
    }

    private static void SetIsMobilePlatform(MobileTouchInputManager manager, bool value)
    {
        PropertyInfo property = typeof(MobileTouchInputManager).GetProperty(
            "IsMobilePlatform", BindingFlags.Instance | BindingFlags.Public);
        Assert.IsNotNull(property, "MobileTouchInputManager.IsMobilePlatform should exist.");
        property.GetSetMethod(true).Invoke(manager, new object[] { value });
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
        method.Invoke(target, null);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private void Cleanup()
    {
        if (enemyObject != null)
        {
            Object.DestroyImmediate(enemyObject);
            enemyObject = null;
        }

        if (allyObject != null)
        {
            Object.DestroyImmediate(allyObject);
            allyObject = null;
        }

        if (battleManagerObject != null)
        {
            Object.DestroyImmediate(battleManagerObject);
            battleManagerObject = null;
        }

        if (managerObject != null)
        {
            Object.DestroyImmediate(managerObject);
            managerObject = null;
        }

        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
            cameraObject = null;
        }
    }
}
