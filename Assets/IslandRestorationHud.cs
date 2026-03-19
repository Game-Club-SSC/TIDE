using UnityEngine;
using UnityEngine.UI;

public class IslandRestorationHud : MonoBehaviour
{
    private const string CanvasName = "RestorationHudCanvas";
    private const string LabelName = "RestorationLabel";

    [Header("Display")]
    [SerializeField] private string islandId = "";
    [SerializeField] private Vector2 labelPosition = new Vector2(-24f, -24f);
    [SerializeField] private int fontSize = 22;
    [SerializeField] private bool showDetailedBreakdown;

    private Text restorationLabel;
    private IslandRestorationTracker tracker;

    private void Awake()
    {
        EnsureLabel();
    }

    private void OnEnable()
    {
        tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            tracker.OnRestorationChanged += HandleRestorationChanged;
            tracker.OnIslandRestored += HandleIslandRestored;
        }

        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (tracker != null)
        {
            tracker.OnRestorationChanged -= HandleRestorationChanged;
            tracker.OnIslandRestored -= HandleIslandRestored;
        }

        tracker = null;
    }

    private void Update()
    {
        if (tracker == null)
        {
            tracker = IslandRestorationTracker.Instance;
            if (tracker != null)
            {
                tracker.OnRestorationChanged += HandleRestorationChanged;
                tracker.OnIslandRestored += HandleIslandRestored;
                RefreshDisplay();
            }
        }
    }

    private void HandleRestorationChanged(string changedIslandId, float progress)
    {
        if (!string.IsNullOrEmpty(islandId) && changedIslandId != islandId)
        {
            return;
        }

        RefreshDisplay();
    }

    private void HandleIslandRestored(string restoredIslandId)
    {
        if (!string.IsNullOrEmpty(islandId) && restoredIslandId != islandId)
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

        string targetIsland = string.IsNullOrEmpty(islandId) ? "default" : islandId;

        if (tracker == null)
        {
            restorationLabel.text = "Restoration: --";
            return;
        }

        IslandRestorationState state = tracker.GetRestorationState(targetIsland);
        float percent = state.RestorationPercent;

        if (showDetailedBreakdown)
        {
            restorationLabel.text =
                $"Island Restoration: {percent:F1}%\n" +
                $"Combat: {state.CombatContribution * 100:F0}% ({state.CombatEncountersCompleted} cleared)\n" +
                $"Puzzle: {state.PuzzleContribution * 100:F0}% ({state.PuzzleEncountersCompleted} solved)";
        }
        else
        {
            restorationLabel.text = $"Restoration: {percent:F1}%";
        }

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
        labelRect.sizeDelta = new Vector2(400f, 100f);

        restorationLabel.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        restorationLabel.fontSize = fontSize;
        restorationLabel.alignment = TextAnchor.UpperRight;
        restorationLabel.color = Color.white;
        restorationLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        restorationLabel.verticalOverflow = VerticalWrapMode.Overflow;
        restorationLabel.raycastTarget = false;
    }
}
