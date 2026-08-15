using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum BattlePhase
{
    StartBattle,
    PlayerInput,
    ActionExecution,
    EndTurn,
    Victory,
    Defeat,
    Fled
}

public enum CombatActionType
{
    Attack,
    Defend,
    Skill,
    TideBreak,
    Swap,
    Pass
}

public struct PlannedAction
{
    public CombatActionType ActionType;
    public CombatUnit Actor;
    public CombatUnit Target;
    public SkillData SelectedSkill;
    public TideBreakData SelectedTideBreak;

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
        SelectedSkill = null;
        SelectedTideBreak = null;
    }

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target, SkillData skill)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
        SelectedSkill = skill;
        SelectedTideBreak = null;
    }

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target, TideBreakData tideBreak)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
        SelectedSkill = null;
        SelectedTideBreak = tideBreak;
    }
}

[DisallowMultipleComponent]
public class BattleManager : MonoBehaviour
{
    [Header("Debug Flow")]
    [SerializeField] private bool autoAdvancePhases = true;
    [NonSerialized] public bool autoStartBattle = true;
    [SerializeField] private float autoAdvanceDelay = 1.25f;
    [SerializeField] private KeyCode advancePhaseKey = KeyCode.N;
    [SerializeField] private KeyCode victoryKey = KeyCode.V;
    [SerializeField] private KeyCode defeatKey = KeyCode.X;

    [Header("Debug UI")]
    [SerializeField] private Vector2 debugLabelPosition = new Vector2(24f, -24f);

    [Header("Turn Flow")]
    [SerializeField] private float actionStepDelay = 0.55f;

    [Header("Battle Visual Feedback")]
    [SerializeField] private bool enableActionAnimations = true;
    [SerializeField] private float lungeDistance = 0.45f;
    [SerializeField] private float lungeDuration = 0.1f;
    [SerializeField] private float hitShakeDuration = 0.14f;
    [SerializeField] private float hitShakeMagnitude = 0.06f;
    [SerializeField] private float hitEffectDuration = 0.28f;
    [SerializeField] private float hitEffectScale = 1f;

    [Header("Flee")]
    [SerializeField] [Range(0f, 1f)] private float fleeBaseChance = 0.35f;
    [SerializeField] private float fleeSpeedDifferenceMultiplier = 0.03f;
    [SerializeField] [Range(0f, 1f)] private float fleeFailureBonusPerAttempt = 0.15f;
    [SerializeField] [Range(0f, 1f)] private float fleeMinimumChance = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float fleeMaximumChance = 0.95f;

    [Header("Neutral Clash QTE")]
    [SerializeField] private bool enableNeutralClashQte = true;
    [SerializeField] private bool allowNeutralClashQteFallbackWhenRuntimeMissing = true;

    [SerializeField] private BattlePhase currentPhase;

    public BattlePhase CurrentPhase => currentPhase;

    private List<CombatUnit> allyUnits = new List<CombatUnit>();
    private List<CombatUnit> enemyUnits = new List<CombatUnit>();
    private List<CombatUnit> allyReserveUnits = new List<CombatUnit>();
    private List<CombatUnit> turnQueue = new List<CombatUnit>();
    private Dictionary<CombatUnit, int> unitRegistrationOrder = new Dictionary<CombatUnit, int>();
    private Dictionary<CombatUnit, PlannedAction> selectedPlayerActions = new Dictionary<CombatUnit, PlannedAction>();
    private Dictionary<CombatUnit, PlannedAction> enemyPlannedActions = new Dictionary<CombatUnit, PlannedAction>();
    private List<CombatUnit> playerInputUnits = new List<CombatUnit>();
    private Dictionary<Transform, Coroutine> activeHitFeedbackCoroutines = new Dictionary<Transform, Coroutine>();
    private MomentumState momentumState = new MomentumState();

    public IReadOnlyList<CombatUnit> AllyUnits => allyUnits;
    public IReadOnlyList<CombatUnit> EnemyUnits => enemyUnits;
    public IReadOnlyList<CombatUnit> AllyReserveUnits => allyReserveUnits;
    public IReadOnlyList<CombatUnit> TurnQueue => turnQueue;
    public MomentumState Momentum => momentumState;

    public struct ClashResult
    {
        public CombatUnit UnitA;
        public CombatUnit UnitB;
        public bool HasWinner;
        public CombatUnit Winner;
        public CombatUnit Loser;
        public string Description;
        public bool NeutralQteTriggered;
        public bool NeutralQteSuccess;
        public string NeutralQteResolution;
    }

    public event Action<ClashResult> OnClashResolved;
    public event Action<CombatUnit, bool> OnDamageDealt;
    public event Func<CombatUnit, CombatUnit, bool?> OnNeutralClashQteRequested;

    public CombatUnit GetEnemyTarget(CombatUnit enemy)
    {
        if (enemy == null) return null;
        if (enemyPlannedActions.TryGetValue(enemy, out PlannedAction action))
        {
            return action.Target;
        }
        return null;
    }

    public IReadOnlyList<CombatUnit> GetAllUnits()
    {
        List<CombatUnit> all = new List<CombatUnit>(allyUnits);
        all.AddRange(enemyUnits);
        return all;
    }

    public IReadOnlyList<CombatUnit> GetAliveUnits(CombatUnit.UnitType unitType)
    {
        List<CombatUnit> units = unitType == CombatUnit.UnitType.Ally ? allyUnits : enemyUnits;
        return units.Where(u => u != null && u.IsAlive).ToList();
    }

    public IReadOnlyList<CombatUnit> GetAllyReserveUnits()
    {
        return allyReserveUnits;
    }

    public void SwapUnits(CombatUnit activeUnit, CombatUnit reserveUnit)
    {
        if (!allowInBattlePartySwap)
        {
            Debug.LogWarning("[BattleManager] In-battle party swapping is disabled by design.");
            return;
        }

        if (activeUnit == null || reserveUnit == null)
        {
            Debug.LogWarning("[BattleManager] SwapUnits called with null unit.");
            return;
        }
        if (!allyUnits.Contains(activeUnit))
        {
            Debug.LogWarning($"[BattleManager] {activeUnit.UnitName} is not in active ally list.");
            return;
        }
        if (!allyReserveUnits.Contains(reserveUnit))
        {
            Debug.LogWarning($"[BattleManager] {reserveUnit.UnitName} is not in reserve ally list.");
            return;
        }
        if (!activeUnit.IsAlive)
        {
            Debug.LogWarning($"[BattleManager] Cannot swap out dead unit {activeUnit.UnitName}.");
            return;
        }
        if (!reserveUnit.IsAlive)
        {
            Debug.LogWarning($"[BattleManager] Cannot swap in dead unit {reserveUnit.UnitName}.");
            return;
        }

        // Swap lists
        allyUnits.Remove(activeUnit);
        allyReserveUnits.Remove(reserveUnit);
        allyUnits.Add(reserveUnit);
        allyReserveUnits.Add(activeUnit);

        // Swap active states
        activeUnit.gameObject.SetActive(false);
        reserveUnit.gameObject.SetActive(true);

        Debug.Log($"[BattleManager] Swapped {activeUnit.UnitName} out with {reserveUnit.UnitName}.");
    }

    public void SetAllyReserveUnits(List<CombatUnit> reserves)
    {
        allyReserveUnits = reserves ?? new List<CombatUnit>();
        Debug.Log($"[BattleManager] Set {allyReserveUnits.Count} reserve units.");
    }

    public void RegisterUnit(CombatUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (!unitRegistrationOrder.ContainsKey(unit))
        {
            unitRegistrationOrder[unit] = nextRegistrationOrder;
            nextRegistrationOrder++;
        }

        if (unit.Type == CombatUnit.UnitType.Ally)
        {
            if (!allyUnits.Contains(unit))
            {
                allyUnits.Add(unit);
                Debug.Log($"[BattleManager] Registered ally unit: {unit.UnitName} (Total: {allyUnits.Count})");
            }
        }
        else
        {
            if (!enemyUnits.Contains(unit))
            {
                enemyUnits.Add(unit);
                Debug.Log($"[BattleManager] Registered enemy unit: {unit.UnitName} (Total: {enemyUnits.Count})");
            }
        }
    }

    private const string DebugCanvasName = "BattleDebugCanvas";
    private const string DebugLabelName = "BattlePhaseLabel";

    private bool hasActivePhase;
    private float phaseTimer;
    private int nextRegistrationOrder;
    private int turnQueueIndex;
    private int playerInputIndex;
    private bool isAwaitingTargetSelection;
    private CombatUnit currentActingUnit;
    private CombatActionType pendingInputActionType = CombatActionType.Attack;
    private SkillData pendingSkillData;
    private TideBreakData pendingTideBreak;
    private float actionStepTimer;
    private bool actionExecutionActive;
    private BattleHud cachedBattleHud;
    private string debugText = "";
    [Header("Party Swap")]
    [SerializeField] private bool allowInBattlePartySwap;
    [SerializeField] private int swapsPerTurn = 1;
    private int swapsRemainingPerTurn;
    private bool canSwapDuringPlayerInput = true;
    private int failedFleeAttemptsThisBattle;

    // Relationship combat effects (computed at battle start from bond levels)
    private float relationshipDamageMultiplier = 1f;
    private float relationshipDefenseMultiplier = 1f;
    private float relationshipHealingMultiplier = 1f;
    private float relationshipTideBreakMultiplier = 1f;
    private float relationshipClashBonus = 0f;
    private float relationshipTeamUpChance = 0f;
    private float averageBondLevel = 50f;

    [Header("Balance")]
    [SerializeField] private BalanceConfig balanceConfig;
    [SerializeField] private DifficultyModeService difficultyService;

    [Header("Envy Mirror")]
    [SerializeField] private bool isBossEncounter;
    public bool EnableEnvyMirror;
    private SkillData lastPlayerSkill;
    private CombatUnit lastAttacker;
    private HashSet<CombatUnit> envyCovetActors = new HashSet<CombatUnit>();
    private Dictionary<CombatUnit, CombatUnit.Element> originalEnemyElements = new Dictionary<CombatUnit, CombatUnit.Element>();
    private bool currentActorShouldSkip;

    [Header("Vice AI")]
    [SerializeField] private ViceAIProfile[] viceProfiles;
    private ViceAIProfile activeViceProfile;

    public void ConfigureEnvyContext(bool enableMirror, bool boss)
    {
        EnableEnvyMirror = enableMirror;
        isBossEncounter = boss;
    }

    /// <summary>
    /// Looks up the ViceAIProfile for the given vice type from the assigned
    /// viceProfiles array and sets it as the active profile for this battle.
    /// Call this during battle setup, before the first turn begins.
    /// </summary>
    public void ConfigureViceAI(ViceType viceType)
    {
        activeViceProfile = null;

        if (viceProfiles == null)
        {
            return;
        }

        for (int i = 0; i < viceProfiles.Length; i++)
        {
            if (viceProfiles[i] != null && viceProfiles[i].vice == viceType)
            {
                activeViceProfile = viceProfiles[i];
                Debug.Log($"[BattleManager] Active vice AI set to {viceType}: {activeViceProfile.gimmickDescription}");
                return;
            }
        }

        Debug.LogWarning($"[BattleManager] No ViceAIProfile found for {viceType}.");
    }

    private void Awake()
    {
        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
#if UNITY_EDITOR
        string phaseName = hasActivePhase ? currentPhase.ToString() : "Waiting";
        int alliesAlive = CountAliveUnits(allyUnits);
        int enemiesAlive = CountAliveUnits(enemyUnits);

        debugText = $"Phase: {phaseName}\n";
        debugText += $"Allies: {alliesAlive} | Enemies: {enemiesAlive}\n";

        if (currentPhase == BattlePhase.PlayerInput && currentActingUnit != null)
        {
            debugText += $"Unit: {currentActingUnit.UnitName}\n";
            debugText += $"Action: {pendingInputActionType}";
        }
#endif
    }

    private void Start()
    {
        if (autoStartBattle)
        {
            StartBattle();
        }
    }

    private void Update()
    {
        HandleDebugInput();
        HandleActionExecution();
        HandleAutoAdvance();
        UpdateDebugText();
    }

    public void StartBattle()
    {
        canSwapDuringPlayerInput = true;
        failedFleeAttemptsThisBattle = 0;
        TransitionToPhase(BattlePhase.StartBattle, "StartBattle");
    }

