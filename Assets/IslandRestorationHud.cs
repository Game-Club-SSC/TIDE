using UnityEngine;
using UnityEngine.UI;

public class IslandRestorationHud : MonoBehaviour
{
    private const string CanvasName = "RestorationHudCanvas";
    private const string LabelName = "RestorationLabel";

    [Header("Display")]
    [SerializeField] private string islandId = "";
    [SerializeField] private string islandName = "Island";
    [SerializeField] private string viceName = "";
    [SerializeField] private Vector2 labelPosition = new Vector2(-24f, -24f);
    [SerializeField] private int fontSize = 22;

    private Text restorationLabel;
    private IslandRestorationTracker tracker;

    private void Awake()
    {
        EnsureLabel();
    }

    private void OnEnable()
    {
        TryFindTracker();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromTracker();
    }

    private void Update()
    {
        if (tracker == null)
        {
            TryFindTracker();
        }
    }

    private void TryFindTracker()
    {
        if (tracker != null)
        {
            return;
        }

        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnRestorationChanged += HandleRestorationChanged;
            tracker.OnIslandRestored += HandleIslandRestored;
        }
    }

    private void UnsubscribeFromTracker()
    {
        if (tracker != null)
        {
            tracker.OnRestorationChanged -= HandleRestorationChanged;
            tracker.OnIslandRestored -= HandleIslandRestored;
        }

        tracker = null;
    }

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        if (changedIslandId != ResolveTargetIslandId())
        {
            return;
        }

        RefreshDisplay();
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        if (restoredIslandId != ResolveTargetIslandId())
        {
            return;
        }

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (restorationLabel == null)
        {
            return;
        }

        string targetIsland = ResolveTargetIslandId();

        if (tracker == null)
        {
            restorationLabel.text = "Restoration: --";
            return;
        }

        IslandRestorationState state = tracker.GetRestorationState(targetIsland);
        float percent = state.RestorationPercent;

        string header = islandName;
        if (!string.IsNullOrEmpty(viceName))
        {
            header += $" — Vice: {viceName}";
        }

        string bossLine = state.BossContribution > 0f
            ? $"\nBoss: {state.BossContribution * 100:F0}%"
            : string.Empty;

        restorationLabel.text =
            $"{header}\n" +
            $"Restoration: {percent:F1}%\n" +
            $"Combat: {state.CombatContribution * 100:F0}% ({state.CombatEncountersCompleted} cleared)\n" +
            $"Puzzle: {state.PuzzleContribution * 100:F0}% ({state.PuzzleEncountersCompleted} solved)" +
            bossLine;

        if (state.IsIslandRestored)
        {
            restorationLabel.color = new Color(0.3f, 1f, 0.3f);
        }
        else
        {
            restorationLabel.color = Color.white;
        }
    }

    private void EnsureLabel()
    {
        Transform existingCanvas = transform.Find(CanvasName);
        if (existingCanvas != null)
        {
            Text existingLabel = existingCanvas.Find(LabelName)?.GetComponent<Text>();
            if (existingLabel != null)
            {
                restorationLabel = existingLabel;
                return;
            }
        }

        GameObject canvasObject = new GameObject(CanvasName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject labelObject = new GameObject(LabelName, typeof(RectTransform));
        labelObject.transform.SetParent(canvasObject.transform, false);

        restorationLabel = labelObject.AddComponent<Text>();
        RectTransform labelRect = restorationLabel.rectTransform;
        labelRect.anchorMin = new Vector2(1f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(1f, 1f);
        labelRect.anchoredPosition = labelPosition;
        labelRect.sizeDelta = new Vector2(400f, 140f);

        restorationLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        restorationLabel.fontSize = fontSize;
        restorationLabel.alignment = TextAnchor.UpperRight;
        restorationLabel.color = Color.white;
        restorationLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        restorationLabel.verticalOverflow = VerticalWrapMode.Overflow;
        restorationLabel.raycastTarget = false;
    }

    private string ResolveTargetIslandId()
    {
        if (!string.IsNullOrEmpty(islandId))
        {
            return IslandThemeRegistry.ResolveIslandId(islandId);
        }

        return IslandThemeRegistry.GetActiveIslandId();
    }
}
