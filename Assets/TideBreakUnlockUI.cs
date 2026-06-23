using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a notification popup when a TideBreak is unlocked.
/// Listens to TideBreakProgressionManager.OnTideBreakUnlocked and displays
/// the ability name, element icon, description, and unlock level.
/// Queues multiple unlocks if they happen simultaneously.
/// </summary>
[DisallowMultipleComponent]
public class TideBreakUnlockUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Text abilityNameText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text unlockLevelText;
    [SerializeField] private Image elementIcon;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Header("Element Colors")]
    [SerializeField] private Color fireColor = new Color(1f, 0.35f, 0.1f);
    [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color earthColor = new Color(0.65f, 0.45f, 0.2f);
    [SerializeField] private Color airColor = new Color(0.7f, 0.95f, 1f);
    [SerializeField] private Color spaceColor = new Color(0.6f, 0.2f, 0.9f);
    [SerializeField] private Color defaultColor = Color.white;

    private readonly Queue<TideBreakData> unlockQueue = new Queue<TideBreakData>();
    private Coroutine displayCoroutine;
    private bool isDisplaying;

    private void OnEnable()
    {
        if (TideBreakProgressionManager.Instance != null)
        {
            TideBreakProgressionManager.Instance.OnTideBreakUnlocked += HandleTideBreakUnlocked;
        }

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (TideBreakProgressionManager.Instance != null)
        {
            TideBreakProgressionManager.Instance.OnTideBreakUnlocked -= HandleTideBreakUnlocked;
        }

        StopAllCoroutines();
        isDisplaying = false;
    }

    private void HandleTideBreakUnlocked(string heroId, TideBreakData tideBreak)
    {
        if (tideBreak == null)
        {
            return;
        }

        unlockQueue.Enqueue(tideBreak);

        if (!isDisplaying)
        {
            displayCoroutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isDisplaying = true;

        while (unlockQueue.Count > 0)
        {
            TideBreakData next = unlockQueue.Dequeue();
            yield return StartCoroutine(DisplayPopup(next));
        }

        isDisplaying = false;
        displayCoroutine = null;
    }

    private IEnumerator DisplayPopup(TideBreakData tideBreak)
    {
        if (popupCanvasGroup == null)
        {
            yield break;
        }

        // Populate the popup content
        if (abilityNameText != null)
        {
            abilityNameText.text = tideBreak.abilityName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = tideBreak.description;
        }

        if (unlockLevelText != null)
        {
            string levelText = tideBreak.isHidden
                ? "Hidden Ability Revealed!"
                : $"Unlocked at Level {tideBreak.unlockLevel}";
            unlockLevelText.text = levelText;
        }

        if (elementIcon != null)
        {
            elementIcon.color = GetElementColor(tideBreak.element);
        }

        // Show and fade in
        popupCanvasGroup.gameObject.SetActive(true);
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 0f, 1f, fadeInDuration));

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(popupCanvasGroup, 1f, 0f, fadeOutDuration));

        // Hide
        popupCanvasGroup.gameObject.SetActive(false);
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }

        cg.alpha = to;
    }

    private Color GetElementColor(int elementId)
    {
        switch (elementId)
        {
            case 1: return fireColor;   // Fire
            case 2: return waterColor;  // Water
            case 3: return earthColor;  // Earth
            case 4: return airColor;    // Air
            case 5: return spaceColor;  // Space
            default: return defaultColor;
        }
    }
}
