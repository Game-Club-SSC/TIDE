using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persona-styled audio settings panel with volume sliders and mute toggle.
/// Creates a procedural UI matching the game's angular dark-blue aesthetic.
/// </summary>
[DisallowMultipleComponent]
public class AudioSettingsUI : MonoBehaviour
{
    private Canvas canvas;
    private Image panelRoot;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Toggle muteToggle;
    private Text languageValueText;
    private bool isVisible;

    public bool IsVisible => isVisible;

    private void Awake()
    {
        EnsureUI();
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (canvas != null)
        {
            canvas.enabled = visible;
        }

        if (visible)
        {
            RefreshFromAudioManager();
        }
    }

    private void EnsureUI()
    {
        if (canvas != null)
        {
            return;
        }

        // Create overlay canvas
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9998;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Ensure EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Panel root
        panelRoot = PersonaUIStyle.CreateAngularPanel(canvas.transform, PersonaUIStyle.PanelBg);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.3f, 0.25f);
        panelRect.anchorMax = new Vector2(0.7f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Title
        Text title = PersonaUIStyle.CreatePersonaLabel(panelRoot.transform, "AUDIO SETTINGS", 28, PersonaUIStyle.BrightBlue, TextAnchor.MiddleCenter);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.88f);
        titleRect.anchorMax = new Vector2(1f, 0.98f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        // Slash divider under title
        PersonaUIStyle.CreateSlashDivider(panelRoot.transform, PersonaUIStyle.SlashColor);

        // Content container
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(panelRoot.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.12f);
        contentRect.anchorMax = new Vector2(0.9f, 0.85f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperCenter;

        // BGM Volume slider
        bgmSlider = CreateVolumeSlider(content.transform, "BGM Volume", 0.7f, OnBgmSliderChanged);

        // SFX Volume slider
        sfxSlider = CreateVolumeSlider(content.transform, "SFX Volume", 1f, OnSfxSliderChanged);

        // Mute toggle
        muteToggle = CreateMuteToggle(content.transform, "Mute All", OnMuteToggled);

        // Language selector
        CreateLanguageRow(content.transform);

        // Close button
        GameObject closeBtnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(content.transform, false);
        Image closeImg = closeBtnObj.AddComponent<Image>();
        closeImg.color = PersonaUIStyle.CloseBtnBg;
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(() => SetVisible(false));
        LayoutElement closeLe = closeBtnObj.AddComponent<LayoutElement>();
        closeLe.preferredHeight = 40f;
        closeLe.flexibleWidth = 1f;

        Text closeText = PersonaUIStyle.CreatePersonaLabel(closeBtnObj.transform, "CLOSE", 18, PersonaUIStyle.White, TextAnchor.MiddleCenter);
        RectTransform closeTextRect = closeText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(closeTextRect);
    }

    private Slider CreateVolumeSlider(Transform parent, string label, float defaultValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = new GameObject(label + "Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 50f;
        rowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Label
        Text labelText = PersonaUIStyle.CreatePersonaLabel(row.transform, label, 18, PersonaUIStyle.OffWhite, TextAnchor.MiddleLeft);
        LayoutElement labelLe = labelText.gameObject.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 140f;
        labelLe.flexibleHeight = 1f;

        // Slider background
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform));
        sliderObj.transform.SetParent(row.transform, false);
        LayoutElement sliderLe = sliderObj.AddComponent<LayoutElement>();
        sliderLe.flexibleWidth = 1f;
        sliderLe.preferredHeight = 30f;

        Image bgImg = sliderObj.AddComponent<Image>();
        bgImg.color = PersonaUIStyle.DeepNavy;

        // Fill area
        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.15f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.85f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.7f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = PersonaUIStyle.BrightBlue;

        // Handle
        GameObject handleArea = new GameObject("HandleSlideArea", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = Vector2.zero;
        handleAreaRect.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(16f, 24f);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = PersonaUIStyle.White;

        // Slider component
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = defaultValue;
        slider.onValueChanged.AddListener(onChanged);

        // Value text
        Text valueText = PersonaUIStyle.CreatePersonaLabel(row.transform, Mathf.RoundToInt(defaultValue * 100f) + "%", 16, PersonaUIStyle.Gold, TextAnchor.MiddleRight);
        LayoutElement valueLe = valueText.gameObject.AddComponent<LayoutElement>();
        valueLe.preferredWidth = 50f;
        valueLe.flexibleHeight = 1f;

        // Update value text on slider change
        slider.onValueChanged.AddListener(v => valueText.text = Mathf.RoundToInt(v * 100f) + "%");

        return slider;
    }

    private Toggle CreateMuteToggle(Transform parent, string label, UnityEngine.Events.UnityAction<bool> onChanged)
    {
        GameObject row = new GameObject(label + "Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 40f;
        rowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        // Label
        Text labelText = PersonaUIStyle.CreatePersonaLabel(row.transform, label, 18, PersonaUIStyle.OffWhite, TextAnchor.MiddleLeft);
        LayoutElement labelLe = labelText.gameObject.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 140f;
        labelLe.flexibleHeight = 1f;

        // Toggle background
        GameObject toggleBg = new GameObject("ToggleBg", typeof(RectTransform), typeof(Image));
        toggleBg.transform.SetParent(row.transform, false);
        LayoutElement toggleBgLe = toggleBg.AddComponent<LayoutElement>();
        toggleBgLe.preferredWidth = 50f;
        toggleBgLe.preferredHeight = 28f;
        Image bgImg = toggleBg.GetComponent<Image>();
        bgImg.color = PersonaUIStyle.DeepNavy;

        // Toggle checkmark
        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(toggleBg.transform, false);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.1f, 0.1f);
        checkRect.anchorMax = new Vector2(0.9f, 0.9f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        Image checkImg = checkmark.GetComponent<Image>();
        checkImg.color = PersonaUIStyle.AccentRed;

        // Toggle component
        Toggle toggle = toggleBg.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = false;
        toggle.onValueChanged.AddListener(onChanged);

        return toggle;
    }

    private void RefreshFromAudioManager()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null)
        {
            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(am.BgmVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(am.SfxVolume);
        }

        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(am.IsMuted);
        }
    }

