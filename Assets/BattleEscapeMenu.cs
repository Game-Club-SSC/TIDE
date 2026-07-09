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
    private GameObject itemsPanel;
    private GameObject abilitiesPanel;
    private List<Button> dynamicButtons = new List<Button>();
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

        if (!open)
        {
            CloseSubPanel(itemsPanel);
            CloseSubPanel(abilitiesPanel);
        }
    }

    private void EnsureCanvas()
    {
        Canvas existingCanvas = FindFirstObjectByType<Canvas>();
        if (existingCanvas != null) return;

        GameObject canvasObj = new GameObject("EscapeMenuCanvas", typeof(RectTransform));
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }

    private void CreateEscapeMenuPanel()
    {
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
        CloseSubPanel(abilitiesPanel);

        if (itemsPanel != null)
        {
            CloseSubPanel(itemsPanel);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        itemsPanel = CreateSubPanel(canvas.transform, "ItemsPanel");
        CreateSubPanelTitle(itemsPanel.transform, "ITEMS");

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(itemsPanel.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        GameObject msgObj = new GameObject("Message", typeof(RectTransform));
        msgObj.transform.SetParent(contentObj.transform, false);
        RectTransform msgRect = msgObj.GetComponent<RectTransform>();
        msgRect.anchorMin = new Vector2(0.1f, 0.3f);
        msgRect.anchorMax = new Vector2(0.9f, 0.7f);
        msgRect.offsetMin = Vector2.zero;
        msgRect.offsetMax = Vector2.zero;

        Text msgText = msgObj.AddComponent<Text>();
        msgText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        msgText.fontSize = 20;
        msgText.alignment = TextAnchor.MiddleCenter;
        msgText.color = new Color(0.7f, 0.7f, 0.8f);
        msgText.text = "No items available.";
        msgText.raycastTarget = false;

        CreateCloseButton(itemsPanel.transform, () => CloseSubPanel(itemsPanel));
    }

    private void OnAbilitiesClicked()
    {
        CloseSubPanel(itemsPanel);

        if (abilitiesPanel != null)
        {
            CloseSubPanel(abilitiesPanel);
            return;
        }

        TryResolveBattleManager();
        if (battleManager == null) return;

        CombatUnit currentUnit = battleManager.GetCurrentInputUnit();
        if (currentUnit == null)
        {
            Debug.LogWarning("[BattleEscapeMenu] No active hero to show abilities for.");
            return;
        }

        IReadOnlyList<SkillData> skills = currentUnit.Skills;
        if (skills.Count == 0)
        {
            Debug.Log("[BattleEscapeMenu] Current hero has no abilities.");
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        abilitiesPanel = CreateSubPanel(canvas.transform, "AbilitiesPanel");
        CreateSubPanelTitle(abilitiesPanel.transform, $"ABILITIES - {currentUnit.UnitName}");

        GameObject contentObj = new GameObject("Content", typeof(RectTransform));
        contentObj.transform.SetParent(abilitiesPanel.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(1f, 0.85f);
        contentRect.offsetMin = new Vector2(8f, 8f);
        contentRect.offsetMax = new Vector2(-8f, -8f);

        for (int i = 0; i < skills.Count; i++)
        {
            SkillData skill = skills[i];
            if (skill == null) continue;

            GameObject btnObj = new GameObject($"Ability_{i}", typeof(RectTransform));
            btnObj.transform.SetParent(contentObj.transform, false);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            float rowY = 1f - (i + 1) / (float)(skills.Count + 1);
            float rowH = 1f / (skills.Count + 1) * 0.85f;
            btnRect.anchorMin = new Vector2(0.05f, rowY - rowH * 0.5f);
            btnRect.anchorMax = new Vector2(0.95f, rowY + rowH * 0.5f);
            btnRect.offsetMin = Vector2.zero;
            btnRect.offsetMax = Vector2.zero;

            bool canUse = currentUnit.CanUseSkill(skill);
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = canUse ? new Color(0.2f, 0.2f, 0.35f, 1f) : new Color(0.25f, 0.15f, 0.15f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = canUse;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = canUse ? Color.white : new Color(0.5f, 0.4f, 0.4f);
            string mpCost = skill.mpCost > 0 ? $"  ({skill.mpCost} MP)" : "";
            text.text = $"{skill.skillName}{mpCost}  -  {skill.description ?? ""}";
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;

            SkillData capturedSkill = skill;
            btn.onClick.AddListener(() => OnAbilitySelected(capturedSkill, currentUnit));
            dynamicButtons.Add(btn);
        }

        CreateCloseButton(abilitiesPanel.transform, () => CloseSubPanel(abilitiesPanel));
    }

    private void OnAbilitySelected(SkillData skill, CombatUnit unit)
    {
        if (skill == null || battleManager == null || unit == null) return;

        if (!unit.CanUseSkill(skill))
        {
            Debug.Log("[BattleEscapeMenu] Not enough MP to use this ability.");
            return;
        }

        if (!battleManager.IsSkillSupportedForCurrentSlice(skill))
        {
            Debug.LogWarning($"[BattleEscapeMenu] Skill '{skill.skillName}' is not supported in the current milestone.");
            return;
        }

        CloseSubPanel(abilitiesPanel);
        SetMenuOpen(false);

        battleManager.SetPendingSkill(skill);

        CombatUnit autoTarget = FindAutoTarget(skill, unit);
        if (autoTarget != null || skill.targetType == SkillTarget.Self)
        {
            battleManager.TryAssignActionFromHud(CombatActionType.Skill, autoTarget);
        }
    }

    private CombatUnit FindAutoTarget(SkillData skill, CombatUnit actor)
    {
        if (skill == null || actor == null) return null;

        switch (skill.targetType)
        {
            case SkillTarget.Self:
                return actor;
            case SkillTarget.SingleAlly:
            case SkillTarget.AllAllies:
                return FindFirstAliveUnit(CombatUnit.UnitType.Ally, actor);
            case SkillTarget.SingleEnemy:
            case SkillTarget.AllEnemies:
            default:
                return FindFirstAliveUnit(CombatUnit.UnitType.Enemy, null);
        }
    }

    private CombatUnit FindFirstAliveUnit(CombatUnit.UnitType type, CombatUnit exclude)
    {
        IReadOnlyList<CombatUnit> units = type == CombatUnit.UnitType.Ally
            ? battleManager.AllyUnits
            : battleManager.EnemyUnits;

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].IsAlive && units[i] != exclude)
            {
                return units[i];
            }
        }

        return null;
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

    private GameObject CreateSubPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.1f);
        rect.anchorMax = new Vector2(0.85f, 0.9f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);
        return panel;
    }

    private void CreateSubPanelTitle(Transform parent, string title)
    {
        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(parent, false);
        RectTransform rect = titleObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.85f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(8f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);

        Text text = titleObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.95f, 0.85f, 0.4f);
        text.text = title;
        text.raycastTarget = false;
    }

    private void CreateCloseButton(Transform parent, UnityEngine.Events.UnityAction onClose)
    {
        CreateButton(parent, "Close", new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.12f), onClose);
    }

    private void CloseSubPanel(GameObject panel)
    {
        if (panel != null)
        {
            Destroy(panel);
        }
    }

    private void TryResolveBattleManager()
    {
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
    }
}
