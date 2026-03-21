using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum BattlePhase
{
    StartBattle,
    PlayerInput,
    ActionExecution,
    EndTurn,
    Victory,
    Defeat
}

public enum CombatActionType
{
    Attack,
    Defend,
    Skill,
    TideBreak
}

public struct PlannedAction
{
    public CombatActionType ActionType;
    public CombatUnit Actor;
    public CombatUnit Target;
    public SkillData SelectedSkill;

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
        SelectedSkill = null;
    }

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target, SkillData skill)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
        SelectedSkill = skill;
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

    [SerializeField] private BattlePhase currentPhase;

    public BattlePhase CurrentPhase => currentPhase;

    private List<CombatUnit> allyUnits = new List<CombatUnit>();
    private List<CombatUnit> enemyUnits = new List<CombatUnit>();
    private List<CombatUnit> turnQueue = new List<CombatUnit>();
    private Dictionary<CombatUnit, int> unitRegistrationOrder = new Dictionary<CombatUnit, int>();
    private Dictionary<CombatUnit, PlannedAction> selectedPlayerActions = new Dictionary<CombatUnit, PlannedAction>();
    private List<CombatUnit> playerInputUnits = new List<CombatUnit>();
    private MomentumState momentumState = new MomentumState();

    public IReadOnlyList<CombatUnit> AllyUnits => allyUnits;
    public IReadOnlyList<CombatUnit> EnemyUnits => enemyUnits;
    public IReadOnlyList<CombatUnit> TurnQueue => turnQueue;
    public MomentumState Momentum => momentumState;

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
    private float actionStepTimer;
    private bool actionExecutionActive;
    private BattleHud cachedBattleHud;
    private string debugText = "";

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
        }
    }

    private static void NotifyCombatEnded(bool playerWon)
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnCombatEnded(playerWon);
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

        if (playerInputUnits.Count == 0)
        {
            SetDefeat();
            return;
        }

        Debug.Log($"[BattleManager] Player input started for {playerInputUnits.Count} allies.", this);
    }

    private void BeginActionExecutionPhase()
    {
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
            if (actionA.ActionType != CombatActionType.Attack && actionA.ActionType != CombatActionType.TideBreak)
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
                if (actionB.ActionType != CombatActionType.Attack && actionB.ActionType != CombatActionType.TideBreak)
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
                    Debug.Log($"[BattleManager] Clash is neutral. Both attack normally.", this);
                    continue;
                }

                clashedUnits.Add(unitA);
                clashedUnits.Add(unitB);
            }
        }
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
            ResolveTideBreak(actor, plannedAction.Target);
        }
        else if (plannedAction.ActionType == CombatActionType.Defend)
        {
            Debug.Log($"[BattleManager] {actor.UnitName} defends.", this);
        }
        else
        {
            Debug.Log($"[BattleManager] {actor.UnitName} passes.", this);
        }

        CheckBattleOutcome();
        UpdateDebugText();
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

        if (momentumState.IsEnemyTideBreakReady)
        {
            CombatUnit tbTarget = GetRandomLivingOpponent(actor);
            if (tbTarget != null)
            {
                return new PlannedAction(CombatActionType.TideBreak, actor, tbTarget);
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

        int baseDamage = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
        float multiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        float variance = Random.Range(0.8f, 1.2f);
        int modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDamage * multiplier * variance));

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

        int baseDamage = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
        float multiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        float skillMultiplier = multiplier * skill.damageMultiplier;
        float variance = Random.Range(0.8f, 1.2f);
        int modifiedDamage = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDamage * skillMultiplier * variance));

        MatchupResult matchup = ElementMatchup.GetResult(actor.ElementType, target.ElementType);
        int hpBefore = target.HP;
        target.TakeDamage(modifiedDamage);
        int hpAfter = target.HP;

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

        Debug.Log(
            $"[BattleManager] {actor.UnitName} uses {skill.skillName} on {target.UnitName} for {modifiedDamage} (base {baseDamage} x{skillMultiplier:F2}, -{skill.mpCost} MP). HP {hpBefore} -> {hpAfter}.{matchupFeedback}",
            this);
    }

    private void ResolveTideBreak(CombatUnit actor, CombatUnit requestedTarget)
    {
        TideBreakAbility tb = actor.Type == CombatUnit.UnitType.Ally
            ? TideBreakAbility.PlayerDefault
            : TideBreakAbility.EnemyDefault;

        Debug.Log($"[BattleManager] *** TIDE BREAK! {actor.UnitName} unleashes {tb.AbilityName}! ***", this);

        if (tb.IsPlayerAbility)
        {
            List<CombatUnit> enemies = GetAliveUnits(CombatUnit.UnitType.Enemy).ToList();
            int totalDamage = 0;
            foreach (CombatUnit enemy in enemies)
            {
                int baseDmg = Mathf.Max(GameConstants.MinimumDamage, actor.Attack);
                int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * tb.DamageMultiplier));
                int hpBefore = enemy.HP;
                enemy.TakeDamage(modifiedDmg);
                totalDamage += modifiedDmg;
                Debug.Log($"  -> {enemy.UnitName} takes {modifiedDmg} damage. HP {hpBefore} -> {enemy.HP}", this);
            }
            Debug.Log($"[BattleManager] {tb.AbilityName} hits {enemies.Count} enemies for {totalDamage} total.", this);
        }
        else
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
            int modifiedDmg = Mathf.Max(GameConstants.MinimumDamage, Mathf.RoundToInt(baseDmg * tb.DamageMultiplier));
            int hpBefore = target.HP;
            target.TakeDamage(modifiedDmg);
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

        return candidates[Random.Range(0, candidates.Count)];
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
            default:
                return phase;
        }
    }

    private bool IsTerminalPhase(BattlePhase phase)
    {
        return phase == BattlePhase.Victory || phase == BattlePhase.Defeat;
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

        Rect debugRect = new Rect(24f, 24f, 400f, 100f);
        GUI.Box(debugRect, debugText);

        if (cachedBattleHud != null)
        {
            return;
        }

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
                if (!IsValidTarget(actor, target) || actor.Skills == null || actor.Skills.Length == 0)
                {
                    return false;
                }

                SkillData skill = actor.Skills[0];
                if (!actor.CanUseSkill(skill))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.Skill, target, skill);
                TryAutoConfirmPlayerActions();
                return true;

            case CombatActionType.TideBreak:
                if (!momentumState.IsPlayerTideBreakReady || !IsValidTarget(actor, target))
                {
                    return false;
                }

                AssignPlayerAction(actor, CombatActionType.TideBreak, target);
                TryAutoConfirmPlayerActions();
                return true;
        }

        return false;
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

    private string GetSkillButtonLabel(CombatUnit unit)
    {
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
