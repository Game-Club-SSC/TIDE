using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class GroundManager : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color defaultColor = new Color(1f, 0.45f, 0.12f);
    [SerializeField] private Color restoredColor = Color.white;
    
    [Header("Island Settings")]
    [Tooltip("Leave empty to respond to all puzzle completions. Set to respond only to specific island.")]
    [SerializeField] private string targetIslandId = "";

    private Renderer cachedRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void Start()
    {
        ApplyColor(defaultColor);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompleted;
            GameStateManager.Instance.OnPuzzleCompleted += HandlePuzzleCompleted;
        }
        else
        {
            StartCoroutine(WaitForGameStateManager());
        }

        if (IslandRestorationTracker.Instance != null)
        {
            IslandRestorationTracker.Instance.OnIslandRestored -= HandleIslandRestored;
            IslandRestorationTracker.Instance.OnIslandRestored += HandleIslandRestored;
        }
        else
        {
            StartCoroutine(WaitForRestorationTracker());
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnPuzzleCompleted -= HandlePuzzleCompleted;
        }

        if (IslandRestorationTracker.Instance != null)
        {
            IslandRestorationTracker.Instance.OnIslandRestored -= HandleIslandRestored;
        }
    }

    private IEnumerator WaitForGameStateManager()
    {
        while (GameStateManager.Instance == null)
        {
            yield return null;
        }
        GameStateManager.Instance.OnPuzzleCompleted += HandlePuzzleCompleted;
    }

    private IEnumerator WaitForRestorationTracker()
    {
        while (IslandRestorationTracker.Instance == null)
        {
            yield return null;
        }
        IslandRestorationTracker.Instance.OnIslandRestored += HandleIslandRestored;
    }

    public void SetRestoredColor()
    {
        ApplyColor(restoredColor);
    }

    public void SetDefaultColor()
    {
        ApplyColor(defaultColor);
    }

    public void SetColors(Color defaultCol, Color restoredCol)
    {
        defaultColor = defaultCol;
        restoredColor = restoredCol;
        ApplyColor(defaultColor);
    }

    private void ApplyColor(Color color)
    {
        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        cachedRenderer.SetPropertyBlock(propertyBlock);
    }

    private void HandlePuzzleCompleted()
    {
        if (string.IsNullOrEmpty(targetIslandId))
        {
            SetRestoredColor();
            Debug.Log("[GroundManager] Ground changed to white after puzzle completion.");
        }
    }

    private void HandleIslandRestored(string islandId)
    {
        if (string.IsNullOrEmpty(targetIslandId) || targetIslandId == islandId)
        {
            SetRestoredColor();
            Debug.Log($"[GroundManager] Island '{islandId}' restored - ground changed to white.");
        }
    }
}
