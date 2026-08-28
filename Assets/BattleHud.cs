using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    private const string CanvasName = "BattleHudCanvas";

    private BattleManager battleManager;
    private BattleEscapeMenu escapeMenu;
    private bool isMenuOpen = false;
    private Camera mainCamera;

    // World health bars
    private class WorldHealthBar
    {
        public RectTransform Root;
        public Image HpFill;
        public Image MpFill;
        public RectTransform MpFillRect;
        public Text NameLabel;
        public Text HpText;
        public Text MpText;
        public Text TargetLabel;
        public Text StatusLabel;
        public CombatUnit TrackedUnit;
        public float DisplayedHpRatio = 1f;
        public float DisplayedMpRatio = 1f;
    }

    private List<WorldHealthBar> worldBars = new List<WorldHealthBar>();
    private Transform worldBarContainer;

    // Panels
    private GameObject actionPanel;
    private GameObject momentumPanel;
    private GameObject victoryOverlay;
    private GameObject defeatOverlay;
    private Text xpRewardText;
    private Text gearDropText;

    // Clash announcement
    private GameObject clashOverlay;
    private Text clashTitle;
    private Text clashSubtitle;
    private float clashDisplayTimer;

    // Crit announcement
    private GameObject critOverlay;
    private Text critLabel;
    private float critDisplayTimer;

    // Battle action FX
    private readonly Dictionary<Transform, Vector3> spriteBasePositions = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, float> spriteAnimPhase = new Dictionary<Transform, float>();
    private readonly Dictionary<Transform, Vector3> spriteAppliedOffsets = new Dictionary<Transform, Vector3>();

    // Momentum
    private Image momentumFill;
    private Text momentumLabel;
    private RectTransform momentumIndicator;
    private float displayedMomentumValue;

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

    // TideBreak selection
    private GameObject tideBreakPanel;
    private List<Button> tideBreakButtons = new List<Button>();

    // Skill selection
    private GameObject skillPanel;
    private List<Button> skillButtons = new List<Button>();
    private SkillData selectedSkillData;

    // State display
    private Text turnLabel;

    // Team dynamic overlay
    private GameObject teamDynamicOverlay;
    private Text teamDynamicLabel;
    private float teamDynamicDisplayTimer;

    private bool targetPanelRequestedOpen;
    private bool tideBreakPanelRequestedOpen;
    private bool skillPanelRequestedOpen;
    private CombatUnit trackedInputUnit;

    private void Awake()
    {
        EnsureCanvas();
        TryFindBattleManager();
        TryFindEscapeMenu();
    }

    private void Start()
    {
        SelectDefaultActionButton();
    }

    private void Update()
    {
        if (escapeMenu == null)
            TryFindEscapeMenu();

        isMenuOpen = escapeMenu != null && escapeMenu.IsMenuOpen;

        // Handle Escape key press to toggle menu (only during PlayerInput phase)
        if (Input.GetKeyDown(KeyCode.Escape) && battleManager != null && battleManager.CurrentPhase == BattlePhase.PlayerInput)
        {
            if (escapeMenu != null)
                escapeMenu.ToggleMenu();

            // Re-sample so panels reflect the new menu state this frame
            isMenuOpen = escapeMenu != null && escapeMenu.IsMenuOpen;
        }

        // Hide other UI elements when menu is open
        if (actionPanel != null)
            actionPanel.SetActive(!isMenuOpen && battleManager != null && battleManager.CurrentPhase == BattlePhase.PlayerInput);
        if (targetPanel != null)
            targetPanel.SetActive(!isMenuOpen && targetPanelRequestedOpen);
        if (tideBreakPanel != null)
            tideBreakPanel.SetActive(!isMenuOpen && tideBreakPanelRequestedOpen);
        if (skillPanel != null)
            skillPanel.SetActive(!isMenuOpen && skillPanelRequestedOpen);

        SelectDefaultActionButton();

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

        if (HasBattleSpriteVisuals())
        {
            UpdateBattleSpriteAnimations();
        }
        else
        {
            ClearBattleSpriteAnimationCache();
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

        if (critOverlay != null && critOverlay.activeSelf)
        {
            critDisplayTimer -= Time.deltaTime;
            if (critDisplayTimer <= 0f)
            {
                critOverlay.SetActive(false);
            }
        }

        if (teamDynamicOverlay != null && teamDynamicOverlay.activeSelf)
        {
            teamDynamicDisplayTimer -= Time.deltaTime;
            if (teamDynamicDisplayTimer <= 0f)
            {
                teamDynamicOverlay.SetActive(false);
            }
        }
    }

    private void TryFindBattleManager()
    {
        battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged -= OnMomentumChanged;
            battleManager.OnClashResolved -= OnClashResolved;
            battleManager.OnDamageDealt -= OnDamageDealt;
            battleManager.Momentum.OnMomentumChanged += OnMomentumChanged;
            battleManager.OnClashResolved += OnClashResolved;
            battleManager.OnDamageDealt += OnDamageDealt;
        }
    }

    private void TryFindEscapeMenu()
    {
        escapeMenu = FindFirstObjectByType<BattleEscapeMenu>();
    }

    private void OnDestroy()
    {
        if (battleManager != null)
        {
            battleManager.Momentum.OnMomentumChanged -= OnMomentumChanged;
            battleManager.OnClashResolved -= OnClashResolved;
            battleManager.OnDamageDealt -= OnDamageDealt;
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
        if (clashOverlay == null || clashTitle == null || clashSubtitle == null)
        {
            Debug.LogWarning("[BattleHud] Clash announcement skipped because UI references are missing.", this);
            return;
        }

        clashOverlay.SetActive(true);
        clashDisplayTimer = 2.5f;

        if (result.HasWinner && result.Winner != null && result.Loser != null)
        {
            bool winnerIsAlly = result.Winner.Type == CombatUnit.UnitType.Ally;

            if (result.NeutralQteTriggered)
            {
                clashTitle.text = result.NeutralQteSuccess ? "QTE CLASH SUCCESS!" : "QTE CLASH FAIL!";
                clashTitle.color = winnerIsAlly ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f);

                string resolutionSuffix = string.IsNullOrEmpty(result.NeutralQteResolution)
                    ? ""
                    : $" [{result.NeutralQteResolution}]";
                clashSubtitle.text = $"{result.Winner.UnitName} wins neutral clash.{resolutionSuffix}";
                clashSubtitle.color = winnerIsAlly ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.5f, 0.5f);
            }
            else
            {
                clashTitle.text = "CLASH!";
                clashTitle.color = winnerIsAlly ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f);
                clashSubtitle.text = $"{result.Winner.UnitName} beats {result.Loser.UnitName}!";
                clashSubtitle.color = winnerIsAlly ? new Color(0.5f, 1f, 0.7f) : new Color(1f, 0.5f, 0.5f);
            }
        }
        else
        {
            if (result.HasWinner)
            {
                Debug.LogWarning("[BattleHud] Clash winner announcement missing unit references. Showing neutral fallback text.", this);
            }

            string unitAName = result.UnitA != null ? result.UnitA.UnitName : "Unknown A";
            string unitBName = result.UnitB != null ? result.UnitB.UnitName : "Unknown B";

            clashTitle.text = "CLASH!";
            clashTitle.color = new Color(1f, 0.9f, 0.4f);
            clashSubtitle.text = $"{unitAName} vs {unitBName} — Neutral!";
            clashSubtitle.color = new Color(1f, 0.85f, 0.5f);
        }
    }

    private void OnDamageDealt(CombatUnit actor, bool isCrit)
    {
        if (isCrit)
        {
            ShowCritAnnouncement(actor);
        }
    }

    private void UpdateBattleSpriteAnimations()
    {
        if (battleManager == null)
        {
            return;
        }

        IReadOnlyList<CombatUnit> allUnits = battleManager.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            CombatUnit unit = allUnits[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            Transform visual = unit.transform.Find("BattleSpriteVisual");
            if (visual == null)
            {
                continue;
            }

            SpriteRenderer visualRenderer = visual.GetComponent<SpriteRenderer>();
            if (visualRenderer == null || !visualRenderer.enabled)
            {
                continue;
            }

            if (!spriteBasePositions.ContainsKey(visual))
            {
                spriteBasePositions[visual] = visual.localPosition;
            }

            if (!spriteAnimPhase.ContainsKey(visual))
            {
                spriteAnimPhase[visual] = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            }

            float phase = spriteAnimPhase[visual] + Time.deltaTime * 5.4f;
            spriteAnimPhase[visual] = phase;

            bool unitIsActive = battleManager.GetCurrentInputUnit() == unit && battleManager.CurrentPhase == BattlePhase.PlayerInput;
            float bob = Mathf.Sin(phase) * (unitIsActive ? 0.05f : 0.03f);
            Vector3 previousOffset = spriteAppliedOffsets.TryGetValue(visual, out Vector3 storedOffset)
                ? storedOffset
                : Vector3.zero;
            Vector3 baseline = visual.localPosition - previousOffset;
            spriteBasePositions[visual] = baseline;

            Vector3 offset = new Vector3(0f, bob, 0f);
            visual.localPosition = baseline + offset;
            spriteAppliedOffsets[visual] = offset;

            Transform shadow = unit.transform.Find("BattleSpriteShadow");
            if (shadow != null)
            {
                SpriteRenderer shadowRenderer = shadow.GetComponent<SpriteRenderer>();
                if (shadowRenderer == null || !shadowRenderer.enabled)
                {
                    continue;
                }

                float pulse = Mathf.Abs(Mathf.Sin(phase * 0.5f));
                shadow.localScale = new Vector3(0.9f + pulse * 0.07f, 0.48f - pulse * 0.05f, 1f);
            }
        }

        List<Transform> removePositions = new List<Transform>();
        foreach (KeyValuePair<Transform, Vector3> pair in spriteBasePositions)
        {
            if (pair.Key == null)
            {
                removePositions.Add(pair.Key);
            }
        }

        for (int i = 0; i < removePositions.Count; i++)
        {
            spriteBasePositions.Remove(removePositions[i]);
            spriteAnimPhase.Remove(removePositions[i]);
            spriteAppliedOffsets.Remove(removePositions[i]);
        }
    }

    private bool HasBattleSpriteVisuals()
    {
        if (battleManager == null)
        {
            return false;
        }

        IReadOnlyList<CombatUnit> allUnits = battleManager.GetAllUnits();
        for (int i = 0; i < allUnits.Count; i++)
        {
            CombatUnit unit = allUnits[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (unit.transform.Find("BattleSpriteVisual") != null)
            {
                Transform visual = unit.transform.Find("BattleSpriteVisual");
                SpriteRenderer visualRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                if (visualRenderer != null && visualRenderer.enabled)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ClearBattleSpriteAnimationCache()
    {
        if (spriteBasePositions.Count == 0 && spriteAnimPhase.Count == 0 && spriteAppliedOffsets.Count == 0)
        {
            return;
        }

        spriteBasePositions.Clear();
        spriteAnimPhase.Clear();
        spriteAppliedOffsets.Clear();
    }

    private void ShowCritAnnouncement(CombatUnit actor)
    {
        if (critOverlay == null || critLabel == null) return;
        critOverlay.SetActive(true);
        critDisplayTimer = 1.2f;
        critLabel.text = "CRITICAL!";
        critLabel.color = new Color(1f, 0.85f, 0.2f);
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
            case BattlePhase.Fled:
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

        // MP text (top-right of MP bar)
        GameObject mpTextObj = new GameObject("MPText", typeof(RectTransform));
        mpTextObj.transform.SetParent(root.transform, false);
        RectTransform mpTextRect = mpTextObj.GetComponent<RectTransform>();
        mpTextRect.anchorMin = new Vector2(0.65f, 0.28f);
        mpTextRect.anchorMax = new Vector2(1f, 0.48f);
        mpTextRect.offsetMin = new Vector2(2f, 0f);
        mpTextRect.offsetMax = new Vector2(-4f, 0f);
        Text mpText = mpTextObj.AddComponent<Text>();
        mpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mpText.fontSize = 11;
        mpText.alignment = TextAnchor.MiddleRight;
        mpText.color = new Color(0.6f, 0.7f, 1f);
        mpText.raycastTarget = false;

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
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
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

        // Status effect label
        GameObject statusObj = new GameObject("StatusLabel", typeof(RectTransform));
        statusObj.transform.SetParent(root.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.03f, 0.0f);
        statusRect.anchorMax = new Vector2(0.97f, 0.09f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        Text statusLabel = statusObj.AddComponent<Text>();
        statusLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusLabel.fontSize = 10;
        statusLabel.alignment = TextAnchor.MiddleLeft;
        statusLabel.color = new Color(1f, 0.9f, 0.5f);
        statusLabel.raycastTarget = false;
        statusLabel.text = "";

        root.SetActive(false);

        return new WorldHealthBar
        {
            Root = rootRect,
            HpFill = hpFill,
            MpFill = mpFill,
            MpFillRect = mpFillRect,
            NameLabel = nameLabel,
            HpText = hpText,
            MpText = mpText,
            TargetLabel = targetLabel,
            StatusLabel = statusLabel,
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
        if (bar.TrackedUnit != unit)
        {
            bar.DisplayedHpRatio = unit.MaxHP > 0 ? (float)unit.HP / unit.MaxHP : 0f;
            bar.DisplayedMpRatio = unit.MaxMP > 0 ? (float)unit.MP / unit.MaxMP : 0f;
        }
        bar.TrackedUnit = unit;

        bar.NameLabel.text = unit.UnitName;
        bar.NameLabel.color = unit.IsAlive ? nameColor : Color.gray;

        float hpRatio = unit.MaxHP > 0 ? (float)unit.HP / unit.MaxHP : 0f;
        bar.DisplayedHpRatio = Mathf.MoveTowards(bar.DisplayedHpRatio, hpRatio, Time.deltaTime * 5f);
        bar.HpFill.fillAmount = bar.DisplayedHpRatio;

        int displayedHp = Mathf.Clamp(Mathf.RoundToInt(bar.DisplayedHpRatio * unit.MaxHP), 0, unit.MaxHP);

        Color hpColor;
        if (bar.DisplayedHpRatio > 0.5f)
            hpColor = Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.2f, 0.85f, 0.3f), (bar.DisplayedHpRatio - 0.5f) * 2f);
        else
            hpColor = Color.Lerp(new Color(0.8f, 0.15f, 0.15f), new Color(1f, 0.8f, 0.2f), bar.DisplayedHpRatio * 2f);
        
        if (unit.IsDefending)
        {
            hpColor = new Color(0.2f, 0.5f, 1f); // Blue tint for defending
        }
        bar.HpFill.color = hpColor;

        float mpRatio = unit.MaxMP > 0 ? (float)unit.MP / unit.MaxMP : 0f;
        if (bar.MpFillRect != null)
        {
            bar.MpFillRect.anchorMax = new Vector2(mpRatio, 1f);
            bar.MpFillRect.offsetMin = Vector2.zero;
            bar.MpFillRect.offsetMax = Vector2.zero;
        }

        bar.HpText.text = $"{displayedHp}/{unit.MaxHP}";
        bar.MpText.text = $"{unit.MP}/{unit.MaxMP}";

        if (!unit.IsAlive)
        {
            bar.NameLabel.text = unit.UnitName + " [KO]";
            bar.HpText.text = "KO";
            bar.HpText.color = new Color(1f, 0.3f, 0.3f);
            bar.MpText.text = "";
            bar.TargetLabel.text = "";
        }
        else
        {
            bar.HpText.color = new Color(0.7f, 1f, 0.7f);
        }

        UpdateTargetLabel(bar);
        UpdateStatusLabel(bar);
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

    private void UpdateStatusLabel(WorldHealthBar bar)
    {
        if (bar.StatusLabel == null || bar.TrackedUnit == null) return;
        if (!bar.TrackedUnit.IsAlive)
        {
            bar.StatusLabel.text = "";
            return;
        }

        IReadOnlyList<StatusEffect> effects = bar.TrackedUnit.ActiveEffects;
        if (effects.Count == 0)
        {
            bar.StatusLabel.text = "";
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (StatusEffect effect in effects)
        {
            if (effect == null || effect.Type == StatusEffectType.None || effect.Duration <= 0)
            {
                continue;
            }

            string abbr = "";
            switch (effect.Type)
            {
                case StatusEffectType.BuffAttack: abbr = "B ATK"; break;
                case StatusEffectType.BuffDefense: abbr = "B DEF"; break;
                case StatusEffectType.DebuffAttack: abbr = "D ATK"; break;
                case StatusEffectType.DebuffDefense: abbr = "D DEF"; break;
                case StatusEffectType.Poison: abbr = "PSN"; break;
                case StatusEffectType.Slow: abbr = "SLW"; break;
                case StatusEffectType.Drowsy: abbr = "DRW"; break;
                case StatusEffectType.Stun: abbr = "STN"; break;
                case StatusEffectType.Burn: abbr = "BRN"; break;
                case StatusEffectType.Regeneration: abbr = "REG"; break;
                case StatusEffectType.Shield: abbr = "SHD"; break;
                case StatusEffectType.Berserk: abbr = "BRK"; break;
                case StatusEffectType.Taunt: abbr = "TNT"; break;
                case StatusEffectType.AccuracyDown: abbr = "ACC-"; break;
                case StatusEffectType.AccuracyBuff: abbr = "ACC+"; break;
                default: continue;
            }
            sb.Append($"{abbr}({effect.Duration}) ");
        }
        bar.StatusLabel.text = sb.ToString().TrimEnd();
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
            actionPanel.SetActive(isPlayerInput && !isMenuOpen);
        }

        if (!isPlayerInput || isMenuOpen)
        {
            trackedInputUnit = null;
            targetPanelRequestedOpen = false;
            tideBreakPanelRequestedOpen = false;
            skillPanelRequestedOpen = false;

            if (targetPanel != null)
            {
                targetPanel.SetActive(false);
            }
            if (tideBreakPanel != null)
            {
                tideBreakPanel.SetActive(false);
            }
            if (skillPanel != null)
            {
                skillPanel.SetActive(false);
            }
            return;
        }

        CombatUnit currentInput = battleManager.GetCurrentInputUnit();
        bool inputUnitChanged = trackedInputUnit != currentInput;
        trackedInputUnit = currentInput;
        if (inputUnitChanged
            && currentInput != null
            && !targetPanelRequestedOpen
            && !skillPanelRequestedOpen
            && !tideBreakPanelRequestedOpen)
        {
            // Each hero starts with Attack selected. This prevents the
            // EventSystem from repeating the prior hero's Defend/Skill action
            // when the player presses Return for the next hero.
            SelectDefaultActionButton(true);
        }

        if (currentInput != null)
        {
            SkillData skill = battleManager.GetFirstSupportedSkillForCurrentSlice(currentInput);
            SkillData usableSkill = battleManager.GetFirstUsableSupportedSkillForCurrentSlice(currentInput);
            if (skillButtonText != null)
            {
                if (usableSkill != null)
                {
                    skillButtonText.text = $"{usableSkill.skillName} ({usableSkill.mpCost} MP)";
                }
                else if (skill != null)
                {
                    skillButtonText.text = $"No MP ({skill.mpCost})";
                }
                else
                {
                    skillButtonText.text = "No Skill";
                }
            }
            if (skillButton != null)
            {
                skillButton.interactable = usableSkill != null;
            }
        }
        else
        {
            if (skillButtonText != null)
            {
                skillButtonText.text = "No Skill";
            }
            if (skillButton != null)
            {
                skillButton.interactable = false;
            }
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
        displayedMomentumValue = Mathf.MoveTowards(displayedMomentumValue, Mathf.Clamp(value, -1f, 1f), Time.deltaTime * 3f);

        if (momentumFill == null) return;
        float normalized = (displayedMomentumValue + 1f) / 2f;
        momentumFill.fillAmount = normalized;

        if (momentumIndicator != null)
        {
            momentumIndicator.anchorMin = new Vector2(normalized, 0f);
            momentumIndicator.anchorMax = new Vector2(normalized, 1f);
            momentumIndicator.anchoredPosition = Vector2.zero;
        }

        if (displayedMomentumValue >= 0)
        {
            float t = Mathf.InverseLerp(0f, 1f, displayedMomentumValue);
            momentumFill.color = Color.Lerp(new Color(0.2f, 0.5f, 1f), new Color(0.1f, 1f, 0.3f), t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, -1f, displayedMomentumValue);
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
        bool isVictory = battleManager.CurrentPhase == BattlePhase.Victory;
        if (victoryOverlay != null)
            victoryOverlay.SetActive(isVictory);

        if (!isVictory && gearDropText != null)
        {
            gearDropText.text = "";
        }

        if (isVictory && xpRewardText != null && HeroProgressionManager.Instance != null)
        {
            int totalXp = HeroProgressionManager.Instance.GetTotalXpFromEnemies(battleManager);
            if (totalXp > 0)
            {
                xpRewardText.text = $"+{totalXp} XP";
            }
            else
            {
                xpRewardText.text = "";
            }
        }

        if (defeatOverlay != null)
        {
            bool isDefeat = battleManager.CurrentPhase == BattlePhase.Defeat;
            bool isFled = battleManager.CurrentPhase == BattlePhase.Fled;
            defeatOverlay.SetActive(isDefeat || isFled);

            if (defeatOverlay.activeSelf)
            {
                Text defeatText = defeatOverlay.GetComponentInChildren<Text>();
                if (defeatText != null)
                {
                    if (isFled)
                    {
                        defeatText.text = "RETREATED";
                        defeatText.color = new Color(1f, 0.85f, 0.3f);
                    }
                    else
                    {
                        defeatText.text = "DEFEAT...";
                        defeatText.color = new Color(1f, 0.3f, 0.3f);
                    }
                }
            }
        }
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
        if (battleManager == null) return;
        CombatUnit currentInput = battleManager.GetCurrentInputUnit();
        if (currentInput == null) return;

        IReadOnlyList<SkillData> currentSkills = currentInput.Skills;
        if (currentSkills.Count == 0)
        {
            Debug.Log("[BattleHud] No skill available for current unit.");
            return;
        }

        int supportedSkillCount = 0;
        for (int i = 0; i < currentSkills.Count; i++)
        {
            SkillData candidate = currentSkills[i];
            if (battleManager.IsSkillSupportedForCurrentSlice(candidate))
            {
                supportedSkillCount++;
            }
        }

        if (supportedSkillCount <= 0)
        {
            Debug.Log("[BattleHud] No usable skill available for current unit.");
            return;
        }

        if (supportedSkillCount > 1)
        {
            ShowSkillSelectionPanel(currentInput);
            return;
        }

        selectedSkillData = battleManager.GetFirstSupportedSkillForCurrentSlice(currentInput);
        if (selectedSkillData == null)
        {
            Debug.Log("[BattleHud] No supported skill available for current unit.");
            return;
        }

        HandleSkillTargetType(selectedSkillData);
    }

    private void HandleSkillTargetType(SkillData skill)
    {
        if (battleManager == null || skill == null)
        {
            return;
        }

        battleManager.SetPendingSkill(skill);

        switch (skill.target)
        {
            case SkillTarget.SingleEnemy:
                ShowTargetSelection(CombatActionType.Skill);
                break;
            case SkillTarget.SingleAlly:
                ShowAllySelection(CombatActionType.Skill);
                break;
            default:
                // Self / AllAllies / AllEnemies resolve without a picked target.
                battleManager.TryAssignActionFromHud(CombatActionType.Skill, null);
                break;
        }
    }

    private void ShowSkillSelectionPanel(CombatUnit actor)
    {
        if (battleManager == null || skillPanel == null) return;

        targetPanelRequestedOpen = false;
        targetPanel.SetActive(false);
        foreach (Button btn in skillButtons) { Destroy(btn.gameObject); }
        skillButtons.Clear();

        IReadOnlyList<SkillData> actorSkills = actor.Skills;
        if (actorSkills.Count == 0)
        {
            skillPanelRequestedOpen = false;
            skillPanel.SetActive(false);
            return;
        }

        int supportedSkillCount = 0;
        for (int i = 0; i < actorSkills.Count; i++)
        {
            if (battleManager.IsSkillSupportedForCurrentSlice(actorSkills[i]))
            {
                supportedSkillCount++;
            }
        }

        if (supportedSkillCount <= 0)
        {
            skillPanelRequestedOpen = false;
            skillPanel.SetActive(false);
            return;
        }

        skillPanelRequestedOpen = true;
        skillPanel.SetActive(true);
        int visibleIndex = 0;
        for (int i = 0; i < actorSkills.Count; i++)
        {
            SkillData skill = actorSkills[i];
            if (!battleManager.IsSkillSupportedForCurrentSlice(skill)) continue;

            GameObject btnObj = new GameObject($"Skill_{i}", typeof(RectTransform));
            btnObj.transform.SetParent(skillPanel.transform, false);

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.55f - visibleIndex * 0.25f);
            rect.anchorMax = new Vector2(0.9f, 0.75f - visibleIndex * 0.25f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            bool canUse = actor.CanUseSkill(skill);
            img.color = canUse ? new Color(0.2f, 0.2f, 0.3f, 1f) : new Color(0.3f, 0.15f, 0.15f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = canUse;
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
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
            text.color = canUse ? new Color(0.9f, 0.9f, 1f) : new Color(0.6f, 0.4f, 0.4f);
            string mpLabel = canUse ? $"({skill.mpCost} MP)" : $"(No MP)";
            text.text = $"{skill.skillName} {mpLabel}";
            text.raycastTarget = false;

            SkillData capturedSkill = skill;
            btn.onClick.AddListener(() => OnSkillSelected(capturedSkill));
            skillButtons.Add(btn);
            visibleIndex++;
        }

        ConfigurePanelButtonNavigation(skillButtons);
        SelectFirstPanelButton(skillButtons);
    }

    private void OnSkillSelected(SkillData skill)
    {
        skillPanelRequestedOpen = false;
        skillPanel.SetActive(false);
        if (battleManager == null || skill == null) return;

        selectedSkillData = skill;
        HandleSkillTargetType(skill);
    }

    private void ShowTBSelectionPanel(CombatUnit actor)
    {
        if (battleManager == null || tideBreakPanel == null) return;
        
        // Hide target panel if open
        targetPanelRequestedOpen = false;
        targetPanel.SetActive(false);
        // Clear previous buttons
        foreach (Button btn in tideBreakButtons) { Destroy(btn.gameObject); }
        tideBreakButtons.Clear();
        
        IReadOnlyList<TideBreakData> abilities = actor.TideBreakAbilities;
        if (abilities == null || abilities.Count == 0)
        {
            Debug.LogWarning("[BattleHud] No TideBreak abilities available.");
            tideBreakPanelRequestedOpen = false;
            tideBreakPanel.SetActive(false);
            return;
        }

        List<TideBreakData> supportedAbilities = new List<TideBreakData>();
        for (int i = 0; i < abilities.Count; i++)
        {
            TideBreakData ability = abilities[i];
            if (battleManager.IsTideBreakSupportedForCurrentSlice(ability))
            {
                supportedAbilities.Add(ability);
            }
        }

        if (supportedAbilities.Count == 0)
        {
            Debug.LogWarning("[BattleHud] No usable TideBreak abilities available.");
            tideBreakPanelRequestedOpen = false;
            tideBreakPanel.SetActive(false);
            return;
        }

        tideBreakPanelRequestedOpen = true;
        tideBreakPanel.SetActive(true);
        for (int i = 0; i < supportedAbilities.Count; i++)
        {
            TideBreakData ability = supportedAbilities[i];
            GameObject btnObj = new GameObject($"TB_{i}", typeof(RectTransform));
            btnObj.transform.SetParent(tideBreakPanel.transform, false);
            
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.55f - i * 0.25f);
            rect.anchorMax = new Vector2(0.9f, 0.75f - i * 0.25f);
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
            text.color = new Color(0.9f, 0.9f, 1f);
            text.text = $"{ability.abilityName} ({ability.damageMultiplier}x)";
            text.raycastTarget = false;
            
            btn.onClick.AddListener(() => OnTideBreakSelected(ability));
            tideBreakButtons.Add(btn);
        }

        ConfigurePanelButtonNavigation(tideBreakButtons);
        SelectFirstPanelButton(tideBreakButtons);
    }

    private void OnTideBreakSelected(TideBreakData ability)
    {
        tideBreakPanelRequestedOpen = false;
        tideBreakPanel.SetActive(false);
        if (battleManager == null) return;

        battleManager.SetPendingTideBreak(ability);
        
        switch (ability.targetType)
        {
            case SkillTarget.SingleEnemy:
                ShowTargetSelection(CombatActionType.TideBreak);
                break;
            case SkillTarget.SingleAlly:
                ShowAllySelection(CombatActionType.TideBreak);
                break;
            default:
                // AllEnemies / AllAllies / Self resolve without a picked target.
                battleManager.TryAssignActionFromHud(CombatActionType.TideBreak, null);
                break;
        }
    }

    private void OnTideBreakClicked()
    {
        if (battleManager == null) return;

        CombatUnit currentInput = battleManager.GetCurrentInputUnit();
        if (currentInput == null) return;

        List<TideBreakData> supportedAbilities = new List<TideBreakData>();
        IReadOnlyList<TideBreakData> abilities = currentInput.TideBreakAbilities;
        if (abilities != null)
        {
            for (int i = 0; i < abilities.Count; i++)
            {
                TideBreakData ability = abilities[i];
                if (battleManager.IsTideBreakSupportedForCurrentSlice(ability))
                {
                    supportedAbilities.Add(ability);
                }
            }
        }

        if (supportedAbilities.Count > 1)
        {
            ShowTBSelectionPanel(currentInput);
        }
        else
        {
            TideBreakData autoSelect = supportedAbilities.Count == 1 ? supportedAbilities[0] : null;
            battleManager.SetPendingTideBreak(autoSelect);

            if (autoSelect != null && autoSelect.targetType == SkillTarget.SingleEnemy)
            {
                ShowTargetSelection(CombatActionType.TideBreak);
                return;
            }

            if (autoSelect != null && autoSelect.targetType == SkillTarget.SingleAlly)
            {
                ShowAllySelection(CombatActionType.TideBreak);
                return;
            }

            // AllEnemies / AllAllies / Self resolve without a picked target; with no custom
            // ability this falls back to the default player Tide Break (AllEnemies).
            battleManager.TryAssignActionFromHud(CombatActionType.TideBreak, null);
        }
    }

    private void ShowTargetSelection(CombatActionType actionType)
    {
        if (battleManager == null || targetPanel == null) return;

        tideBreakPanelRequestedOpen = false;
        if (tideBreakPanel != null) tideBreakPanel.SetActive(false);
        skillPanelRequestedOpen = false;
        if (skillPanel != null) skillPanel.SetActive(false);

        targetPanelRequestedOpen = true;
        targetPanel.SetActive(true);
        IReadOnlyList<CombatUnit> enemies = battleManager.GetAliveUnits(CombatUnit.UnitType.Enemy);

        Text title = targetPanel.GetComponentInChildren<Text>();
        if (title != null)
        {
            title.text = "Select Target";
            title.color = new Color(1f, 0.85f, 0.5f);
        }

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

        ConfigurePanelButtonNavigation(targetButtons);
        SelectFirstPanelButton(targetButtons);
    }

    private void ShowAllySelection(CombatActionType actionType)
    {
        if (battleManager == null || targetPanel == null) return;

        tideBreakPanelRequestedOpen = false;
        if (tideBreakPanel != null) tideBreakPanel.SetActive(false);
        skillPanelRequestedOpen = false;
        if (skillPanel != null) skillPanel.SetActive(false);

        targetPanelRequestedOpen = true;
        targetPanel.SetActive(true);
        IReadOnlyList<CombatUnit> allies = battleManager.GetAliveUnits(CombatUnit.UnitType.Ally);

        Text title = targetPanel.GetComponentInChildren<Text>();
        if (title != null)
        {
            title.text = "Select Ally";
            title.color = new Color(0.5f, 0.9f, 1f);
        }

        for (int i = 0; i < targetButtons.Count; i++)
        {
            if (i < allies.Count)
            {
                int index = i;
                targetButtons[i].gameObject.SetActive(true);
                targetButtons[i].GetComponentInChildren<Text>().text = allies[i].UnitName;
                targetButtons[i].onClick.RemoveAllListeners();
                targetButtons[i].onClick.AddListener(() => OnAllySelected(actionType, allies[index]));
            }
            else
            {
                targetButtons[i].gameObject.SetActive(false);
            }
        }

        ConfigurePanelButtonNavigation(targetButtons);
        SelectFirstPanelButton(targetButtons);
    }

    private void OnAllySelected(CombatActionType actionType, CombatUnit target)
    {
        targetPanelRequestedOpen = false;
        targetPanel.SetActive(false);
        if (battleManager == null) return;

        Text title = targetPanel.GetComponentInChildren<Text>();
        if (title != null)
        {
            title.text = "Select Target";
            title.color = new Color(1f, 0.85f, 0.5f);
        }

        battleManager.TryAssignActionFromHud(actionType, target);
    }

    private void OnTargetSelected(CombatActionType actionType, CombatUnit target)
    {
        targetPanelRequestedOpen = false;
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
        CreateSkillSelectionPanel(canvasObject.transform);
        CreateTBSelectionPanel(canvasObject.transform);
        CreateMomentumPanel(canvasObject.transform);
        CreateTurnLabel(canvasObject.transform);
        CreateVictoryOverlay(canvasObject.transform);
        CreateDefeatOverlay(canvasObject.transform);
        CreateClashOverlay(canvasObject.transform);
        CreateCritOverlay(canvasObject.transform);
        CreateTeamDynamicOverlay(canvasObject.transform);
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
        Debug.Log("[BattleHud] Created EventSystem for combat scene.");
    }

    private void ConfigureActionNavigation()
    {
        Button[] buttons = { attackButton, defendButton, skillButton, tideBreakButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnLeft = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnRight = buttons[(i + 1) % buttons.Length];
            navigation.selectOnUp = button;
            navigation.selectOnDown = button;
            button.navigation = navigation;
        }
    }

    /// <summary>
    /// Sets explicit navigation for buttons that are created or activated at
    /// runtime. Unity's automatic navigation can skip inactive entries and
    /// leave the EventSystem focused on a hidden action button, which makes a
    /// keyboard Return press resolve the wrong action.
    /// </summary>
    private void ConfigurePanelButtonNavigation(IList<Button> buttons)
    {
        if (buttons == null)
        {
            return;
        }

        List<Button> activeButtons = new List<Button>();
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button != null && button.gameObject.activeInHierarchy)
            {
                activeButtons.Add(button);
            }
        }

        for (int i = 0; i < activeButtons.Count; i++)
        {
            Button button = activeButtons[i];
            Button previous = i > 0 ? activeButtons[i - 1] : button;
            Button next = i + 1 < activeButtons.Count ? activeButtons[i + 1] : button;

            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = previous;
            navigation.selectOnDown = next;
            navigation.selectOnLeft = previous;
            navigation.selectOnRight = next;
            button.navigation = navigation;
        }
    }

    private void SelectFirstPanelButton(IList<Button> buttons)
    {
        if (buttons == null)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button == null || !button.gameObject.activeInHierarchy || !button.interactable)
            {
                continue;
            }

            eventSystem.SetSelectedGameObject(button.gameObject);
            button.Select();
            return;
        }

        eventSystem.SetSelectedGameObject(null);
    }

    private void SelectDefaultActionButton(bool force = false)
    {
        if (isMenuOpen
            || targetPanelRequestedOpen
            || skillPanelRequestedOpen
            || tideBreakPanelRequestedOpen
            || actionPanel == null
            || !actionPanel.activeInHierarchy
            || attackButton == null
            || !attackButton.interactable)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            eventSystem = FindFirstObjectByType<EventSystem>();
        }

        if (eventSystem == null)
        {
            return;
        }

        GameObject selected = eventSystem.currentSelectedGameObject;
        if (!force && selected != null && selected.activeInHierarchy)
        {
            return;
        }

        eventSystem.SetSelectedGameObject(attackButton.gameObject);
        attackButton.Select();
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

        ConfigureActionNavigation();

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

    private void CreateSkillSelectionPanel(Transform parent)
    {
        skillPanel = CreatePanel("SkillSelectionPanel", parent,
            new Vector2(0.32f, 0.14f), new Vector2(0.68f, 0.42f),
            new Color(0.08f, 0.12f, 0.08f, 0.95f));
        skillPanel.SetActive(false);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(skillPanel.transform, false);
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
        title.color = new Color(0.5f, 1f, 0.6f);
        title.text = "Select Skill";
        title.raycastTarget = false;
    }

    private void CreateTBSelectionPanel(Transform parent)
    {
        tideBreakPanel = CreatePanel("TBSelectionPanel", parent,
            new Vector2(0.32f, 0.14f), new Vector2(0.68f, 0.42f),
            new Color(0.08f, 0.08f, 0.15f, 0.95f));
        tideBreakPanel.SetActive(false);

        GameObject titleObj = new GameObject("Title", typeof(RectTransform));
        titleObj.transform.SetParent(tideBreakPanel.transform, false);
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
        title.color = new Color(0.6f, 0.9f, 1f);
        title.text = "Select TideBreak Ability";
        title.raycastTarget = false;
        // Buttons will be created dynamically when needed
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
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        momentumFill = fillObj.AddComponent<Image>();
        momentumFill.type = Image.Type.Filled;
        momentumFill.fillMethod = Image.FillMethod.Horizontal;
        momentumFill.fillAmount = 0.5f;
        momentumFill.color = new Color(0.2f, 0.5f, 1f);
        momentumFill.raycastTarget = false;

        // Center line for tug-of-war baseline
        GameObject centerObj = new GameObject("CenterLine", typeof(RectTransform));
        centerObj.transform.SetParent(bgObj.transform, false);
        RectTransform centerRect = centerObj.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0f);
        centerRect.anchorMax = new Vector2(0.5f, 1f);
        centerRect.sizeDelta = new Vector2(2f, 0f);
        centerRect.anchoredPosition = Vector2.zero;
        Image centerImage = centerObj.AddComponent<Image>();
        centerImage.color = new Color(1f, 1f, 1f, 0.35f);
        centerImage.raycastTarget = false;

        // Momentum indicator pin
        GameObject markerObj = new GameObject("MomentumPin", typeof(RectTransform));
        markerObj.transform.SetParent(bgObj.transform, false);
        momentumIndicator = markerObj.GetComponent<RectTransform>();
        momentumIndicator.anchorMin = new Vector2(0.5f, 0f);
        momentumIndicator.anchorMax = new Vector2(0.5f, 1f);
        momentumIndicator.sizeDelta = new Vector2(4f, 0f);
        momentumIndicator.anchoredPosition = Vector2.zero;
        Image markerImage = markerObj.AddComponent<Image>();
        markerImage.color = new Color(1f, 0.95f, 0.7f, 0.9f);
        markerImage.raycastTarget = false;

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
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -18f);
        rect.sizeDelta = new Vector2(360f, 36f);

        turnLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        turnLabel.fontSize = 24;
        turnLabel.fontStyle = FontStyle.Bold;
        turnLabel.alignment = TextAnchor.MiddleLeft;
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

        GameObject xpObj = new GameObject("XpRewardText", typeof(RectTransform));
        xpObj.transform.SetParent(victoryOverlay.transform, false);
        xpRewardText = xpObj.AddComponent<Text>();
        RectTransform xpRect = xpRewardText.rectTransform;
        xpRect.anchorMin = new Vector2(0.2f, 0.2f);
        xpRect.anchorMax = new Vector2(0.8f, 0.35f);
        xpRect.offsetMin = Vector2.zero;
        xpRect.offsetMax = Vector2.zero;
        xpRewardText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        xpRewardText.fontSize = 28;
        xpRewardText.fontStyle = FontStyle.Bold;
        xpRewardText.alignment = TextAnchor.MiddleCenter;
        xpRewardText.color = new Color(1f, 0.9f, 0.4f);
        xpRewardText.text = "";
        xpRewardText.raycastTarget = false;

        GameObject gearDropObj = new GameObject("GearDropText", typeof(RectTransform));
        gearDropObj.transform.SetParent(victoryOverlay.transform, false);
        gearDropText = gearDropObj.AddComponent<Text>();
        RectTransform gearDropRect = gearDropText.rectTransform;
        gearDropRect.anchorMin = new Vector2(0.2f, 0.05f);
        gearDropRect.anchorMax = new Vector2(0.8f, 0.18f);
        gearDropRect.offsetMin = Vector2.zero;
        gearDropRect.offsetMax = Vector2.zero;
        gearDropText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        gearDropText.fontSize = 24;
        gearDropText.fontStyle = FontStyle.Bold;
        gearDropText.alignment = TextAnchor.MiddleCenter;
        gearDropText.color = new Color(0.6f, 0.9f, 1f);
        gearDropText.text = "";
        gearDropText.raycastTarget = false;

        victoryOverlay.SetActive(false);
    }

    /// <summary>Shows a freshly awarded gear drop on the victory overlay (battle results exposure).</summary>
    public void ShowGearDrop(string message)
    {
        if (gearDropText == null)
        {
            return;
        }

        gearDropText.text = string.IsNullOrEmpty(message) ? string.Empty : $"Gear acquired: {message}";
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

    private void CreateCritOverlay(Transform parent)
    {
        critOverlay = new GameObject("CritOverlay", typeof(RectTransform));
        critOverlay.transform.SetParent(parent, false);

        RectTransform rootRect = critOverlay.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.3f, 0.52f);
        rootRect.anchorMax = new Vector2(0.7f, 0.6f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image bgImg = critOverlay.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;

        GameObject labelObj = new GameObject("CritLabel", typeof(RectTransform));
        labelObj.transform.SetParent(critOverlay.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        critLabel = labelObj.AddComponent<Text>();
        critLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        critLabel.fontSize = 40;
        critLabel.fontStyle = FontStyle.Bold;
        critLabel.alignment = TextAnchor.MiddleCenter;
        critLabel.color = new Color(1f, 0.85f, 0.2f);
        critLabel.text = "";
        critLabel.raycastTarget = false;

        critOverlay.SetActive(false);
    }

    private void CreateTeamDynamicOverlay(Transform parent)
    {
        teamDynamicOverlay = new GameObject("TeamDynamicOverlay", typeof(RectTransform));
        teamDynamicOverlay.transform.SetParent(parent, false);

        RectTransform rootRect = teamDynamicOverlay.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.15f, 0.88f);
        rootRect.anchorMax = new Vector2(0.85f, 0.97f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image bgImg = teamDynamicOverlay.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        bgImg.raycastTarget = false;

        GameObject labelObj = new GameObject("TeamDynamicLabel", typeof(RectTransform));
        labelObj.transform.SetParent(teamDynamicOverlay.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);
        teamDynamicLabel = labelObj.AddComponent<Text>();
        teamDynamicLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        teamDynamicLabel.fontSize = 18;
        teamDynamicLabel.fontStyle = FontStyle.Bold;
        teamDynamicLabel.alignment = TextAnchor.MiddleCenter;
        teamDynamicLabel.color = new Color(0.8f, 0.9f, 1f);
        teamDynamicLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        teamDynamicLabel.verticalOverflow = VerticalWrapMode.Overflow;
        teamDynamicLabel.raycastTarget = false;
        teamDynamicLabel.text = "";

        teamDynamicOverlay.SetActive(false);
    }

    /// <summary>
    /// Displays the team dynamic description overlay at the top of the battle HUD.
    /// Shows for a few seconds at battle start.
    /// </summary>
    public void ShowTeamDynamicDescription(string description)
    {
        if (teamDynamicOverlay == null || teamDynamicLabel == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(description))
        {
            return;
        }

        teamDynamicLabel.text = description;
        teamDynamicOverlay.SetActive(true);
        teamDynamicDisplayTimer = 4.0f;
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
