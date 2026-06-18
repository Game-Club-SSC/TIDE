using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class MobileTouchControllerTest : MonoBehaviour
{
    [ContextMenu("Run Mobile Touch Controller Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Mobile Touch Controller Tests ===");

        TestControllerSingletonExists();
        TestDpadInputClamped();
        TestActionButtonPressAndRelease();
        TestActionButtonIdEnumCoverage();
        TestVisibilityToggle();

        Debug.Log("=== All Mobile Touch Controller Tests Passed ===");
    }

    private void TestControllerSingletonExists()
    {
        GameObject host = new GameObject("Test_MobileTouch");
        MobileTouchController controller = host.AddComponent<MobileTouchController>();
        Assert.IsNotNull(MobileTouchController.Instance, "MobileTouchController.Instance should be set.");
        Object.DestroyImmediate(host);
    }

    private void TestDpadInputClamped()
    {
        GameObject host = new GameObject("Test_Dpad");
        MobileTouchController controller = host.AddComponent<MobileTouchController>();
        controller.SetDpadInput(new Vector2(5f, 5f));
        Assert.AreEqual(1f, controller.DpadInput.magnitude, 0.001f, "Dpad input should clamp to unit magnitude.");
        Object.DestroyImmediate(host);
    }

    private void TestActionButtonPressAndRelease()
    {
        GameObject host = new GameObject("Test_Action");
        MobileTouchController controller = host.AddComponent<MobileTouchController>();
        controller.SimulateActionButtonPress(MobileTouchController.ActionButtonId.Interact);
        Assert.IsTrue(controller.IsActionButtonHeld(MobileTouchController.ActionButtonId.Interact), "Button should be held after press.");
        controller.SimulateActionButtonRelease(MobileTouchController.ActionButtonId.Interact);
        Assert.IsFalse(controller.IsActionButtonHeld(MobileTouchController.ActionButtonId.Interact), "Button should be released.");
        Object.DestroyImmediate(host);
    }

    private void TestActionButtonIdEnumCoverage()
    {
        MobileTouchController.ActionButtonId[] ids = (MobileTouchController.ActionButtonId[])System.Enum.GetValues(typeof(MobileTouchController.ActionButtonId));
        Assert.GreaterOrEqual(ids.Length, 4, "Should have at least 4 action buttons.");
    }

    private void TestVisibilityToggle()
    {
        GameObject host = new GameObject("Test_Visible");
        MobileTouchController controller = host.AddComponent<MobileTouchController>();
        controller.IsVisible = false;
        Assert.IsFalse(controller.IsVisible, "IsVisible should reflect false.");
        controller.IsVisible = true;
        Assert.IsTrue(controller.IsVisible, "IsVisible should reflect true.");
        Object.DestroyImmediate(host);
    }
}
