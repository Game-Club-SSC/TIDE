using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PuzzleGuardSpawner : MonoBehaviour
{
    [Header("Guard Spawn")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0.6f, 2.5f);
    [SerializeField] private Vector3 guardScale = new Vector3(0.9f, 1.2f, 0.9f);

    private readonly Dictionary<string, OverworldEnemy> activeGuards = new Dictionary<string, OverworldEnemy>();
    private readonly Dictionary<string, string> encounterIslandLookup = new Dictionary<string, string>();

    public void RefreshGuards()
    {
        CleanupDestroyedGuards();
        HashSet<string> validEncounterIds = new HashSet<string>();
        GameStateManager gsm = GameStateManager.Instance;

        List<string> clearedEncounterIds = new List<string>();
        foreach (KeyValuePair<string, OverworldEnemy> pair in activeGuards)
        {
            string encounterId = pair.Key;
            if (string.IsNullOrEmpty(encounterId))
            {
                continue;
            }

            string islandId = encounterIslandLookup.TryGetValue(encounterId, out string storedIslandId)
                ? storedIslandId
                : "default";

            if (IsEncounterCleared(islandId, encounterId))
            {
                clearedEncounterIds.Add(encounterId);
            }
        }

        for (int i = 0; i < clearedEncounterIds.Count; i++)
        {
            RemoveGuard(clearedEncounterIds[i]);
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

            string scopedIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
            encounterIslandLookup[encounterId] = scopedIslandId;
            validEncounterIds.Add(encounterId);

            if (isBoxSolved)
            {
                box.MarkSolved();

                if (IsEncounterCleared(scopedIslandId, encounterId))
                {
                    RemoveGuard(encounterId);
                    continue;
                }
            }

            if (IsEncounterCleared(scopedIslandId, encounterId))
            {
                RemoveGuard(encounterId);
                continue;
            }

            if (activeGuards.ContainsKey(encounterId) && activeGuards[encounterId] != null)
            {
                continue;
            }

            SpawnGuard(box, lockedTilePosition, encounterId, scopedIslandId);
        }

        List<string> staleEncounterIds = new List<string>();
        foreach (KeyValuePair<string, OverworldEnemy> pair in activeGuards)
        {
            if (!validEncounterIds.Contains(pair.Key))
            {
                staleEncounterIds.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleEncounterIds.Count; i++)
        {
            RemoveGuard(staleEncounterIds[i]);
        }
    }

    private void SpawnGuard(PuzzleBoxInteractable box, Vector2Int lockedTilePosition, string encounterId, string islandId)
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
            1.5f,
            8f,
            1.6f,
            6f);

        activeGuards[encounterId] = enemy;
        encounterIslandLookup[encounterId] = islandId;
    }

    private void RemoveGuard(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return;
        }

        if (!activeGuards.TryGetValue(encounterId, out OverworldEnemy guard))
        {
            return;
        }

        if (guard != null)
        {
            Destroy(guard.gameObject);
        }

        activeGuards.Remove(encounterId);
        encounterIslandLookup.Remove(encounterId);
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
            encounterIslandLookup.Remove(staleKeys[i]);
        }
    }

    private static bool IsEncounterCleared(string islandId, string encounterId)
    {
        if (IslandRestorationTracker.Instance == null || string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        string scopedIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
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