    /// <summary>
    /// Computes all relationship-based combat multipliers from hero bond levels
    /// and stores them for use during this battle.
    /// </summary>
    private void ComputeRelationshipEffects()
    {
        IReadOnlyList<CombatUnit> allies = allyUnits;
        relationshipDamageMultiplier = RelationshipCombatEffects.GetTeamDamageMultiplier(allies);
        relationshipDefenseMultiplier = RelationshipCombatEffects.GetTeamDefenseMultiplier(allies);
        relationshipHealingMultiplier = RelationshipCombatEffects.GetTeamHealingMultiplier(allies);
        relationshipTideBreakMultiplier = RelationshipCombatEffects.GetTideBreakMultiplier(allies);
        relationshipClashBonus = RelationshipCombatEffects.GetClashBonus(allies);
        relationshipTeamUpChance = RelationshipCombatEffects.GetTeamUpChance(allies);

        // Store average bond for logging
        DialogueSystem dialogue = DialogueSystem.Instance;
        if (dialogue != null && allies.Count >= 2)
        {
            int totalBond = 0;
            int pairCount = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                CombatUnit a = allies[i];
                if (a == null || !a.IsAlive) continue;
                for (int j = i + 1; j < allies.Count; j++)
                {
                    CombatUnit b = allies[j];
                    if (b == null || !b.IsAlive) continue;
                    totalBond += dialogue.GetBondLevel(a.UnitName, b.UnitName);
                    pairCount++;
                }
            }
            averageBondLevel = pairCount > 0 ? (float)totalBond / pairCount : 50f;
        }
        else
        {
            averageBondLevel = 50f;
        }

        Debug.Log($"[BattleManager] Relationship effects: DMG x{relationshipDamageMultiplier:F2}, " +
            $"DEF x{relationshipDefenseMultiplier:F2}, Heal x{relationshipHealingMultiplier:F2}, " +
            $"TideBreak x{relationshipTideBreakMultiplier:F2}, Clash +{relationshipClashBonus:F2}, " +
            $"TeamUp {relationshipTeamUpChance:F2} (avg bond {averageBondLevel:F0})");
    }

    public void AdvancePhase()
    {
        if (!hasActivePhase)
        {
            StartBattle();
            return;
        }

        if (IsTerminalPhase(currentPhase))
        {
            Debug.Log($"[BattleManager] AdvancePhase ignored because battle is already in terminal phase {currentPhase}.", this);
            return;
        }

        TransitionToPhase(GetNextPhase(currentPhase), "AdvancePhase");
    }

    public void SetVictory()
    {
        TransitionToPhase(BattlePhase.Victory, "SetVictory");
    }

    public void SetDefeat()
    {
        TransitionToPhase(BattlePhase.Defeat, "SetDefeat");
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(victoryKey))
        {
            SetVictory();
            return;
        }

        if (Input.GetKeyDown(defeatKey))
        {
            SetDefeat();
            return;
        }

        if (Input.GetKeyDown(advancePhaseKey))
        {
            AdvancePhase();
        }
