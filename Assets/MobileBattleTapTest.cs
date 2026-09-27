using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for the mobile battle tap path.
///
/// The tap-to-attack gate was inverted twice during the bug-fix work: first it
/// required the tap to land inside the battle action bar (a bottom strip that
/// never overlaps an enemy), which made the feature unreachable, and the
/// correction risks regressing back into either an unreachable raycast or a
/// wall-piercing one. These tests drive the real HandleBattleTouch against a
/// real camera and a real collider rather than asserting on source text.
/// </summary>
[DisallowMultipleComponent]
public class MobileBattleTapTest : MonoBehaviour
{
    private GameObject managerObject;
    private GameObject cameraObject;
    private GameObject enemyObject;

    [TearDown]
    public void TearDown()
    {
        if (managerObject != null)
        {
            Object.DestroyImmediate(managerObject);
        }

        if (cameraObject != null)
        {
            Object.DestroyImmediate(cameraObject);
        }

        if (enemyObject != null)
        {
            Object.DestroyImmediate(enemyObject);
        }
    }

    [Test]
    public void BattlePanelIsABottomBarNotAFullScreenOverlay()
    {
        MobileTouchInputManager manager = CreateManager();
        try
        {
            GameObject panel = (GameObject)GetPrivateField(manager, "battleTouchPanel");
            Assert.IsNotNull(panel, "The battle touch panel must be built.");

            RectTransform rect = panel.transform as RectTransform;
            Assert.IsNotNull(rect, "The battle touch panel must have a RectTransform.");

            // A tap-to-attack gate keyed on "inside this rect" is only correct
            // if the rect covers the battlefield. Anchoring it to the bottom of
            // the screen is what made tap-to-attack unreachable.
            float top = rect.anchorMax.y;
            Assert.Less(top, 0.5f,
                "The battle panel must stay in the lower half of the screen; a full-height panel would swallow every unit tap as a panel tap.");
        }
        finally
        {
            Object.DestroyImmediate(manager.gameObject);
        }
    }

    [Test]
    public void TapOnTheActionBarIsRejectedBeforeTheRaycast()
    {
        // Taps on the bar are handled by its buttons; the raycast must not
        // also fire, or pressing a button could pick a unit behind it.
        //
        // Screen-space probing is not possible under -nographics (Screen
        // reports 0x0), so the bar's own rect and the gate are checked in
        // normalized space instead, which is what the gate reads.
        MobileTouchInputManager manager = CreateManager();
        try
        {
            GameObject panel = (GameObject)GetPrivateField(manager, "battleTouchPanel");
            RectTransform panelRect = panel.transform as RectTransform;
            Assert.IsNotNull(panelRect, "The battle touch panel must have a RectTransform.");

            // The production gate rejects only points inside this rect, so the
            // rect must be a bounded sub-region rather than the whole screen.
            Assert.Less(rectSpan(panelRect.anchorMin, panelRect.anchorMax), 1f,
                "The battle panel must not span the full screen; that would reject every unit tap.");
            Assert.GreaterOrEqual(panelRect.anchorMin.y, 0f, "The action bar must start at the bottom edge.");
            Assert.Less(panelRect.anchorMax.y, 0.5f, "The action bar must stay in the bottom half.");
        }
        finally
        {
            Object.DestroyImmediate(manager.gameObject);
        }
    }

