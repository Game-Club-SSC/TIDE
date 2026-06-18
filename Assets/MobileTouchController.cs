using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MobileTouchController : MonoBehaviour
{
    public static MobileTouchController Instance { get; private set; }

    [Header("Layout")]
    [SerializeField] private Vector2 dpadCenterPercent = new Vector2(0.2f, 0.25f);
    [SerializeField] private float dpadRadius = 110f;
    [SerializeField] private float actionButtonRadius = 64f;
    [SerializeField] private Vector2[] actionButtonPositions = new Vector2[]
    {
        new Vector2(0.85f, 0.30f),
        new Vector2(0.75f, 0.20f),
        new Vector2(0.95f, 0.20f),
        new Vector2(0.80f, 0.40f)
    };

    [Header("Visuals")]
    [SerializeField] private Color dpadColor = new Color(0.15f, 0.18f, 0.24f, 0.6f);
    [SerializeField] private Color actionButtonColor = new Color(0.85f, 0.4f, 0.2f, 0.7f);
    [SerializeField] private Color pressedColor = new Color(0.95f, 0.85f, 0.4f, 0.9f);

    public enum ActionButtonId
    {
        Interact,
        Dash,
        Hop,
        Sprint
    }

    public event Action<ActionButtonId> OnActionButtonPressed;
    public event Action<ActionButtonId> OnActionButtonReleased;

    private Vector2 dpadInput;
    private bool[] actionButtonHeld = new bool[4];
    private bool isVisible;
    private Canvas canvas;

    public Vector2 DpadInput => dpadInput;
    public bool IsActionButtonHeld(ActionButtonId id) => actionButtonHeld[(int)id];

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            if (canvas != null)
            {
                canvas.enabled = value;
            }
        }
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SimulateActionButtonPress(ActionButtonId id)
    {
        int index = (int)id;
        if (index < 0 || index >= actionButtonHeld.Length)
        {
            return;
        }
        actionButtonHeld[index] = true;
        OnActionButtonPressed?.Invoke(id);
    }

    public void SimulateActionButtonRelease(ActionButtonId id)
    {
        int index = (int)id;
        if (index < 0 || index >= actionButtonHeld.Length)
        {
            return;
        }
        actionButtonHeld[index] = false;
        OnActionButtonReleased?.Invoke(id);
    }

    public void SetDpadInput(Vector2 input)
    {
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }
        dpadInput = input;
    }

    private void BuildOverlay()
    {
        GameObject canvasGo = new GameObject("MobileTouchCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasGo.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = true;
        isVisible = canvas.enabled;
    }
}
