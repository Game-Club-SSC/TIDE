using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AncientTextLogUI : MonoBehaviour
{
    private const string CanvasName = "AncientTextLogCanvas";

    private Canvas canvas;
    private GameObject panel;
    private Text titleText;
    private Text bodyText;
    private Text footerText;
    private bool isVisible;
    private bool wasPlayerMoveEnabled;
    private IsometricPlayer cachedPlayer;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureCanvas();
        Hide();
    }

    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.Space))
        {
            Hide();
        }
    }

    public void ShowEntry(string textId, string title, string body, bool newlyDiscovered)
    {
        EnsureCanvas();

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? textId : title;
        }

        if (bodyText != null)
        {
            bodyText.text = string.IsNullOrEmpty(body)
                ? "No readable inscription remains on this fragment."
                : body;
        }

        if (footerText != null)
        {
            StringBuilder footerBuilder = new StringBuilder();
            if (newlyDiscovered)
            {
                footerBuilder.Append("New ancient text discovered.");
            }
            else
            {
                footerBuilder.Append("Previously discovered entry.");
            }

            GameStateManager gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                string[] discoveredIds = gsm.GetDiscoveredAncientTextIds();
                footerBuilder.Append($"  Found: {discoveredIds.Length}");
            }

            footerBuilder.Append("  [Enter/Esc] Close");
            footerText.text = footerBuilder.ToString();
        }

        isVisible = true;
        if (panel != null)
        {
            panel.SetActive(true);
        }

        LockPlayerMovement(true);
    }

    public void ShowDiscoveredLog()
    {
        EnsureCanvas();
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        string[] discoveredIds = gsm.GetDiscoveredAncientTextIds();
        if (discoveredIds.Length == 0)
        {
            ShowEntry("log_empty", "Ancient Texts", "No texts have been discovered yet.", false);
            return;
        }

        StringBuilder listBody = new StringBuilder();
        for (int i = 0; i < discoveredIds.Length; i++)
        {
            string id = discoveredIds[i];
            if (gsm.TryGetAncientTextEntry(id, out string entryTitle, out _, out bool discovered) && discovered)
            {
                listBody.Append($"- {entryTitle}");
            }
            else
            {
                listBody.Append($"- {id}");
            }

            if (i < discoveredIds.Length - 1)
            {
                listBody.Append('\n');
            }
        }

        ShowEntry("log_index", "Ancient Text Archive", listBody.ToString(), false);
    }

    private void Hide()
    {
        isVisible = false;
        if (panel != null)
        {
            panel.SetActive(false);
        }

        LockPlayerMovement(false);
    }

    private void LockPlayerMovement(bool locked)
    {
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<IsometricPlayer>();
        }

        if (cachedPlayer == null)
        {
            return;
        }

        if (locked)
        {
            wasPlayerMoveEnabled = cachedPlayer.canMove;
            cachedPlayer.canMove = false;
        }
        else
        {
            if (GameStateManager.Instance != null
                && GameStateManager.Instance.currentState == GameStateManager.GameState.Exploration
                && !GameStateManager.Instance.IsTransitioning)
            {
                cachedPlayer.canMove = wasPlayerMoveEnabled;
            }
        }
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName);
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        panel = CreatePanel(canvasObject.transform);
    }

    private GameObject CreatePanel(Transform parent)
    {
        GameObject panelObject = new GameObject("AncientTextPanel", typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.12f, 0.12f);
        panelRect.anchorMax = new Vector2(0.88f, 0.88f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);
        panelImage.raycastTarget = true;

        titleText = CreateLabel(panelRect, "Title", 30, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.7f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0.04f, 0.82f);
        titleRect.anchorMax = new Vector2(0.96f, 0.96f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        bodyText = CreateLabel(panelRect, "Body", 22, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.9f, 0.94f, 1f));
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0.06f, 0.2f);
        bodyRect.anchorMax = new Vector2(0.94f, 0.8f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;

        footerText = CreateLabel(panelRect, "Footer", 16, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(0.74f, 0.82f, 0.92f));
        RectTransform footerRect = footerText.rectTransform;
        footerRect.anchorMin = new Vector2(0.04f, 0.05f);
        footerRect.anchorMax = new Vector2(0.96f, 0.16f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = Vector2.zero;

        return panelObject;
    }

    private static Text CreateLabel(Transform parent, string name, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }
}
