using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TeleportAnchor : MonoBehaviour
{
    [SerializeField] private string anchorId;
    [SerializeField] private string islandId = IslandThemeRegistry.DefaultIslandId;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private bool isSceneEntrance = true;
    [SerializeField] private bool isBoatDock;

    public string AnchorId => anchorId;
    public string IslandId => islandId;
    public Vector3 SpawnPosition => spawnPosition;
    public bool IsSceneEntrance => isSceneEntrance;
    public bool IsBoatDock => isBoatDock;

    public event Action<TeleportAnchor> OnAnchorUsed;

    private static readonly Dictionary<string, TeleportAnchor> anchorsById = new Dictionary<string, TeleportAnchor>();
    private static readonly Dictionary<string, List<TeleportAnchor>> anchorsByIsland = new Dictionary<string, List<TeleportAnchor>>();

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

        return anchorsById.TryGetValue(anchorId, out TeleportAnchor anchor) ? anchor : null;
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
