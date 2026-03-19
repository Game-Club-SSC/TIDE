using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private BattlePhase currentPhase;

    public BattlePhase CurrentPhase => currentPhase;

    private List<CombatUnit> allyUnits = new List<CombatUnit>();
    private List<CombatUnit> enemyUnits = new List<CombatUnit>();

    public IReadOnlyList<CombatUnit> AllyUnits => allyUnits;
    public IReadOnlyList<CombatUnit> EnemyUnits => enemyUnits;

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
        if (unit == null) return;

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
        if (!autoAdvancePhases || !hasActivePhase || IsTerminalPhase(currentPhase))
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
        UpdatePhaseLabel();
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
        labelRect.sizeDelta = new Vector2(520f, 180f);

        phaseLabel.font = LoadDebugFont();
        phaseLabel.fontSize = 24;
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
        string autoAdvanceState = autoAdvancePhases ? "On" : "Off";
        int alliesAlive = GetAliveUnits(CombatUnit.UnitType.Ally).Count;
        int enemiesAlive = GetAliveUnits(CombatUnit.UnitType.Enemy).Count;
        phaseLabel.text =
            $"Battle Phase: {phaseName}\n" +
            $"Allies: {allyUnits.Count} ({alliesAlive} alive)\n" +
            $"Enemies: {enemyUnits.Count} ({enemiesAlive} alive)\n" +
            $"Next: {advancePhaseKey} | Victory: {victoryKey} | Defeat: {defeatKey}";
    }

    private static Font LoadDebugFont()
    {
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
