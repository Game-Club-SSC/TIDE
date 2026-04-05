using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SmithyUI : MonoBehaviour
{
    private SmithyInteractable smithy;
    private Canvas menuCanvas;
    private GameObject panelRoot;
    private int selectedIndex = -1;

    private const float PanelWidth = 500f;
    private const float PanelHeight = 520f;
    private const float RowHeight = 64f;
    private const float Padding = 16f;

    private static readonly Color PanelBg = new Color(0.15f, 0.1f, 0.05f, 0.95f);
    private static readonly Color TitleColor = new Color(0.95f, 0.85f, 0.4f);
    private static readonly Color TextColor = new Color(0.9f, 0.85f, 0.75f);
    private static readonly Color RowColor = new Color(0.25f, 0.2f, 0.12f, 0.8f);
    private static readonly Color SelectedColor = new Color(0.45f, 0.35f, 0.15f, 0.9f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.6f, 0.2f, 0.9f);
    private static readonly Color DisabledButtonColor = new Color(0.4f, 0.2f, 0.2f, 0.7f);

    public void Initialize(SmithyInteractable smithyInteractable)
    {
        smithy = smithyInteractable;
        EnsureCanvas();
        RebuildPanel();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSmithy();
        }
    }

    public void CloseSmithy()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        if (menuCanvas != null)
        {
            Destroy(menuCanvas.gameObject);
            menuCanvas = null;
        }

        if (smithy != null)
        {
            smithy.OnSmithyClosed();
        }

        Destroy(gameObject);
    }

    private void EnsureCanvas()
    {
        GameObject canvasObject = new GameObject("SmithyCanvas");
        canvasObject.transform.SetParent(transform, false);

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 950;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void RebuildPanel()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        if (HeroProgressionManager.Instance == null)
        {
            return;
        }

        List<GearInstance> allGear = HeroProgressionManager.Instance.GetAllGearInstances();

        float rowArea = allGear.Count * RowHeight + (allGear.Count > 0 ? 0 : RowHeight);
        float totalHeight = Mathf.Max(PanelHeight, 130f + rowArea + 80f);

        panelRoot = new GameObject("SmithyPanel");
        panelRoot.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(PanelWidth, totalHeight);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = PanelBg;

        float currentY = -Padding;

        CreateLabel(panelRoot, "SMITHY - Gear Duplication", new Vector2(Padding, currentY), new Vector2(PanelWidth - Padding * 2, 30f), TitleColor, 20, FontStyle.Bold);
        currentY += 36f;

        int gold = HeroProgressionManager.Instance.Currency;
        CreateLabel(panelRoot, $"Currency: {gold}g", new Vector2(Padding, currentY), new Vector2(PanelWidth - Padding * 2, 20f), new Color(0.95f, 0.85f, 0.4f), 13, FontStyle.Normal);
        currentY += 24f;

        currentY += 4f;
        CreateLabel(panelRoot, "Select finalized gear to duplicate:", new Vector2(Padding, currentY), new Vector2(PanelWidth - Padding * 2, 18f), TextColor, 12, FontStyle.Italic);
        currentY += 22f;

        if (allGear.Count == 0)
        {
            CreateLabel(panelRoot, "No gear owned yet.", new Vector2(Padding, currentY), new Vector2(PanelWidth - Padding * 2, 30f), TextColor, 14, FontStyle.Italic);
            currentY += 40f;
        }
        else
        {
            for (int i = 0; i < allGear.Count; i++)
            {
                CreateGearRow(panelRoot, allGear[i], i, currentY);
                currentY += RowHeight;
            }
        }

        currentY += 8f;
        CreateLabel(panelRoot, "Press ESC to close", new Vector2(Padding, currentY), new Vector2(PanelWidth - Padding * 2, 20f), TextColor, 11, FontStyle.Italic);
    }

    private void CreateGearRow(GameObject parent, GearInstance gear, int index, float yPos)
    {
        GameObject rowObject = new GameObject($"GearRow_{index}");
        rowObject.transform.SetParent(parent.transform, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.offsetMin = new Vector2(Padding, -(yPos + RowHeight - 4f));
        rowRect.offsetMax = new Vector2(-Padding, -yPos);

        Image rowBg = rowObject.AddComponent<Image>();
        rowBg.color = index == selectedIndex ? SelectedColor : RowColor;

        Button selectButton = rowObject.AddComponent<Button>();
        ColorBlock colors = selectButton.colors;
        colors.normalColor = RowColor;
        colors.highlightedColor = SelectedColor;
        colors.pressedColor = new Color(0.55f, 0.45f, 0.2f, 0.9f);
        selectButton.colors = colors;

        int gearIndex = index;
        selectButton.onClick.AddListener(() => OnGearSelected(gearIndex));

        float textX = 10f;
        float textWidth = PanelWidth - Padding * 2 - 20f;
        string setName = gear != null && gear.template != null ? gear.template.displayName : (gear != null ? gear.setId : "Unknown");
        string header = gear != null
            ? $"{setName}  Lv.{gear.level}  [{gear.UnlockedSlotCount}/{GearInstance.MaxBonusSlots} slots]"
            : "Unknown gear";

        CreateLabel(rowObject, header, new Vector2(textX, 2f), new Vector2(textWidth, 18f), TextColor, 13, FontStyle.Bold);

        string slots = gear != null ? gear.GetSlotDisplayString() : "No bonus slots";
        CreateLabel(rowObject, slots, new Vector2(textX, 20f), new Vector2(textWidth * 0.62f, 14f), new Color(0.85f, 0.85f, 0.6f), 10, FontStyle.Normal);

        int cost = HeroProgressionManager.Instance != null ? HeroProgressionManager.Instance.GetGearDuplicateCost(gear) : 0;
        bool hasFunds = HeroProgressionManager.Instance != null && HeroProgressionManager.Instance.Currency >= cost;
        bool isFinalized = gear != null && gear.UnlockedSlotCount >= GearInstance.MaxBonusSlots;

        string buttonText;
        bool isEnabled;
        if (!isFinalized)
        {
            buttonText = "Finalize First";
            isEnabled = false;
        }
        else if (!hasFunds)
        {
            buttonText = $"Need {cost}g";
            isEnabled = false;
        }
        else
        {
            buttonText = $"Duplicate {cost}g";
            isEnabled = true;
        }

        CreateDuplicateButton(
            rowObject,
            gear,
            cost,
            isEnabled,
            buttonText,
            new Vector2(textWidth * 0.64f, 18f),
            new Vector2(textWidth * 0.33f, 28f));
    }

    private void CreateDuplicateButton(GameObject parent, GearInstance gear, int cost, bool isEnabled, string buttonText, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject("DuplicateBtn");
        buttonObject.transform.SetParent(parent.transform, false);

        RectTransform btnRect = buttonObject.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = position;
        btnRect.sizeDelta = size;

        Image btnBg = buttonObject.AddComponent<Image>();
        btnBg.color = isEnabled ? ButtonColor : DisabledButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        if (!isEnabled)
        {
            ColorBlock btnColors = button.colors;
            btnColors.disabledColor = DisabledButtonColor;
            button.colors = btnColors;
            button.interactable = false;
        }

        GearInstance gearCopy = gear;
        int costCopy = cost;
        button.onClick.AddListener(() => OnDuplicateClicked(gearCopy, costCopy));

        CreateLabel(buttonObject, buttonText, Vector2.zero, size, isEnabled ? Color.white : new Color(0.6f, 0.6f, 0.6f), 11, FontStyle.Bold);
    }

    private void OnGearSelected(int index)
    {
        selectedIndex = index;
        RebuildPanel();
    }

    private void OnDuplicateClicked(GearInstance source, int cost)
    {
        if (HeroProgressionManager.Instance == null || source == null)
        {
            return;
        }

        if (source.UnlockedSlotCount < GearInstance.MaxBonusSlots)
        {
            Debug.Log("[SmithyUI] Only fully rolled gear (all bonus slots unlocked) can be duplicated.");
            return;
        }

        if (!HeroProgressionManager.Instance.TrySpendCurrency(cost))
        {
            Debug.Log("[SmithyUI] Not enough currency to duplicate.");
            return;
        }

        GearInstance duplicate = source.Duplicate();
        HeroProgressionManager.Instance.RegisterGearInstance(duplicate);

        Debug.Log($"[SmithyUI] Duplicated '{source.setId}' (Lv.{source.level}) -> new instance '{duplicate.instanceId}' for {cost}g.");
        RebuildPanel();
    }

    private static void CreateLabel(GameObject parent, string text, Vector2 position, Vector2 size, Color color, int fontSize, FontStyle fontStyle)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = position;
        labelRect.sizeDelta = size;

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
    }
}
