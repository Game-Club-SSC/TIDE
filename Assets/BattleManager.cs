using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

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
    Pass
}

public struct PlannedAction
{
    public CombatActionType ActionType;
    public CombatUnit Actor;
    public CombatUnit Target;

    public PlannedAction(CombatActionType actionType, CombatUnit actor, CombatUnit target)
    {
        ActionType = actionType;
        Actor = actor;
        Target = target;
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

    public IReadOnlyList<CombatUnit> AllyUnits => allyUnits;
    public IReadOnlyList<CombatUnit> EnemyUnits => enemyUnits;
    public IReadOnlyList<CombatUnit> TurnQueue => turnQueue;

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
    private Text phaseLabel;
    private int nextRegistrationOrder;
    private int turnQueueIndex;
    private int playerInputIndex;
    private bool isAwaitingTargetSelection;
    private CombatUnit currentActingUnit;
    private CombatActionType pendingInputActionType = CombatActionType.Attack;
    private float actionStepTimer;
    private bool actionExecutionActive;

    private void Awake()
    {
        EnsureDebugLabel();
        UpdatePhaseLabel();
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
        UpdatePhaseLabel();
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
        UpdatePhaseLabel();
    }

    private void OnPhaseEntered(BattlePhase phase)
    {
        switch (phase)
        {
            case BattlePhase.StartBattle:
                BuildTurnQueueFromLivingUnits();
                selectedPlayerActions.Clear();
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
            case BattlePhase.Defeat:
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.OnCombatEnded();
                }
                break;
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
        if (CheckBattleOutcome())
        {
            return;
        }

        selectedPlayerActions.Clear();
        playerInputUnits = GetAliveUnits(CombatUnit.UnitType.Ally).ToList();
        playerInputIndex = 0;
        isAwaitingTargetSelection = false;
        pendingInputActionType = CombatActionType.Attack;

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

        actionExecutionActive = true;
        actionStepTimer = 0f;
        currentActingUnit = null;

        Debug.Log("[BattleManager] Action execution started.", this);
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

        PlannedAction plannedAction = GetPlannedAction(actor);
        if (plannedAction.ActionType == CombatActionType.Attack)
        {
            ResolveAttack(actor, plannedAction.Target);
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
    }

    private PlannedAction GetPlannedAction(CombatUnit actor)
    {
        if (actor.Type == CombatUnit.UnitType.Ally)
        {
            if (selectedPlayerActions.TryGetValue(actor, out PlannedAction plannedAction))
            {
                return plannedAction;
            }

            return new PlannedAction(CombatActionType.Pass, actor, null);
        }

        CombatUnit target = GetFirstLivingOpponent(actor);
        if (target == null)
        {
            return new PlannedAction(CombatActionType.Pass, actor, null);
        }

        return new PlannedAction(CombatActionType.Attack, actor, target);
    }

    private void ResolveAttack(CombatUnit actor, CombatUnit requestedTarget)
    {
        CombatUnit target = requestedTarget;
        if (!IsValidTarget(actor, target))
        {
            target = GetFirstLivingOpponent(actor);
        }

        if (!IsValidTarget(actor, target))
        {
            Debug.Log($"[BattleManager] {actor.UnitName} has no valid target and passes.", this);
            return;
        }

        int baseDamage = Mathf.Max(1, actor.Attack);
        float multiplier = ElementMatchup.GetDamageMultiplier(actor.ElementType, target.ElementType);
        int modifiedDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));

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

        Debug.Log(
            $"[BattleManager] {actor.UnitName} attacks {target.UnitName} for {modifiedDamage} (base {baseDamage} x{multiplier:F2}). HP {hpBefore} -> {hpAfter}.{matchupFeedback}",
            this);
    }

    private CombatUnit GetFirstLivingOpponent(CombatUnit actor)
    {
        CombatUnit.UnitType targetType = actor.Type == CombatUnit.UnitType.Ally
            ? CombatUnit.UnitType.Enemy
            : CombatUnit.UnitType.Ally;

        return GetAliveUnits(targetType).FirstOrDefault();
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

    private void EnsureDebugLabel()
    {
        Transform canvasTransform = transform.Find(DebugCanvasName);
        if (canvasTransform == null)
        {
            GameObject canvasObject = new GameObject(DebugCanvasName, typeof(RectTransform));
            canvasTransform = canvasObject.transform;
            canvasTransform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Transform labelTransform = canvasTransform.Find(DebugLabelName);
        if (labelTransform == null)
        {
            GameObject labelObject = new GameObject(DebugLabelName, typeof(RectTransform));
            labelTransform = labelObject.transform;
            labelTransform.SetParent(canvasTransform, false);
        }

        phaseLabel = labelTransform.GetComponent<Text>();
        if (phaseLabel == null)
        {
            phaseLabel = labelTransform.gameObject.AddComponent<Text>();
        }

        RectTransform labelRect = phaseLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = debugLabelPosition;
        labelRect.sizeDelta = new Vector2(640f, 220f);

        phaseLabel.font = LoadDebugFont();
        phaseLabel.fontSize = 20;
        phaseLabel.alignment = TextAnchor.UpperLeft;
        phaseLabel.color = Color.white;
        phaseLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        phaseLabel.verticalOverflow = VerticalWrapMode.Overflow;
        phaseLabel.raycastTarget = false;
    }

    private void UpdatePhaseLabel()
    {
        if (phaseLabel == null)
        {
            return;
        }

        string phaseName = hasActivePhase ? currentPhase.ToString() : "Waiting";
        int alliesAlive = CountAliveUnits(allyUnits);
        int enemiesAlive = CountAliveUnits(enemyUnits);
        string queuePreview = BuildQueuePreview();

        phaseLabel.text =
            $"Battle Phase: {phaseName}\n" +
            $"Allies: {allyUnits.Count} ({alliesAlive} alive)\n" +
            $"Enemies: {enemyUnits.Count} ({enemiesAlive} alive)\n" +
            $"Queue: {queuePreview}\n" +
            $"Acting: {(currentActingUnit != null ? currentActingUnit.UnitName : "None")}\n" +
            $"Next: {advancePhaseKey} | Victory: {victoryKey} | Defeat: {defeatKey}";
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

                if (GUI.Button(new Rect(panelRect.x + 276f, y, 120f, 28f), "Pass"))
                {
                    AssignPlayerAction(currentInputUnit, CombatActionType.Pass, null);
                }
            }
            else
            {
                GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 22f), "Select Attack Target:");
                y += 26f;

                IReadOnlyList<CombatUnit> targets = GetAliveUnits(CombatUnit.UnitType.Enemy);
                for (int i = 0; i < targets.Count; i++)
                {
                    CombatUnit target = targets[i];
                    if (GUI.Button(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 24f), $"{target.UnitName} (HP {target.HP}/{target.MaxHP})"))
                    {
                        AssignPlayerAction(currentInputUnit, pendingInputActionType, target);
                        isAwaitingTargetSelection = false;
                    }

                    y += 28f;
                }

                if (GUI.Button(new Rect(panelRect.x + 12f, y, 120f, 26f), "Cancel"))
                {
                    isAwaitingTargetSelection = false;
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

    private static Font LoadDebugFont()
    {
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
