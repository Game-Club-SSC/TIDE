using System.Collections.Generic;
using UnityEngine;

public static class TravelValidationService
{
    public sealed class ValidationResult
    {
        public bool CanTravel { get; }
        public string FailureReason { get; }
        public TeleportAnchor Destination { get; }

        public ValidationResult(bool canTravel, string failureReason, TeleportAnchor destination)
        {
            CanTravel = canTravel;
            FailureReason = failureReason;
            Destination = destination;
        }
    }

    public static ValidationResult ValidateTravel(string fromIslandId, string toIslandId)
    {
        if (string.IsNullOrEmpty(toIslandId))
        {
            return new ValidationResult(false, "Destination island id is empty.", null);
        }

        if (IslandProgressionManager.Instance == null)
        {
            return new ValidationResult(false, "Island progression manager not initialized.", null);
        }

        if (!IslandProgressionManager.Instance.IsIslandUnlocked(toIslandId))
        {
            return new ValidationResult(false, $"Destination '{toIslandId}' is not yet unlocked.", null);
        }

        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            if (!tracker.IsIslandRestored(toIslandId))
            {
                float restoration = tracker.GetRestorationPercent(toIslandId);
                return new ValidationResult(false, $"Destination '{toIslandId}' is unrestored ({restoration:F0}%).", null);
            }
        }

        TeleportAnchor dock = TeleportAnchor.FindBoatDockForIsland(toIslandId);
        if (dock == null)
        {
            return new ValidationResult(false, $"No boat dock found at '{toIslandId}'.", null);
        }

        return new ValidationResult(true, string.Empty, dock);
    }

    public static IReadOnlyList<TeleportAnchor> GetAvailableTravelDestinations(string fromIslandId)
    {
        List<TeleportAnchor> list = new List<TeleportAnchor>();
        if (IslandProgressionManager.Instance == null)
        {
            return list;
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (string.Equals(islandId, fromIslandId, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (!IslandProgressionManager.Instance.IsIslandUnlocked(islandId))
            {
                continue;
            }

            TeleportAnchor dock = TeleportAnchor.FindBoatDockForIsland(islandId);
            if (dock != null)
            {
                list.Add(dock);
            }
        }

        return list;
    }
}
