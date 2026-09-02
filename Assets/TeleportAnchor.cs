using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class TeleportAnchor : MonoBehaviour
{
    [SerializeField] internal string anchorId;
    [SerializeField] internal string islandId = IslandThemeRegistry.DefaultIslandId;
    [SerializeField] internal Vector3 spawnPosition = Vector3.zero;
    [SerializeField] internal bool isSceneEntrance = true;
    [SerializeField] internal bool isBoatDock;

    public string AnchorId => anchorId;
    public string IslandId => islandId;
    public Vector3 SpawnPosition => spawnPosition;
    public bool IsSceneEntrance => isSceneEntrance;
    public bool IsBoatDock => isBoatDock;

    public event Action<TeleportAnchor> OnAnchorUsed;

    private static readonly Dictionary<string, TeleportAnchor> anchorsById = new Dictionary<string, TeleportAnchor>();
    private static readonly Dictionary<string, List<TeleportAnchor>> anchorsByIsland = new Dictionary<string, List<TeleportAnchor>>();
    private static bool sceneUnloadHooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitStatics()
    {
        anchorsById.Clear();
        anchorsByIsland.Clear();
        if (!sceneUnloadHooked)
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            sceneUnloadHooked = true;
        }
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        anchorsById.Clear();
        anchorsByIsland.Clear();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(anchorId))
        {
            anchorId = $"anchor_{islandId}_{GetInstanceID()}";
        }

        RegisterAnchor(this);
    }

    private void OnDisable()
    {
        UnregisterAnchor(this);
    }

    /// <summary>
    /// Configures the anchor's registration keys and re-registers it.
    /// Runtime-created anchors must use this instead of assigning fields
    /// directly: OnEnable registers the anchor before field assignments run
    /// (play mode) or never runs at all (edit-mode verification), so direct
    /// field writes leave the registry keyed on stale values.
    /// </summary>
    public void Configure(string newAnchorId, string newIslandId, Vector3 newSpawnPosition, bool newIsBoatDock, bool newIsSceneEntrance = true)
    {
        UnregisterAnchor(this);

        islandId = string.IsNullOrEmpty(newIslandId) ? IslandThemeRegistry.DefaultIslandId : newIslandId;
        anchorId = string.IsNullOrEmpty(newAnchorId) ? $"anchor_{islandId}_{GetInstanceID()}" : newAnchorId;
        spawnPosition = newSpawnPosition;
        isBoatDock = newIsBoatDock;
        isSceneEntrance = newIsSceneEntrance;

        RegisterAnchor(this);
    }

    public bool TryTeleport(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        target.position = spawnPosition;
        OnAnchorUsed?.Invoke(this);
        return true;
    }

    public static TeleportAnchor FindAnchor(string anchorId)
    {
        if (string.IsNullOrEmpty(anchorId))
        {
            return null;
        }

        if (!anchorsById.TryGetValue(anchorId, out TeleportAnchor anchor))
        {
            return null;
        }

        // A destroyed anchor can linger in the registry when its OnDisable never
        // ran (edit-mode DestroyImmediate). Drop the stale entry instead of
        // handing out a dead reference.
        if (anchor == null)
        {
            anchorsById.Remove(anchorId);
            return null;
        }

        return anchor;
    }

    public static TeleportAnchor FindBoatDockForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return null;
        }

        if (!anchorsByIsland.TryGetValue(islandId, out List<TeleportAnchor> list))
        {
            return null;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].isBoatDock)
            {
                return list[i];
            }
        }

        return null;
    }

    public static IReadOnlyList<TeleportAnchor> FindAnchorsForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return Array.Empty<TeleportAnchor>();
        }

        return anchorsByIsland.TryGetValue(islandId, out List<TeleportAnchor> list)
            ? (IReadOnlyList<TeleportAnchor>)list
            : Array.Empty<TeleportAnchor>();
    }

    public static void RegisterAnchor(TeleportAnchor anchor)
    {
        if (anchor == null || string.IsNullOrEmpty(anchor.anchorId))
        {
            return;
        }

        anchorsById[anchor.anchorId] = anchor;
        if (!anchorsByIsland.TryGetValue(anchor.islandId, out List<TeleportAnchor> list))
        {
            list = new List<TeleportAnchor>();
            anchorsByIsland[anchor.islandId] = list;
        }
        if (!list.Contains(anchor))
        {
            list.Add(anchor);
        }
    }

    public static void UnregisterAnchor(TeleportAnchor anchor)
    {
        if (anchor == null || string.IsNullOrEmpty(anchor.anchorId))
        {
            return;
        }

        anchorsById.Remove(anchor.anchorId);
        if (anchorsByIsland.TryGetValue(anchor.islandId, out List<TeleportAnchor> list))
        {
            list.Remove(anchor);
        }
    }

    public static void ClearRegistryForDebug()
    {
        anchorsById.Clear();
        anchorsByIsland.Clear();
    }
}
