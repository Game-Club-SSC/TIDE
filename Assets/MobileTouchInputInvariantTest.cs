#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for the on-screen (touch) input path.
///
/// The virtual joystick had a real defect: its release smoothing ran on
/// Time.deltaTime, so while the game was paused (timeScale 0) the decay step
/// was zero and MoveTowards never advanced. A stick that was still held when
/// a menu opened stayed latched at full deflection, and the player ran in one
/// direction with no input on resume. These tests drive the real smoothing
/// method with a zeroed timeScale, which is the exact paused condition.
/// </summary>
[DisallowMultipleComponent]
public class MobileTouchInputInvariantTest : MonoBehaviour
{
    private GameObject managerObject;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        Cleanup();
        previousTimeScale = Time.timeScale;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = previousTimeScale;
        Cleanup();
    }

    [Test]
    public void ReleasedJoystickRecentersWhilePaused()
    {
        // The core on-device bug. With the stick released and the game paused,
        // the movement vector must still fall back to zero instead of staying
        // latched, which would make the player run in one direction with no
        // input. Time.deltaTime is 0 while paused, so the smoothing must be on
        // the unscaled clock; the editor provides that clock even headless.
        MobileTouchInputManager manager = CreateManager();
        try
        {
            ForceFullDeflection(manager);
            Assert.Greater(Mathf.Abs(manager.MoveH) + Mathf.Abs(manager.MoveV), 0.5f,
                "Setup: the stick should be at full deflection before release.");

            ReleaseStick(manager);

            // Pause, then step the real smoothing repeatedly: a single step
            // only covers part of the deflection, and the editor's unscaled
            // delta is a real frame step.
            Time.timeScale = 0f;
            for (int step = 0; step < 200; step++)
            {
                InvokePrivate(manager, "HandleJoystickInput");
            }

            Assert.AreEqual(0f, manager.MoveH, 0.0001f,
                "A released stick must return to zero even while the game is paused (timeScale 0).");
            Assert.AreEqual(0f, manager.MoveV, 0.0001f,
                "A released stick must return to zero on both axes while paused.");
        }
        finally
        {
            Time.timeScale = previousTimeScale;
        }
    }

    [Test]
    public void HeldJoystickIsNotDecayed()
    {
        // The counterpart: while the finger is still down, the smoothing must
        // not pull the stick back to centre, or movement would die under the
        // player's finger.
        MobileTouchInputManager manager = CreateManager();
        try
        {
            ForceFullDeflection(manager);
            HoldStick(manager);

            InvokePrivate(manager, "HandleJoystickInput");

            Assert.Greater(Mathf.Abs(manager.MoveH), 0.5f,
                "A held stick must keep its deflection; it may only be smoothed while dragging.");
        }
        finally
        {
            ReleaseStick(manager);
        }
    }

    [Test]
    public void ReleasedStickClearsItsFingerId()
    {
        // A missed touch-end would otherwise leave the manager believing the
        // stick is still held, so the player could not re-grip it.
        MobileTouchInputManager manager = CreateManager();
        try
        {
            HoldStick(manager);
            Assert.AreEqual(7, (int)GetPrivateField(manager, "joystickTouchId"),
                "Setup: the held stick should track its finger.");

            ReleaseStick(manager);

            Assert.IsFalse((bool)GetPrivateField(manager, "isDraggingJoystick"),
                "Releasing the stick must clear the dragging state.");
            Assert.AreEqual(-1, (int)GetPrivateField(manager, "joystickTouchId"),
                "Releasing the stick must clear its finger id so it can be re-gripped.");
        }
        finally
        {
            ReleaseStick(manager);
        }
    }

    [Test]
    public void TouchButtonsAreConsumedExactlyOnce()
    {
        // A queued interact that is never cleared would fire every frame; one
        // that is consumed but not cleared would either repeat or be lost.
        // ConsumeOneShots is the real production path for both directions.
        MobileTouchInputManager manager = CreateManager();
        try
        {
            SetPrivateField(manager, "interactQueued", true);
            SetPrivateField(manager, "dashQueued", true);
            SetPrivateField(manager, "menuQueued", true);

            InvokePrivate(manager, "ConsumeOneShots");

            Assert.IsFalse((bool)GetPrivateField(manager, "interactQueued"),
                "A queued interact must be consumed so it cannot repeat every frame.");
            Assert.IsFalse((bool)GetPrivateField(manager, "dashQueued"),
                "A queued dash must be consumed so it cannot repeat every frame.");
            Assert.IsFalse((bool)GetPrivateField(manager, "menuQueued"),
                "A queued menu press must be consumed so it cannot repeat every frame.");

            // A second frame with nothing queued must not resurrect the press.
            InvokePrivate(manager, "ConsumeOneShots");
            Assert.IsFalse((bool)GetPrivateField(manager, "interactPressed"),
                "Once consumed, the interact press must not remain latched on the next frame.");
        }
        finally
        {
            ReleaseStick(manager);
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

        SetForceMobileInEditor(manager, true);
        SetIsMobilePlatform(manager, true);
        InvokePrivate(manager, "BuildUI");
        return manager;
    }

    private static void ForceFullDeflection(MobileTouchInputManager manager)
    {
        SetPrivateField(manager, "moveH", 1f);
        SetPrivateField(manager, "moveV", 1f);
    }

    private static void HoldStick(MobileTouchInputManager manager)
    {
        SetPrivateField(manager, "isDraggingJoystick", true);
        SetPrivateField(manager, "joystickTouchId", 7);
    }

    private static void ReleaseStick(MobileTouchInputManager manager)
    {
        SetPrivateField(manager, "isDraggingJoystick", false);
        SetPrivateField(manager, "joystickTouchId", -1);
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    private static object GetPrivateField(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Expected private field '{fieldName}' on {target.GetType().Name}.");
        return field.GetValue(target);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
        method.Invoke(target, null);
    }

    private void Cleanup()
    {
        if (MobileTouchInputManager.Instance != null)
        {
            Object.DestroyImmediate(MobileTouchInputManager.Instance.gameObject);
        }

        if (managerObject != null)
        {
            Object.DestroyImmediate(managerObject);
            managerObject = null;
        }
    }
}
#endif
