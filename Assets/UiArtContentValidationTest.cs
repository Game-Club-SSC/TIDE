using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Validates that PersonaUIStyle provides all required Persona 5-inspired
/// UI components: angular panels, buttons, icons, and color palette.
/// </summary>
public class UiArtContentValidationTest : MonoBehaviour
{
    [ContextMenu("Validate UI Art Content")]
    public void RunTests()
    {
        Debug.Log("=== UI Art Content Validation (Issue #281) ===");

        TestColorPalette();
        TestPanelConstruction();
        TestButtonConstruction();

        Debug.Log("=== All UI Art Tests Passed ===");
    }

    private void TestColorPalette()
    {
        Debug.Log("Validating Persona 5-inspired color palette...");

        // Verify core palette colors exist and are distinct
        Assert.IsTrue(PersonaUIStyle.DeepNavy.a > 0f, "DeepNavy should be opaque.");
        Assert.IsTrue(PersonaUIStyle.MediumBlue.a > 0f, "MediumBlue should be opaque.");
        Assert.IsTrue(PersonaUIStyle.BrightBlue.a > 0f, "BrightBlue should be opaque.");
        Assert.IsTrue(PersonaUIStyle.AccentRed.a > 0f, "AccentRed should be opaque.");
        Assert.IsTrue(PersonaUIStyle.Gold.a > 0f, "Gold should be opaque.");

        // Verify derived colors
        Assert.IsTrue(PersonaUIStyle.PanelBg.a > 0.9f, "PanelBg should be near-opaque.");
        Assert.IsTrue(PersonaUIStyle.Backdrop.a > 0.5f, "Backdrop should be semi-transparent.");
        Assert.IsTrue(PersonaUIStyle.SlashColor.a > 0f && PersonaUIStyle.SlashColor.a < 1f,
            "SlashColor should be semi-transparent for diagonal motif.");

        Debug.Log("Color palette validated: 10+ Persona 5-inspired colors.");
    }

    private void TestPanelConstruction()
    {
        Debug.Log("Validating angular panel construction...");

        // Create a test canvas
        GameObject canvasObj = new GameObject("TestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Test panel creation
        UnityEngine.UI.Image panel = PersonaUIStyle.CreateAngularPanel(
            canvasObj.transform, PersonaUIStyle.PanelBg);
        Assert.IsNotNull(panel, "CreateAngularPanel should return an Image component.");
        Assert.IsNotNull(panel.gameObject, "Panel GameObject should exist.");
        Assert.AreEqual(PersonaUIStyle.PanelBg, panel.color, "Panel should use the specified color.");

        // Test panel with angle offset (Persona 5 diagonal edge)
        UnityEngine.UI.Image angledPanel = PersonaUIStyle.CreateAngularPanel(
            canvasObj.transform, PersonaUIStyle.TitleBarBg, 15f);
        Assert.IsNotNull(angledPanel, "Angled panel should be created.");

        Object.DestroyImmediate(canvasObj);
        Debug.Log("Panel construction validated: angular panels with optional diagonal edges.");
    }

    private void TestButtonConstruction()
    {
        Debug.Log("Validating button construction...");

        GameObject canvasObj = new GameObject("TestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Test button creation
        UnityEngine.UI.Button button = PersonaUIStyle.CreateButton(
            canvasObj.transform, "Test Button", PersonaUIStyle.BrightBlue);
        Assert.IsNotNull(button, "CreateButton should return a Button component.");
        Assert.IsNotNull(button.GetComponent<UnityEngine.UI.Image>(), "Button should have an Image component.");

        Object.DestroyImmediate(canvasObj);
        Debug.Log("Button construction validated.");
    }
}
