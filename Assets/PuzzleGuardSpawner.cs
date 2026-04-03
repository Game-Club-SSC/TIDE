using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PuzzleGuardSpawner : MonoBehaviour
{
    [Header("Guard Spawn")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.6f, 2.5f);
    [SerializeField] private Vector3 guardScale = new Vector3(0.9f, 1.2f, 0.9f);

    private readonly Dictionary<string, OverworldEnemy> activeGuards = new Dictionary<string, OverworldEnemy>();
    private readonly Dictionary<string, GuardTrackingInfo> guardTrackingLookup = new Dictionary<string, GuardTrackingInfo>();

    private struct GuardTrackingInfo
    {
        public readonly string IslandId;
        public readonly string EncounterId;

        public GuardTrackingInfo(string islandId, string encounterId)
        {
            IslandId = islandId;
            EncounterId = encounterId;
        }
    }

    public void RefreshGuards()
    {
        CleanupDestroyedGuards();
        HashSet<string> validGuardKeys = new HashSet<string>();
        GameStateManager gsm = GameStateManager.Instance;

        List<string> clearedGuardKeys = new List<string>();
        List<string> missingTrackingGuardKeys = new List<string>();
        foreach (KeyValuePair<string, OverworldEnemy> pair in activeGuards)
        {
            string guardKey = pair.Key;
            if (string.IsNullOrEmpty(guardKey))
            {
                continue;
            }

            if (!guardTrackingLookup.TryGetValue(guardKey, out GuardTrackingInfo trackingInfo))
            {
                missingTrackingGuardKeys.Add(guardKey);
                continue;
            }

            if (IsEncounterCleared(trackingInfo.IslandId, trackingInfo.EncounterId))
            {
                clearedGuardKeys.Add(guardKey);
            }
        }

        for (int i = 0; i < clearedGuardKeys.Count; i++)
        {
            RemoveGuard(clearedGuardKeys[i]);
        }

        for (int i = 0; i < missingTrackingGuardKeys.Count; i++)
        {
            RemoveGuard(missingTrackingGuardKeys[i]);
        }

        PuzzleBoxInteractable[] puzzleBoxes = FindObjectsByType<PuzzleBoxInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < puzzleBoxes.Length; i++)
        {
            PuzzleBoxInteractable box = puzzleBoxes[i];
            if (box == null)
            {
                continue;
            }

            if (!box.TryGetLockedTileInfo(out Vector2Int lockedTilePosition, out string encounterId, out string islandId))
            {
                continue;
            }

            if (string.IsNullOrEmpty(encounterId))
            {
                continue;
            }

            string boxId = box.GetPuzzleBoxId();
            bool isBoxSolved = gsm != null && gsm.IsPuzzleBoxSolved(boxId);
            if (!box.isActiveAndEnabled && !isBoxSolved)
            {
                continue;
            }

            string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
            string stableBoxId = GetStableBoxIdentifier(box, lockedTilePosition);
            string guardKey = BuildGuardKey(scopedIslandId, encounterId, stableBoxId);
            validGuardKeys.Add(guardKey);

            if (isBoxSolved)
            {
                box.MarkSolved();

                if (IsEncounterCleared(scopedIslandId, encounterId))
                {
                    RemoveGuard(guardKey);
                    continue;
                }
            }

            if (IsEncounterCleared(scopedIslandId, encounterId))
            {
                RemoveGuard(guardKey);
                continue;
            }

            if (activeGuards.ContainsKey(guardKey) && activeGuards[guardKey] != null)
            {
                continue;
            }

            SpawnGuard(box, lockedTilePosition, encounterId, scopedIslandId, guardKey);
        }

        List<string> staleGuardKeys = new List<string>();
        foreach (KeyValuePair<string, OverworldEnemy> pair in activeGuards)
        {
            if (!validGuardKeys.Contains(pair.Key))
            {
                staleGuardKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleGuardKeys.Count; i++)
        {
            RemoveGuard(staleGuardKeys[i]);
        }
    }

    private void SpawnGuard(PuzzleBoxInteractable box, Vector2Int lockedTilePosition, string encounterId, string islandId, string guardKey)
    {
        Vector3 anchorPosition = box.GetOverlayBoardCenterWorldPosition() + GetTileOffset(lockedTilePosition) + spawnOffset;

        GameObject guardObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        guardObject.name = $"PuzzleGuard_{encounterId}";
        guardObject.transform.position = anchorPosition;
        guardObject.transform.localScale = guardScale;
        guardObject.layer = box.gameObject.layer;

        Renderer renderer = guardObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.89f, 0.38f, 0.25f);
        }

        Rigidbody body = guardObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = guardObject.AddComponent<Rigidbody>();
        }

        body.freezeRotation = true;

        EncounterConfig encounterConfig = LoadEncounterById(encounterId);
        if (encounterConfig == null)
        {
            encounterConfig = LoadEncounterById("encounter_imp_trio");
        }

        OverworldEnemy enemy = guardObject.AddComponent<OverworldEnemy>();
        enemy.ConfigureAsPuzzleGuard(
            encounterConfig,
            islandId,
            encounterId,
            box.GetSealedTileCombatRestorationValue(),
            anchorPosition,
            8f,
            1.6f,
            6f);

        activeGuards[guardKey] = enemy;
        guardTrackingLookup[guardKey] = new GuardTrackingInfo(islandId, encounterId);
    }

    private void RemoveGuard(string guardKey)
    {
        if (string.IsNullOrEmpty(guardKey))
        {
            return;
        }

        if (activeGuards.TryGetValue(guardKey, out OverworldEnemy guard) && guard != null)
        {
            Destroy(guard.gameObject);
        }

        activeGuards.Remove(guardKey);
        guardTrackingLookup.Remove(guardKey);
    }

    private void CleanupDestroyedGuards()
    {
        List<string> staleKeys = new List<string>();
        foreach (KeyValuePair<string, OverworldEnemy> pair in activeGuards)
        {
            if (pair.Value == null)
            {
                staleKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            activeGuards.Remove(staleKeys[i]);
            guardTrackingLookup.Remove(staleKeys[i]);
        }
    }

    private static string BuildGuardKey(string islandId, string encounterId, string boxId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return string.Empty;
        }

        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        string stableBoxId = string.IsNullOrEmpty(boxId) ? "unknown_box" : boxId;
        return $"{scopedIslandId}::{encounterId}::{stableBoxId}";
    }

    private static string GetStableBoxIdentifier(PuzzleBoxInteractable box, Vector2Int lockedTilePosition)
    {
        if (box == null)
        {
            return $"fallback_{lockedTilePosition.x}_{lockedTilePosition.y}";
        }

        string boxId = box.GetPuzzleBoxId();
        if (!string.IsNullOrEmpty(boxId))
        {
            return $"{boxId}@{lockedTilePosition.x}_{lockedTilePosition.y}";
        }

        string scenePath = box.gameObject.scene.path;
        if (string.IsNullOrEmpty(scenePath))
        {
            scenePath = box.gameObject.scene.name;
        }

        if (string.IsNullOrEmpty(scenePath))
        {
            scenePath = "unknown_scene";
        }

        string hierarchyPath = BuildHierarchyPath(box.transform);
        return $"{scenePath}/{hierarchyPath}@{lockedTilePosition.x}_{lockedTilePosition.y}";
    }

    private static string BuildHierarchyPath(Transform current)
    {
        if (current == null)
        {
            return "unknown_transform";
        }

        List<string> pathParts = new List<string>();
        Transform walker = current;
        while (walker != null)
        {
            int siblingIndex = walker.GetSiblingIndex();
            pathParts.Add($"{walker.name}[{siblingIndex}]");
            walker = walker.parent;
        }

        pathParts.Reverse();
        return string.Join("/", pathParts);
    }

    private static bool IsEncounterCleared(string islandId, string encounterId)
    {
        if (IslandRestorationTracker.Instance == null || string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        string scopedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        return IslandRestorationTracker.Instance.HasClearedEncounter(scopedIslandId, encounterId);
    }

    private static EncounterConfig LoadEncounterById(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return null;
        }

        EncounterConfig direct = Resources.Load<EncounterConfig>($"Encounters/{encounterId}");
        if (direct != null)
        {
            return direct;
        }

        EncounterConfig[] allEncounters = Resources.LoadAll<EncounterConfig>("Encounters");
        for (int i = 0; i < allEncounters.Length; i++)
        {
            EncounterConfig candidate = allEncounters[i];
            if (candidate != null && candidate.encounterId == encounterId)
            {
                return candidate;
            }
        }

        return null;
    }

    private static Vector3 GetTileOffset(Vector2Int tilePosition)
    {
        const float tileSpacing = 2.25f;
        float xOffset = (tilePosition.x - 1) * tileSpacing;
        float zOffset = (1 - tilePosition.y) * tileSpacing;
        return new Vector3(xOffset, 0f, zOffset);
    }
}
