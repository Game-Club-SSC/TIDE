using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class IslandRestorationStateSnapshot
{
    public string islandId;
    public float combatContribution;
    public float puzzleContribution;
    public float totalContribution;
    public int combatEncountersCompleted;
    public int puzzleEncountersCompleted;
    public List<string> completedEncounterIds = new List<string>();
}

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

    public IslandRestorationStateSnapshot CaptureSnapshot()
    {
        IslandRestorationStateSnapshot snapshot = new IslandRestorationStateSnapshot
        {
            islandId = this.islandId,
            combatContribution = this.combatContribution,
            puzzleContribution = this.puzzleContribution,
            totalContribution = this.totalContribution,
            combatEncountersCompleted = this.combatEncountersCompleted,
            puzzleEncountersCompleted = this.puzzleEncountersCompleted,
            completedEncounterIds = new List<string>(completedEncounterIds)
        };

        return snapshot;
    }

    public void ApplySnapshot(IslandRestorationStateSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(snapshot.islandId))
        {
            islandId = snapshot.islandId;
        }

        combatContribution = Mathf.Max(0f, snapshot.combatContribution);
        puzzleContribution = Mathf.Max(0f, snapshot.puzzleContribution);
        totalContribution = Mathf.Clamp01(combatContribution + puzzleContribution);
        combatEncountersCompleted = Mathf.Max(0, snapshot.combatEncountersCompleted);
        puzzleEncountersCompleted = Mathf.Max(0, snapshot.puzzleEncountersCompleted);

        completedEncounterIds.Clear();
        if (snapshot.completedEncounterIds != null)
        {
            for (int i = 0; i < snapshot.completedEncounterIds.Count; i++)
            {
                string encounterId = snapshot.completedEncounterIds[i];
                if (!string.IsNullOrEmpty(encounterId) && !completedEncounterIds.Contains(encounterId))
                {
                    completedEncounterIds.Add(encounterId);
                }
            }
        }
    }
}
