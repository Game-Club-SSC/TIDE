using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private Image[] actionButtonImages;
    private Image dpadKnobImage;
    private RectTransform dpadBaseRect;
    private RectTransform dpadKnobRect;

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
        GameObject canvasGo = new GameObject("MobileTouchControllerCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGo.GetComponent<GraphicRaycaster>().enabled = true;
        isVisible = canvas.enabled;

        BuildDpad(canvasGo.transform);
        BuildActionButtons(canvasGo.transform);
    }

    private void BuildDpad(Transform parent)
    {
        float diameter = dpadRadius * 2f;
        Vector2 center = new Vector2(dpadCenterPercent.x * 1920f, dpadCenterPercent.y * 1080f);

        GameObject baseGo = new GameObject("DpadBase", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(parent, false);
        dpadBaseRect = baseGo.GetComponent<RectTransform>();
        dpadBaseRect.anchorMin = new Vector2(0f, 0f);
        dpadBaseRect.anchorMax = new Vector2(0f, 0f);
        dpadBaseRect.pivot = new Vector2(0.5f, 0.5f);
        dpadBaseRect.anchoredPosition = center;
        dpadBaseRect.sizeDelta = new Vector2(diameter, diameter);
        Image baseImg = baseGo.GetComponent<Image>();
        baseImg.color = dpadColor;

        float knobDiameter = diameter * 0.45f;
        GameObject knobGo = new GameObject("DpadKnob", typeof(RectTransform), typeof(Image));
        knobGo.transform.SetParent(baseGo.transform, false);
        dpadKnobRect = knobGo.GetComponent<RectTransform>();
        dpadKnobRect.anchorMin = new Vector2(0.5f, 0.5f);
        dpadKnobRect.anchorMax = new Vector2(0.5f, 0.5f);
        dpadKnobRect.pivot = new Vector2(0.5f, 0.5f);
        dpadKnobRect.anchoredPosition = Vector2.zero;
        dpadKnobRect.sizeDelta = new Vector2(knobDiameter, knobDiameter);
        dpadKnobImage = knobGo.GetComponent<Image>();
        dpadKnobImage.color = new Color(dpadColor.r + 0.2f, dpadColor.g + 0.2f, dpadColor.b + 0.2f, dpadColor.a + 0.2f);

        string[] labels = { "U", "D", "L", "R" };
        Vector2[] offsets = { new Vector2(0f, dpadRadius * 0.65f), new Vector2(0f, -dpadRadius * 0.65f), new Vector2(-dpadRadius * 0.65f, 0f), new Vector2(dpadRadius * 0.65f, 0f) };
        for (int i = 0; i < 4; i++)
        {
            GameObject dirGo = new GameObject("Dir_" + labels[i], typeof(RectTransform), typeof(Text));
            dirGo.transform.SetParent(baseGo.transform, false);
            RectTransform dirRect = dirGo.GetComponent<RectTransform>();
            dirRect.anchorMin = new Vector2(0.5f, 0.5f);
            dirRect.anchorMax = new Vector2(0.5f, 0.5f);
            dirRect.pivot = new Vector2(0.5f, 0.5f);
            dirRect.anchoredPosition = offsets[i];
            dirRect.sizeDelta = new Vector2(30f, 30f);
            Text dirText = dirGo.GetComponent<Text>();
            dirText.text = labels[i];
            dirText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dirText.fontSize = 18;
            dirText.alignment = TextAnchor.MiddleCenter;
            dirText.color = new Color(1f, 1f, 1f, 0.5f);
        }

        EventTrigger trigger = baseGo.AddComponent<EventTrigger>();

        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = (PointerEventData)data;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dpadBaseRect, pointerData.position, pointerData.pressEventCamera, out localPoint);
            Vector2 normalized = localPoint / dpadRadius;
            if (normalized.sqrMagnitude > 1f)
            {
                normalized.Normalize();
            }
            SetDpadInput(normalized);
            dpadKnobRect.anchoredPosition = normalized * dpadRadius;
        });
        trigger.triggers.Add(dragEntry);

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = (PointerEventData)data;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dpadBaseRect, pointerData.position, pointerData.pressEventCamera, out localPoint);
            Vector2 normalized = localPoint / dpadRadius;
            if (normalized.sqrMagnitude > 1f)
            {
                normalized.Normalize();
            }
            SetDpadInput(normalized);
            dpadKnobRect.anchoredPosition = normalized * dpadRadius;
        });
        trigger.triggers.Add(pointerDownEntry);

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) =>
        {
            SetDpadInput(Vector2.zero);
            dpadKnobRect.anchoredPosition = Vector2.zero;
        });
        trigger.triggers.Add(pointerUpEntry);

        EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) =>
        {
            PointerEventData pointerData = (PointerEventData)data;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(dpadBaseRect, pointerData.position, pointerData.pressEventCamera, out localPoint);
            Vector2 normalized = localPoint / dpadRadius;
            if (normalized.sqrMagnitude > 1f)
            {
                normalized.Normalize();
            }
            SetDpadInput(normalized);
            dpadKnobRect.anchoredPosition = normalized * dpadRadius;
        });
        trigger.triggers.Add(beginDragEntry);

        EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
        endDragEntry.eventID = EventTriggerType.EndDrag;
        endDragEntry.callback.AddListener((data) =>
        {
            SetDpadInput(Vector2.zero);
            dpadKnobRect.anchoredPosition = Vector2.zero;
        });
        trigger.triggers.Add(endDragEntry);
    }

    private void BuildActionButtons(Transform parent)
    {
        string[] labels = { "ACT", "DASH", "HOP", "RUN" };
        actionButtonImages = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = new Vector2(actionButtonPositions[i].x * 1920f, actionButtonPositions[i].y * 1080f);
            float diameter = actionButtonRadius * 2f;

            GameObject btnGo = new GameObject("ActionBtn_" + labels[i], typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(diameter, diameter);

            Image img = btnGo.GetComponent<Image>();
            img.color = actionButtonColor;
            actionButtonImages[i] = img;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            Text text = labelGo.GetComponent<Text>();
            text.text = labels[i];
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            ActionButtonId capturedId = (ActionButtonId)i;
            EventTrigger btnTrigger = btnGo.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) =>
            {
                SimulateActionButtonPress(capturedId);
                actionButtonImages[(int)capturedId].color = pressedColor;
            });
            btnTrigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) =>
            {
                SimulateActionButtonRelease(capturedId);
                actionButtonImages[(int)capturedId].color = actionButtonColor;
            });
            btnTrigger.triggers.Add(pointerUpEntry);
        }
    }
}