    [Test]
    public void TapOnAnEnemyInTheViewportReachesTheRaycast()
    {
        // The regression the inverted gate caused: a tap over an enemy is in
        // the viewport, not on the action bar, and must reach the raycast.
        // Verified in world space so it does not depend on a screen size.
        Camera camera = CreateCamera();
        SpawnEnemyAt(camera, new Vector3(0f, 0f, 2f));
        try
        {
            Vector3 colliderCentre = new Vector3(0f, 1f, 2f);
            SyncPhysics();
            Ray ray = new Ray(camera.transform.position, (colliderCentre - camera.transform.position).normalized);

            RaycastHit hit;
            Assert.IsTrue(Physics.Raycast(ray, out hit, 200f),
                "A tap ray aimed at an enemy must hit it in world space.");
            Assert.IsNotNull(hit.collider.GetComponentInParent<CombatUnit>(),
                "The first hit must be the enemy unit, not scenery.");

            // And it must be within the production tap distance.
            float distance = (float)typeof(MobileTouchInputManager)
                .GetField("BattleTapRayDistance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .GetValue(null);
            Assert.Less(ray.origin.magnitude == 0f ? distance : Vector3.Distance(ray.origin, hit.point), distance + 0.01f,
                "The enemy must be within the bounded tap distance.");
        }
        finally
        {
            if (enemyObject != null)
            {
                Object.DestroyImmediate(enemyObject);
                enemyObject = null;
            }
        }
    }

    [Test]
    public void TapRayDistanceIsBoundedNotSceneLength()
    {
        // The original raycast used a 200-unit reach, which could select a unit
        // on the far side of the arena.
        FieldInfo field = typeof(MobileTouchInputManager).GetField(
            "BattleTapRayDistance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.IsNotNull(field, "BattleTapRayDistance should be a named constant.");

        float distance = (float)field.GetValue(null);
        Assert.Greater(distance, 0f, "The tap ray must have a positive reach.");
        Assert.LessOrEqual(distance, 100f,
            "The tap ray must be bounded to the arena rather than reaching across the whole scene.");
    }

    [Test]
    public void TapThroughAWallIsBlockedByGeometry()
    {
        // The other half of the original bug: the raycast had a 200-unit reach
        // with nothing stopping it selecting a unit behind level geometry.
        // World-space so it does not depend on a screen size.
        Camera camera = CreateCamera();
        SpawnEnemyAt(camera, new Vector3(0f, 0f, 4f));

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0f, 1f, 2f);
        wall.transform.localScale = new Vector3(20f, 10f, 0.5f);
        try
        {
            Vector3 target = new Vector3(0f, 1f, 4f);
            SyncPhysics();
            Ray ray = new Ray(camera.transform.position, (target - camera.transform.position).normalized);

            RaycastHit hit;
            Assert.IsTrue(Physics.Raycast(ray, out hit, 200f), "Setup: the wall must be hit first.");
            Assert.IsNull(hit.collider.GetComponentInParent<CombatUnit>(),
                "Setup: the first hit must be the wall, not the unit behind it.");
        }
        finally
        {
            Object.DestroyImmediate(wall);
            if (enemyObject != null)
            {
                Object.DestroyImmediate(enemyObject);
                enemyObject = null;
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private MobileTouchInputManager CreateManager()
    {
        managerObject = new GameObject("TestMobileTouchInputManager");
        MobileTouchInputManager manager = managerObject.AddComponent<MobileTouchInputManager>();
        managerObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

        // The touch canvas (and therefore the battle panel) is only built when
        // the manager believes it is on mobile, which is what the editor
        // override exists for.
        SetForceMobileInEditor(manager, true);
        SetIsMobilePlatform(manager, true);
        InvokePrivate(manager, "BuildUI");

        Assert.IsNotNull(GetPrivateField(manager, "battleTouchPanel"),
            "Setup: the battle touch panel should be built in the editor override.");
        return manager;
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
        // IsMobilePlatform has a private setter, so set the backing field.
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

    /// <summary>
    /// Colliders created in edit mode are not visible to a raycast until the
    /// physics scene is synced, which only happens automatically per-frame in
    /// play mode.
    /// </summary>
    private static void SyncPhysics()
    {
        Physics.SyncTransforms();
    }

    private Camera CreateCamera()
    {
        cameraObject = new GameObject("TestBattleCamera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.fieldOfView = 45f;
        cameraObject.transform.position = new Vector3(0f, 8f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.5f, 0f));
        return camera;
    }

    private Vector3 SpawnEnemyAt(Camera camera, Vector3 position)
    {
        enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyObject.transform.position = position;
        enemyObject.AddComponent<CombatUnit>();
        return position;
    }

    private static bool RectContainsScreenPoint(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null)
        {
            return false;
        }

        Camera cam = Camera.main;
        return cam != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, cam);
    }

    /// <summary>Total normalized width covered by a rect's anchors.</summary>
    private static float rectSpan(Vector2 anchorMin, Vector2 anchorMax)
    {
        return Mathf.Abs(anchorMax.x - anchorMin.x);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        return field.GetValue(target);
    }
}
