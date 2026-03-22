using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PartySwapPanel : MonoBehaviour
{
    private BattleManager battleManager;
    private Transform contentRoot;
    private Transform activeHeroesContainer;
    private Transform reserveHeroesContainer;
    private List<Button> activeHeroButtons = new List<Button>();
    private List<Button> reserveHeroButtons = new List<Button>();
    private List<CombatUnit> activeUnits = new List<CombatUnit>();
    private List<CombatUnit> reserveUnits = new List<CombatUnit>();

    private CombatUnit selectedActiveUnit = null;

    public void Initialize(BattleManager manager)
    {
        battleManager = manager;
        CreateLayout();
        RefreshDisplay();
    }

    private void CreateLayout()
    {
        if (contentRoot == null)
        {
            GameObject content = new GameObject("ContentRoot", typeof(RectTransform));
            content.transform.SetParent(transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRoot = content.transform;
        }

        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        GameObject activeColumn = new GameObject("ActiveColumn", typeof(RectTransform));
        activeColumn.transform.SetParent(contentRoot, false);
        RectTransform activeRect = activeColumn.GetComponent<RectTransform>();
        activeRect.anchorMin = new Vector2(0.05f, 0.2f);
        activeRect.anchorMax = new Vector2(0.45f, 0.8f);
        activeRect.offsetMin = Vector2.zero;
        activeRect.offsetMax = Vector2.zero;

        GameObject reserveColumn = new GameObject("ReserveColumn", typeof(RectTransform));
        reserveColumn.transform.SetParent(contentRoot, false);
        RectTransform reserveRect = reserveColumn.GetComponent<RectTransform>();
        reserveRect.anchorMin = new Vector2(0.55f, 0.2f);
        reserveRect.anchorMax = new Vector2(0.95f, 0.8f);
        reserveRect.offsetMin = Vector2.zero;
        reserveRect.offsetMax = Vector2.zero;

        // Labels
        CreateLabel(activeColumn.transform, "Active Heroes", new Vector2(0f, 0.9f), new Vector2(1f, 1f));
        CreateLabel(reserveColumn.transform, "Reserve Heroes", new Vector2(0f, 0.9f), new Vector2(1f, 1f));

        // Containers for buttons
        activeHeroesContainer = new GameObject("ActiveHeroes", typeof(RectTransform)).transform;
        activeHeroesContainer.SetParent(activeColumn.transform, false);
        RectTransform activeContainerRect = activeHeroesContainer.GetComponent<RectTransform>();
        activeContainerRect.anchorMin = Vector2.zero;
        activeContainerRect.anchorMax = new Vector2(1f, 0.85f);
        activeContainerRect.offsetMin = Vector2.zero;
        activeContainerRect.offsetMax = Vector2.zero;

        reserveHeroesContainer = new GameObject("ReserveHeroes", typeof(RectTransform)).transform;
        reserveHeroesContainer.SetParent(reserveColumn.transform, false);
        RectTransform reserveContainerRect = reserveHeroesContainer.GetComponent<RectTransform>();
        reserveContainerRect.anchorMin = Vector2.zero;
        reserveContainerRect.anchorMax = new Vector2(1f, 0.85f);
        reserveContainerRect.offsetMin = Vector2.zero;
        reserveContainerRect.offsetMax = Vector2.zero;
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(parent, false);
        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = labelObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;
        label.raycastTarget = false;
    }

    private void RefreshDisplay()
    {
        if (battleManager == null) return;

        // Clear existing buttons
        ClearButtons(activeHeroesContainer, activeHeroButtons);
        ClearButtons(reserveHeroesContainer, reserveHeroButtons);
        activeUnits.Clear();
        reserveUnits.Clear();

        // Get current active and reserve units
        IReadOnlyList<CombatUnit> active = battleManager.AllyUnits;
        IReadOnlyList<CombatUnit> reserve = battleManager.AllyReserveUnits;

        // Create buttons for active units
        for (int i = 0; i < active.Count; i++)
        {
            CombatUnit unit = active[i];
            if (unit == null) continue;
            activeUnits.Add(unit);
            Button btn = CreateHeroButton(activeHeroesContainer, unit, i, true);
            activeHeroButtons.Add(btn);
        }

        // Create buttons for reserve units
        for (int i = 0; i < reserve.Count; i++)
        {
            CombatUnit unit = reserve[i];
            if (unit == null) continue;
            reserveUnits.Add(unit);
            Button btn = CreateHeroButton(reserveHeroesContainer, unit, i, false);
            reserveHeroButtons.Add(btn);
        }

        UpdateButtonHighlights();
    }

    private void ClearButtons(Transform container, List<Button> buttonList)
    {
        foreach (Button btn in buttonList)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        buttonList.Clear();
        if (container != null)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private Button CreateHeroButton(Transform parent, CombatUnit unit, int index, bool isActive)
    {
        GameObject btnObj = new GameObject($"HeroButton_{unit.UnitName}", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f - (index + 1) * 0.33f);
        rect.anchorMax = new Vector2(1f, 1f - index * 0.33f);
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

        // Unit info
        GameObject infoObj = new GameObject("Info", typeof(RectTransform));
        infoObj.transform.SetParent(btnObj.transform, false);
        RectTransform infoRect = infoObj.GetComponent<RectTransform>();
        infoRect.anchorMin = Vector2.zero;
        infoRect.anchorMax = Vector2.one;
        infoRect.offsetMin = new Vector2(4f, 2f);
        infoRect.offsetMax = new Vector2(-4f, -2f);

        Text infoText = infoObj.AddComponent<Text>();
        infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        infoText.fontSize = 18;
        infoText.alignment = TextAnchor.MiddleLeft;
        infoText.color = unit.IsAlive ? Color.white : Color.gray;
        infoText.text = $"{unit.UnitName}\nHP: {unit.HP}/{unit.MaxHP}  MP: {unit.MP}/{unit.MaxMP}";
        infoText.raycastTarget = false;

        // Button click handler
        if (isActive)
        {
            btn.onClick.AddListener(() => OnActiveHeroClicked(unit));
        }
        else
        {
            btn.onClick.AddListener(() => OnReserveHeroClicked(unit));
        }

        return btn;
    }

    private void OnActiveHeroClicked(CombatUnit unit)
    {
        selectedActiveUnit = unit;
        Debug.Log($"[PartySwapPanel] Selected active hero: {unit.UnitName}");
        UpdateButtonHighlights();
    }

    private void OnReserveHeroClicked(CombatUnit unit)
    {
        if (selectedActiveUnit == null)
        {
            Debug.Log("[PartySwapPanel] No active hero selected. Select an active hero first.");
            return;
        }

        Debug.Log($"[PartySwapPanel] Attempting swap {selectedActiveUnit.UnitName} with {unit.UnitName}");
        bool success = battleManager.TrySwapWithReserve(selectedActiveUnit, unit);
        if (success)
        {
            selectedActiveUnit = null;
            RefreshDisplay();
        }
        else
        {
            Debug.Log($"[PartySwapPanel] Swap failed.");
        }
    }

    private void UpdateButtonHighlights()
    {
        // Highlight selected active button
        for (int i = 0; i < activeHeroButtons.Count; i++)
        {
            Button btn = activeHeroButtons[i];
            if (btn == null) continue;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = (activeUnits[i] == selectedActiveUnit) ? new Color(0.4f, 0.4f, 0.6f, 1f) : new Color(0.2f, 0.2f, 0.3f, 1f);
            }
        }
        // Highlight reserve buttons if an active hero is selected (optional)
        foreach (Button btn in reserveHeroButtons)
        {
            if (btn == null) continue;
            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = (selectedActiveUnit != null) ? new Color(0.25f, 0.25f, 0.35f, 1f) : new Color(0.2f, 0.2f, 0.3f, 1f);
            }
        }
    }

    private void Update()
    {
        // Refresh display if units changed (e.g., HP updates)
        // We could poll but for simplicity, we'll rely on manual refresh after swap.
    }
}
