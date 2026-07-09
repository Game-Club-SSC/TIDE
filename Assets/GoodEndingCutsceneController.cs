using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GoodEndingCutsceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private CanvasGroup partyCanvasGroup;
    [SerializeField] private CanvasGroup creditsCanvasGroup;
    [SerializeField] private Text creditsText;

    [Header("Timing")]
    [SerializeField] private float introFadeSeconds = 1.2f;
    [SerializeField] private float partyHoldSeconds = 3.0f;
    [SerializeField] private float acceptanceFadeSeconds = 2.5f;
    [SerializeField] private float creditsFadeInSeconds = 1.5f;
    [SerializeField] private float creditsHoldSeconds = 8.0f;
    [SerializeField] private float creditsScrollPixelsPerSecond = 32f;
    [SerializeField] private float exitFadeSeconds = 2.0f;

    [Header("Credits Content")]
    [SerializeField] private string creditsHeading = "TIDE";
    [SerializeField]
    private string[] creditsLines =
    {
        "Designed and built by the TIDE team.",
        "Special thanks to all playtest contributors.",
        "Music, sound, and art placeholders by the dev team.",
        "Engine: Unity 6",
        "Thank you for playing."
    };

    [Header("Exit")]
    [SerializeField] private string exitSceneName = "";

    public bool IsCutsceneActive { get; private set; }
    public bool CreditsStarted { get; private set; }

    private RectTransform creditsRect;
    private float creditsScrollOffset;
    private bool hasPlayed;

    private void OnEnable()
    {
        if (hasPlayed)
        {
            return;
        }

        hasPlayed = true;
        EnsureFadeCanvasGroup();
        EnsurePartyCanvasGroup();
        EnsureCreditsCanvasGroup();
        EnsureCreditsText();

        IsCutsceneActive = true;
        StartCoroutine(PlayCutsceneRoutine());
    }

    private void Update()
    {
        if (creditsCanvasGroup != null && creditsCanvasGroup.alpha > 0f && creditsRect != null)
        {
            creditsScrollOffset += creditsScrollPixelsPerSecond * Time.unscaledDeltaTime;
            creditsRect.anchoredPosition = new Vector2(
                creditsRect.anchoredPosition.x,
                creditsRect.anchoredPosition.y + creditsScrollPixelsPerSecond * Time.unscaledDeltaTime);
        }
    }

    private IEnumerator PlayCutsceneRoutine()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleEndingMusic();
        }

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.RegisterAncientText("beat_ending_good", "Sunset In Balance",
                "The party accepts that peace costs them their own fading light.");
            gsm.DiscoverAncientText("beat_ending_good");
        }

        yield return FadeCanvasGroup(fadeCanvasGroup, 0f, introFadeSeconds);

        if (partyCanvasGroup != null)
        {
            partyCanvasGroup.gameObject.SetActive(true);
        }

        yield return FadeCanvasGroup(partyCanvasGroup, 1f, introFadeSeconds);
        yield return new WaitForSecondsRealtime(partyHoldSeconds);

        yield return FadeCanvasGroup(partyCanvasGroup, 0f, acceptanceFadeSeconds);

        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.gameObject.SetActive(true);
            creditsCanvasGroup.alpha = 0f;
        }

        CreditsStarted = true;
        yield return FadeCanvasGroup(creditsCanvasGroup, 1f, creditsFadeInSeconds);
        yield return new WaitForSecondsRealtime(creditsHoldSeconds);

        IsCutsceneActive = false;

        yield return FadeCanvasGroup(creditsCanvasGroup, 0f, exitFadeSeconds);
        yield return FadeCanvasGroup(fadeCanvasGroup, 1f, exitFadeSeconds);

        if (!string.IsNullOrEmpty(exitSceneName))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(exitSceneName);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        float startAlpha = group.alpha;
        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private void EnsureFadeCanvasGroup()
    {
        if (fadeCanvasGroup != null)
        {
            return;
        }

        fadeCanvasGroup = GetOrCreateOverlayCanvas("GoodEndingFadeCanvas");
    }

    private void EnsurePartyCanvasGroup()
    {
        if (partyCanvasGroup != null)
        {
            return;
        }

        partyCanvasGroup = GetOrCreateOverlayCanvas("GoodEndingPartyCanvas");
        if (partyCanvasGroup != null)
        {
            partyCanvasGroup.alpha = 0f;
        }
    }

    private void EnsureCreditsCanvasGroup()
    {
        if (creditsCanvasGroup != null)
        {
            return;
        }

        creditsCanvasGroup = GetOrCreateOverlayCanvas("GoodEndingCreditsCanvas");
        if (creditsCanvasGroup != null)
        {
            creditsCanvasGroup.alpha = 0f;
        }
    }

    private void EnsureCreditsText()
    {
        if (creditsText == null && creditsCanvasGroup != null)
        {
            GameObject textObject = new GameObject("GoodEndingCreditsText");
            textObject.transform.SetParent(creditsCanvasGroup.transform, false);
            creditsRect = textObject.AddComponent<RectTransform>();
            creditsRect.anchorMin = new Vector2(0.5f, 0f);
            creditsRect.anchorMax = new Vector2(0.5f, 0f);
            creditsRect.pivot = new Vector2(0.5f, 0f);
            creditsRect.anchoredPosition = new Vector2(0f, 32f);
            creditsRect.sizeDelta = new Vector2(720f, 600f);

            creditsText = textObject.AddComponent<Text>();
            creditsText.alignment = TextAnchor.UpperCenter;
            creditsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            creditsText.fontSize = 28;
            creditsText.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            creditsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        if (creditsText != null)
        {
            string composed = string.IsNullOrEmpty(creditsHeading) ? string.Empty : creditsHeading + "\n\n";
            if (creditsLines != null && creditsLines.Length > 0)
            {
                for (int i = 0; i < creditsLines.Length; i++)
                {
                    composed += creditsLines[i];
                    if (i < creditsLines.Length - 1)
                    {
                        composed += "\n\n";
                    }
                }
            }

            creditsText.text = composed;
        }
    }

    private static CanvasGroup GetOrCreateOverlayCanvas(string canvasName)
    {
        GameObject existing = GameObject.Find(canvasName);
        GameObject canvasObject;
        if (existing != null && existing.GetComponent<Canvas>() != null)
        {
            canvasObject = existing;
        }
        else
        {
            canvasObject = new GameObject(canvasName);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1500;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup group = canvasObject.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = canvasObject.AddComponent<CanvasGroup>();
        }

        return group;
    }
}
