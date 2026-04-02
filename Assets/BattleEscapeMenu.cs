using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleEscapeMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleHud battleHud;

    private GameObject escapeMenuPanel;
    private bool isMenuOpen = false;

    public bool IsMenuOpen => isMenuOpen;

    private void Awake()
    {
        TryResolveBattleManager();

        if (battleHud == null)
            battleHud = FindFirstObjectByType<BattleHud>();

        EnsureCanvas();
        CreateEscapeMenuPanel();
        SetMenuOpen(false);
    }



    public void ToggleMenu()
    {
        SetMenuOpen(!isMenuOpen);
    }

    private void SetMenuOpen(bool open)
    {
        isMenuOpen = open;
        if (escapeMenuPanel != null)
            escapeMenuPanel.SetActive(open);

        // Hide other UI elements when menu is open
        if (battleHud != null)
        {
            // We'll let BattleHud handle its own visibility; we can just toggle action panel etc.
            // For simplicity, we can just set the whole HUD inactive? That might break things.
            // Instead, we'll rely on BattleHud to hide its panels when Escape is pressed (as per requirement).
            // Since we are toggling via Escape, BattleHud should also listen.
            // We'll implement later.
        }

    }

    private void EnsureCanvas()
    {
        // Ensure there is a canvas for our UI
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas != null) return;

        GameObject canvasObj = new GameObject("EscapeMenuCanvas", typeof(RectTransform));
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // Above battle HUD
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateEscapeMenuPanel()
    {
        // Create panel as child of canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        escapeMenuPanel = new GameObject("EscapeMenuPanel", typeof(RectTransform));
        escapeMenuPanel.transform.SetParent(canvas.transform, false);
        RectTransform rect = escapeMenuPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.2f, 0.2f);
        rect.anchorMax = new Vector2(0.8f, 0.8f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = escapeMenuPanel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

        // Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(escapeMenuPanel.transform, false);
        Text title = titleObj.AddComponent<Text>();
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.8f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 32;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.text = "PAUSED";
        title.raycastTarget = false;

        // Buttons
        CreateButton(escapeMenuPanel.transform, "Items", new Vector2(0.1f, 0.5f), new Vector2(0.9f, 0.68f), OnItemsClicked);
        CreateButton(escapeMenuPanel.transform, "Abilities", new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.46f), OnAbilitiesClicked);
        CreateButton(escapeMenuPanel.transform, "Flee", new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.24f), OnFleeClicked);
    }

    private Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(label + "Button", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 2f);
        textRect.offsetMax = new Vector2(-4f, -2f);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;

        return btn;
    }

    private void Update()
    {
        TryResolveBattleManager();
    }

    private void OnItemsClicked()
    {
        Debug.Log("[BattleEscapeMenu] Items not implemented.");
    }

    private void OnAbilitiesClicked()
    {
        Debug.Log("[BattleEscapeMenu] Abilities not implemented.");
    }

    private void OnFleeClicked()
    {
        TryResolveBattleManager();

        if (battleManager == null)
        {
            Debug.LogWarning("[BattleEscapeMenu] Cannot flee because BattleManager is missing.");
            return;
        }

        bool accepted = battleManager.TryAttemptFleeFromMenu(out bool fledSuccessfully, out float fleeChance, out float fleeRoll);
        if (!accepted)
        {
            Debug.LogWarning("[BattleEscapeMenu] Flee attempt ignored because battle state does not allow it.");
            SetMenuOpen(false);
            return;
        }

        if (fledSuccessfully)
        {
            Debug.Log($"[BattleEscapeMenu] Flee success ({fleeRoll * 100f:F1}% <= {fleeChance * 100f:F1}%).");
        }
        else
        {
            Debug.LogWarning($"[BattleEscapeMenu] Flee failed ({fleeRoll * 100f:F1}% > {fleeChance * 100f:F1}%).");
        }

        SetMenuOpen(false);
    }

    private void TryResolveBattleManager()
    {
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
    }
}