    private void OnBgmSliderChanged(float value)
    {
        AudioManager am = AudioManager.Instance;
        if (am != null)
        {
            am.SetBgmVolume(value);
        }
    }

    private void OnSfxSliderChanged(float value)
    {
        AudioManager am = AudioManager.Instance;
        if (am != null)
        {
            am.SetSfxVolume(value);
        }
    }

    private void OnMuteToggled(bool isMuted)
    {
        AudioManager am = AudioManager.Instance;
        if (am != null)
        {
            am.SetMute(isMuted);
        }
    }

    private void CreateLanguageRow(Transform parent)
    {
        GameObject row = new GameObject("LanguageRow", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        LayoutElement rowLe = row.AddComponent<LayoutElement>();
        rowLe.preferredHeight = 50f;
        rowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        Text labelText = PersonaUIStyle.CreatePersonaLabel(row.transform, "Language", 18, PersonaUIStyle.OffWhite, TextAnchor.MiddleLeft);
        LayoutElement labelLe = labelText.gameObject.AddComponent<LayoutElement>();
        labelLe.preferredWidth = 140f;
        labelLe.flexibleHeight = 1f;

        GameObject btnObj = new GameObject("CycleLangBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(row.transform, false);
        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = PersonaUIStyle.DeepNavy;
        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(OnLanguageCycled);
        LayoutElement btnLe = btnObj.AddComponent<LayoutElement>();
        btnLe.flexibleWidth = 1f;
        btnLe.preferredHeight = 30f;

        languageValueText = PersonaUIStyle.CreatePersonaLabel(btnObj.transform, LocalizationService.CurrentLanguage.ToString(), 16, PersonaUIStyle.Gold, TextAnchor.MiddleCenter);
        RectTransform valueRect = languageValueText.GetComponent<RectTransform>();
        PersonaUIStyle.StretchFull(valueRect);
    }

    private void OnLanguageCycled()
    {
        int count = System.Enum.GetValues(typeof(LocalizationService.Language)).Length;
        int next = ((int)LocalizationService.CurrentLanguage + 1) % count;
        LocalizationService.SetLanguage((LocalizationService.Language)next);

        if (languageValueText != null)
        {
            languageValueText.text = LocalizationService.CurrentLanguage.ToString();
        }
    }
}
