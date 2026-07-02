using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TideTile : MonoBehaviour
{
    [Header("Tide Properties")]
    [Tooltip("The current amount of Tide on this tile (1-10)")]
    [Range(1, 10)]
    public int currentTideValue = 5;

    [Header("Tile State")]
    [Tooltip("Check this box if this is an 'X' tile that requires combat to unlock.")]
    public bool isSealed = false;

    [Header("Corruption Transition")]
    [Tooltip("Duration of smooth color transition when tide value changes (seconds).")]
    [SerializeField] private float corruptionTransitionDuration = 0.4f;

    [SerializeField] private Vector2Int gridPosition;

    private Renderer cachedRenderer;
    private Material cachedMaterial;
    private TextMeshPro valueLabel;
    private Vector3 baseScale;
    private Coroutine activeFlashCoroutine;
    private Coroutine activeTransitionCoroutine;
    private Color currentDisplayedColor;
    private static readonly Quaternion LabelTopDownRotation = Quaternion.Euler(90f, 0f, 0f);

    public Vector2Int GridPosition => gridPosition;
    public int CurrentTideValue => currentTideValue;
    public bool IsSealed => isSealed;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        if (cachedRenderer != null)
        {
            cachedMaterial = cachedRenderer.material;
        }
        baseScale = transform.localScale;
        EnsureLabel();
        RefreshVisuals();
    }

    public void Configure(Vector2Int newGridPosition, int tideValue, bool sealedTile)
    {
        gridPosition = newGridPosition;
        currentTideValue = Mathf.Clamp(tideValue, 1, 10);
        isSealed = sealedTile;
        baseScale = transform.localScale;
        EnsureLabel();
        RefreshVisuals();
    }

    public int GetMaxTake()
    {
        if (isSealed)
        {
            return 0;
        }

        if (currentTideValue > 5)
        {
            return currentTideValue - 5;
        }

        if (currentTideValue < 5)
        {
            return currentTideValue - 1;
        }

        return 0;
    }

    public bool CanReceive(int amount)
    {
        return !isSealed && amount > 0 && currentTideValue + amount <= 10;
    }

    public void ApplyTake(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentTideValue -= amount;
        currentTideValue = Mathf.Clamp(currentTideValue, 1, 10);
        StartCorruptionTransition();
    }

    public void ApplyPlace(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentTideValue += amount;
        currentTideValue = Mathf.Clamp(currentTideValue, 1, 10);
        StartCorruptionTransition();
    }

    public void ApplyDecay(int decay)
    {
        if (decay <= 0 || isSealed || currentTideValue <= 5)
        {
            return;
        }

        currentTideValue -= decay;
        currentTideValue = Mathf.Max(currentTideValue, 5);
        StartCorruptionTransition();
        StartFlash(FlashDecay());
    }

    public void FlashInvalid()
    {
        StartFlash(FlashColor(new Color(1f, 0.25f, 0.25f), 0.3f));
    }

    public void FlashComplete()
    {
        StartFlash(FlashColor(new Color(0.2f, 1f, 0.4f), 0.5f));
    }

    private void StartFlash(IEnumerator routine)
    {
        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
            RefreshVisuals();
        }
        activeFlashCoroutine = StartCoroutine(RunFlash(routine));
    }

    private IEnumerator RunFlash(IEnumerator routine)
    {
        yield return StartCoroutine(routine);
        activeFlashCoroutine = null;
    }

    private IEnumerator FlashDecay()
    {
        Color original = GetBaseColor();
        Color flash = new Color(0.55f, 0.2f, 0.65f);
        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 originalScale = baseScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pingPong = Mathf.PingPong(t * 4f, 1f);
            if (cachedMaterial != null)
            {
                cachedMaterial.color = Color.Lerp(original, flash, pingPong);
            }
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 0.85f, pingPong);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        RefreshVisuals();
    }

    private IEnumerator FlashColor(Color flashColor, float duration)
    {
        Color original = GetBaseColor();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float pingPong = Mathf.PingPong(t * 3f, 1f);
            if (cachedMaterial != null)
            {
                cachedMaterial.color = Color.Lerp(original, flashColor, pingPong);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        RefreshVisuals();
    }

    public void SetVisualState(bool isSelected, bool isReachable, bool isHovered, bool isUnavailable = false)
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        Color displayColor = GetBaseColor();

        if (isUnavailable)
        {
            displayColor = Color.Lerp(displayColor, Color.black, 0.25f);
        }

        if (isReachable)
        {
            displayColor = Color.Lerp(displayColor, Color.white, 0.2f);
        }

        if (isHovered)
        {
            displayColor = Color.Lerp(displayColor, Color.white, 0.35f);
        }

        if (isSelected)
        {
            displayColor = Color.Lerp(displayColor, new Color(1f, 0.95f, 0.4f), 0.7f);
        }

        if (cachedMaterial != null)
        {
            cachedMaterial.color = displayColor;
        }

        float scaleBoost = 1f;

        if (isHovered)
        {
            scaleBoost += 0.03f;
        }

        if (isSelected)
        {
            scaleBoost += 0.08f;
        }

        transform.localScale = baseScale * scaleBoost;
    }

    public void RefreshVisuals()
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        EnsureLabel();
        if (cachedMaterial != null)
        {
            currentDisplayedColor = GetBaseColor();
            cachedMaterial.color = currentDisplayedColor;
        }

        if (valueLabel != null)
        {
            valueLabel.text = isSealed ? "X" : currentTideValue.ToString();
            valueLabel.color = isSealed ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
        }
    }

    private void StartCorruptionTransition()
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(TransitionCorruptionColor());
    }

    private IEnumerator TransitionCorruptionColor()
    {
        Color startColor = currentDisplayedColor;
        Color targetColor = GetBaseColor();
        float elapsed = 0f;

        while (elapsed < corruptionTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / corruptionTransitionDuration);
            float smoothedT = Mathf.SmoothStep(0f, 1f, t);

            if (cachedMaterial != null)
            {
                cachedMaterial.color = Color.Lerp(startColor, targetColor, smoothedT);
            }

            currentDisplayedColor = cachedMaterial != null ? cachedMaterial.color : targetColor;
            yield return null;
        }

        currentDisplayedColor = targetColor;
        if (cachedMaterial != null)
        {
            cachedMaterial.color = targetColor;
        }

        EnsureLabel();
        if (valueLabel != null)
        {
            valueLabel.text = isSealed ? "X" : currentTideValue.ToString();
            valueLabel.color = isSealed ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
        }

        activeTransitionCoroutine = null;
    }

    private void EnsureLabel()
    {
        if (valueLabel != null)
        {
            return;
        }

        Transform existingLabel = transform.Find("ValueLabel");
        if (existingLabel != null)
        {
            valueLabel = existingLabel.GetComponent<TextMeshPro>();
            existingLabel.localRotation = LabelTopDownRotation;
            return;
        }

        GameObject labelObject = new GameObject("ValueLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        labelObject.transform.localScale = Vector3.one * 0.3f;
        labelObject.transform.localRotation = LabelTopDownRotation;

        valueLabel = labelObject.AddComponent<TextMeshPro>();
        if (valueLabel.font == null)
        {
            valueLabel.font = TMP_Settings.defaultFontAsset;
        }
        valueLabel.alignment = TextAlignmentOptions.Center;
        valueLabel.fontSize = 12f;
        valueLabel.textWrappingMode = TextWrappingModes.NoWrap;
        valueLabel.text = currentTideValue.ToString();
        valueLabel.color = new Color(0.1f, 0.1f, 0.1f);
    }

    private Color GetBaseColor()
    {
        if (isSealed)
        {
            return new Color(0.22f, 0.22f, 0.22f);
        }

        // Tide 5 = normal/clean visuals (green)
        if (currentTideValue == 5)
        {
            return new Color(0.6f, 0.85f, 0.65f);
        }

        // Tide 6-9 = corruption buildup (warm tones to bright white)
        if (currentTideValue > 5)
        {
            float intensity = Mathf.InverseLerp(6f, 10f, currentTideValue);
            return Color.Lerp(new Color(0.85f, 0.78f, 0.5f), new Color(1f, 1f, 1f), intensity);
        }

        // Tide 1-4 = evil corruption (cool tones to complete black)
        float deficit = Mathf.InverseLerp(4f, 1f, currentTideValue);
        return Color.Lerp(new Color(0.45f, 0.62f, 0.8f), Color.black, deficit);
    }

    private void OnValidate()
    {
        if (valueLabel != null)
        {
            valueLabel.transform.localRotation = LabelTopDownRotation;
        }
    }

    private void OnDestroy()
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        if (activeFlashCoroutine != null)
        {
            StopCoroutine(activeFlashCoroutine);
        }

        if (cachedMaterial != null)
        {
            Destroy(cachedMaterial);
            cachedMaterial = null;
        }
    }
}
