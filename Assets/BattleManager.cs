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
    }

    public event Action<ClashResult> OnClashResolved;
    public event Action<CombatUnit, bool> OnDamageDealt;

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
    private bool canSwapDuringPlayerInput = true;
    private int failedFleeAttemptsThisBattle;

    private void Awake()
    {
        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
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
    }

    private void Start()
    {
        StartBattle();
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
                BuildTurnQueueFromLivingUnits();
                selectedPlayerActions.Clear();
                momentumState.Reset();
                clashedUnits.Clear();
                break;
            case BattlePhase.PlayerInput:
                BeginPlayerInputPhase();
                break;
            case BattlePhase.ActionExecution:
                BeginActionExecutionPhase();
                break;
            case BattlePhase.EndTurn:
                BeginEndTurnPhase();
                break;
            case BattlePhase.Victory:
                NotifyCombatEnded(true);
                break;
            case BattlePhase.Defeat:
                NotifyCombatEnded(false);
                break;
            case BattlePhase.Fled:
                NotifyCombatEnded(false, true);
                break;
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

        turnQueue = allLiving
            .OrderByDescending(unit => unit.Speed)
            .ThenBy(unit => GetRegistrationOrder(unit))
            .ToList();

        turnQueueIndex = 0;
        currentActingUnit = null;

        Debug.Log($"[BattleManager] Turn queue rebuilt with {turnQueue.Count} living units.", this);
        for (int i = 0; i < turnQueue.Count; i++)
        {
            CombatUnit queueUnit = turnQueue[i];
            Debug.Log($"[BattleManager] Queue {i + 1}: {queueUnit.UnitName} (SPD {queueUnit.Speed})", this);
        }
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
            if (candidate.SkipTurnThisRound)
            {
                Debug.Log($"[BattleManager] {candidate.UnitName} skips turn this round.");
                continue;
            }

            candidate.ProcessTurnStartEffects();
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

        // Clear skip turn flags for all ally units
        foreach (CombatUnit ally in allyUnits)
        {
            if (ally != null) ally.SkipTurnThisRound = false;
        }

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
        float speedDelta = actor.Speed - GetAverageLivingEnemySpeed();

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
            totalSpeed += livingEnemies[i].Speed;
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
        if (momentumState.IsEnemyTideBreakReady)
        {
            CombatUnit tbTarget = GetRandomLivingOpponent(actor);
            if (tbTarget != null)
            {
                TideBreakData tbData = GetTideBreakForActor(actor);
                return new PlannedAction(CombatActionType.TideBreak, actor, tbTarget, tbData);
            }
        }

        CombatUnit target = GetRandomLivingOpponent(actor);
        if (target == null)
        {
            return new PlannedAction(CombatActionType.Attack, actor, null);
        }

        if (actor.Skills != null && actor.Skills.Length > 0)
        {
            SkillData skill = actor.Skills[0];
            if (actor.CanUseSkill(skill))
            {
                return new PlannedAction(CombatActionType.Skill, actor, target, skill);
            }
        }

        return new PlannedAction(CombatActionType.Attack, actor, target);
    }

    private TideBreakData GetTideBreakForActor(CombatUnit actor)
    {
        if (actor.TideBreakAbilities != null && actor.TideBreakAbilities.Count > 0)
        {
            // Pick random
            int index = UnityEngine.Random.Range(0, actor.TideBreakAbilities.Count);
            return actor.TideBreakAbilities[index];
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
            }
        }
    }

    private static bool IsClashableAction(CombatActionType actionType)
    {
        return actionType == CombatActionType.Attack
            || actionType == CombatActionType.Skill
            || actionType == CombatActionType.TideBreak;
    }

    private void ExecuteClash(CombatUnit winner, CombatUnit loser)
    {
        Debug.Log($"[BattleManager] {winner.UnitName} wins the clash! (element advantage)", this);

        int winnerDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(winner.Attack * GameConstants.ClashWinnerMultiplier));
        int loserDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(loser.Attack * GameConstants.ClashLoserMultiplier));

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
            Description = $"{winner.UnitName} WINS!"
        });
    }

    private void ExecuteNeutralClash(CombatUnit unitA, CombatUnit unitB)
    {
        Debug.Log($"[BattleManager] Clash is neutral. Both deal reduced damage.", this);

        int dmgA = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(unitA.Attack * GameConstants.ClashNeutralMultiplier));
        int dmgB = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(unitB.Attack * GameConstants.ClashNeutralMultiplier));

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
            Description = "NEUTRAL CLASH"
        });
    }

    private void BeginEndTurnPhase()
    {
        selectedPlayerActions.Clear();
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
        CombatUnit target = requestedTarget;
        if (!IsValidTarget(actor, target))
        {
            target = GetRandomLivingOpponent(actor);
        }

        if (!IsValidTarget(actor, target))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} has no valid target and passes.", this);
            return;
        }

        float attackMod = actor.GetAttackModifier();
        int baseDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + attackMod)));
        float multiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        float variance = UnityEngine.Random.Range(0.8f, 1.2f);
        float modifiedDamageFloat = baseDamage * multiplier * variance;
        
        bool isCrit = UnityEngine.Random.value < actor.CritRate;
        if (isCrit)
        {
            modifiedDamageFloat *= actor.CritDamage;
            Debug.Log($"[BattleManager] CRITICAL HIT! {actor.UnitName} crits {target.UnitName}!");
        }
        
        int modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageFloat));

        MatchupResult matchup = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        int hpBefore = target.HP;
        target.TakeDamage(modifiedDamage);
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
            $"[BattleManager] {actor.UnitName} attacks {target.UnitName} for {modifiedDamage} (base {baseDamage} x{multiplier:F2}). HP {hpBefore} -> {hpAfter}.{matchupFeedback}",
            this);
    }

    private void ResolveSkill(CombatUnit actor, CombatUnit requestedTarget, SkillData skill)
    {
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

        // Handle different skill target types
        switch (skill.target)
        {
            case SkillTarget.AllEnemies:
            {
                // AoE skill: apply to all living enemies (opposite faction)
                CombatUnit.UnitType targetType = actor.Type == CombatUnit.UnitType.Ally ? CombatUnit.UnitType.Enemy : CombatUnit.UnitType.Ally;
                List<CombatUnit> aoeTargets = GetAliveUnits(targetType).ToList();
                if (aoeTargets.Count == 0)
                {
                    Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} but there are no living targets.", this);
                    return;
                }

                actor.SpendMp(skill.mpCost);
                int totalDamage = 0;
                float aoeAttackMod = actor.GetAttackModifier();
                int baseDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + aoeAttackMod)));

                foreach (CombatUnit aoeTarget in aoeTargets)
                {
                    if (!aoeTarget.IsAlive) continue;

                    float elementMultiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, aoeTarget.ElementType);
                    float skillMultiplier = elementMultiplier * skill.damageMultiplier;
                    float variance = UnityEngine.Random.Range(0.8f, 1.2f);
                    float modifiedDamageFloat = baseDamage * skillMultiplier * variance;

                    bool isCrit = UnityEngine.Random.value < actor.CritRate;
                    if (isCrit)
                    {
                        modifiedDamageFloat *= actor.CritDamage;
                        Debug.Log($"[BattleManager] CRITICAL HIT! {actor.UnitName} crits {aoeTarget.UnitName} with {skill.skillName}!");
                    }

                    int modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageFloat));
                    MatchupResult matchup = ElementMatchup.GetResult(actor.ElementType, aoeTarget.ElementType);

                    int hpBefore = aoeTarget.HP;
                    aoeTarget.TakeDamage(modifiedDamage);
                    int hpAfter = aoeTarget.HP;
                    totalDamage += modifiedDamage;

                    if (skill.appliedEffectType != StatusEffectType.None && aoeTarget.IsAlive)
                    {
                        StatusEffect effect = new StatusEffect(skill.appliedEffectType, skill.effectDuration, skill.effectMagnitude, actor.UnitName);
                        aoeTarget.ApplyStatusEffect(effect);
                    }

                    string matchupFeedback = "";
                    switch (matchup)
                    {
                        case MatchupResult.Strong:
                            matchupFeedback = " Super effective!";
                            break;
                        case MatchupResult.Weak:
                            matchupFeedback = " Not very effective...";
                            break;
                    }

                    momentumState.ShiftForAction(actor, matchup);
                    OnDamageDealt?.Invoke(actor, isCrit);
                    TriggerBattleHitFeedback(actor, aoeTarget, isCrit, true);

                    Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} on {aoeTarget.UnitName} for {modifiedDamage} (base {baseDamage} x{skillMultiplier:F2}, -{skill.mpCost} MP). HP {hpBefore} -> {hpAfter}.{matchupFeedback}", this);
                }
                Debug.Log($"[BattleManager] {skill.skillName} hits {aoeTargets.Count} targets for {totalDamage} total.", this);
            }
            return;

            case SkillTarget.SingleAlly:
                Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} targeting ally (not implemented). Attacking instead.", this);
                ResolveAttack(actor, requestedTarget);
                return;

            case SkillTarget.Self:
                if (!actor.CanUseSkill(skill))
                {
                    Debug.Log($"[BattleManager] {actor.UnitName} lacks MP for {skill.skillName}. Attacking instead.", this);
                    ResolveAttack(actor, requestedTarget);
                    return;
                }

                actor.SpendMp(skill.mpCost);

                if (skill.appliedEffectType != StatusEffectType.None)
                {
                    StatusEffect selfEffect = new StatusEffect(skill.appliedEffectType, skill.effectDuration, skill.effectMagnitude, actor.UnitName);
                    actor.ApplyStatusEffect(selfEffect);
                    Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} on self. Applied {skill.appliedEffectType}.", this);
                }
                else
                {
                    Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} on self.", this);
                }
                return;

            case SkillTarget.SingleEnemy:
            default:
                // Single-target skill (existing behavior)
                break;
        }

        // Single-target logic (original code)
        CombatUnit target = requestedTarget;
        if (!IsValidTarget(actor, target))
        {
            target = GetRandomLivingOpponent(actor);
        }

        if (!IsValidTarget(actor, target))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} uses {skill.skillName} but has no valid target.", this);
            return;
        }

        actor.SpendMp(skill.mpCost);

        float attackMod = actor.GetAttackModifier();
        int baseDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(actor.Attack * (1f + attackMod)));
        float multiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        float skillMultiplierSingle = multiplier * skill.damageMultiplier;
        float varianceSingle = UnityEngine.Random.Range(0.8f, 1.2f);
        float modifiedDamageFloatSingle = baseDamageSingle * skillMultiplierSingle * varianceSingle;
        
        bool isCritSingle = UnityEngine.Random.value < actor.CritRate;
        if (isCritSingle)
        {
            modifiedDamageFloatSingle *= actor.CritDamage;
            Debug.Log($"[BattleManager] CRITICAL HIT! {actor.UnitName} crits {target.UnitName} with {skill.skillName}!");
        }
        
        int modifiedDamageSingle = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(modifiedDamageFloatSingle));

        MatchupResult matchupSingle = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        int hpBeforeSingle = target.HP;
        target.TakeDamage(modifiedDamageSingle);
        int hpAfterSingle = target.HP;

        if (skill.appliedEffectType != StatusEffectType.None && target.IsAlive)
        {
            StatusEffect effect = new StatusEffect(skill.appliedEffectType, skill.effectDuration, skill.effectMagnitude, actor.UnitName);
            target.ApplyStatusEffect(effect);
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
        OnDamageDealt?.Invoke(actor, isCritSingle);
        TriggerBattleHitFeedback(actor, target, isCritSingle, true);

        Debug.Log(
            $"[BattleManager] {actor.UnitName} uses {skill.skillName} on {target.UnitName} for {modifiedDamageSingle} (base {baseDamageSingle} x{skillMultiplierSingle:F2}, -{skill.mpCost} MP). HP {hpBeforeSingle} -> {hpAfterSingle}.{matchupFeedbackSingle}",
            this);
    }

    private void ResolveTideBreak(CombatUnit actor, CombatUnit requestedTarget, TideBreakData tideBreak)
    {
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
            // Fallback to static defaults
            TideBreakAbility tb = actor.Type == CombatUnit.UnitType.Ally
                ? TideBreakAbility.PlayerDefault
                : TideBreakAbility.EnemyDefault;
            abilityName = tb.AbilityName;
            damageMultiplier = tb.DamageMultiplier;
            targetType = tb.IsPlayerAbility ? SkillTarget.AllEnemies : SkillTarget.SingleEnemy;
        }

        Debug.Log($"[BattleManager] *** TIDE BREAK! {actor.UnitName} unleashes {abilityName}! ***", this);

        if (targetType == SkillTarget.AllEnemies)
        {
            CombatUnit.UnitType targetTypeUnit = actor.Type == CombatUnit.UnitType.Ally ? CombatUnit.UnitType.Enemy : CombatUnit.UnitType.Ally;
            List<CombatUnit> targets = GetAliveUnits(targetTypeUnit).ToList();
            int totalDamage = 0;
            foreach (CombatUnit target in targets)
            {
                int baseDmg = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
                float elementMultiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
                float variance = UnityEngine.Random.Range(0.8f, 1.2f);
                int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * damageMultiplier * elementMultiplier * variance));
                int hpBefore = target.HP;
                target.TakeDamage(modifiedDmg);
                TriggerBattleHitFeedback(actor, target, false, true);
                totalDamage += modifiedDmg;
                Debug.Log($"  -> {target.UnitName} takes {modifiedDmg} damage. HP {hpBefore} -> {target.HP}", this);
            }
            Debug.Log($"[BattleManager] {abilityName} hits {targets.Count} targets for {totalDamage} total.", this);
        }
        else // SingleEnemy (or other, but treat as single)
        {
            CombatUnit target = requestedTarget;
            if (!IsValidTarget(actor, target))
            {
                target = GetRandomLivingOpponent(actor);
            }

            if (target == null)
            {
                momentumState.Reset();
                Debug.Log("[BattleManager] Momentum reset after Tide Break.", this);
                return;
            }

            int baseDmg = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
            float elementMultiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
            float variance = UnityEngine.Random.Range(0.8f, 1.2f);
            int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * damageMultiplier * elementMultiplier * variance));
            int hpBefore = target.HP;
            target.TakeDamage(modifiedDmg);
            TriggerBattleHitFeedback(actor, target, false, true);
            Debug.Log($"  -> {target.UnitName} takes {modifiedDmg} damage. HP {hpBefore} -> {target.HP}", this);
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

        Transform actorVisual = ResolveActionVisualTransform(actor);
        if (actorVisual != null)
        {
            float direction = actor.Type == CombatUnit.UnitType.Ally ? 1f : -1f;
            StartCoroutine(AnimateLunge(actorVisual, direction, isHeavy));
        }

        Transform targetVisual = ResolveActionVisualTransform(target);
        if (targetVisual != null)
        {
            StartCoroutine(AnimateHitShake(targetVisual, isCrit));
        }

        if (isHeavy)
        {
            Transform targetShadow = target.transform.Find("BattleSpriteShadow");
            if (targetShadow != null)
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
        return spriteVisual != null ? spriteVisual : unit.transform;
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
                    SkillData skill = currentInputUnit.Skills != null && currentInputUnit.Skills.Length > 0
                        ? currentInputUnit.Skills[0]
                        : null;
                    if (skill != null && currentInputUnit.CanUseSkill(skill))
                    {
                        // Check skill target type
                        switch (skill.target)
                        {
                            case SkillTarget.AllEnemies:
                                // AoE skill: skip target selection, assign with null target
                                AssignPlayerAction(currentInputUnit, CombatActionType.Skill, null, skill);
                                break;
                            case SkillTarget.SingleAlly:
                                Debug.Log("[BattleManager] SingleAlly skill target not implemented. Skipping.");
                                break;
                            case SkillTarget.Self:
                                Debug.Log("[BattleManager] Self skill target not implemented. Skipping.");
                                break;
                            case SkillTarget.SingleEnemy:
                            default:
                                pendingInputActionType = CombatActionType.Skill;
                                pendingSkillData = skill;
                                isAwaitingTargetSelection = true;
                                break;
                        }
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
                if (!IsValidTarget(actor, target))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.Attack, target);
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.Skill:
                if (actor.Skills == null || actor.Skills.Length == 0)
                {
                    return false;
                }
                SkillData skill = pendingSkillData ?? actor.Skills[0];
                if (!actor.CanUseSkill(skill))
                {
                    return false;
                }
                
                // For AoE skills, target can be null
                if (skill.target == SkillTarget.AllEnemies)
                {
                    // target can be null, we still assign action
                    AssignPlayerAction(actor, CombatActionType.Skill, target, skill);
                    pendingSkillData = null;
                    TryAutoConfirmPlayerActions();
                    return true;
                }

                // For self-target skills, target can be null
                if (skill.target == SkillTarget.Self)
                {
                    AssignPlayerAction(actor, CombatActionType.Skill, null, skill);
                    pendingSkillData = null;
                    TryAutoConfirmPlayerActions();
                    return true;
                }
                
                // For single-target skills, require valid target
                if (!IsValidTarget(actor, target))
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
                    // No pending TB selected; pick first if available, else fallback to default
                    if (actor.TideBreakAbilities != null && actor.TideBreakAbilities.Count > 0)
                        tbData = actor.TideBreakAbilities[0];
                    // else keep null, will fallback to default in ResolveTideBreak
                }
                
                // Determine if target is required based on targetType
                bool targetRequired = true;
                if (tbData != null)
                {
                    targetRequired = tbData.targetType != SkillTarget.AllEnemies;
                }
                else
                {
                    // Fallback defaults: player -> AllEnemies, enemy -> SingleEnemy
                    // Since this is player input, actor is always Ally (player)
                    targetRequired = false; // AllEnemies fallback, target not required
                }
                
                if (targetRequired && !IsValidTarget(actor, target))
                {
                    return false;
                }
                
                AssignPlayerAction(actor, CombatActionType.TideBreak, target, tbData);
                pendingTideBreak = null; // Clear after use
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.Swap:
                if (!canSwapDuringPlayerInput)
                {
                    return false;
                }

                // target is the reserve unit to swap IN
                if (target == null || !allyReserveUnits.Contains(target))
                {
                    return false;
                }
                if (!target.IsAlive)
                {
                    return false;
                }
                // Perform swap instantly (no turn consumed)
                // Assign swap action for the outgoing unit
                AssignPlayerAction(actor, CombatActionType.Swap, target);
                // Swap units
                SwapUnits(actor, target);
                // Mark incoming unit to skip turn this round
                target.SkipTurnThisRound = true;
                // Refresh player input units to reflect new composition
                RefreshPlayerInputUnits();
                // Since swap consumes the outgoing unit's turn (they are removed), we can consider action assigned.
                // No need to auto-confirm because there may be other units needing actions.
                return true;
        }

        return false;
    }

    public bool TrySwapWithReserve(CombatUnit activeUnit, CombatUnit reserveUnit)
    {
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
        // Refresh player input units to reflect new composition
        RefreshPlayerInputUnits();
        return true;
    }

    public bool IsPartySwapAllowedThisRound()
    {
        return canSwapDuringPlayerInput && hasActivePhase && currentPhase == BattlePhase.PlayerInput;
    }

    public void SetPendingTideBreak(TideBreakData tb)
    {
        pendingTideBreak = tb;
    }

    public void SetPendingSkill(SkillData skill)
    {
        pendingSkillData = skill;
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
        if (unit.Skills == null || unit.Skills.Length == 0)
        {
            return "No Skill";
        }

        SkillData skill = unit.Skills[0];
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