#endif
    }

    private void HandleAutoAdvance()
    {
        if (!autoAdvancePhases || !hasActivePhase || IsTerminalPhase(currentPhase) || !CanAutoAdvancePhase(currentPhase))
        {
            return;
        }

        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
        {
            return;
        }

        AdvancePhase();
    }

    private void TransitionToPhase(BattlePhase nextPhase, string transitionSource)
    {
        string transitionDescription = hasActivePhase
            ? $"{currentPhase} -> {nextPhase}"
            : $"<initial> -> {nextPhase}";

        currentPhase = nextPhase;
        hasActivePhase = true;
        phaseTimer = autoAdvanceDelay;

        Debug.Log($"[BattleManager] Phase transition {transitionDescription} via {transitionSource}.", this);
        OnPhaseEntered(nextPhase);
        UpdateDebugText();
    }

    private void OnPhaseEntered(BattlePhase phase)
    {
        switch (phase)
        {
            case BattlePhase.StartBattle:
                ResetTurnRegistration();
                BuildTurnQueueFromLivingUnits();
                selectedPlayerActions.Clear();
                momentumState.Reset();
                clashedUnits.Clear();
                lastAttacker = null;
                lastPlayerSkill = null;
                envyCovetActors.Clear();
                originalEnemyElements.Clear();
                foreach (CombatUnit enemy in enemyUnits)
                {
                    if (enemy != null)
                    {
                        originalEnemyElements[enemy] = enemy.ElementType;
                    }
                }

                // Compute relationship combat effects from hero bond levels
                ComputeRelationshipEffects();

                // Show team dynamic description in the HUD
                if (cachedBattleHud == null)
                {
                    cachedBattleHud = FindFirstObjectByType<BattleHud>();
                }
                if (cachedBattleHud != null)
                {
                    cachedBattleHud.ShowTeamDynamicDescription(
                        RelationshipCombatEffects.GetTeamDynamicDescription(allyUnits));
                }
                break;
            case BattlePhase.PlayerInput:
                // Reset enemy elements to originals before computing new actions (for Envy Mirror)
                if (EnableEnvyMirror)
                {
                    foreach (CombatUnit enemy in enemyUnits)
                    {
                        if (enemy != null && originalEnemyElements.TryGetValue(enemy, out CombatUnit.Element original))
                        {
                            enemy.ElementType = original;
                        }
                    }
                }
                BeginPlayerInputPhase();
                break;
            case BattlePhase.ActionExecution:
                BeginActionExecutionPhase();
                break;
            case BattlePhase.EndTurn:
                BeginEndTurnPhase();
                break;
            case BattlePhase.Victory:
                PlayVictorySting();
                NotifyCombatEnded(true);
                break;
            case BattlePhase.Defeat:
                PlayDefeatSting();
                NotifyCombatEnded(false);
                break;
            case BattlePhase.Fled:
                NotifyCombatEnded(false, true);
                break;
        }
    }

    private static void PlayVictorySting()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleCombatVictory();
        }
    }

    private static void PlayDefeatSting()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleCombatDefeat();
        }
    }

    private static void NotifyCombatEnded(bool playerWon, bool playerFled = false)
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnCombatEnded(playerWon, playerFled);
        }
    }

    private bool CanAutoAdvancePhase(BattlePhase phase)
    {
        return phase == BattlePhase.StartBattle || phase == BattlePhase.EndTurn;
    }

    private void BuildTurnQueueFromLivingUnits()
    {
        List<CombatUnit> allLiving = allyUnits
            .Concat(enemyUnits)
            .Where(unit => unit != null && unit.IsAlive)
            .ToList();

        // Cache effective speeds to avoid O(N log N) re-computation inside the
        // sort comparers and to keep registration-order side effects out of
        // the sort path. The sort key is the cached value, not the live read.
        Dictionary<CombatUnit, int> effectiveSpeeds = new Dictionary<CombatUnit, int>(allLiving.Count);
        for (int i = 0; i < allLiving.Count; i++)
        {
            effectiveSpeeds[allLiving[i]] = allLiving[i].GetEffectiveSpeed();
        }

        turnQueue = allLiving
            .OrderByDescending(unit => effectiveSpeeds[unit])
            .ThenBy(unit => GetRegistrationOrder(unit))
            .ToList();

        turnQueueIndex = 0;
        currentActingUnit = null;

        Debug.Log($"[BattleManager] Turn queue rebuilt with {turnQueue.Count} living units.", this);
        for (int i = 0; i < turnQueue.Count; i++)
        {
            CombatUnit queueUnit = turnQueue[i];
            Debug.Log($"[BattleManager] Queue {i + 1}: {queueUnit.UnitName} (SPD {effectiveSpeeds[queueUnit]})", this);
        }
    }

    private void ResetTurnRegistration()
    {
        unitRegistrationOrder.Clear();
        nextRegistrationOrder = 0;
    }

    private bool TryGetNextActingUnit(out CombatUnit actor)
    {
        actor = null;
        while (turnQueueIndex < turnQueue.Count)
        {
            CombatUnit candidate = turnQueue[turnQueueIndex];
            turnQueueIndex++;

            if (candidate == null || !candidate.IsAlive)
            {
                continue;
            }
            // SkipTurnThisRound can be set by a party swap. Consume that flag
            // here so it cannot carry into a later round.
            bool skipForRound = candidate.SkipTurnThisRound;
            candidate.SkipTurnThisRound = false;

            candidate.ProcessTurnStartEffects();
            if (!candidate.IsAlive)
            {
                continue;
            }

            currentActorShouldSkip = skipForRound || candidate.ShouldSkipTurn();
            candidate.ClearDefend();
            actor = candidate;
            currentActingUnit = candidate;
            return true;
        }

        currentActingUnit = null;
        return false;
    }

    private void BeginPlayerInputPhase()
    {
        playerInputUnits = GetAliveUnits(CombatUnit.UnitType.Ally).ToList();
        playerInputIndex = 0;
        isAwaitingTargetSelection = false;
        pendingInputActionType = CombatActionType.Attack;
        pendingSkillData = null;
        pendingTideBreak = null;
        swapsRemainingPerTurn = swapsPerTurn;

        // Clear skip turn flags for all ally units
        foreach (CombatUnit ally in allyUnits)
        {
            if (ally != null) ally.SkipTurnThisRound = false;
        }

        // Clear envy covet state from previous turn
        envyCovetActors.Clear();

        CacheEnemyActions();

        if (playerInputUnits.Count == 0)
        {
            SetDefeat();
            return;
        }

        Debug.Log($"[BattleManager] Player input started for {playerInputUnits.Count} allies.", this);
    }

    public bool TryAttemptFleeFromMenu(out bool fledSuccessfully, out float fleeChance, out float fleeRoll)
    {
        fledSuccessfully = false;
        fleeChance = 0f;
        fleeRoll = 0f;

        if (!hasActivePhase || currentPhase != BattlePhase.PlayerInput || IsTerminalPhase(currentPhase))
        {
            return false;
        }

        if (difficultyService != null && !difficultyService.AllowsFleeInCombat())
        {
            Debug.LogWarning("[BattleManager] Fleeing is not allowed on this difficulty.");
            return false;
        }

        if (CheckBattleOutcome())
        {
            return false;
        }

        CombatUnit actor = GetCurrentPlayerInputUnit();
        if (actor == null || !actor.IsAlive)
        {
            return false;
        }

        fleeChance = CalculateFleeSuccessChance(actor);
        fleeRoll = UnityEngine.Random.value;
        fledSuccessfully = fleeRoll <= fleeChance;

        if (fledSuccessfully)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} fled successfully ({fleeRoll * 100f:F1}% <= {fleeChance * 100f:F1}%).");
            selectedPlayerActions.Clear();
            enemyPlannedActions.Clear();
            actionExecutionActive = false;
            isAwaitingTargetSelection = false;
            pendingSkillData = null;
            pendingTideBreak = null;
            currentActingUnit = null;

            TransitionToPhase(BattlePhase.Fled, "FleeSuccess");
            return true;
        }

        failedFleeAttemptsThisBattle++;
        Debug.LogWarning($"[BattleManager] {actor.UnitName} failed to flee ({fleeRoll * 100f:F1}% > {fleeChance * 100f:F1}%). Action consumed.");

        AssignPlayerAction(actor, CombatActionType.Pass, null);
        TryAutoConfirmPlayerActions();
        return true;
    }

    private float CalculateFleeSuccessChance(CombatUnit actor)
    {
        if (actor == null)
        {
            return 0f;
        }

        float minChance = Mathf.Clamp01(Mathf.Min(fleeMinimumChance, fleeMaximumChance));
        float maxChance = Mathf.Clamp01(Mathf.Max(fleeMinimumChance, fleeMaximumChance));
        float baseChance = Mathf.Clamp01(fleeBaseChance);
        float failureBonus = Mathf.Max(0f, fleeFailureBonusPerAttempt);
        float speedDelta = actor.GetEffectiveSpeed() - GetAverageLivingEnemySpeed();

        float chance =
            baseChance
            + speedDelta * fleeSpeedDifferenceMultiplier
            + failedFleeAttemptsThisBattle * failureBonus;

        return Mathf.Clamp(chance, minChance, maxChance);
    }

    private float GetAverageLivingEnemySpeed()
    {
        IReadOnlyList<CombatUnit> livingEnemies = GetAliveUnits(CombatUnit.UnitType.Enemy);
        if (livingEnemies.Count == 0)
        {
            return 0f;
        }

        float totalSpeed = 0f;
        for (int i = 0; i < livingEnemies.Count; i++)
        {
            totalSpeed += livingEnemies[i].GetEffectiveSpeed();
        }

        return totalSpeed / livingEnemies.Count;
    }

    private void RefreshPlayerInputUnits()
    {
        playerInputUnits = GetAliveUnits(CombatUnit.UnitType.Ally).ToList();
        // Remove units that have already been assigned actions
        playerInputUnits.RemoveAll(u => selectedPlayerActions.ContainsKey(u));
        // Remove units that are flagged to skip this round
        playerInputUnits.RemoveAll(u => u.SkipTurnThisRound);
        playerInputIndex = 0;
    }

    private void CacheEnemyActions()
    {
        enemyPlannedActions.Clear();
        IReadOnlyList<CombatUnit> enemies = GetAliveUnits(CombatUnit.UnitType.Enemy);
        for (int i = 0; i < enemies.Count; i++)
        {
            CombatUnit enemy = enemies[i];
            enemyPlannedActions[enemy] = ComputeEnemyAction(enemy);
        }
    }

    private PlannedAction ComputeEnemyAction(CombatUnit actor)
    {
        // TideBreak has absolute priority regardless of vice profile
        if (momentumState.IsEnemyTideBreakReady)
        {
            CombatUnit tbTarget = GetRandomLivingOpponent(actor);
            if (tbTarget != null)
            {
                TideBreakData tbData = GetTideBreakForActor(actor);
                bool requiresTarget = TideBreakRequiresExplicitTarget(tbData, actor);
                return new PlannedAction(CombatActionType.TideBreak, actor, requiresTarget ? tbTarget : null, tbData);
            }
        }

        ViceAIProfile profile = activeViceProfile;

        // Vice-driven: defend when HP is low
        float hpRatio = actor.MaxHP > 0 ? (float)actor.HP / actor.MaxHP : 1f;
        if (hpRatio < 0.3f && profile != null && UnityEngine.Random.value < profile.defendLowHpWeight)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} defends due to low HP ({hpRatio:P0}) and vice profile.");
            return new PlannedAction(CombatActionType.Defend, actor, null);
        }

        // Select target using profile-aware logic
        CombatUnit target = SelectTargetWithProfile(actor, profile);
        if (target == null)
        {
            return new PlannedAction(CombatActionType.Attack, actor, null);
        }

        SkillData advantageousSkill = SelectAdvantageousSkillForActor(actor);
        if (advantageousSkill != null && actor.CanUseSkill(advantageousSkill))
        {
            float skillChance = profile != null ? Mathf.Max(profile.skillUsageWeight, 0.70f) : 1f;
            if (UnityEngine.Random.value < skillChance)
            {
                Debug.Log($"[BattleManager] {actor.UnitName} picks element-advantageous skill {advantageousSkill.skillName}.");
                return new PlannedAction(CombatActionType.Skill, actor, ResolveSkillTargetForAi(actor, advantageousSkill), advantageousSkill);
            }
        }

        // Skill usage: always use when no profile; weighted by skillUsageWeight when profile is set
        SkillData skill = GetFirstSupportedSkillForCurrentSlice(actor);
        if (skill != null && actor.CanUseSkill(skill))
        {
            float skillChance = profile != null ? profile.skillUsageWeight : 1f;
            if (UnityEngine.Random.value < skillChance)
            {
                return new PlannedAction(CombatActionType.Skill, actor, ResolveSkillTargetForAi(actor, skill), skill);
            }
        }

        // Vice-driven: may choose to defend instead of attacking
        float attackChance = profile != null ? profile.aggressionWeight : 1f;
        if (UnityEngine.Random.value >= attackChance)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} defends due to low aggression weight ({attackChance}).");
            return new PlannedAction(CombatActionType.Defend, actor, null);
        }

        // Envy Mirror (existing mechanic, unaffected by vice profile)
        if (EnableEnvyMirror && lastAttacker != null)
        {
            actor.ElementType = lastAttacker.ElementType;
            Debug.Log($"[BattleManager] Envy Mirror: {actor.UnitName} copies element {lastAttacker.ElementType} from {lastAttacker.UnitName}.");

            if (isBossEncounter && lastPlayerSkill != null && actor.CanUseSkill(lastPlayerSkill))
            {
                Debug.Log($"[BattleManager] Envy Covet: {actor.UnitName} reuses {lastPlayerSkill.skillName} at 0.7x damage.");
                envyCovetActors.Add(actor);
                return new PlannedAction(CombatActionType.Skill, actor, ResolveSkillTargetForAi(actor, lastPlayerSkill), lastPlayerSkill);
            }
        }

        return new PlannedAction(CombatActionType.Attack, actor, target);
    }

    private CombatUnit ResolveSkillTargetForAi(CombatUnit actor, SkillData skill)
    {
        if (skill == null)
        {
            return null;
        }

        switch (skill.target)
        {
            case SkillTarget.SingleAlly:
                return GetLowestHpAlly(actor);
            case SkillTarget.SingleEnemy:
                return SelectTargetWithProfile(actor, activeViceProfile);
            default:
                // Self, AllAllies, and AllEnemies do not need an explicit target.
                return null;
        }
    }

    private SkillData SelectAdvantageousSkillForActor(CombatUnit actor)
    {
        if (actor == null || actor.Skills == null || actor.Skills.Count == 0)
        {
            return null;
        }

        CombatUnit.Element dominantPlayerElement = ResolveDominantPlayerElement();
        if (dominantPlayerElement == CombatUnit.Element.None)
        {
            return null;
        }

        List<SkillData> advantageous = new List<SkillData>();
        List<SkillData> neutral = new List<SkillData>();
        for (int i = 0; i < actor.Skills.Count; i++)
        {
            SkillData candidate = actor.Skills[i];
            if (candidate == null || !IsSkillSupportedForCurrentSlice(candidate))
            {
                continue;
            }

            CombatUnit.Element skillElement = ResolveSkillElement(candidate, actor);
            MatchupResult result = ElementMatchup.GetResult(skillElement, dominantPlayerElement);
            if (result == MatchupResult.Strong)
            {
                advantageous.Add(candidate);
            }
            else if (result == MatchupResult.Neutral)
            {
                neutral.Add(candidate);
            }
        }

        if (advantageous.Count > 0)
        {
            int pickIndex = UnityEngine.Random.Range(0, advantageous.Count);
            return advantageous[pickIndex];
        }

        if (isBossEncounter && neutral.Count > 0)
        {
            int pickIndex = UnityEngine.Random.Range(0, neutral.Count);
            return neutral[pickIndex];
        }

        return null;
    }

    private CombatUnit.Element ResolveDominantPlayerElement()
    {
        Dictionary<CombatUnit.Element, int> tally = new Dictionary<CombatUnit.Element, int>();
        List<CombatUnit> allies = allyUnits != null ? new List<CombatUnit>(allyUnits) : new List<CombatUnit>();
        for (int i = 0; i < allies.Count; i++)
        {
            CombatUnit ally = allies[i];
            if (ally == null || !ally.IsAlive)
            {
                continue;
            }

            CombatUnit.Element element = ally.ElementType;
            if (element == CombatUnit.Element.None)
            {
                continue;
            }

            tally.TryGetValue(element, out int count);
            tally[element] = count + 1;
        }

        CombatUnit.Element dominant = CombatUnit.Element.None;
        int dominantCount = 0;
        foreach (KeyValuePair<CombatUnit.Element, int> pair in tally)
        {
            if (pair.Value > dominantCount)
            {
                dominant = pair.Key;
                dominantCount = pair.Value;
            }
        }

        return dominant;
    }

    private static CombatUnit.Element ResolveSkillElement(SkillData skill, CombatUnit actor)
    {
        if (skill != null && skill.element != CombatUnit.Element.None)
        {
            return skill.element;
        }

        return actor != null ? actor.ElementType : CombatUnit.Element.None;
    }

    private TideBreakData GetTideBreakForActor(CombatUnit actor)
    {
        if (actor.TideBreakAbilities != null && actor.TideBreakAbilities.Count > 0)
        {
            List<TideBreakData> supported = new List<TideBreakData>();
            for (int i = 0; i < actor.TideBreakAbilities.Count; i++)
            {
                TideBreakData candidate = actor.TideBreakAbilities[i];
                if (IsTideBreakSupportedForCurrentSlice(candidate))
                {
                    supported.Add(candidate);
                }
            }

            if (supported.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, supported.Count);
                return supported[index];
            }

            Debug.LogWarning($"[BattleManager] {actor.UnitName} has no usable Tide Break data. Falling back to the default Tide Break.");
        }

        return null;
    }

    private void BeginActionExecutionPhase()
    {
        canSwapDuringPlayerInput = false;

        if (CheckBattleOutcome())
        {
            return;
        }

        ResolveClashes();

        actionExecutionActive = true;
        actionStepTimer = 0f;
        currentActingUnit = null;

        Debug.Log("[BattleManager] Action execution started.", this);
    }

    private HashSet<CombatUnit> clashedUnits = new HashSet<CombatUnit>();

    private void ResolveClashes()
    {
        clashedUnits.Clear();

        List<CombatUnit> allUnits = new List<CombatUnit>(allyUnits);
        allUnits.AddRange(enemyUnits);

        for (int i = 0; i < allUnits.Count; i++)
        {
            CombatUnit unitA = allUnits[i];
            if (unitA == null || !unitA.IsAlive)
            {
                continue;
            }

            if (clashedUnits.Contains(unitA))
            {
                continue;
            }

            PlannedAction actionA = GetPlannedAction(unitA);
            if (!IsClashableAction(actionA.ActionType))
            {
                continue;
            }

            for (int j = i + 1; j < allUnits.Count; j++)
            {
                CombatUnit unitB = allUnits[j];
                if (unitB == null || !unitB.IsAlive)
                {
                    continue;
                }

                if (clashedUnits.Contains(unitB))
                {
                    continue;
                }

                PlannedAction actionB = GetPlannedAction(unitB);
                if (!IsClashableAction(actionB.ActionType))
                {
                    continue;
                }

                if (actionA.Target == null || actionB.Target == null)
                {
                    continue;
                }

                bool mutualTarget = (actionA.Target == unitB && actionB.Target == unitA);
                if (!mutualTarget)
                {
                    continue;
                }

                MatchupResult matchupA = ElementMatchup.GetResult(unitA.ElementType, unitB.ElementType);
                MatchupResult matchupB = ElementMatchup.GetResult(unitB.ElementType, unitA.ElementType);

                Debug.Log($"[BattleManager] *** CLASH! {unitA.UnitName} vs {unitB.UnitName}! ***", this);

                if (matchupA == MatchupResult.Strong)
                {
                    ExecuteClash(unitA, unitB);
                }
                else if (matchupB == MatchupResult.Strong)
                {
                    ExecuteClash(unitB, unitA);
                }
                else
                {
                    ExecuteNeutralClash(unitA, unitB);
                }

                clashedUnits.Add(unitA);
                clashedUnits.Add(unitB);
                break;
            }
        }
    }

    private static bool IsClashableAction(CombatActionType actionType)
    {
        return actionType == CombatActionType.Attack
            || actionType == CombatActionType.Skill
            || actionType == CombatActionType.TideBreak;
    }

    private void ExecuteClash(
        CombatUnit winner,
        CombatUnit loser,
        bool neutralQteTriggered = false,
        bool neutralQteSuccess = false,
        string neutralQteResolution = "")
    {
        if (winner == null || loser == null)
        {
            Debug.LogWarning("[BattleManager] Clash resolution skipped due to missing winner/loser.", this);
            return;
        }

        string clashReason = neutralQteTriggered ? "neutral QTE" : "element advantage";
        Debug.Log($"[BattleManager] {winner.UnitName} wins the clash! ({clashReason})", this);

        // Apply relationship clash bonus: allies deal more damage, enemies deal less
        float winnerClashMod = winner.Type == CombatUnit.UnitType.Ally
            ? 1f + Mathf.Max(0f, relationshipClashBonus)
            : 1f - Mathf.Max(0f, relationshipClashBonus);
        float loserClashMod = loser.Type == CombatUnit.UnitType.Ally
            ? 1f + Mathf.Max(0f, relationshipClashBonus)
            : 1f - Mathf.Max(0f, relationshipClashBonus);

        int winnerDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(winner.Attack * GameConstants.ClashWinnerMultiplier * winnerClashMod));
        int loserDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(loser.Attack * GameConstants.ClashLoserMultiplier * loserClashMod));

        int loserHpBefore = loser.HP;
        loser.TakeDamage(winnerDmg);
        Debug.Log($"  -> {winner.UnitName} deals {winnerDmg} to {loser.UnitName}. HP {loserHpBefore} -> {loser.HP}", this);

        int winnerHpBefore = winner.HP;
        winner.TakeDamage(loserDmg);
        Debug.Log($"  -> {loser.UnitName} deals {loserDmg} to {winner.UnitName}. HP {winnerHpBefore} -> {winner.HP}", this);

        momentumState.ShiftForAction(winner, MatchupResult.Strong);

        OnClashResolved?.Invoke(new ClashResult
        {
            UnitA = winner,
            UnitB = loser,
            HasWinner = true,
            Winner = winner,
            Loser = loser,
            Description = neutralQteTriggered
                ? (neutralQteSuccess ? $"QTE SUCCESS! {winner.UnitName} WINS!" : $"QTE FAIL! {winner.UnitName} WINS!")
                : $"{winner.UnitName} WINS!",
            NeutralQteTriggered = neutralQteTriggered,
            NeutralQteSuccess = neutralQteSuccess,
            NeutralQteResolution = neutralQteResolution
        });
    }

    private void ExecuteNeutralClash(CombatUnit unitA, CombatUnit unitB)
    {
        if (unitA == null || unitB == null || !unitA.IsAlive || !unitB.IsAlive)
        {
            Debug.LogWarning("[BattleManager] Neutral clash skipped due to invalid participants.", this);
            OnClashResolved?.Invoke(new ClashResult
            {
                UnitA = unitA,
                UnitB = unitB,
                HasWinner = false,
                Winner = null,
                Loser = null,
                Description = "NEUTRAL CLASH",
                NeutralQteTriggered = false,
                NeutralQteSuccess = false,
                NeutralQteResolution = "InvalidParticipants"
            });
            return;
        }

        string qteResolution = "NotTriggered";
        if (TryResolveNeutralClashQte(
            unitA,
            unitB,
            out CombatUnit qteWinner,
            out CombatUnit qteLoser,
            out bool qteSuccess,
            out qteResolution))
        {
            ExecuteClash(qteWinner, qteLoser, true, qteSuccess, qteResolution);
            return;
        }

        if (!string.Equals(qteResolution, "NotTriggered", StringComparison.Ordinal))
        {
            Debug.Log($"[BattleManager] Neutral clash QTE skipped: {qteResolution}.", this);
        }

        Debug.Log($"[BattleManager] Clash is neutral. Both deal reduced damage.", this);

        // Apply relationship clash bonus to neutral clash damage
        float clashModA = unitA.Type == CombatUnit.UnitType.Ally
            ? 1f + Mathf.Max(0f, relationshipClashBonus)
            : 1f - Mathf.Max(0f, relationshipClashBonus);
        float clashModB = unitB.Type == CombatUnit.UnitType.Ally
            ? 1f + Mathf.Max(0f, relationshipClashBonus)
            : 1f - Mathf.Max(0f, relationshipClashBonus);

        int dmgA = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(unitA.Attack * GameConstants.ClashNeutralMultiplier * clashModA));
        int dmgB = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(unitB.Attack * GameConstants.ClashNeutralMultiplier * clashModB));

        int hpBBefore = unitB.HP;
        unitB.TakeDamage(dmgA);
        Debug.Log($"  -> {unitA.UnitName} deals {dmgA} to {unitB.UnitName}. HP {hpBBefore} -> {unitB.HP}", this);

        int hpABefore = unitA.HP;
        unitA.TakeDamage(dmgB);
        Debug.Log($"  -> {unitB.UnitName} deals {dmgB} to {unitA.UnitName}. HP {hpABefore} -> {unitA.HP}", this);

        OnClashResolved?.Invoke(new ClashResult
        {
            UnitA = unitA,
            UnitB = unitB,
            HasWinner = false,
            Winner = null,
            Loser = null,
            Description = "NEUTRAL CLASH",
            NeutralQteTriggered = false,
            NeutralQteSuccess = false,
            NeutralQteResolution = qteResolution
        });
    }

    private bool TryResolveNeutralClashQte(
        CombatUnit unitA,
        CombatUnit unitB,
        out CombatUnit winner,
        out CombatUnit loser,
        out bool qteSuccess,
        out string qteResolution)
    {
        winner = null;
        loser = null;
        qteSuccess = false;
        qteResolution = "NotTriggered";

        if (!enableNeutralClashQte)
        {
            qteResolution = "Disabled";
            return false;
        }

        if (!TryGetNeutralClashParticipants(unitA, unitB, out CombatUnit ally, out CombatUnit enemy))
        {
            qteResolution = "Ineligible";
            return false;
        }

        bool? runtimeResult = RequestNeutralClashQteResult(ally, enemy);
        if (runtimeResult.HasValue)
        {
            qteSuccess = runtimeResult.Value;
            qteResolution = "Runtime";
        }
        else
        {
            if (!allowNeutralClashQteFallbackWhenRuntimeMissing)
            {
                Debug.LogWarning("[BattleManager] Neutral clash QTE requested but runtime was unavailable. Falling back to neutral clash.", this);
                qteResolution = "RuntimeUnavailable";
                return false;
            }

            qteSuccess = ResolveNeutralClashQteFallback(ally, enemy);
            qteResolution = "Fallback";
        }

        winner = qteSuccess ? ally : enemy;
        loser = qteSuccess ? enemy : ally;

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            if (qteSuccess)
            {
                audioManager.HandleQTESuccess();
            }
            else
            {
                audioManager.HandleQTEFail();
            }
        }

        string outcome = qteSuccess ? "SUCCESS (momentum toward player)" : "FAIL (momentum toward enemy)";
        Debug.Log($"[BattleManager] Neutral clash QTE resolved via {qteResolution}: {outcome}.", this);
        return true;
    }

    private bool? RequestNeutralClashQteResult(CombatUnit ally, CombatUnit enemy)
    {
        if (ally == null || enemy == null)
        {
            return null;
        }

        if (OnNeutralClashQteRequested == null)
        {
            return null;
        }

        Delegate[] callbacks = OnNeutralClashQteRequested.GetInvocationList();
        for (int i = 0; i < callbacks.Length; i++)
        {
            Func<CombatUnit, CombatUnit, bool?> callback = callbacks[i] as Func<CombatUnit, CombatUnit, bool?>;
            if (callback == null)
            {
                continue;
            }

            bool? result = callback.Invoke(ally, enemy);
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return null;
    }

    private bool ResolveNeutralClashQteFallback(CombatUnit ally, CombatUnit enemy)
    {
        if (ally == null || enemy == null)
        {
            return false;
        }

        // Apply relationship clash bonus to speed comparison
        float allyEffectiveSpeed = ally.Speed + relationshipClashBonus * 10f;
        float enemyEffectiveSpeed = enemy.Speed - relationshipClashBonus * 10f;

        if (allyEffectiveSpeed != enemyEffectiveSpeed)
        {
            return allyEffectiveSpeed > enemyEffectiveSpeed;
        }

        return GetRegistrationOrder(ally) <= GetRegistrationOrder(enemy);
    }

    private static bool TryGetNeutralClashParticipants(
        CombatUnit unitA,
        CombatUnit unitB,
        out CombatUnit ally,
        out CombatUnit enemy)
    {
        ally = null;
        enemy = null;

        if (unitA == null || unitB == null || !unitA.IsAlive || !unitB.IsAlive)
        {
            return false;
        }

        if (unitA.Type == CombatUnit.UnitType.Ally && unitB.Type == CombatUnit.UnitType.Enemy)
        {
            ally = unitA;
            enemy = unitB;
        }
        else if (unitB.Type == CombatUnit.UnitType.Ally && unitA.Type == CombatUnit.UnitType.Enemy)
        {
            ally = unitB;
            enemy = unitA;
        }
        else
        {
            return false;
        }

        if (ally.ElementType == CombatUnit.Element.None || enemy.ElementType == CombatUnit.Element.None)
        {
            return false;
        }

        if (ElementMatchup.GetResult(ally.ElementType, enemy.ElementType) != MatchupResult.Neutral)
        {
            return false;
        }

        if (ElementMatchup.GetResult(enemy.ElementType, ally.ElementType) != MatchupResult.Neutral)
        {
            return false;
        }

        return true;
    }

    private void BeginEndTurnPhase()
    {
        selectedPlayerActions.Clear();

        // Clear skip-turn flags for all living units so stun/drowsy only lasts one round
        foreach (CombatUnit unit in turnQueue)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.SkipTurnThisRound = false;
            }
        }

        BuildTurnQueueFromLivingUnits();
        CheckBattleOutcome();
    }

    private void HandleActionExecution()
    {
        if (!hasActivePhase || currentPhase != BattlePhase.ActionExecution || !actionExecutionActive)
        {
            return;
        }

        actionStepTimer -= Time.deltaTime;
        if (actionStepTimer > 0f)
        {
            return;
        }

        if (!TryGetNextActingUnit(out CombatUnit actor))
        {
            actionExecutionActive = false;
            TransitionToPhase(BattlePhase.EndTurn, "ActionExecutionComplete");
            return;
        }

        ResolveActionForActor(actor);
        actionStepTimer = actionStepDelay;
    }

    private void ResolveActionForActor(CombatUnit actor)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        if (currentActorShouldSkip)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} is drowsy and skips their turn.");
            return;
        }

        if (clashedUnits.Contains(actor))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} already resolved via clash.", this);
            return;
        }

        PlannedAction plannedAction = GetPlannedAction(actor);
        if (plannedAction.ActionType == CombatActionType.Attack)
        {
            ResolveAttack(actor, plannedAction.Target);
        }
        else if (plannedAction.ActionType == CombatActionType.Skill)
        {
            ResolveSkill(actor, plannedAction.Target, plannedAction.SelectedSkill);
        }
        else if (plannedAction.ActionType == CombatActionType.TideBreak)
        {
            ResolveTideBreak(actor, plannedAction.Target, plannedAction.SelectedTideBreak);
        }
        else if (plannedAction.ActionType == CombatActionType.Defend)
        {
            actor.StartDefend();
        }
        else if (plannedAction.ActionType == CombatActionType.Pass)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} passes after a failed flee attempt.", this);
        }
        else
        {
            Debug.Log($"[BattleManager] {actor.UnitName} passes.", this);
        }

        CheckBattleOutcome();
        UpdateDebugText();

        if (actor.Type == CombatUnit.UnitType.Enemy)
        {
            enemyPlannedActions.Remove(actor);
        }
    }

    private PlannedAction GetPlannedAction(CombatUnit actor)
    {
        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            if (selectedPlayerActions.TryGetValue(actor, out PlannedAction plannedAction))
            {
                return plannedAction;
            }

            return new PlannedAction(CombatActionType.Attack, actor, GetRandomLivingOpponent(actor));
        }

        if (enemyPlannedActions.TryGetValue(actor, out PlannedAction cachedAction))
        {
            return cachedAction;
        }

        return ComputeEnemyAction(actor);
    }

    private void ResolveAttack(CombatUnit actor, CombatUnit requestedTarget)
    {
        CombatUnit target = ResolveOpponentTarget(actor, requestedTarget);

        if (!IsValidTarget(actor, target))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} has no valid target and passes.", this);
            return;
        }

        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            lastAttacker = actor;
            lastPlayerSkill = null;
        }

        float attackMod = actor.GetAttackModifier();
        int baseDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + attackMod)));
        MatchupResult matchupForMult = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        float multiplier = balanceConfig != null
            ? balanceConfig.GetElementMultiplier(matchupForMult)
            : ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);

        // Apply relationship damage multiplier (only for ally attacks)
        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            multiplier *= relationshipDamageMultiplier;
        }
        float variance = UnityEngine.Random.Range(0.8f, 1.2f);
        float modifiedDamageFloat = baseDamage * multiplier * variance;
        
        bool isCrit = UnityEngine.Random.value < actor.CritRate;
        if (isCrit)
        {
            modifiedDamageFloat *= actor.CritDamage;
            Debug.Log($"[BattleManager] CRITICAL HIT! {actor.UnitName} crits {target.UnitName}!");
        }
        
        int modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageFloat));

        if (difficultyService != null)
        {
            float diffMult = actor.Type == CombatUnit.UnitType.Ally
                ? difficultyService.GetDamageMultiplierForPlayer()
                : difficultyService.GetDamageMultiplierForEnemy();
            modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamage * diffMult));
        }

        MatchupResult matchup = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        int hpBefore = target.HP;

        // Apply relationship defense multiplier (reduces damage when enemies hit allies)
        int finalDamage = modifiedDamage;
        if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
        {
            finalDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamage / relationshipDefenseMultiplier));
        }
        target.TakeDamage(finalDamage);

        int hpAfter = target.HP;

        string matchupFeedback = "";
        switch (matchup)
        {
            case MatchupResult.Strong:
                matchupFeedback = " It's super effective!";
                break;
            case MatchupResult.Weak:
                matchupFeedback = " Not very effective...";
                break;
        }

        momentumState.ShiftForAction(actor, matchup);
        OnDamageDealt?.Invoke(actor, isCrit);
        TriggerBattleHitFeedback(actor, target, isCrit, false);

        Debug.Log(
            $"[BattleManager] {actor.UnitName} attacks {target.UnitName} for {finalDamage} (base {baseDamage} x{multiplier:F2}). HP {hpBefore} -> {hpAfter}.{matchupFeedback}",
            this);

        if (actor.Type == CombatUnit.UnitType.Ally && target.IsAlive)
        {
            float teamUpRoll = UnityEngine.Random.value;
            if (teamUpRoll <= relationshipTeamUpChance)
            {
                CombatUnit partner = FindTeamUpPartner(actor);
                if (partner != null && partner.IsAlive)
                {
                    float partnerMod = partner.GetAttackModifier();
                    int partnerBase = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(partner.Attack * (1f + partnerMod)));
                    MatchupResult partnerMatchup = ElementMatchup.GetResult(partner.ElementType, target.ElementType);
                    float partnerMult = balanceConfig != null
                        ? balanceConfig.GetElementMultiplier(partnerMatchup)
                        : ElementMatchup.GetDamageMultiplier(partner.ElementType, target.ElementType);
                    partnerMult *= relationshipDamageMultiplier;
                    float partnerVariance = UnityEngine.Random.Range(0.8f, 1.2f);
                    int partnerDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(partnerBase * partnerMult * 0.7f * partnerVariance));

                    if (difficultyService != null)
                    {
                        partnerDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(partnerDmg * difficultyService.GetDamageMultiplierForPlayer()));
                    }

                    int partnerHpBefore = target.HP;
                    target.TakeDamage(partnerDmg);
                    TriggerBattleHitFeedback(partner, target, false, false);
                    Debug.Log($"[BattleManager] TEAM-UP! {partner.UnitName} joins {actor.UnitName}'s attack on {target.UnitName} for {partnerDmg}! HP {partnerHpBefore} -> {target.HP}", this);
                }
            }
        }
    }

    private void ResolveSkill(CombatUnit actor, CombatUnit requestedTarget, SkillData skill)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        if (skill == null)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} has no skill selected. Attacking instead.", this);
            ResolveAttack(actor, requestedTarget);
            return;
        }

        if (!actor.CanUseSkill(skill))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} lacks MP for {skill.skillName}. Attacking instead.", this);
            ResolveAttack(actor, requestedTarget);
            return;
        }

        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            lastAttacker = actor;
            lastPlayerSkill = skill;
        }

        if (skill.target == SkillTarget.SingleAlly || skill.target == SkillTarget.AllAllies)
        {
            bool heals = skill.healMultiplier > 0f;
            bool appliesEffect = skill.appliedEffectType != StatusEffectType.None;

            if (!heals && !appliesEffect)
            {
                Debug.LogWarning($"[BattleManager] {skill.skillName} targets {skill.target} but has no heal or status effect configured. Resolving as a no-op.", this);
                return;
            }
        }

        actor.SpendMp(skill.mpCost);

        if (skill.target == SkillTarget.Self)
        {
            ResolveSelfSkill(actor, skill);
            return;
        }

        if (skill.target == SkillTarget.SingleAlly || skill.target == SkillTarget.AllAllies)
        {
            ResolveAllyTargetedSkill(actor, requestedTarget, skill);
            return;
        }

        if (skill.target == SkillTarget.AllEnemies)
        {
            CombatUnit.UnitType enemyType = actor.Type == CombatUnit.UnitType.Ally
                ? CombatUnit.UnitType.Enemy
                : CombatUnit.UnitType.Ally;
            IReadOnlyList<CombatUnit> aoeTargets = GetAliveUnits(enemyType);

            if (actor.Type == CombatUnit.UnitType.Ally)
            {
                lastAttacker = actor;
                lastPlayerSkill = skill;
            }

            float aoeAttackMod = actor.GetAttackModifier();
            int baseDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + aoeAttackMod)));
            float relDmgMult = actor.Type == CombatUnit.UnitType.Ally ? relationshipDamageMultiplier : 1f;
            int totalDmg = 0;

            for (int i = 0; i < aoeTargets.Count; i++)
            {
                CombatUnit aoeTarget = aoeTargets[i];
                float elemMult = ElementMatchup.GetDamageMultiplier(actor.ElementType, aoeTarget.ElementType);
                if (balanceConfig != null)
                {
                    elemMult = balanceConfig.GetElementMultiplier(ElementMatchup.GetResult(actor.ElementType, aoeTarget.ElementType));
                }
                float skillMult = elemMult * skill.damageMultiplier * relDmgMult;
                float variance = UnityEngine.Random.Range(0.8f, 1.2f);
                float dmgFloat = baseDmg * skillMult * variance;
                int dmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(dmgFloat));

                if (difficultyService != null)
                {
                    float diffMult = actor.Type == CombatUnit.UnitType.Ally
                        ? difficultyService.GetDamageMultiplierForPlayer()
                        : difficultyService.GetDamageMultiplierForEnemy();
                    dmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(dmg * diffMult));
                }

                if (aoeTarget.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
                {
                    dmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(dmg / relationshipDefenseMultiplier));
                }

                int hpBefore = aoeTarget.HP;
                aoeTarget.TakeDamage(dmg);
                totalDmg += (hpBefore - aoeTarget.HP);
                TriggerBattleHitFeedback(actor, aoeTarget, false, true);
            }

            MatchupResult aoeMatchup = ElementMatchup.GetResult(actor.ElementType, aoeTargets.Count > 0 ? aoeTargets[0].ElementType : CombatUnit.Element.None);
            momentumState.ShiftForAction(actor, aoeMatchup);
            OnDamageDealt?.Invoke(actor, false);

            if (skill.appliedEffectType != StatusEffectType.None)
            {
                for (int j = 0; j < aoeTargets.Count; j++)
                {
                    TryApplySkillStatusEffect(actor, aoeTargets[j], skill);
                }
            }

            Debug.Log($"[BattleManager] {actor.UnitName} uses AoE {skill.skillName} on {aoeTargets.Count} targets for {totalDmg} total (-{skill.mpCost} MP).", this);
            return;
        }

        CombatUnit target = ResolveOpponentTarget(actor, requestedTarget);

        if (!IsValidTarget(actor, target))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} but has no valid target.", this);
            return;
        }

        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            lastAttacker = actor;
            lastPlayerSkill = skill;
        }

        float attackMod = actor.GetAttackModifier();
        int baseDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + attackMod)));
        MatchupResult matchupForSkillMult = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        float multiplier = balanceConfig != null
            ? balanceConfig.GetElementMultiplier(matchupForSkillMult)
            : ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        float skillMultiplierSingle = multiplier * skill.damageMultiplier;

        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            skillMultiplierSingle *= relationshipDamageMultiplier;
        }

        float varianceSingle = UnityEngine.Random.Range(0.8f, 1.2f);
        float modifiedDamageFloatSingle = baseDamageSingle * skillMultiplierSingle * varianceSingle;

        if (envyCovetActors.Remove(actor))
        {
            modifiedDamageFloatSingle *= 0.7f;
        }
        
        bool isCritSingle = UnityEngine.Random.value < actor.CritRate;
        if (isCritSingle)
        {
            modifiedDamageFloatSingle *= actor.CritDamage;
            Debug.Log($"[BattleManager] CRITICAL HIT! {actor.UnitName} crits {target.UnitName} with {skill.skillName}!");
        }
        
        int modifiedDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageFloatSingle));

        if (difficultyService != null)
        {
            float diffMult = actor.Type == CombatUnit.UnitType.Ally
                ? difficultyService.GetDamageMultiplierForPlayer()
                : difficultyService.GetDamageMultiplierForEnemy();
            modifiedDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageSingle * diffMult));
        }

        MatchupResult matchupSingle = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        int hpBeforeSingle = target.HP;

        int finalDamageSingle = modifiedDamageSingle;
        if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
        {
            finalDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageSingle / relationshipDefenseMultiplier));
        }
        target.TakeDamage(finalDamageSingle);

        int hpAfterSingle = target.HP;
        int actualDamageSingle = Mathf.Max(0, hpBeforeSingle - hpAfterSingle);

        if (skill.restoreCasterPercentOfDamage > 0f && actualDamageSingle > 0)
        {
            float healMultiplier = actor.Type == CombatUnit.UnitType.Ally ? relationshipHealingMultiplier : 1f;
            int restoredAmount = Mathf.Max(1, Mathf.RoundToInt(actualDamageSingle * skill.restoreCasterPercentOfDamage * healMultiplier));
            actor.Heal(restoredAmount);

            AudioManager audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.HandleHeal();
            }
        }

        string matchupFeedbackSingle = "";
        switch (matchupSingle)
        {
            case MatchupResult.Strong:
                matchupFeedbackSingle = " Super effective!";
                break;
            case MatchupResult.Weak:
                matchupFeedbackSingle = " Not very effective...";
                break;
        }

        momentumState.ShiftForAction(actor, matchupSingle);
        TryApplySkillStatusEffect(actor, target, skill);
        OnDamageDealt?.Invoke(actor, isCritSingle);
        TriggerBattleHitFeedback(actor, target, isCritSingle, true);

        if (skill.currencyStealAmount > 0 && actor.Type == CombatUnit.UnitType.Enemy && HeroProgressionManager.Instance != null)
        {
            if (HeroProgressionManager.Instance.TrySpendCurrency(skill.currencyStealAmount))
            {
                Debug.Log($"[BattleManager] Greed steal: {actor.UnitName} drained {skill.currencyStealAmount} currency via {skill.skillName}.");
            }
        }

        Debug.Log(
            $"[BattleManager] {actor.UnitName} uses {skill.skillName} on {target.UnitName} for {actualDamageSingle} (rolled {modifiedDamageSingle}, base {baseDamageSingle} x{skillMultiplierSingle:F2}, -{skill.mpCost} MP). HP {hpBeforeSingle} -> {hpAfterSingle}.{matchupFeedbackSingle}",
            this);
    }

    private void ResolveSelfSkill(CombatUnit actor, SkillData skill)
    {
        if (skill.healMultiplier > 0f)
        {
            float selfHealMod = actor.GetAttackModifier();
            int baseSelfHeal = Mathf.Max(1, Mathf.RoundToInt(actor.Attack * (1f + selfHealMod)));
            float selfVariance = UnityEngine.Random.Range(0.9f, 1.1f);
            float selfRelMod = actor.Type == CombatUnit.UnitType.Ally ? relationshipHealingMultiplier : 1f;
            int selfHealAmount = Mathf.Max(1, Mathf.RoundToInt(baseSelfHeal * skill.healMultiplier * selfVariance * selfRelMod));
            if (selfHealAmount > 0)
            {
                int hpBefore = actor.HP;
                actor.Heal(selfHealAmount);
                momentumState.ShiftForHeal(actor);
                Debug.Log($"[BattleManager] {actor.UnitName} heals self for {selfHealAmount} via {skill.skillName}. HP {hpBefore} -> {actor.HP}.");

                AudioManager selfAudio = AudioManager.Instance;
                if (selfAudio != null)
                {
                    selfAudio.HandleHeal();
                }
            }
        }

        TryApplySkillStatusEffect(actor, actor, skill);
    }

    private void ResolveAllyTargetedSkill(CombatUnit actor, CombatUnit requestedTarget, SkillData skill)
    {
        bool heals = skill.healMultiplier > 0f;
        bool appliesEffect = skill.appliedEffectType != StatusEffectType.None;

        if (!heals && !appliesEffect)
        {
            Debug.LogWarning($"[BattleManager] {skill.skillName} targets {skill.target} but has no heal or status effect configured. Resolving as a no-op.", this);
            return;
        }

        int healAmount = heals ? ComputeAllyHealAmount(actor, skill) : 0;

        if (skill.target == SkillTarget.SingleAlly)
        {
            CombatUnit allyTarget = requestedTarget != null
                && requestedTarget.IsAlive
                && requestedTarget.Type == actor.Type
                    ? requestedTarget
                    : actor;

            if (heals)
            {
                int hpBefore = allyTarget.HP;
                allyTarget.Heal(healAmount);
                momentumState.ShiftForHeal(actor);
                Debug.Log($"[BattleManager] {actor.UnitName} heals {allyTarget.UnitName} for {healAmount} via {skill.skillName}. HP {hpBefore} -> {allyTarget.HP}.");
                PlayHealAudio();
            }

            if (appliesEffect)
            {
                TryApplySkillStatusEffect(actor, allyTarget, skill);
            }
            return;
        }

        IReadOnlyList<CombatUnit> allies = GetAliveUnits(actor.Type);
        for (int i = 0; i < allies.Count; i++)
        {
            CombatUnit ally = allies[i];
            if (heals)
            {
                int hpBefore = ally.HP;
                ally.Heal(healAmount);
                Debug.Log($"[BattleManager] {actor.UnitName} heals {ally.UnitName} for {healAmount} via {skill.skillName}. HP {hpBefore} -> {ally.HP}.");
            }

            if (appliesEffect)
            {
                TryApplySkillStatusEffect(actor, ally, skill);
            }
        }

        if (heals && allies.Count > 0)
        {
            momentumState.ShiftForHeal(actor);
            PlayHealAudio();
        }
    }

    private int ComputeAllyHealAmount(CombatUnit actor, SkillData skill)
    {
        float healMod = actor.GetAttackModifier();
        int baseHeal = Mathf.Max(1, Mathf.RoundToInt(actor.Attack * (1f + healMod)));
        float healVariance = UnityEngine.Random.Range(0.9f, 1.1f);
        float relationshipHealMod = actor.Type == CombatUnit.UnitType.Ally ? relationshipHealingMultiplier : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseHeal * skill.healMultiplier * healVariance * relationshipHealMod));
    }

    private void PlayHealAudio()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleHeal();
        }
    }

    private void ResolveTideBreak(CombatUnit actor, CombatUnit requestedTarget, TideBreakData tideBreak)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleTideBreakActivation();
        }

        // Determine which TideBreak data to use
        string abilityName;
        float damageMultiplier;
        SkillTarget targetType;

        if (tideBreak != null)
        {
            abilityName = tideBreak.abilityName;
            damageMultiplier = tideBreak.damageMultiplier;
            targetType = tideBreak.targetType;
        }
        else
        {
            // Missing TideBreak data falls back to the static default ability.
            TideBreakAbility tb = actor.Type == CombatUnit.UnitType.Ally
                ? TideBreakAbility.PlayerDefault
                : TideBreakAbility.EnemyDefault;
            abilityName = tb.AbilityName;
            damageMultiplier = balanceConfig != null
                ? balanceConfig.GetTideBreakMultiplier(actor.Type == CombatUnit.UnitType.Ally)
                : tb.DamageMultiplier;
            targetType = tb.IsPlayerAbility ? SkillTarget.AllEnemies : SkillTarget.SingleEnemy;
        }

        Debug.Log($"[BattleManager] *** TIDE BREAK! {actor.UnitName} unleashes {abilityName}! ***", this);

        // Apply relationship tide break multiplier (only for ally tide breaks)
        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            damageMultiplier *= relationshipTideBreakMultiplier;
        }

        if (difficultyService != null)
        {
            damageMultiplier *= actor.Type == CombatUnit.UnitType.Ally
                ? difficultyService.GetDamageMultiplierForPlayer()
                : difficultyService.GetDamageMultiplierForEnemy();
        }

        if (targetType == SkillTarget.AllEnemies)
        {
            CombatUnit.UnitType targetTypeUnit = actor.Type == CombatUnit.UnitType.Ally ? CombatUnit.UnitType.Enemy : CombatUnit.UnitType.Ally;
            List<CombatUnit> targets = GetAliveUnits(targetTypeUnit).ToList();
            int totalDamage = 0;
            foreach (CombatUnit target in targets)
            {
                int baseDmg = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
                MatchupResult tbMatchup = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
                float elementMultiplier = balanceConfig != null
                    ? balanceConfig.GetElementMultiplier(tbMatchup)
                    : ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
                float variance = UnityEngine.Random.Range(0.8f, 1.2f);
                int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * damageMultiplier * elementMultiplier * variance));

                int finalDmg = modifiedDmg;
                if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
                {
                    finalDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDmg / relationshipDefenseMultiplier));
                }

                int hpBefore = target.HP;
                target.TakeDamage(finalDmg);
                TriggerBattleHitFeedback(actor, target, false, true);
                totalDamage += (hpBefore - target.HP);
                Debug.Log($"  -> {target.UnitName} takes {finalDmg} damage. HP {hpBefore} -> {target.HP}", this);
            }
            Debug.Log($"[BattleManager] {abilityName} hits {targets.Count} targets for {totalDamage} total.", this);
        }
        else if (targetType == SkillTarget.SingleAlly)
        {
            CombatUnit allyTarget = requestedTarget != null && requestedTarget.IsAlive && requestedTarget.Type == actor.Type
                ? requestedTarget
                : GetLowestHpAlly(actor);
            if (allyTarget != null)
            {
                float reduction = Mathf.Clamp01(damageMultiplier * 0.3f);
                StatusEffect shield = new StatusEffect(StatusEffectType.Shield, 3, reduction, actor.UnitName);
                allyTarget.ApplyStatusEffect(shield);
                Debug.Log($"[BattleManager] {abilityName}: {actor.UnitName} grants {allyTarget.UnitName} a shield (-{reduction:P0} damage, 3 turns).", this);
            }
        }
        else if (targetType == SkillTarget.AllAllies)
        {
            IReadOnlyList<CombatUnit> allies = GetAliveUnits(actor.Type);
            float relationshipHealMod = actor.Type == CombatUnit.UnitType.Ally ? relationshipHealingMultiplier : 1f;
            int healBase = Mathf.Max(1, Mathf.RoundToInt(actor.Attack * damageMultiplier * 0.5f * relationshipHealMod));
            for (int i = 0; i < allies.Count; i++)
            {
                CombatUnit ally = allies[i];
                int hpBefore = ally.HP;
                ally.Heal(healBase);
                Debug.Log($"[BattleManager] {abilityName}: {actor.UnitName} heals {ally.UnitName} for {healBase}. HP {hpBefore} -> {ally.HP}.", this);
            }
            AudioManager healAudio = AudioManager.Instance;
            if (healAudio != null)
            {
                healAudio.HandleHeal();
            }
        }
        else if (targetType == SkillTarget.Self)
        {
            float buffMag = Mathf.Clamp01(damageMultiplier * 0.25f);
            StatusEffect selfBuff = new StatusEffect(StatusEffectType.BuffAttack, 3, buffMag, actor.UnitName);
            actor.ApplyStatusEffect(selfBuff);
            int selfHeal = Mathf.Max(1, Mathf.RoundToInt(actor.Attack * damageMultiplier * 0.3f));
            int hpBefore = actor.HP;
            actor.Heal(selfHeal);
            Debug.Log($"[BattleManager] {abilityName}: {actor.UnitName} buffs self (+{buffMag:P0} ATK, healed {selfHeal}). HP {hpBefore} -> {actor.HP}.", this);
        }
        else
        {
            CombatUnit target = ResolveOpponentTarget(actor, requestedTarget);

            if (target == null)
            {
                momentumState.Reset();
                Debug.Log("[BattleManager] Momentum reset after Tide Break.", this);
                return;
            }

            int baseDmg = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
            MatchupResult tbSingleMatchup = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
            float elementMultiplier = balanceConfig != null
                ? balanceConfig.GetElementMultiplier(tbSingleMatchup)
                : ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
            float variance = UnityEngine.Random.Range(0.8f, 1.2f);
            int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * damageMultiplier * elementMultiplier * variance));

            int finalDmg = modifiedDmg;
            if (target.Type == CombatUnit.UnitType.Ally && actor.Type == CombatUnit.UnitType.Enemy)
            {
                finalDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDmg / relationshipDefenseMultiplier));
            }

            int hpBefore = target.HP;
            target.TakeDamage(finalDmg);
            TriggerBattleHitFeedback(actor, target, false, true);
            Debug.Log($"  -> {target.UnitName} takes {finalDmg} damage. HP {hpBefore} -> {target.HP}", this);
        }

        momentumState.Reset();
        Debug.Log("[BattleManager] Momentum reset after Tide Break.", this);
    }

    private CombatUnit GetRandomLivingOpponent(CombatUnit actor)
    {
        CombatUnit.UnitType targetType = actor.Type == CombatUnit.UnitType.Ally
            ? CombatUnit.UnitType.Enemy
            : CombatUnit.UnitType.Ally;

        IReadOnlyList<CombatUnit> candidates = GetAliveUnits(targetType);
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private CombatUnit ResolveOpponentTarget(CombatUnit actor, CombatUnit requestedTarget)
    {
        if (actor != null && actor.IsTaunted && IsValidTarget(actor, actor.TauntedBy))
        {
            return actor.TauntedBy;
        }

        return IsValidTarget(actor, requestedTarget)
            ? requestedTarget
            : GetRandomLivingOpponent(actor);
    }

    /// <summary>
    /// Selects a target using the active vice profile's targetWeakestWeight.
    /// When no profile is set or the weight roll fails, falls back to random targeting.
    /// </summary>
    private CombatUnit SelectTargetWithProfile(CombatUnit actor, ViceAIProfile profile)
    {
        if (profile != null && profile.targetWeakestWeight > 0f
            && UnityEngine.Random.value < profile.targetWeakestWeight)
        {
            return GetWeakestLivingOpponent(actor);
        }

        return GetRandomLivingOpponent(actor);
    }

    /// <summary>
    /// Returns the living opponent with the lowest HP ratio (current HP / max HP).
    /// Used by vices that focus fire on weakened targets.
    /// </summary>
    private CombatUnit GetWeakestLivingOpponent(CombatUnit actor)
    {
        CombatUnit.UnitType targetType = actor.Type == CombatUnit.UnitType.Ally
            ? CombatUnit.UnitType.Enemy
            : CombatUnit.UnitType.Ally;

        IReadOnlyList<CombatUnit> candidates = GetAliveUnits(targetType);
        if (candidates.Count == 0)
        {
            return null;
        }

        CombatUnit weakest = candidates[0];
        float lowestHpRatio = weakest.MaxHP > 0 ? (float)weakest.HP / weakest.MaxHP : 1f;

        for (int i = 1; i < candidates.Count; i++)
        {
            float hpRatio = candidates[i].MaxHP > 0
                ? (float)candidates[i].HP / candidates[i].MaxHP
                : 1f;

            if (hpRatio < lowestHpRatio)
            {
                lowestHpRatio = hpRatio;
                weakest = candidates[i];
            }
        }

        return weakest;
    }

    private CombatUnit GetLowestHpAlly(CombatUnit actor)
    {
        IReadOnlyList<CombatUnit> allies = GetAliveUnits(actor.Type);
        if (allies.Count == 0)
        {
            return null;
        }

        CombatUnit lowest = allies[0];
        for (int i = 1; i < allies.Count; i++)
        {
            if (allies[i].HP < lowest.HP)
            {
                lowest = allies[i];
            }
        }

        return lowest;
    }

    private CombatUnit FindTeamUpPartner(CombatUnit actor)
    {
        IReadOnlyList<CombatUnit> allies = GetAliveUnits(CombatUnit.UnitType.Ally);
        List<CombatUnit> candidates = new List<CombatUnit>();
        for (int i = 0; i < allies.Count; i++)
        {
            if (allies[i] != actor && allies[i].IsAlive)
            {
                candidates.Add(allies[i]);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private bool IsValidTarget(CombatUnit actor, CombatUnit target)
    {
        if (actor == null || target == null)
        {
            return false;
        }

        if (!actor.IsAlive || !target.IsAlive)
        {
            return false;
        }

        return actor.Type != target.Type;
    }

    private bool IsValidTargetForSkill(CombatUnit actor, CombatUnit target, SkillTarget targetType)
    {
        if (actor == null || target == null)
        {
            return false;
        }

        if (!actor.IsAlive || !target.IsAlive)
        {
            return false;
        }

        if (targetType == SkillTarget.SingleAlly || targetType == SkillTarget.AllAllies)
        {
            return actor.Type == target.Type;
        }

        return actor.Type != target.Type;
    }

    private bool CheckBattleOutcome()
    {
        if (IsTerminalPhase(currentPhase))
        {
            return true;
        }

        bool alliesAlive = GetAliveUnits(CombatUnit.UnitType.Ally).Count > 0;
        bool enemiesAlive = GetAliveUnits(CombatUnit.UnitType.Enemy).Count > 0;

        if (!alliesAlive)
        {
            SetDefeat();
            return true;
        }

        if (!enemiesAlive)
        {
            SetVictory();
            return true;
        }

        return false;
    }

    private int GetRegistrationOrder(CombatUnit unit)
    {
        if (unit == null)
        {
            return int.MaxValue;
        }

        if (unitRegistrationOrder.TryGetValue(unit, out int registrationOrder))
        {
            return registrationOrder;
        }

        unitRegistrationOrder[unit] = nextRegistrationOrder;
        nextRegistrationOrder++;
        return unitRegistrationOrder[unit];
    }

    private BattlePhase GetNextPhase(BattlePhase phase)
    {
        switch (phase)
        {
            case BattlePhase.StartBattle:
                return BattlePhase.PlayerInput;
            case BattlePhase.PlayerInput:
                return BattlePhase.ActionExecution;
            case BattlePhase.ActionExecution:
                return BattlePhase.EndTurn;
            case BattlePhase.EndTurn:
                return BattlePhase.PlayerInput;
            case BattlePhase.Victory:
            case BattlePhase.Defeat:
            case BattlePhase.Fled:
            default:
                return phase;
        }
    }

    private bool IsTerminalPhase(BattlePhase phase)
    {
        return phase == BattlePhase.Victory || phase == BattlePhase.Defeat || phase == BattlePhase.Fled;
    }

    private void TriggerBattleHitFeedback(CombatUnit actor, CombatUnit target, bool isCrit, bool isHeavy)
    {
        if (!enableActionAnimations || actor == null || target == null)
        {
            return;
        }

        // Audio feedback for combat hits
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            if (isCrit)
            {
                audioManager.HandleAttackCrit();
            }
            else
            {
                audioManager.HandleAttackHit();
            }
        }

        Transform actorVisual = ResolveActionVisualTransform(actor);
        if (actorVisual != null)
        {
            float direction = actor.Type == CombatUnit.UnitType.Ally ? 1f : -1f;
            if (activeHitFeedbackCoroutines.TryGetValue(actorVisual, out Coroutine existing))
            {
                StopCoroutine(existing);
            }
            activeHitFeedbackCoroutines[actorVisual] = StartCoroutine(AnimateLunge(actorVisual, direction, isHeavy));
        }

        Transform targetVisual = ResolveActionVisualTransform(target);
        if (targetVisual != null)
        {
            if (activeHitFeedbackCoroutines.TryGetValue(targetVisual, out Coroutine existing))
            {
                StopCoroutine(existing);
            }
            activeHitFeedbackCoroutines[targetVisual] = StartCoroutine(AnimateHitShake(targetVisual, isCrit));
        }

        if (isHeavy)
        {
            Transform targetShadow = target.transform.Find("BattleSpriteShadow");
            SpriteRenderer shadowRenderer = targetShadow != null ? targetShadow.GetComponent<SpriteRenderer>() : null;
            if (targetShadow != null && shadowRenderer != null && shadowRenderer.enabled)
            {
                StartCoroutine(AnimateShadowPulse(targetShadow));
            }
        }

        SpawnHitEffect(target, actor.ElementType, isCrit, isHeavy);
    }

    private static Transform ResolveActionVisualTransform(CombatUnit unit)
    {
        if (unit == null)
        {
            return null;
        }

        Transform spriteVisual = unit.transform.Find("BattleSpriteVisual");
        SpriteRenderer spriteRenderer = spriteVisual != null ? spriteVisual.GetComponent<SpriteRenderer>() : null;
        return spriteRenderer != null && spriteRenderer.enabled ? spriteVisual : unit.transform;
    }

    private IEnumerator AnimateLunge(Transform visualTransform, float direction, bool isHeavy)
    {
        if (visualTransform == null)
        {
            yield break;
        }

        Vector3 start = visualTransform.localPosition;
        float distance = Mathf.Max(0.1f, lungeDistance) * (isHeavy ? 1.25f : 1f);
        Vector3 apex = start + new Vector3(direction * distance, 0f, 0f);
        float duration = Mathf.Max(0.04f, lungeDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            visualTransform.localPosition = Vector3.Lerp(start, apex, eased);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            visualTransform.localPosition = Vector3.Lerp(apex, start, eased);
            yield return null;
        }

        visualTransform.localPosition = start;
        activeHitFeedbackCoroutines.Remove(visualTransform);
    }

    private IEnumerator AnimateHitShake(Transform visualTransform, bool isCrit)
    {
        if (visualTransform == null)
        {
            yield break;
        }

        Vector3 start = visualTransform.localPosition;
        float duration = Mathf.Max(0.04f, hitShakeDuration) * (isCrit ? 1.2f : 1f);
        float magnitude = Mathf.Max(0.01f, hitShakeMagnitude) * (isCrit ? 1.6f : 1f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            float shakeX = (UnityEngine.Random.value * 2f - 1f) * magnitude * damper;
            float shakeY = (UnityEngine.Random.value * 2f - 1f) * magnitude * 0.55f * damper;
            visualTransform.localPosition = start + new Vector3(shakeX, shakeY, 0f);
            yield return null;
        }

        visualTransform.localPosition = start;
        activeHitFeedbackCoroutines.Remove(visualTransform);
    }

    private IEnumerator AnimateShadowPulse(Transform shadowTransform)
    {
        if (shadowTransform == null)
        {
            yield break;
        }

        Vector3 startScale = shadowTransform.localScale;
        Vector3 expanded = new Vector3(startScale.x * 1.2f, startScale.y * 0.74f, startScale.z);
        float duration = 0.11f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.Sin(t * Mathf.PI);
            shadowTransform.localScale = Vector3.Lerp(startScale, expanded, eased);
            yield return null;
        }

        shadowTransform.localScale = startScale;
    }

    private void SpawnHitEffect(CombatUnit target, CombatUnit.Element element, bool isCrit, bool isHeavy)
    {
        if (target == null)
        {
            return;
        }

        GameObject effectObject = new GameObject("BattleHitFx");
        effectObject.transform.SetParent(target.transform, false);
        effectObject.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        effectObject.transform.localScale = Vector3.one * Mathf.Max(0.1f, hitEffectScale) * (isHeavy ? 1.2f : 1f) * (isCrit ? 1.2f : 1f);

        SpriteRenderer renderer = effectObject.AddComponent<SpriteRenderer>();
        renderer.sprite = FuturisticSpriteLibrary.GetHitEffectSprite();
        renderer.color = Color.Lerp(GetElementFxColor(element), Color.white, isCrit ? 0.4f : 0.2f);
        renderer.sortingOrder = 48;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        StartCoroutine(AnimateHitEffect(effectObject.transform, renderer));
    }

    private IEnumerator AnimateHitEffect(Transform effectTransform, SpriteRenderer effectRenderer)
    {
        if (effectTransform == null || effectRenderer == null)
        {
            yield break;
        }

        Vector3 startScale = effectTransform.localScale;
        Vector3 endScale = startScale * 1.7f;
        Color startColor = effectRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        float duration = Mathf.Max(0.08f, hitEffectDuration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            effectTransform.localScale = Vector3.Lerp(startScale, endScale, eased);
            effectRenderer.color = Color.Lerp(startColor, endColor, eased);
            yield return null;
        }

        if (effectTransform != null)
        {
            Destroy(effectTransform.gameObject);
        }
    }

    private static Color GetElementFxColor(CombatUnit.Element element)
    {
        switch (element)
        {
            case CombatUnit.Element.Fire:
                return new Color(1f, 0.43f, 0.24f, 1f);
            case CombatUnit.Element.Water:
                return new Color(0.36f, 0.78f, 1f, 1f);
            case CombatUnit.Element.Earth:
                return new Color(0.66f, 0.86f, 0.45f, 1f);
            case CombatUnit.Element.Air:
                return new Color(0.84f, 0.97f, 1f, 1f);
            case CombatUnit.Element.Space:
                return new Color(0.78f, 0.66f, 1f, 1f);
            default:
                return new Color(1f, 0.75f, 0.35f, 1f);
        }
    }

    private void EnsureDebugLabel() { }
    
    private void UpdatePhaseLabel() { }

    private string BuildMomentumBar()
    {
        float value = momentumState.Value;
        int barLength = 20;
        int filled = Mathf.RoundToInt(Mathf.Abs(value) * barLength);
        string direction = value >= 0 ? "Player" : "Enemy";
        string bar = new string('█', filled).PadRight(barLength, '░');

        if (momentumState.IsPlayerTideBreakReady)
        {
            return $"[PLAYER TB READY] {bar}";
        }

        if (momentumState.IsEnemyTideBreakReady)
        {
            return $"[ENEMY TB READY] {bar}";
        }

        string sign = value >= 0f ? "+" : "-";
        return $"[{direction} {sign}{Mathf.Abs(value):F2}] {bar}";
    }

    private string BuildQueuePreview()
    {
        if (turnQueue == null || turnQueue.Count == 0)
        {
            return "<empty>";
        }

        StringBuilder previewBuilder = new StringBuilder();
        int previewCount = Mathf.Min(turnQueue.Count, 6);
        for (int i = 0; i < previewCount; i++)
        {
            CombatUnit unit = turnQueue[i];
            if (unit == null)
            {
                continue;
            }

            if (previewBuilder.Length > 0)
            {
                previewBuilder.Append(" > ");
            }

            previewBuilder.Append(unit.UnitName);
            previewBuilder.Append("(");
            previewBuilder.Append(unit.Speed);
            previewBuilder.Append(")");
        }

        if (turnQueue.Count > previewCount)
        {
            previewBuilder.Append(" ...");
        }

        return previewBuilder.ToString();
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        if (cachedBattleHud == null)
        {
            cachedBattleHud = FindFirstObjectByType<BattleHud>();
        }

        if (cachedBattleHud != null)
        {
            return;
        }

        Rect debugRect = new Rect(24f, 24f, 400f, 100f);
        GUI.Box(debugRect, debugText);

        if (!hasActivePhase || currentPhase != BattlePhase.PlayerInput || IsTerminalPhase(currentPhase))
        {
            return;
        }

        Rect panelRect = new Rect(20f, 210f, 420f, 360f);
        GUI.Box(panelRect, "Player Action Selection");

        float y = panelRect.y + 28f;
        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f), $"Assigned: {selectedPlayerActions.Count}/{playerInputUnits.Count}");
        y += 28f;

        CombatUnit currentInputUnit = GetCurrentPlayerInputUnit();
        if (currentInputUnit != null)
        {
            GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f), $"Current Ally: {currentInputUnit.UnitName}");
            y += 26f;

            if (!isAwaitingTargetSelection)
            {
                if (GUI.Button(new Rect(panelRect.x + 12f, y, 120f, 28f), "Attack"))
                {
                    pendingInputActionType = CombatActionType.Attack;
                    isAwaitingTargetSelection = true;
                }

                if (GUI.Button(new Rect(panelRect.x + 144f, y, 120f, 28f), "Defend"))
                {
                    AssignPlayerAction(currentInputUnit, CombatActionType.Defend, null);
                }

                if (GUI.Button(new Rect(panelRect.x + 276f, y, 120f, 28f), GetSkillButtonLabel(currentInputUnit)))
                {
                    SkillData skill = GetFirstSupportedSkillForCurrentSlice(currentInputUnit);
                    if (skill != null && currentInputUnit.CanUseSkill(skill))
                    {
                        pendingInputActionType = CombatActionType.Skill;
                        pendingSkillData = skill;
                        isAwaitingTargetSelection = true;
                    }
                }
            }
            else
            {
                string targetPrompt = pendingInputActionType == CombatActionType.Skill
                    ? "Select Skill Target:"
                    : pendingInputActionType == CombatActionType.TideBreak
                        ? "Select Tide Break Target:"
                        : "Select Attack Target:";
                GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f), targetPrompt);
                y += 26f;

                IReadOnlyList<CombatUnit> targets = GetAliveUnits(CombatUnit.UnitType.Enemy);
                for (int i = 0; i < targets.Count; i++)
                {
                    CombatUnit target = targets[i];
                    if (GUI.Button(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 24f), $"{target.UnitName} (HP {target.HP}/{target.MaxHP})"))
                    {
                        if (pendingInputActionType == CombatActionType.Skill && pendingSkillData != null)
                        {
                            AssignPlayerAction(currentInputUnit, pendingInputActionType, target, pendingSkillData);
                            pendingSkillData = null;
                        }
                        else
                        {
                            AssignPlayerAction(currentInputUnit, pendingInputActionType, target);
                        }
                        isAwaitingTargetSelection = false;
                    }

                    y += 28f;
                }

                if (GUI.Button(new Rect(panelRect.x + 12f, y, 120f, 26f), "Cancel"))
                {
                    isAwaitingTargetSelection = false;
                    pendingSkillData = null;
                }
            }
        }
        else
        {
            GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f), "All ally actions assigned.");
            y += 28f;
        }

        y = panelRect.y + panelRect.height - 116f;
        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 20f), "Selected Actions:");
        y += 22f;

        int shown = 0;
        foreach (KeyValuePair<CombatUnit, PlannedAction> pair in selectedPlayerActions)
        {
            if (shown >= 3)
            {
                break;
            }

            string targetName = pair.Value.Target != null ? pair.Value.Target.UnitName : "-";
            GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 20f), $"{pair.Key.UnitName}: {pair.Value.ActionType} {targetName}");
            y += 20f;
            shown++;
        }

        if (AreAllPlayerActionsAssigned())
        {
            if (GUI.Button(new Rect(panelRect.x + panelRect.width - 136f, panelRect.y + panelRect.height - 34f, 120f, 24f), "Confirm"))
            {
                TransitionToPhase(BattlePhase.ActionExecution, "PlayerConfirmedActions");
            }
        }
