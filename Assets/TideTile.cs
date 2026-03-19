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

    [SerializeField] private Vector2Int gridPosition;

    private Renderer cachedRenderer;
    private TextMeshPro valueLabel;
    private Vector3 baseScale;
    private static readonly Quaternion LabelTopDownRotation = Quaternion.Euler(90f, 0f, 0f);

    public Vector2Int GridPosition => gridPosition;
    public int CurrentTideValue => currentTideValue;
    public bool IsSealed => isSealed;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
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
        RefreshVisuals();
    }

    public void ApplyPlace(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentTideValue += amount;
        currentTideValue = Mathf.Clamp(currentTideValue, 1, 10);
        RefreshVisuals();
    }

    public void SetVisualState(bool isSelected, bool isReachable, bool isHovered)
    {
        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<Renderer>();
        }

        Color displayColor = GetBaseColor();

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

        cachedRenderer.material.color = displayColor;

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
        cachedRenderer.material.color = GetBaseColor();
        valueLabel.text = isSealed ? "X" : currentTideValue.ToString();
        valueLabel.color = isSealed ? new Color(0.9f, 0.9f, 0.9f) : new Color(0.1f, 0.1f, 0.1f);
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

        if (currentTideValue == 5)
        {
            return new Color(0.6f, 0.85f, 0.65f);
        }

        if (currentTideValue > 5)
        {
            float intensity = Mathf.InverseLerp(6f, 10f, currentTideValue);
            return Color.Lerp(new Color(0.93f, 0.82f, 0.55f), new Color(1f, 0.96f, 0.82f), intensity);
        }

        float deficit = Mathf.InverseLerp(4f, 1f, currentTideValue);
        return Color.Lerp(new Color(0.45f, 0.62f, 0.8f), new Color(0.2f, 0.32f, 0.55f), deficit);
    }

    private void OnValidate()
    {
        if (valueLabel != null)
        {
            valueLabel.transform.localRotation = LabelTopDownRotation;
        }
    }
}
