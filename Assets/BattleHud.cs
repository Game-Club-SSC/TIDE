using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    private const string CanvasName = "BattleHudCanvas";

    private BattleManager battleManager;

    // Panels
    private GameObject allyPanel;
    private GameObject enemyPanel;
    private GameObject actionPanel;
    private GameObject momentumPanel;
    private GameObject victoryOverlay;
    private GameObject defeatOverlay;

    // Momentum
    private Image momentumFill;
    private Text momentumLabel;

    // Ally status
    private List<Text> allyLabels = new List<Text>();

    // Enemy status
    private List<Text> enemyLabels = new List<Text>();

    // Action buttons
    private Button attackButton;
    private Button defendButton;
    private Button skillButton;
    private Button tideBreakButton;
    private Text skillButtonText;
    private Text tideBreakButtonText;

    // Target selection
    private GameObject targetPanel;
    private List<Button> targetButtons = new List<Button>();

    // State display
    private Text phaseLabel;

    private void Awake()
    {
        EnsureCanvas();
        TryFindBattleManager();
    }

    private void Update()
    {
        if (battleManager == null)
        {
            TryFindBattleManager();
        }

        if (battleManager != null)
        {
            RefreshDisplay();
        }
    }

    private void TryFindBattleManager()
    {
        battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged += OnMomentumChanged;
        }
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged -= OnMomentumChanged;
        }
    }

    private void OnMomentumChanged(float value)
    {
        UpdateMomentumBar(value);
    }

    private void RefreshDisplay()
    {
        UpdatePhase();
        UpdateAllyStatus();
        UpdateEnemyStatus();
        UpdateActionButtons();
        UpdateMomentumBar(battleManager.Momentum.Value);
        UpdateOverlays();
    }

    private void UpdatePhase()
    {
        if (phaseLabel == null) return;
        phaseLabel.text = battleManager.CurrentPhase.ToString();
    }

    private void UpdateAllyStatus()
    {
        IReadOnlyList<CombatUnit> allies = battleManager.AllyUnits;
        for (int i = 0; i < allyLabels.Count; i++)
        {
            if (i < allies.Count)
            {
                CombatUnit unit = allies[i];
                string hpBar = BuildBar(unit.HP, unit.MaxHP, 10);
                string mpBar = BuildBar(unit.MP, unit.MaxMP, 6);
                string alive = unit.IsAlive ? "" : " [KO]";
                allyLabels[i].text = $"{unit.UnitName}{alive}\nHP: {unit.HP}/{unit.MaxHP} {hpBar}\nMP: {unit.MP}/{unit.MaxMP} {mpBar}";
                allyLabels[i].color = unit.IsAlive ? Color.white : Color.gray;
            }
            else
            {
                allyLabels[i].text = "---";
            }
        }
    }

    private void UpdateEnemyStatus()
    {
        IReadOnlyList<CombatUnit> enemies = battleManager.EnemyUnits;
        for (int i = 0; i < enemyLabels.Count; i++)
        {
            if (i < enemies.Count)
            {
                CombatUnit unit = enemies[i];
                string hpBar = BuildBar(unit.HP, unit.MaxHP, 10);
                string alive = unit.IsAlive ? "" : " [KO]";
                enemyLabels[i].text = $"{unit.UnitName} ({unit.ElementType}){alive}\nHP: {unit.HP}/{unit.MaxHP} {hpBar}";
                enemyLabels[i].color = unit.IsAlive ? new Color(1f, 0.7f, 0.7f) : Color.gray;
            }
            else
            {
                enemyLabels[i].text = "---";
            }
        }
    }

    private void UpdateActionButtons()
    {
        bool isPlayerInput = battleManager.CurrentPhase == BattlePhase.PlayerInput;
        if (actionPanel != null)
        {
            actionPanel.SetActive(isPlayerInput);
        }

        if (!isPlayerInput)
        {
            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }
            return;
        }

        CombatUnit currentInput = battleManager.GetCurrentInputUnit();
        if (currentInput != null && currentInput.Skills != null && currentInput.Skills.Length > 0)
        {
            SkillData skill = currentInput.Skills[0];
            if (skillButtonText != null)
            {
                skillButtonText.text = currentInput.CanUseSkill(skill)
                    ? $"{skill.skillName} ({skill.mpCost} MP)"
                    : $"No MP ({skill.mpCost})";
            }
            skillButton.interactable = currentInput.CanUseSkill(skill);
        }
        else
        {
            if (skillButtonText != null)
            {
                skillButtonText.text = "No Skill";
            }
            skillButton.interactable = false;
        }

        // Tide Break button
        bool tbReady = battleManager.Momentum.IsPlayerTideBreakReady;
        if (tideBreakButton != null)
        {
            tideBreakButton.gameObject.SetActive(tbReady);
        }
        if (tideBreakButtonText != null)
        {
            tideBreakButtonText.text = "TIDE BREAK";
        }
    }

    private void UpdateMomentumBar(float value)
    {
        if (momentumFill == null) return;
        float normalized = (value + 1f) / 2f;
        momentumFill.fillAmount = normalized;

        if (value >= 0)
        {
            float t = Mathf.InverseLerp(0f, 1f, value);
            momentumFill.color = Color.Lerp(new Color(0.2f, 0.5f, 1f), new Color(0.1f, 1f, 0.3f), t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, -1f, value);
            momentumFill.color = Color.Lerp(new Color(0.2f, 0.5f, 1f), new Color(1f, 0.2f, 0.2f), t);
        }

        if (momentumLabel != null)
        {
            if (battleManager.Momentum.IsPlayerTideBreakReady)
                momentumLabel.text = "PLAYER TB READY!";
            else if (battleManager.Momentum.IsEnemyTideBreakReady)
                momentumLabel.text = "ENEMY TB READY!";
            else
                momentumLabel.text = $"Momentum: {value:F2}";
        }
    }

    private void UpdateOverlays()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(battleManager.CurrentPhase == BattlePhase.Victory);
        if (defeatOverlay != null)
            defeatOverlay.SetActive(battleManager.CurrentPhase == BattlePhase.Defeat);
    }

    private string BuildBar(int current, int max, int length)
    {
        if (max <= 0) return new string('.', length);
        int filled = Mathf.Clamp(Mathf.RoundToInt((float)current / max * length), 0, length);
        return new string('█', filled) + new string('░', length - filled);
    }

    // --- Button callbacks ---
    private void OnAttackClicked()
    {
        ShowTargetSelection(CombatActionType.Attack);
    }

    private void OnDefendClicked()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.TryAssignActionFromHud(CombatActionType.Defend, null);
    }

    private void OnSkillClicked()
    {
        ShowTargetSelection(CombatActionType.Skill);
    }

    private void OnTideBreakClicked()
    {
        ShowTargetSelection(CombatActionType.TideBreak);
    }

    private void ShowTargetSelection(CombatActionType actionType)
    {
        if (battleManager == null || targetPanel == null)
        {
            return;
        }

        targetPanel.SetActive(true);
        IReadOnlyList<CombatUnit> enemies = battleManager.GetAliveUnits(CombatUnit.UnitType.Enemy);

        for (int i = 0; i < targetButtons.Count; i++)
        {
            if (i < enemies.Count)
            {
                int index = i;
                targetButtons[i].gameObject.SetActive(true);
                targetButtons[i].GetComponentInChildren<Text>().text = enemies[i].UnitName;
                targetButtons[i].onClick.RemoveAllListeners();
                targetButtons[i].onClick.AddListener(() => OnTargetSelected(actionType, enemies[index]));
            }
            else
            {
                targetButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnTargetSelected(CombatActionType actionType, CombatUnit target)
    {
        targetPanel.SetActive(false);
        if (battleManager == null)
        {
            return;
        }

        battleManager.TryAssignActionFromHud(actionType, target);
    }

    // --- Canvas construction ---
    private void EnsureCanvas()
    {
        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        CreateAllyPanel(canvasObject.transform);
        CreateEnemyPanel(canvasObject.transform);
        CreateActionPanel(canvasObject.transform);
        CreateTargetPanel(canvasObject.transform);
        CreateMomentumPanel(canvasObject.transform);
        CreatePhaseLabel(canvasObject.transform);
        CreateVictoryOverlay(canvasObject.transform);
        CreateDefeatOverlay(canvasObject.transform);
    }

    private void CreateAllyPanel(Transform parent)
    {
        allyPanel = CreatePanel("AllyPanel", parent,
            new Vector2(0f, 0f), new Vector2(0.3f, 0.4f),
            new Color(0.1f, 0.1f, 0.2f, 0.85f));

        for (int i = 0; i < 3; i++)
        {
            GameObject labelObj = new GameObject($"Ally_{i}", typeof(RectTransform));
            labelObj.transform.SetParent(allyPanel.transform, false);

            Text label = labelObj.AddComponent<Text>();
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 1f - (i + 1) * 0.33f);
            rect.anchorMax = new Vector2(0.95f, 1f - i * 0.33f);
            rect.offsetMin = new Vector2(10f, 2f);
            rect.offsetMax = new Vector2(-10f, -2f);

            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.UpperLeft;
            label.color = Color.white;
            label.raycastTarget = false;
            allyLabels.Add(label);
        }
    }

    private void CreateEnemyPanel(Transform parent)
    {
        enemyPanel = CreatePanel("EnemyPanel", parent,
            new Vector2(0.7f, 0.6f), new Vector2(1f, 1f),
            new Color(0.2f, 0.1f, 0.1f, 0.85f));

        for (int i = 0; i < 3; i++)
        {
            GameObject labelObj = new GameObject($"Enemy_{i}", typeof(RectTransform));
            labelObj.transform.SetParent(enemyPanel.transform, false);

            Text label = labelObj.AddComponent<Text>();
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 1f - (i + 1) * 0.33f);
            rect.anchorMax = new Vector2(0.95f, 1f - i * 0.33f);
            rect.offsetMin = new Vector2(10f, 2f);
            rect.offsetMax = new Vector2(-10f, -2f);

            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.UpperLeft;
            label.color = new Color(1f, 0.7f, 0.7f);
            label.raycastTarget = false;
            enemyLabels.Add(label);
        }
    }

    private void CreateActionPanel(Transform parent)
    {
        actionPanel = CreatePanel("ActionPanel", parent,
            new Vector2(0.25f, 0f), new Vector2(0.75f, 0.18f),
            new Color(0.15f, 0.15f, 0.15f, 0.9f));

        attackButton = CreateActionButton(actionPanel.transform, "Attack", 0, OnAttackClicked);
        defendButton = CreateActionButton(actionPanel.transform, "Defend", 1, OnDefendClicked);
        skillButton = CreateActionButton(actionPanel.transform, "Skill", 2, OnSkillClicked);
        tideBreakButton = CreateActionButton(actionPanel.transform, "TIDE BREAK", 3, OnTideBreakClicked);
        tideBreakButton.gameObject.SetActive(false);

        skillButtonText = skillButton.GetComponentInChildren<Text>();
        tideBreakButtonText = tideBreakButton.GetComponentInChildren<Text>();
    }

    private Button CreateActionButton(Transform parent, string label, int index, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{label}", typeof(RectTransform));
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        float width = 0.23f;
        rect.anchorMin = new Vector2(0.02f + index * width, 0.15f);
        rect.anchorMax = new Vector2(0.02f + (index + 1) * width - 0.02f, 0.85f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = label;
        text.raycastTarget = false;

        return btn;
    }

    private void CreateTargetPanel(Transform parent)
    {
        targetPanel = CreatePanel("TargetPanel", parent,
            new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.45f),
            new Color(0.2f, 0.15f, 0.1f, 0.95f));
        targetPanel.SetActive(false);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(targetPanel.transform, false);
        Text title = titleObj.AddComponent<Text>();
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.8f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 18;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = Color.white;
        title.text = "Select Target";
        title.raycastTarget = false;

        for (int i = 0; i < 3; i++)
        {
            GameObject btnObj = new GameObject($"Target_{i}", typeof(RectTransform));
            btnObj.transform.SetParent(targetPanel.transform, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.55f - i * 0.25f);
            rect.anchorMax = new Vector2(0.9f, 0.75f - i * 0.25f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.5f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;

            targetButtons.Add(btn);
        }
    }

    private void CreateMomentumPanel(Transform parent)
    {
        momentumPanel = CreatePanel("MomentumPanel", parent,
            new Vector2(0.25f, 0.92f), new Vector2(0.75f, 0.98f),
            new Color(0.1f, 0.1f, 0.15f, 0.9f));

        // Background bar
        GameObject bgObj = new GameObject("BarBG", typeof(RectTransform));
        bgObj.transform.SetParent(momentumPanel.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.05f, 0.2f);
        bgRect.anchorMax = new Vector2(0.75f, 0.8f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f, 1f);

        // Fill bar
        GameObject fillObj = new GameObject("BarFill", typeof(RectTransform));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0.5f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        momentumFill = fillObj.AddComponent<Image>();
        momentumFill.type = Image.Type.Filled;
        momentumFill.fillMethod = Image.FillMethod.Horizontal;
        momentumFill.fillAmount = 0.5f;
        momentumFill.color = new Color(0.2f, 0.5f, 1f);

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(momentumPanel.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.77f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        momentumLabel = labelObj.AddComponent<Text>();
        momentumLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        momentumLabel.fontSize = 14;
        momentumLabel.alignment = TextAnchor.MiddleCenter;
        momentumLabel.color = Color.white;
        momentumLabel.text = "Momentum: 0.00";
        momentumLabel.raycastTarget = false;
    }

    private void CreatePhaseLabel(Transform parent)
    {
        GameObject labelObj = new GameObject("PhaseLabel", typeof(RectTransform));
        labelObj.transform.SetParent(parent, false);

        phaseLabel = labelObj.AddComponent<Text>();
        RectTransform rect = phaseLabel.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(300f, 30f);

        phaseLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        phaseLabel.fontSize = 20;
        phaseLabel.alignment = TextAnchor.MiddleCenter;
        phaseLabel.color = Color.yellow;
        phaseLabel.raycastTarget = false;
    }

    private void CreateVictoryOverlay(Transform parent)
    {
        victoryOverlay = CreatePanel("VictoryOverlay", parent,
            Vector2.zero, Vector2.one,
            new Color(0f, 0f, 0f, 0.7f));

        GameObject textObj = new GameObject("VictoryText", typeof(RectTransform));
        textObj.transform.SetParent(victoryOverlay.transform, false);
        Text text = textObj.AddComponent<Text>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.2f, 0.3f);
        rect.anchorMax = new Vector2(0.8f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 72;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.3f, 1f, 0.3f);
        text.text = "VICTORY!";
        text.raycastTarget = false;

        victoryOverlay.SetActive(false);
    }

    private void CreateDefeatOverlay(Transform parent)
    {
        defeatOverlay = CreatePanel("DefeatOverlay", parent,
            Vector2.zero, Vector2.one,
            new Color(0f, 0f, 0f, 0.7f));

        GameObject textObj = new GameObject("DefeatText", typeof(RectTransform));
        textObj.transform.SetParent(defeatOverlay.transform, false);
        Text text = textObj.AddComponent<Text>();
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.2f, 0.3f);
        rect.anchorMax = new Vector2(0.8f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 72;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.3f, 0.3f);
        text.text = "DEFEAT...";
        text.raycastTarget = false;

        defeatOverlay.SetActive(false);
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = bgColor;

        return panel;
    }
}