#endif
    }

    private CombatUnit GetCurrentPlayerInputUnit()
    {
        while (playerInputIndex < playerInputUnits.Count)
        {
            CombatUnit unit = playerInputUnits[playerInputIndex];
            if (unit != null && unit.IsAlive && !selectedPlayerActions.ContainsKey(unit))
            {
                return unit;
            }

            playerInputIndex++;
        }

        return null;
    }

    public CombatUnit GetCurrentInputUnit()
    {
        return GetCurrentPlayerInputUnit();
    }

    public bool TryAssignActionFromHud(CombatActionType actionType, CombatUnit target)
    {
        if (!hasActivePhase || currentPhase != BattlePhase.PlayerInput || IsTerminalPhase(currentPhase))
        {
            return false;
        }

        CombatUnit actor = GetCurrentPlayerInputUnit();
        if (actor == null)
        {
            return false;
        }

        switch (actionType)
        {
            case CombatActionType.Defend:
                AssignPlayerAction(actor, CombatActionType.Defend, null);
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.Attack:
                CombatUnit attackTarget = target != null ? target : GetRandomLivingOpponent(actor);
                if (!IsValidTarget(actor, attackTarget))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.Attack, attackTarget);
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.Skill:
                SkillData skill = pendingSkillData;
                if (skill == null)
                {
                    skill = GetFirstSupportedSkillForCurrentSlice(actor);
                }

                if (!IsSkillSupportedForCurrentSlice(skill))
                {
                    return false;
                }

                if (!actor.CanUseSkill(skill))
                {
                    return false;
                }

                if (RequiresExplicitTarget(skill.target) && !IsValidTargetForSkill(actor, target, skill.target))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.Skill, target, skill);
                pendingSkillData = null;
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.TideBreak:
                if (!momentumState.IsPlayerTideBreakReady)
                {
                    return false;
                }
                
                TideBreakData tbData = pendingTideBreak;
                if (tbData == null)
                {
                    tbData = GetFirstSupportedTideBreakForCurrentSlice(actor);
                }

                if (tbData == null)
                {
                    Debug.LogWarning($"[BattleManager] No TideBreak ability available for {actor.UnitName}. Use a regular action instead.", this);
                    return false;
                }

                if (!IsTideBreakSupportedForCurrentSlice(tbData))
                {
                    return false;
                }

                // Determine if target is required based on targetType
                bool targetRequired = TideBreakRequiresExplicitTarget(tbData, actor);

                if (targetRequired && !IsValidTargetForSkill(actor, target, tbData.targetType))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.TideBreak, target, tbData);
                pendingTideBreak = null; // Clear after use
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.Swap:
                if (!allowInBattlePartySwap || swapsRemainingPerTurn <= 0)
                {
                    Debug.LogWarning("[BattleManager] Swap action rejected. In-battle party swapping is disabled or no swaps remaining.");
                    return false;
                }
                return false;
        }

        return false;
    }

    public bool TrySwapWithReserve(CombatUnit activeUnit, CombatUnit reserveUnit)
    {
        if (!allowInBattlePartySwap)
        {
            Debug.LogWarning("[BattleManager] TrySwapWithReserve rejected. In-battle party swapping is disabled by design.");
            return false;
        }

        if (swapsRemainingPerTurn <= 0)
        {
            Debug.LogWarning("[BattleManager] No swaps remaining this turn.");
            return false;
        }

        if (!canSwapDuringPlayerInput)
        {
            Debug.LogWarning("[BattleManager] Party swapping is only allowed during the first input round.");
            return false;
        }

        if (!hasActivePhase || currentPhase != BattlePhase.PlayerInput)
        {
            Debug.LogWarning("[BattleManager] Party swapping is only allowed during player input.");
            return false;
        }

        if (activeUnit == null || reserveUnit == null)
        {
            Debug.LogWarning("[BattleManager] TrySwapWithReserve called with null unit.");
            return false;
        }
        if (!activeUnit.IsAlive)
        {
            Debug.LogWarning($"[BattleManager] {activeUnit.UnitName} is dead.");
            return false;
        }
        if (!allyUnits.Contains(activeUnit))
        {
            Debug.LogWarning($"[BattleManager] {activeUnit.UnitName} is not in active ally list.");
            return false;
        }
        if (!allyReserveUnits.Contains(reserveUnit))
        {
            Debug.LogWarning($"[BattleManager] {reserveUnit.UnitName} is not in reserve ally list.");
            return false;
        }
        if (!reserveUnit.IsAlive)
        {
            Debug.LogWarning($"[BattleManager] Cannot swap in dead unit {reserveUnit.UnitName}.");
            return false;
        }
        // Check if activeUnit already has an action assigned
        if (selectedPlayerActions.ContainsKey(activeUnit))
        {
            Debug.LogWarning($"[BattleManager] {activeUnit.UnitName} already has an action assigned.");
            return false;
        }
        // Assign swap action for the outgoing unit
        AssignPlayerAction(activeUnit, CombatActionType.Swap, reserveUnit);
        // Swap units
        SwapUnits(activeUnit, reserveUnit);
        // Mark incoming unit to skip turn this round
        reserveUnit.SkipTurnThisRound = true;
        swapsRemainingPerTurn--;
        RefreshPlayerInputUnits();
        return true;
    }

    public bool IsPartySwapAllowedThisRound()
    {
        return allowInBattlePartySwap && swapsRemainingPerTurn > 0 && canSwapDuringPlayerInput && hasActivePhase && currentPhase == BattlePhase.PlayerInput;
    }

    public void SetPendingTideBreak(TideBreakData tb)
    {
        pendingTideBreak = tb;
    }

    public void SetPendingSkill(SkillData skill)
    {
        pendingSkillData = skill;
    }

    public bool IsSkillSupportedForCurrentSlice(SkillData skill)
    {
        return skill != null && (skill.target == SkillTarget.SingleEnemy
            || skill.target == SkillTarget.AllEnemies
            || skill.target == SkillTarget.SingleAlly
            || skill.target == SkillTarget.AllAllies
            || skill.target == SkillTarget.Self);
    }

    public SkillData GetFirstUsableSupportedSkillForCurrentSlice(CombatUnit actor)
    {
        if (actor == null)
        {
            return null;
        }

        IReadOnlyList<SkillData> actorSkills = actor.Skills;
        if (actorSkills.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < actorSkills.Count; i++)
        {
            SkillData skill = actorSkills[i];
            if (IsSkillSupportedForCurrentSlice(skill) && actor.CanUseSkill(skill))
            {
                return skill;
            }
        }

        return null;
    }

    public bool IsTideBreakSupportedForCurrentSlice(TideBreakData tideBreak)
    {
        if (tideBreak == null)
        {
            return false;
        }

        return tideBreak.targetType == SkillTarget.SingleEnemy
            || tideBreak.targetType == SkillTarget.AllEnemies
            || tideBreak.targetType == SkillTarget.SingleAlly
            || tideBreak.targetType == SkillTarget.AllAllies
            || tideBreak.targetType == SkillTarget.Self;
    }

    public TideBreakData GetFirstSupportedTideBreakForCurrentSlice(CombatUnit actor)
    {
        if (actor == null || actor.TideBreakAbilities == null || actor.TideBreakAbilities.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < actor.TideBreakAbilities.Count; i++)
        {
            TideBreakData tideBreak = actor.TideBreakAbilities[i];
            if (IsTideBreakSupportedForCurrentSlice(tideBreak))
            {
                return tideBreak;
            }
        }

        return null;
    }

    public SkillData GetFirstSupportedSkillForCurrentSlice(CombatUnit actor)
    {
        if (actor == null)
        {
            return null;
        }

        IReadOnlyList<SkillData> actorSkills = actor.Skills;
        if (actorSkills.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < actorSkills.Count; i++)
        {
            SkillData skill = actorSkills[i];
            if (IsSkillSupportedForCurrentSlice(skill))
            {
                return skill;
            }
        }

        return null;
    }

    private static bool RequiresExplicitTarget(SkillTarget targetType)
    {
        return targetType == SkillTarget.SingleEnemy || targetType == SkillTarget.SingleAlly;
    }

    private bool TideBreakRequiresExplicitTarget(TideBreakData tideBreak, CombatUnit actor)
    {
        if (tideBreak != null)
        {
            return IsTideBreakSupportedForCurrentSlice(tideBreak) && RequiresExplicitTarget(tideBreak.targetType);
        }

        return actor != null && actor.Type == CombatUnit.UnitType.Enemy;
    }

    private void TryApplySkillStatusEffect(CombatUnit actor, CombatUnit target, SkillData skill)
    {
        if (actor == null || target == null || skill == null)
        {
            return;
        }

        if (!target.IsAlive)
        {
            return;
        }

        if (skill.appliedEffectType == StatusEffectType.None)
        {
            return;
        }

        if (skill.effectDuration <= 0)
        {
            Debug.LogWarning($"[BattleManager] {skill.skillName} has {skill.appliedEffectType} configured with non-positive duration. Effect skipped.");
            return;
        }

        StatusEffect effect = new StatusEffect(
            skill.appliedEffectType,
            skill.effectDuration,
            skill.effectMagnitude,
            actor.UnitName);

        target.ApplyStatusEffect(effect);

        // Taunt requires linking the taunter reference so IsTaunted/TauntedBy work
        if (skill.appliedEffectType == StatusEffectType.Taunt)
        {
            target.SetTaunter(actor);
        }

        Debug.Log($"[BattleManager] {actor.UnitName} applied {skill.appliedEffectType} to {target.UnitName} via {skill.skillName} ({skill.effectDuration} turns).", this);
    }

    private void TryAutoConfirmPlayerActions()
    {
        if (AreAllPlayerActionsAssigned() && currentPhase == BattlePhase.PlayerInput)
        {
            TransitionToPhase(BattlePhase.ActionExecution, "HudAssignedAllActions");
        }
    }

    private void AssignPlayerAction(CombatUnit actor, CombatActionType actionType, CombatUnit target)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        if (actionType == CombatActionType.Attack && !IsValidTarget(actor, target))
        {
            return;
        }

        selectedPlayerActions[actor] = new PlannedAction(actionType, actor, target);
        Debug.Log($"[BattleManager] Action assigned: {actor.UnitName} => {actionType}", this);

        if (playerInputIndex < playerInputUnits.Count && playerInputUnits[playerInputIndex] == actor)
        {
            playerInputIndex++;
        }
    }

    private void AssignPlayerAction(CombatUnit actor, CombatActionType actionType, CombatUnit target, SkillData skill)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        selectedPlayerActions[actor] = new PlannedAction(actionType, actor, target, skill);
        Debug.Log($"[BattleManager] Action assigned: {actor.UnitName} => {actionType} ({skill?.skillName})", this);

        if (playerInputIndex < playerInputUnits.Count && playerInputUnits[playerInputIndex] == actor)
        {
            playerInputIndex++;
        }
    }

    private void AssignPlayerAction(CombatUnit actor, CombatActionType actionType, CombatUnit target, TideBreakData tideBreak)
    {
        if (actor == null || !actor.IsAlive)
        {
            return;
        }

        selectedPlayerActions[actor] = new PlannedAction(actionType, actor, target, tideBreak);
        Debug.Log($"[BattleManager] Action assigned: {actor.UnitName} => {actionType} ({tideBreak?.abilityName})", this);

        if (playerInputIndex < playerInputUnits.Count && playerInputUnits[playerInputIndex] == actor)
        {
            playerInputIndex++;
        }
    }

    private string GetSkillButtonLabel(CombatUnit unit)
    {
        if (unit == null) return "No Skill";

        SkillData skill = GetFirstSupportedSkillForCurrentSlice(unit);
        if (skill == null)
        {
            return "No Skill";
        }

        if (!unit.CanUseSkill(skill))
        {
            return $"No MP ({skill.mpCost})";
        }

        return $"{skill.skillName} ({skill.mpCost} MP)";
    }

    private bool AreAllPlayerActionsAssigned()
    {
        for (int i = 0; i < playerInputUnits.Count; i++)
        {
            CombatUnit unit = playerInputUnits[i];
            if (unit == null || !unit.IsAlive)
            {
                continue;
            }

            if (!selectedPlayerActions.ContainsKey(unit))
            {
                return false;
            }
        }

        return true;
    }

    private static int CountAliveUnits(List<CombatUnit> units)
    {
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != null && units[i].IsAlive)
            {
                count++;
            }
        }

        return count;
    }
}
