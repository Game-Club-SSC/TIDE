using System.Collections.Generic;
using UnityEngine;

public enum NarrativeAct
{
    Any,
    ActI,
    ActII,
    ActIII
}

[DisallowMultipleComponent]
public class AncientTextDiscoverable : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private AncientTextData textData;

    [Header("Act Gating")]
    [SerializeField] private NarrativeAct requiredAct = NarrativeAct.Any;
    [SerializeField] private string targetIslandId = string.Empty;
    [Tooltip("If false, the discoverable is hidden until the act matches. If true, it remains visible but cannot be read.")]
    [SerializeField] private bool disableOnActMismatch = true;

    [Header("Visual")]
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private Color hiddenColor = new Color(0.18f, 0.16f, 0.18f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.32f, 0.28f, 0.22f, 1f);

    private bool lastActMatched = true;
    private bool lastIslandMatched = true;
    private Collider interactionCollider;

    public AncientTextData TextData => textData;
    public NarrativeAct RequiredAct => requiredAct;
    public string TargetIslandId => targetIslandId;

    private void Awake()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponent<Renderer>();
        }

        interactionCollider = GetComponent<Collider>();
        ApplyVisualState();
    }

    private void Start()
    {
        RegisterTextData();
        RefreshGate();
    }

    private void Update()
    {
        RefreshGate();
    }

    public void RefreshGate()
    {
        bool actMatched = IsActMatched();
        bool islandMatched = IsIslandMatched();
        bool shouldShow = (!disableOnActMismatch || actMatched) && islandMatched;

        if (actMatched != lastActMatched || islandMatched != lastIslandMatched)
        {
            lastActMatched = actMatched;
            lastIslandMatched = islandMatched;
            ApplyVisibility(shouldShow);
        }
    }

    public bool IsActMatched()
    {
        if (requiredAct == NarrativeAct.Any)
        {
            return true;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return false;
        }

        return ResolveActForState(gsm) == ResolveConfiguredAct(requiredAct);
    }

    public bool IsIslandMatched()
    {
        if (string.IsNullOrEmpty(targetIslandId))
        {
            return true;
        }

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (progressionManager == null)
        {
            return false;
        }

        string resolvedTarget = IslandThemeRegistry.ResolveIslandId(targetIslandId);
        string resolvedActive = IslandThemeRegistry.ResolveIslandId(progressionManager.ActiveIslandId);
        return string.Equals(resolvedTarget, resolvedActive, System.StringComparison.Ordinal);
    }

    private void ApplyVisibility(bool shouldShow)
    {
        if (visualRenderer != null)
        {
            visualRenderer.enabled = shouldShow;
        }

        if (interactionCollider != null)
        {
            interactionCollider.enabled = shouldShow;
        }
    }

    private void ApplyVisualState()
    {
        if (visualRenderer == null || visualRenderer.material == null)
        {
            return;
        }

        if (!lastActMatched)
        {
            visualRenderer.material.color = lockedColor;
        }
        else if (!lastIslandMatched)
        {
            visualRenderer.material.color = hiddenColor;
        }
        else
        {
            visualRenderer.material.color = Color.white;
        }
    }

    private void RegisterTextData()
    {
        if (textData == null || !textData.IsValid())
        {
            return;
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.RegisterAncientText(textData.textId, textData.title, textData.body);
        }
    }

    public static GameStateManager.StoryAct ResolveActForState(GameStateManager gsm)
    {
        if (gsm == null)
        {
            return GameStateManager.StoryAct.ActI;
        }

        return gsm.CurrentStoryAct;
    }

    public static GameStateManager.StoryAct ResolveConfiguredAct(NarrativeAct narrativeAct)
    {
        switch (narrativeAct)
        {
            case NarrativeAct.ActI: return GameStateManager.StoryAct.ActI;
            case NarrativeAct.ActII: return GameStateManager.StoryAct.ActII;
            case NarrativeAct.ActIII: return GameStateManager.StoryAct.ActIII;
            default: return GameStateManager.StoryAct.ActI;
        }
    }

    public static NarrativeAct DetermineActForIsland(string islandId)
    {
        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        int lastIndex = Mathf.Max(0, progressionOrder.Count - 1);
        int midpoint = Mathf.Max(0, lastIndex / 2);
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            if (string.Equals(progressionOrder[i], islandId, System.StringComparison.Ordinal))
            {
                if (i <= midpoint / 2)
                {
                    return NarrativeAct.ActI;
                }

                if (i < lastIndex)
                {
                    return NarrativeAct.ActII;
                }

                return NarrativeAct.ActIII;
            }
        }

        return NarrativeAct.Any;
    }
}
