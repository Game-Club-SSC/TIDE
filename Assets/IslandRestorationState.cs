using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IslandRestorationState
{
    [SerializeField] private string islandId;
    [SerializeField] private float combatContribution;
    [SerializeField] private float puzzleContribution;
    [SerializeField] private float totalContribution;
    [SerializeField] private int combatEncountersCompleted;
    [SerializeField] private int puzzleEncountersCompleted;
    [SerializeField] private List<string> completedEncounterIds = new List<string>();

    public string IslandId => islandId;
    public float CombatContribution => combatContribution;
    public float PuzzleContribution => puzzleContribution;
    public float TotalContribution => totalContribution;
    public int CombatEncountersCompleted => combatEncountersCompleted;
    public int PuzzleEncountersCompleted => puzzleEncountersCompleted;
    public IReadOnlyList<string> CompletedEncounterIds => completedEncounterIds;
    public float RestorationPercent => Mathf.Clamp01(totalContribution) * 100f;
    public bool IsIslandRestored => totalContribution >= 1f;

    public IslandRestorationState(string islandId)
    {
        this.islandId = islandId;
        combatContribution = 0f;
        puzzleContribution = 0f;
        totalContribution = 0f;
        combatEncountersCompleted = 0;
        puzzleEncountersCompleted = 0;
    }

    public bool HasCompleted(string encounterId)
    {
        return !string.IsNullOrEmpty(encounterId) && completedEncounterIds.Contains(encounterId);
    }

    public void RecordCompletion(string encounterId, EncounterType type, float value)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return;
        }

        if (HasCompleted(encounterId))
        {
            return;
        }

        completedEncounterIds.Add(encounterId);

        if (type == EncounterType.Combat)
        {
            combatContribution += value;
            combatEncountersCompleted++;
        }
        else
        {
            puzzleContribution += value;
            puzzleEncountersCompleted++;
        }

        totalContribution = Mathf.Clamp01(combatContribution + puzzleContribution);
    }

    public void Reset()
    {
        combatContribution = 0f;
        puzzleContribution = 0f;
        totalContribution = 0f;
        combatEncountersCompleted = 0;
        puzzleEncountersCompleted = 0;
        completedEncounterIds.Clear();
    }
}
