using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    private const string CanvasName = "BattleHudCanvas";

    private BattleManager battleManager;
    private Camera mainCamera;

    // World health bars
    private class WorldHealthBar
    {
        public RectTransform Root;
        public Image HpFill;
        public Image MpFill;
        public Text NameLabel;
        public Text HpText;
        public Text TargetLabel;
        public CombatUnit TrackedUnit;
    }

    private List<WorldHealthBar> worldBars = new List<WorldHealthBar>();
    private Transform worldBarContainer;

    // Panels
    private GameObject actionPanel;
    private GameObject momentumPanel;
    private GameObject victoryOverlay;
    private GameObject defeatOverlay;

    // Clash announcement
    private GameObject clashOverlay;
    private Text clashTitle;
    private Text clashSubtitle;
    private float clashDisplayTimer;

    // Momentum
    private Image momentumFill;
    private Text momentumLabel;

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
    private Text turnLabel;

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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (battleManager != null)
        {
            RefreshDisplay();
        }

        UpdateWorldBarPositions();

        if (clashOverlay != null && clashOverlay.activeSelf)
        {
            clashDisplayTimer -= Time.deltaTime;
            if (clashDisplayTimer <= 0f)
            {
                clashOverlay.SetActive(false);
            }
        }
    }

    private void TryFindBattleManager()
    {
        battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged += OnMomentumChanged;
            battleManager.OnClashResolved += OnClashResolved;
        }
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged -= OnMomentumChanged;
            battleManager.OnClashResolved -= OnClashResolved;
        }
    }

    private void OnMomentumChanged(float value)
    {
        UpdateMomentumBar(value);
    }

    private void OnClashResolved(BattleManager.ClashResult result)
    {
        ShowClashAnnouncement(result);
    }

    private void ShowClashAnnouncement(BattleManager.ClashResult result)
    {
        if (clashOverlay == null) return;

        clashOverlay.SetActive(true);
        clashDisplayTimer = 2.5f;

        if (result.HasWinner)
        {
            bool winnerIsAlly = result.Winner.Type == CombatUnit.UnitType.Ally;
            clashTitle.text = "CLASH!";
            clashTitle.color = winnerIsAlly ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f);
            clashSubtitle.text = $"{result.Winner.UnitName} beats {result.Loser.UnitName}!";
            clashSubtitle.color = winnerIsAlly ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.5f, 0.5f);
        }
        else
        {
            clashTitle.text = "CLASH!";
            clashTitle.color = new Color(1f, 0.9f, 0.4f);
            clashSubtitle.text = $"{result.UnitA.UnitName} vs {result.UnitB.UnitName} — Neutral!";
            clashSubtitle.color = new Color(1f, 0.85f, 0.5f);
        }
    }

    private void RefreshDisplay()
    {
        UpdateTurnLabel();
        UpdateWorldBars();
        UpdateActionButtons();
        UpdateMomentumBar(battleManager.Momentum.Value);
        UpdateOverlays();
    }

    private void UpdateTurnLabel()
    {
        if (turnLabel == null) return;

        BattlePhase phase = battleManager.CurrentPhase;
        switch (phase)
        {
            case BattlePhase.PlayerInput:
                CombatUnit current = battleManager.GetCurrentInputUnit();
                turnLabel.text = current != null ? $"{current.UnitName}'s Turn" : "Player Turn";
                turnLabel.color = new Color(0.4f, 0.9f, 1f);
                break;
            case BattlePhase.ActionExecution:
                turnLabel.text = "Executing...";
                turnLabel.color = new Color(1f, 0.8f, 0.3f);
                break;
            case BattlePhase.Victory:
                turnLabel.text = "";
                break;
            case BattlePhase.Defeat:
                turnLabel.text = "";
                break;
            default:
                turnLabel.text = phase.ToString();
                turnLabel.color = Color.white;
                break;
        }
    }

    // --- World health bars ---

    private void EnsureWorldBars()
    {
        if (worldBars.Count > 0) return;

        for (int i = 0; i < 6; i++)
        {
            worldBars.Add(CreateWorldBar(i));
        }
    }

    private WorldHealthBar CreateWorldBar(int index)
    {
        GameObject root = new GameObject($"WorldBar_{index}", typeof(RectTransform));
        root.transform.SetParent(worldBarContainer, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(120f, 64f);

        // Background
        GameObject bgObj = new GameObject("BG", typeof(RectTransform));
        bgObj.transform.SetParent(root.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;

        // Name label
        GameObject nameObj = new GameObject("Name", typeof(RectTransform));
        nameObj.transform.SetParent(root.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.65f);
        nameRect.anchorMax = new Vector2(0.65f, 1f);
        nameRect.offsetMin = new Vector2(4f, 0f);
        nameRect.offsetMax = new Vector2(-2f, 0f);
        Text nameLabel = nameObj.AddComponent<Text>();
        nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameLabel.fontSize = 12;
        nameLabel.alignment = TextAnchor.MiddleLeft;
        nameLabel.color = Color.white;
        nameLabel.raycastTarget = false;

        // HP text
        GameObject hpTextObj = new GameObject("HPText", typeof(RectTransform));
        hpTextObj.transform.SetParent(root.transform, false);
        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchorMin = new Vector2(0.65f, 0.65f);
        hpTextRect.anchorMax = new Vector2(1f, 1f);
        hpTextRect.offsetMin = new Vector2(2f, 0f);
        hpTextRect.offsetMax = new Vector2(-4f, 0f);
        Text hpText = hpTextObj.AddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 11;
        hpText.alignment = TextAnchor.MiddleRight;
        hpText.color = new Color(0.7f, 1f, 0.7f);
        hpText.raycastTarget = false;

        // HP bar background
        GameObject hpBgObj = new GameObject("HPBG", typeof(RectTransform));
        hpBgObj.transform.SetParent(root.transform, false);
        RectTransform hpBgRect = hpBgObj.GetComponent<RectTransform>();
        hpBgRect.anchorMin = new Vector2(0.03f, 0.32f);
        hpBgRect.anchorMax = new Vector2(0.97f, 0.6f);
        hpBgRect.offsetMin = Vector2.zero;
        hpBgRect.offsetMax = Vector2.zero;
        Image hpBgImg = hpBgObj.AddComponent<Image>();
        hpBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        hpBgImg.raycastTarget = false;

        // HP bar fill
        GameObject hpFillObj = new GameObject("HPFill", typeof(RectTransform));
        hpFillObj.transform.SetParent(hpBgObj.transform, false);
        RectTransform hpFillRect = hpFillObj.GetComponent<RectTransform>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;
        Image hpFill = hpFillObj.AddComponent<Image>();
        hpFill.color = new Color(0.2f, 0.85f, 0.3f);
        hpFill.raycastTarget = false;

        // MP bar background
        GameObject mpBgObj = new GameObject("MPBG", typeof(RectTransform));
        mpBgObj.transform.SetParent(root.transform, false);
        RectTransform mpBgRect = mpBgObj.GetComponent<RectTransform>();
        mpBgRect.anchorMin = new Vector2(0.03f, 0.1f);
        mpBgRect.anchorMax = new Vector2(0.97f, 0.28f);
        mpBgRect.offsetMin = Vector2.zero;
        mpBgRect.offsetMax = Vector2.zero;
        Image mpBgImg = mpBgObj.AddComponent<Image>();
        mpBgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        mpBgImg.raycastTarget = false;

        // MP bar fill
        GameObject mpFillObj = new GameObject("MPFill", typeof(RectTransform));
        mpFillObj.transform.SetParent(mpBgObj.transform, false);
        RectTransform mpFillRect = mpFillObj.GetComponent<RectTransform>();
        mpFillRect.anchorMin = Vector2.zero;
        mpFillRect.anchorMax = Vector2.one;
        mpFillRect.offsetMin = Vector2.zero;
        mpFillRect.offsetMax = Vector2.zero;
        Image mpFill = mpFillObj.AddComponent<Image>();
        mpFill.color = new Color(0.3f, 0.5f, 1f);
        mpFill.raycastTarget = false;

        // Target label
        GameObject targetObj = new GameObject("TargetLabel", typeof(RectTransform));
        targetObj.transform.SetParent(root.transform, false);
        RectTransform targetRect = targetObj.GetComponent<RectTransform>();
        targetRect.anchorMin = new Vector2(0f, -0.55f);
        targetRect.anchorMax = new Vector2(1f, 0f);
        targetRect.offsetMin = Vector2.zero;
        targetRect.offsetMax = Vector2.zero;
        Text targetLabel = targetObj.AddComponent<Text>();
        targetLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        targetLabel.fontSize = 11;
        targetLabel.fontStyle = FontStyle.Italic;
        targetLabel.alignment = TextAnchor.MiddleCenter;
        targetLabel.color = new Color(1f, 0.6f, 0.3f);
        targetLabel.raycastTarget = false;
        targetLabel.text = "";

        root.SetActive(false);

        return new WorldHealthBar
        {
            Root = rootRect,
            HpFill = hpFill,
            MpFill = mpFill,
            NameLabel = nameLabel,
            HpText = hpText,
            TargetLabel = targetLabel,
            TrackedUnit = null
        };
    }

    private void UpdateWorldBars()
    {
        EnsureWorldBars();

        IReadOnlyList<CombatUnit> allies = battleManager.AllyUnits;
        IReadOnlyList<CombatUnit> enemies = battleManager.EnemyUnits;

        int barIndex = 0;

        // Allies first (left side of screen)
        for (int i = 0; i < allies.Count && barIndex < worldBars.Count; i++, barIndex++)
        {
            SetupWorldBar(worldBars[barIndex], allies[i], new Color(0.4f, 0.9f, 1f));
        }

        // Enemies (right side)
        for (int i = 0; i < enemies.Count && barIndex < worldBars.Count; i++, barIndex++)
        {
            SetupWorldBar(worldBars[barIndex], enemies[i], new Color(1f, 0.5f, 0.4f));
        }

        // Hide unused bars
        for (int i = barIndex; i < worldBars.Count; i++)
        {
            worldBars[i].Root.gameObject.SetActive(false);
        }
    }

    private void SetupWorldBar(WorldHealthBar bar, CombatUnit unit, Color nameColor)
    {
        bar.Root.gameObject.SetActive(true);
        bar.TrackedUnit = unit;

        bar.NameLabel.text = unit.UnitName;
        bar.NameLabel.color = unit.IsAlive ? nameColor : Color.gray;

        float hpRatio = unit.MaxHP > 0 ? (float)unit.HP / unit.MaxHP : 0f;
        bar.HpFill.fillAmount = hpRatio;

        if (hpRatio > 0.5f)
            bar.HpFill.color = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.85f, 0.3f), (hpRatio - 0.5f) * 2f);
        else
            bar.HpFill.color = Color.Lerp(new Color(0.8f, 0.15f, 0.15f), new Color(1f, 0.8f, 0.2f), hpRatio * 2f);

        float mpRatio = unit.MaxMP > 0 ? (float)unit.MP / unit.MaxMP : 0f;
        bar.MpFill.fillAmount = mpRatio;

        bar.HpText.text = $"{unit.HP}/{unit.MaxHP}";

        if (!unit.IsAlive)
        {
            bar.NameLabel.text = unit.UnitName + " [KO]";
            bar.HpText.text = "KO";
            bar.HpText.color = new Color(1f, 0.3f, 0.3f);
            bar.TargetLabel.text = "";
        }
        else
        {
            bar.HpText.color = new Color(0.7f, 1f, 0.7f);
        }

        UpdateTargetLabel(bar);
    }

    private void UpdateTargetLabel(WorldHealthBar bar)
    {
        if (bar.TargetLabel == null || bar.TrackedUnit == null) return;

        bool showTargets = battleManager.CurrentPhase == BattlePhase.PlayerInput
                        || battleManager.CurrentPhase == BattlePhase.ActionExecution;

        if (!showTargets || !bar.TrackedUnit.IsAlive)
        {
            bar.TargetLabel.text = "";
            return;
        }

        if (bar.TrackedUnit.Type == CombatUnit.UnitType.Enemy)
        {
            CombatUnit target = battleManager.GetEnemyTarget(bar.TrackedUnit);
            bar.TargetLabel.text = target != null ? $"\u25B6 {target.UnitName}" : "";
        }
        else
        {
            bar.TargetLabel.text = "";
        }
    }

    private void UpdateWorldBarPositions()
    {
        if (mainCamera == null || battleManager == null) return;

        IReadOnlyList<CombatUnit> allies = battleManager.AllyUnits;
        IReadOnlyList<CombatUnit> enemies = battleManager.EnemyUnits;

        int barIndex = 0;

        for (int i = 0; i < allies.Count && barIndex < worldBars.Count; i++, barIndex++)
        {
            PositionBarAboveUnit(worldBars[barIndex], allies[i]);
        }

        for (int i = 0; i < enemies.Count && barIndex < worldBars.Count; i++, barIndex++)
        {
            PositionBarAboveUnit(worldBars[barIndex], enemies[i]);
        }
    }

    private void PositionBarAboveUnit(WorldHealthBar bar, CombatUnit unit)
    {
        if (unit == null || !bar.Root.gameObject.activeSelf) return;

        Vector3 worldPos = unit.transform.position + Vector3.up * 2.2f;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f)
        {
            bar.Root.gameObject.SetActive(false);
            return;
        }

        bar.Root.position = screenPos;
    }

    // --- Action buttons ---

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
                momentumLabel.text = "TIDE BREAK READY!";
            else if (battleManager.Momentum.IsEnemyTideBreakReady)
                momentumLabel.text = "ENEMY TB READY!";
            else
                momentumLabel.text = "";
        }
    }

    private void UpdateOverlays()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(battleManager.CurrentPhase == BattlePhase.Victory);
        if (defeatOverlay != null)
            defeatOverlay.SetActive(battleManager.CurrentPhase == BattlePhase.Defeat);
    }

    // --- Button callbacks ---
    private void OnAttackClicked()
    {
        ShowTargetSelection(CombatActionType.Attack);
    }

    private void OnDefendClicked()
    {
        if (battleManager == null) return;
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
        if (battleManager == null || targetPanel == null) return;

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
        if (battleManager == null) return;
        battleManager.TryAssignActionFromHud(actionType, target);
    }

    // --- Canvas construction ---
    private void EnsureCanvas()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        // World bar container (no raycasting, just for layout)
        worldBarContainer = new GameObject("WorldBars", typeof(RectTransform)).transform;
        worldBarContainer.SetParent(canvasObject.transform, false);
        RectTransform wbRect = worldBarContainer.GetComponent<RectTransform>();
        wbRect.anchorMin = Vector2.zero;
        wbRect.anchorMax = Vector2.one;
        wbRect.offsetMin = Vector2.zero;
        wbRect.offsetMax = Vector2.zero;

        CreateActionPanel(canvasObject.transform);
        CreateTargetPanel(canvasObject.transform);
        CreateMomentumPanel(canvasObject.transform);
        CreateTurnLabel(canvasObject.transform);
        CreateVictoryOverlay(canvasObject.transform);
        CreateDefeatOverlay(canvasObject.transform);
        CreateClashOverlay(canvasObject.transform);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
        Debug.Log("[BattleHud] Created EventSystem for combat scene.");
    }

    private void CreateActionPanel(Transform parent)
    {
        actionPanel = CreatePanel("ActionPanel", parent,
            new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.13f),
            new Color(0.08f, 0.08f, 0.12f, 0.92f));

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
        float width = 0.235f;
        rect.anchorMin = new Vector2(0.015f + index * width, 0.12f);
        rect.anchorMax = new Vector2(0.015f + (index + 1) * width - 0.015f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.24f, 1f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.18f, 0.18f, 0.24f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.4f, 1f);
        colors.pressedColor = new Color(0.12f, 0.12f, 0.18f, 1f);
        colors.selectedColor = new Color(0.25f, 0.25f, 0.35f, 1f);
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
        text.fontSize = 16;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.9f, 0.95f);
        text.text = label;
        text.raycastTarget = false;

        return btn;
    }

    private void CreateTargetPanel(Transform parent)
    {
        targetPanel = CreatePanel("TargetPanel", parent,
            new Vector2(0.32f, 0.14f), new Vector2(0.68f, 0.42f),
            new Color(0.1f, 0.08f, 0.06f, 0.95f));
        targetPanel.SetActive(false);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(targetPanel.transform, false);
        Text title = titleObj.AddComponent<Text>();
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0.8f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 18;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 0.85f, 0.5f);
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
            img.color = new Color(0.35f, 0.15f, 0.12f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.5f, 0.25f, 0.18f, 1f);
            colors.pressedColor = new Color(0.25f, 0.1f, 0.08f, 1f);
            btn.colors = colors;

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform tr = textObj.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.7f);
            text.raycastTarget = false;

            targetButtons.Add(btn);
        }
    }

    private void CreateMomentumPanel(Transform parent)
    {
        momentumPanel = CreatePanel("MomentumPanel", parent,
            new Vector2(0.28f, 0.94f), new Vector2(0.72f, 0.99f),
            new Color(0.06f, 0.06f, 0.1f, 0.85f));

        // Background bar
        GameObject bgObj = new GameObject("BarBG", typeof(RectTransform));
        bgObj.transform.SetParent(momentumPanel.transform, false);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.03f, 0.15f);
        bgRect.anchorMax = new Vector2(0.65f, 0.85f);
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);
        bgImg.raycastTarget = false;

        // Fill bar
        GameObject fillObj = new GameObject("BarFill", typeof(RectTransform));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        momentumFill = fillObj.AddComponent<Image>();
        momentumFill.type = Image.Type.Filled;
        momentumFill.fillMethod = Image.FillMethod.Horizontal;
        momentumFill.fillAmount = 0.5f;
        momentumFill.color = new Color(0.2f, 0.5f, 1f);
        momentumFill.raycastTarget = false;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform));
        labelObj.transform.SetParent(momentumPanel.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.68f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        momentumLabel = labelObj.AddComponent<Text>();
        momentumLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        momentumLabel.fontSize = 13;
        momentumLabel.fontStyle = FontStyle.Bold;
        momentumLabel.alignment = TextAnchor.MiddleCenter;
        momentumLabel.color = new Color(1f, 0.9f, 0.4f);
        momentumLabel.text = "";
        momentumLabel.raycastTarget = false;
    }

    private void CreateTurnLabel(Transform parent)
    {
        GameObject labelObj = new GameObject("TurnLabel", typeof(RectTransform));
        labelObj.transform.SetParent(parent, false);

        turnLabel = labelObj.AddComponent<Text>();
        RectTransform rect = turnLabel.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(400f, 28f);

        turnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        turnLabel.fontSize = 22;
        turnLabel.fontStyle = FontStyle.Bold;
        turnLabel.alignment = TextAnchor.MiddleCenter;
        turnLabel.color = Color.white;
        turnLabel.raycastTarget = false;
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
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
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
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.3f, 0.3f);
        text.text = "DEFEAT...";
        text.raycastTarget = false;

        defeatOverlay.SetActive(false);
    }

    private void CreateClashOverlay(Transform parent)
    {
        clashOverlay = new GameObject("ClashOverlay", typeof(RectTransform));
        clashOverlay.transform.SetParent(parent, false);

        RectTransform rootRect = clashOverlay.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.2f, 0.6f);
        rootRect.anchorMax = new Vector2(0.8f, 0.85f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image bgImg = clashOverlay.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.75f);
        bgImg.raycastTarget = false;

        // Title (CLASH!)
        GameObject titleObj = new GameObject("ClashTitle", typeof(RectTransform));
        titleObj.transform.SetParent(clashOverlay.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        clashTitle = titleObj.AddComponent<Text>();
        clashTitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        clashTitle.fontSize = 48;
        clashTitle.fontStyle = FontStyle.Bold;
        clashTitle.alignment = TextAnchor.MiddleCenter;
        clashTitle.color = Color.white;
        clashTitle.text = "";
        clashTitle.raycastTarget = false;

        // Subtitle (who won / neutral)
        GameObject subObj = new GameObject("ClashSubtitle", typeof(RectTransform));
        subObj.transform.SetParent(clashOverlay.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 0f);
        subRect.anchorMax = new Vector2(1f, 0.5f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        clashSubtitle = subObj.AddComponent<Text>();
        clashSubtitle.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        clashSubtitle.fontSize = 24;
        clashSubtitle.fontStyle = FontStyle.Bold;
        clashSubtitle.alignment = TextAnchor.MiddleCenter;
        clashSubtitle.color = Color.white;
        clashSubtitle.text = "";
        clashSubtitle.raycastTarget = false;

        clashOverlay.SetActive(false);
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
        img.raycastTarget = false;

        return panel;
    }
}
