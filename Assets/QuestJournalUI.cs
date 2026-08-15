using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Comprehensive quest/story tracking UI with 3 tabs: Story Progress,
/// Ancient Texts, and Hero Bonds. Toggle with J key or Escape when open.
/// </summary>
[DisallowMultipleComponent]
public class QuestJournalUI : MonoBehaviour
{
    private enum Tab
    {
        StoryProgress,
        AncientTexts,
        HeroBonds
    }

    private const string CanvasName = "QuestJournalCanvas";
    private const int CanvasSortOrder = 800;
    private const float AnimDuration = 0.2f;

    private static readonly string[] HeroIds =
        { "hero_fire", "hero_water", "hero_earth", "hero_air", "hero_space" };

    private static readonly string[] HeroDisplayNames =
        { "Fire", "Water", "Earth", "Air", "Space" };

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GameObject panelRoot;
    private bool isVisible;
    private bool isAnimating;
    private float animTarget;
    private float animVelocity;
    private Tab activeTab = Tab.StoryProgress;

    // Tab button references for highlighting
    private Button storyTabButton;
    private Button textsTabButton;
    private Button bondsTabButton;
    private Image storyTabImage;
    private Image textsTabImage;
    private Image bondsTabImage;

    // Content area roots
    private GameObject storyContent;
    private GameObject textsContent;
    private GameObject bondsContent;

    // Story content labels
    private Text storyActLabel;
    private Text storyIslandsLabel;
    private Text storyRestorationLabel;
    private Text storyObjectiveLabel;
    private Text storyDefeatLabel;

    // Texts scroll view
    private RectTransform textsScrollContent;

    // Bonds scroll view
    private RectTransform bondsScrollContent;

    private static readonly Color TabActiveColor = PersonaUIStyle.TabActive;
    private static readonly Color TabInactiveColor = PersonaUIStyle.TabInactive;
    private static readonly Color PanelBgColor = PersonaUIStyle.PanelBg;
    private static readonly Color AccentColor = PersonaUIStyle.BrightBlue;
    private static readonly Color TextColor = PersonaUIStyle.OffWhite;
    private static readonly Color DimTextColor = PersonaUIStyle.DimText;
    private static readonly Color HeaderColor = PersonaUIStyle.Gold;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (isAnimating)
        {
            canvasGroup.alpha = Mathf.SmoothDamp(canvasGroup.alpha, animTarget, ref animVelocity, AnimDuration);
            if (Mathf.Abs(canvasGroup.alpha - animTarget) < 0.01f)
            {
                canvasGroup.alpha = animTarget;
                isAnimating = false;
                if (animTarget <= 0f)
                {
                    panelRoot.SetActive(false);
                    canvasGroup.blocksRaycasts = false;
                }
            }
        }

        if (!isVisible || isAnimating)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.J))
        {
            Hide();
            return;
        }
    }

    public void Toggle()
    {
        if (isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        EnsureCanvas();
        isVisible = true;
        panelRoot.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;
        isAnimating = true;
        animTarget = 1f;
        animVelocity = 0f;
        RefreshAllTabs();
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        isAnimating = true;
        animTarget = 0f;
        animVelocity = 0f;
    }

    // ------------------------------------------------------------------
    // Canvas construction
    // ------------------------------------------------------------------

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        GameObject canvasObj = new GameObject(CanvasName);
        canvasObj.transform.SetParent(transform, false);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        // Full-screen dark backdrop
        GameObject backdropObj = CreateUIElement("Backdrop", canvasObj.transform);
        RectTransform backdropRect = backdropObj.GetComponent<RectTransform>();
        StretchFull(backdropRect);
        Image backdropImg = backdropObj.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.6f);
        backdropImg.raycastTarget = true;
        backdropObj.AddComponent<Button>().onClick.AddListener(Hide);

        // Main panel
        panelRoot = CreateUIElement("JournalPanel", canvasObj.transform);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.08f);
        panelRect.anchorMax = new Vector2(0.9f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImg = panelRoot.AddComponent<Image>();
        panelImg.color = PanelBgColor;
        panelImg.raycastTarget = true;

        // Persona-style diagonal edge on the main panel
        PersonaUIStyle.AddDiagonalEdge(panelRect, 12f);

        // Accent slash along the left edge of the panel
        PersonaUIStyle.CreateAccentSlash(panelRoot.transform, PersonaUIStyle.BrightBlue, 3f);

        // Title bar
        GameObject titleBar = CreateUIElement("TitleBar", panelRoot.transform);
        RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
        titleBarRect.anchorMin = new Vector2(0f, 0.92f);
        titleBarRect.anchorMax = new Vector2(1f, 1f);
        titleBarRect.offsetMin = Vector2.zero;
        titleBarRect.offsetMax = Vector2.zero;
        Image titleBarBg = titleBar.AddComponent<Image>();
        titleBarBg.color = PersonaUIStyle.TitleBarBg;

        Text titleText = CreateLabel(titleBar.transform, "Title", 28, FontStyle.Bold, TextAnchor.MiddleCenter, HeaderColor);
        RectTransform titleTextRect = titleText.GetComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0.05f, 0f);
        titleTextRect.anchorMax = new Vector2(0.95f, 1f);
        titleTextRect.offsetMin = Vector2.zero;
        titleTextRect.offsetMax = Vector2.zero;
        titleText.text = "Quest Journal";

        // Close button
        GameObject closeBtn = CreateButton(titleBar.transform, "CloseBtn", "X", 22, PersonaUIStyle.CloseBtnBg);
        RectTransform closeBtnRect = closeBtn.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(0.93f, 0.15f);
        closeBtnRect.anchorMax = new Vector2(0.98f, 0.85f);
        closeBtnRect.offsetMin = Vector2.zero;
        closeBtnRect.offsetMax = Vector2.zero;
        closeBtn.GetComponent<Button>().onClick.AddListener(Hide);

        // Tab bar
        GameObject tabBar = CreateUIElement("TabBar", panelRoot.transform);
        RectTransform tabBarRect = tabBar.GetComponent<RectTransform>();
        tabBarRect.anchorMin = new Vector2(0.02f, 0.87f);
        tabBarRect.anchorMax = new Vector2(0.98f, 0.92f);
        tabBarRect.offsetMin = Vector2.zero;
        tabBarRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.spacing = 0f;
        tabHlg.padding = new RectOffset(0, 0, 0, 0);
        tabHlg.childAlignment = TextAnchor.MiddleCenter;
        tabHlg.childForceExpandWidth = true;
        tabHlg.childForceExpandHeight = true;

        storyTabButton = CreateTabButton(tabBar.transform, "StoryTab", "Story Progress", 0);
        storyTabImage = storyTabButton.GetComponent<Image>();
        LayoutElement storyTabLe = storyTabButton.gameObject.AddComponent<LayoutElement>();
        storyTabLe.flexibleWidth = 1f;

        // Slash divider between Story and Texts tabs
        PersonaUIStyle.CreateSlashDivider(tabBar.transform, PersonaUIStyle.SlashColor);

        textsTabButton = CreateTabButton(tabBar.transform, "TextsTab", "Ancient Texts", 1);
        textsTabImage = textsTabButton.GetComponent<Image>();
        LayoutElement textsTabLe = textsTabButton.gameObject.AddComponent<LayoutElement>();
        textsTabLe.flexibleWidth = 1f;

        // Slash divider between Texts and Bonds tabs
        PersonaUIStyle.CreateSlashDivider(tabBar.transform, PersonaUIStyle.SlashColor);

        bondsTabButton = CreateTabButton(tabBar.transform, "BondsTab", "Hero Bonds", 2);
        bondsTabImage = bondsTabButton.GetComponent<Image>();
        LayoutElement bondsTabLe = bondsTabButton.gameObject.AddComponent<LayoutElement>();
        bondsTabLe.flexibleWidth = 1f;

        // Content area
        GameObject contentArea = CreateUIElement("ContentArea", panelRoot.transform);
        RectTransform contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.02f, 0.02f);
        contentRect.anchorMax = new Vector2(0.98f, 0.86f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        storyContent = CreateStoryContent(contentArea.transform);
        textsContent = CreateTextsContent(contentArea.transform);
        bondsContent = CreateBondsContent(contentArea.transform);

        SetTab(Tab.StoryProgress);
    }

    // ------------------------------------------------------------------
    // Tab buttons
    // ------------------------------------------------------------------

    private Button CreateTabButton(Transform parent, string name, string label, int tabIndex)
    {
        GameObject obj = CreateUIElement(name, parent);
        Image img = obj.AddComponent<Image>();
        img.color = TabInactiveColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = PersonaUIStyle.MediumBlue;
        cb.pressedColor = PersonaUIStyle.DeepNavy;
        btn.colors = cb;
        int capturedIndex = tabIndex;
        btn.onClick.AddListener(() => SetTab((Tab)capturedIndex));

        Text txt = CreateLabel(obj.transform, "Label", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        StretchFull(txtRect);
        txt.text = label;

        return btn;
    }

    private void SetTab(Tab tab)
    {
        activeTab = tab;
        storyTabImage.color = tab == Tab.StoryProgress ? AccentColor : TabInactiveColor;
        textsTabImage.color = tab == Tab.AncientTexts ? AccentColor : TabInactiveColor;
        bondsTabImage.color = tab == Tab.HeroBonds ? AccentColor : TabInactiveColor;

        // Slash-transition the outgoing content out and the incoming content in
        CanvasGroup outgoing = GetContentGroup(activeTab == Tab.StoryProgress ? Tab.AncientTexts :
                                               activeTab == Tab.AncientTexts ? Tab.HeroBonds : Tab.StoryProgress);
        CanvasGroup incoming = GetContentGroup(tab);

        // Activate incoming immediately for the transition to play
        if (incoming != null)
        {
            incoming.gameObject.SetActive(true);
            PersonaUIStyle.ApplySlashTransition(incoming, true, 0.25f);
        }

        // Deactivate old content after its slash-out finishes
        // Stop any previously queued deactivation to prevent it from disabling a tab
        // the user just switched back to during rapid tab switching
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }

        if (outgoing != null && outgoing.gameObject != incoming?.gameObject)
        {
            PersonaUIStyle.ApplySlashTransition(outgoing, false, 0.2f);
            deactivateCoroutine = StartCoroutine(DeactivateAfterDelay(outgoing.gameObject, 0.22f));
        }

        // Ensure the selected content is active (fallback if no outgoing)
        storyContent.SetActive(tab == Tab.StoryProgress || storyContent.activeSelf);
        textsContent.SetActive(tab == Tab.AncientTexts || textsContent.activeSelf);
        bondsContent.SetActive(tab == Tab.HeroBonds || bondsContent.activeSelf);

        // Only the correct tab should be active
        storyContent.SetActive(tab == Tab.StoryProgress);
        textsContent.SetActive(tab == Tab.AncientTexts);
        bondsContent.SetActive(tab == Tab.HeroBonds);

        RefreshAllTabs();
    }

    // ------------------------------------------------------------------
    // Story Progress tab
    // ------------------------------------------------------------------

    private GameObject CreateStoryContent(Transform parent)
    {
        GameObject root = CreateUIElement("StoryContent", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);
        root.AddComponent<CanvasGroup>();

        VerticalLayoutGroup vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f;
        vlg.padding = new RectOffset(20, 20, 16, 16);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;

        storyActLabel = CreateLayoutLabel(root.transform, "ActLabel", 26, FontStyle.Bold, HeaderColor, 40f);
        storyIslandsLabel = CreateLayoutLabel(root.transform, "IslandsLabel", 20, FontStyle.Normal, TextColor, 30f);
        storyRestorationLabel = CreateLayoutLabel(root.transform, "RestorationLabel", 20, FontStyle.Normal, TextColor, 30f);
        storyObjectiveLabel = CreateLayoutLabel(root.transform, "ObjectiveLabel", 20, FontStyle.Italic, DimTextColor, 40f);
        storyDefeatLabel = CreateLayoutLabel(root.transform, "DefeatLabel", 18, FontStyle.Normal, PersonaUIStyle.AccentRed, 28f);

        return root;
    }

    private void RefreshStoryProgress()
    {
        GameStateManager gsm = GameStateManager.Instance;
        IslandProgressionManager ipm = IslandProgressionManager.Instance;
        IslandRestorationTracker irt = IslandRestorationTracker.Instance;
        AncientTextRevealDirector atdr = AncientTextRevealDirector.Instance;

        if (gsm == null)
        {
            SetLabel(storyActLabel, "Story data unavailable.");
            return;
        }

        string actRoman = ActToRoman(gsm.CurrentStoryAct);
        string actDesc = GetActDescription(gsm.CurrentStoryAct);
        SetLabel(storyActLabel, $"Act {actRoman}  --  {actDesc}");

        // Island progress
        IReadOnlyList<string> progression = IslandThemeRegistry.ProgressionOrder;
        int totalIslands = progression != null ? progression.Count : 7;
        int clearedIslands = 0;
        if (ipm != null && progression != null)
        {
            for (int i = 0; i < totalIslands; i++)
            {
                float restoration = gsm.GetIslandRestorationPercent(progression[i]);
                if (restoration >= 99.9f)
                {
                    clearedIslands++;
                }
            }
        }

        SetLabel(storyIslandsLabel, $"Islands Restored: {clearedIslands} / {totalIslands}");

        // Current island restoration
        string activeIslandId = ipm != null ? ipm.ActiveIslandId : "island_lust";
        float currentRestoration = gsm.GetIslandRestorationPercent(activeIslandId);
        string islandDisplayName = FormatIslandName(activeIslandId);
        SetLabel(storyRestorationLabel, $"{islandDisplayName} Restoration: {Mathf.RoundToInt(currentRestoration)}%");

        // Next objective
        string objective = DetermineNextObjective(gsm, ipm, progression);
        SetLabel(storyObjectiveLabel, $"Next: {objective}");

        // Bad ending defeats
        if (gsm.ResolvedEndingBranch == GameStateManager.EndingBranch.Bad)
        {
            string finalIslandId = progression != null && progression.Count > 0
                ? progression[progression.Count - 1]
                : "";
            int defeats = gsm.GetFinalBossDefeatCount(finalIslandId);
            int threshold = gsm.GetConfiguredFinalBossDefeatThreshold(finalIslandId);
            SetLabel(storyDefeatLabel, $"Bad Ending Threshold: {defeats} / {threshold} defeats");
            storyDefeatLabel.gameObject.SetActive(true);
        }
        else
        {
            storyDefeatLabel.gameObject.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // Ancient Texts tab
    // ------------------------------------------------------------------

    private GameObject CreateTextsContent(Transform parent)
    {
        GameObject root = CreateUIElement("TextsContent", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);
        root.AddComponent<CanvasGroup>();

        // Create a scroll view
        textsContent = CreateScrollView(root.transform, "TextsScroll");
        textsScrollContent = textsContent.GetComponentInChildren<RectTransform>();

        return root;
    }

    private void RefreshAncientTexts()
    {
        ClearChildren(textsScrollContent);

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            CreateReadOnlyLabel(textsScrollContent, "No data available.", 20, TextColor);
            return;
        }

        string[] discoveredIds = gsm.GetDiscoveredAncientTextIds();

        // Also include 100-year cycle fragments
        List<string> allIds = new List<string>();
        if (discoveredIds != null)
        {
            allIds.AddRange(discoveredIds);
        }

        // Add cycle fragments from AncientTextRevealDirector
        AncientTextRevealDirector atdr = AncientTextRevealDirector.Instance;
        if (atdr != null)
        {
            List<AncientTextRevealDirector.AncientTextFragment> fragments = atdr.GetDiscoveredFragments();
            for (int i = 0; i < fragments.Count; i++)
            {
                if (!allIds.Contains(fragments[i].fragmentId))
                {
                    allIds.Add(fragments[i].fragmentId);
                }
            }
        }

        // Add expanded texts
        if (ExpandedAncientTexts.AllTexts != null)
        {
            for (int i = 0; i < ExpandedAncientTexts.AllTexts.Length; i++)
            {
                ExpandedAncientTexts.ExtraTextDefinition txt = ExpandedAncientTexts.AllTexts[i];
                if (gsm.IsAncientTextDiscovered(txt.textId) && !allIds.Contains(txt.textId))
                {
                    allIds.Add(txt.textId);
                }
            }
        }

        if (allIds.Count == 0)
        {
            CreateReadOnlyLabel(textsScrollContent, "No ancient texts discovered yet.\n\nExplore the islands and restore them to uncover hidden inscriptions.", 20, DimTextColor);
            return;
        }

        // Section header
        CreateReadOnlyLabel(textsScrollContent, $"Discovered Texts: {allIds.Count}", 22, HeaderColor);

        for (int i = 0; i < allIds.Count; i++)
        {
            string textId = allIds[i];
            string title = textId;
            string body = "";
            bool discovered = false;

            if (gsm.TryGetAncientTextEntry(textId, out string entryTitle, out string entryBody, out bool entryDiscovered))
            {
                title = entryTitle;
                body = entryBody;
                discovered = entryDiscovered;
            }

            if (!discovered) continue;

            CreateTextEntry(textsScrollContent, i + 1, title, body, textId);
        }

        // Add spacing element at bottom
        GameObject spacer = CreateUIElement("Spacer", textsScrollContent);
        LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
        spacerLe.preferredHeight = 20f;
    }

    private void CreateTextEntry(RectTransform parent, int index, string title, string body, string textId)
    {
        // Entry card
        GameObject card = CreateUIElement($"TextEntry_{index}", parent);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(PersonaUIStyle.MediumBlue.r, PersonaUIStyle.MediumBlue.g, PersonaUIStyle.MediumBlue.b, 0.8f);

        VerticalLayoutGroup cardVlg = card.AddComponent<VerticalLayoutGroup>();
        cardVlg.spacing = 6f;
        cardVlg.padding = new RectOffset(14, 14, 10, 10);
        cardVlg.childForceExpandWidth = true;
        cardVlg.childForceExpandHeight = false;

        LayoutElement cardLe = card.AddComponent<LayoutElement>();
        cardLe.preferredHeight = 60f;
        cardLe.minHeight = 50f;

        // Title row
        string heroTag = ResolveHeroTagForText(textId);
        string heroPart = string.IsNullOrEmpty(heroTag) ? "" : $"  [{heroTag}]";
        string titleLine = $"{index}. {title}{heroPart}";
        CreateReadOnlyLabel(card.GetComponent<RectTransform>(), titleLine, 18, FontStyle.Bold, HeaderColor);

        // Body preview (truncated)
        string preview = body;
        if (!string.IsNullOrEmpty(preview) && preview.Length > 120)
        {
            preview = preview.Substring(0, 117) + "...";
        }
        if (!string.IsNullOrEmpty(preview))
        {
            CreateReadOnlyLabel(card.GetComponent<RectTransform>(), preview, 15, DimTextColor);
        }
    }

    private static string ResolveHeroTagForText(string textId)
    {
        // Check expanded texts for related hero
        if (ExpandedAncientTexts.AllTexts != null)
        {
            for (int i = 0; i < ExpandedAncientTexts.AllTexts.Length; i++)
            {
                if (ExpandedAncientTexts.AllTexts[i].textId == textId)
                {
                    return FormatHeroName(ExpandedAncientTexts.AllTexts[i].relatedHeroId);
                }
            }
        }

        return "";
    }

    // ------------------------------------------------------------------
    // Hero Bonds tab
    // ------------------------------------------------------------------

    private GameObject CreateBondsContent(Transform parent)
    {
        GameObject root = CreateUIElement("BondsContent", parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        StretchFull(rootRect);
        root.AddComponent<CanvasGroup>();

        bondsContent = CreateScrollView(root.transform, "BondsScroll");
        bondsScrollContent = bondsContent.GetComponentInChildren<RectTransform>();

        return root;
    }

    private void RefreshHeroBonds()
    {
        ClearChildren(bondsScrollContent);

        DialogueSystem ds = DialogueSystem.Instance;
        if (ds == null)
        {
            CreateReadOnlyLabel(bondsScrollContent, "Dialogue system not available.", 20, TextColor);
            return;
        }

        CreateReadOnlyLabel(bondsScrollContent, "Hero Pair Bonds", 22, HeaderColor);

        // Generate all 10 pairs from 5 heroes
        int pairCount = 0;
        for (int a = 0; a < HeroIds.Length; a++)
        {
            for (int b = a + 1; b < HeroIds.Length; b++)
            {
                pairCount++;
                int bondLevel = ds.GetBondLevel(HeroIds[a], HeroIds[b]);
                string labelA = HeroDisplayNames[a];
                string labelB = HeroDisplayNames[b];
                string relationship = GetBondRelationshipName(bondLevel);
                CreateBondEntry(bondsScrollContent, pairCount, labelA, labelB, bondLevel, relationship);
            }
        }

        // Spacer
        GameObject spacer = CreateUIElement("Spacer", bondsScrollContent);
        LayoutElement spacerLe = spacer.AddComponent<LayoutElement>();
        spacerLe.preferredHeight = 20f;
    }

    private void CreateBondEntry(RectTransform parent, int index, string heroA, string heroB, int level, string relationship)
    {
        GameObject card = CreateUIElement($"Bond_{index}", parent);
        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(PersonaUIStyle.MediumBlue.r, PersonaUIStyle.MediumBlue.g, PersonaUIStyle.MediumBlue.b, 0.8f);

        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 16f;
        hlg.padding = new RectOffset(14, 14, 8, 8);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        LayoutElement cardLe = card.AddComponent<LayoutElement>();
        cardLe.preferredHeight = 44f;

        // Pair label
        GameObject pairObj = CreateUIElement("Pair", card.transform);
        LayoutElement pairLe = pairObj.AddComponent<LayoutElement>();
        pairLe.preferredWidth = 240f;
        pairLe.flexibleWidth = 0f;
        Text pairText = CreateLabel(pairObj.transform, "Label", 16, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
        RectTransform pairTextRect = pairText.GetComponent<RectTransform>();
        StretchFull(pairTextRect);
        pairText.text = $"{heroA} + {heroB}";

        // Bond bar container
        GameObject barContainer = CreateUIElement("BarContainer", card.transform);
        LayoutElement barContLe = barContainer.AddComponent<LayoutElement>();
        barContLe.preferredWidth = 300f;
        barContLe.flexibleWidth = 0f;

        // Background bar
        Image barBg = barContainer.AddComponent<Image>();
        barBg.color = new Color(PersonaUIStyle.DeepNavy.r, PersonaUIStyle.DeepNavy.g, PersonaUIStyle.DeepNavy.b, 1f);

        // Fill bar
        GameObject fillObj = CreateUIElement("Fill", barContainer.transform);
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2((float)level / 100f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = GetBondColor(level);

        // Level text on bar
        Text levelText = CreateLabel(barContainer.transform, "Level", 14, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform levelTextRect = levelText.GetComponent<RectTransform>();
        StretchFull(levelTextRect);
        levelText.text = $"{level}";

        // Relationship label
        GameObject relObj = CreateUIElement("Rel", card.transform);
        LayoutElement relLe = relObj.AddComponent<LayoutElement>();
        relLe.preferredWidth = 160f;
        relLe.flexibleWidth = 0f;
        Text relText = CreateLabel(relObj.transform, "Label", 15, FontStyle.Normal, TextAnchor.MiddleLeft, DimTextColor);
        RectTransform relTextRect = relText.GetComponent<RectTransform>();
        StretchFull(relTextRect);
        relText.text = relationship;
    }

    // ------------------------------------------------------------------
    // Refresh
    // ------------------------------------------------------------------

    private void RefreshAllTabs()
    {
        switch (activeTab)
        {
            case Tab.StoryProgress:
                RefreshStoryProgress();
                break;
            case Tab.AncientTexts:
                RefreshAncientTexts();
                break;
            case Tab.HeroBonds:
                RefreshHeroBonds();
                break;
        }
    }

    // ------------------------------------------------------------------
    // Tab transition helpers
    // ------------------------------------------------------------------

    private CanvasGroup GetContentGroup(Tab tab)
    {
        GameObject contentObj = tab == Tab.StoryProgress ? storyContent :
                                tab == Tab.AncientTexts ? textsContent : bondsContent;
        return contentObj != null ? contentObj.GetComponent<CanvasGroup>() : null;
    }

    private Coroutine deactivateCoroutine;

    private System.Collections.IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (obj != null)
        {
            obj.SetActive(false);
        }
        deactivateCoroutine = null;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string ActToRoman(GameStateManager.StoryAct act)
    {
        switch (act)
        {
            case GameStateManager.StoryAct.ActI: return "I";
            case GameStateManager.StoryAct.ActII: return "II";
            case GameStateManager.StoryAct.ActIII: return "III";
            default: return "I";
        }
    }

    private static string GetActDescription(GameStateManager.StoryAct act)
    {
        switch (act)
        {
            case GameStateManager.StoryAct.ActI:
                return "The Awakening -- Restore the first islands and learn of the cycle.";
            case GameStateManager.StoryAct.ActII:
                return "The Deepening -- The corruption grows, and the truth unfolds.";
            case GameStateManager.StoryAct.ActIII:
                return "The Reckoning -- Face the final island and the weight of destiny.";
            default:
                return "";
        }
    }

    private static string DetermineNextObjective(GameStateManager gsm, IslandProgressionManager ipm, IReadOnlyList<string> progression)
    {
        if (progression == null || progression.Count == 0)
        {
            return "Continue exploring.";
        }

        string activeIsland = ipm != null ? ipm.ActiveIslandId : progression[0];
        float restoration = gsm.GetIslandRestorationPercent(activeIsland);

        if (restoration >= 99.9f)
        {
            // Current island fully restored -- find next un-restored island
            for (int i = 0; i < progression.Count; i++)
            {
                float r = gsm.GetIslandRestorationPercent(progression[i]);
                if (r < 99.9f)
                {
                    return $"Travel to {FormatIslandName(progression[i])} and restore its balance.";
                }
            }
            return "All islands restored. The cycle nears its end.";
        }

        string currentName = FormatIslandName(activeIsland);
        if (restoration < 75f)
        {
            return $"Restore {currentName} by completing encounters and puzzles. ({Mathf.RoundToInt(restoration)}% complete)";
        }

        return $"The final challenge on {currentName} awaits. Push toward full restoration.";
    }

    private static string FormatIslandName(string islandId)
    {
        if (string.IsNullOrEmpty(islandId)) return "Unknown Island";

        // Convert "island_lust" to "Lust"
        string cleaned = islandId.Replace("island_", "");
        if (cleaned.Length == 0) return islandId;
        return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
    }

    private static string FormatHeroName(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return "";
        for (int i = 0; i < HeroIds.Length; i++)
        {
            if (HeroIds[i] == heroId) return HeroDisplayNames[i];
        }
        return heroId;
    }

    private static string GetBondRelationshipName(int level)
    {
        if (level <= 0) return "Strangers";
        if (level <= 20) return "Uneasy";
        if (level <= 40) return "Acquaintances";
        if (level <= 60) return "Comrades";
        if (level <= 80) return "Close Allies";
        return "Inseparable";
    }

    private static Color GetBondColor(int level)
    {
        float t = Mathf.Clamp01((float)level / 100f);
        if (t < 0.4f)
        {
            return Color.Lerp(new Color(0.7f, 0.2f, 0.2f, 1f), new Color(0.8f, 0.7f, 0.2f, 1f), t / 0.4f);
        }
        return Color.Lerp(new Color(0.8f, 0.7f, 0.2f, 1f), new Color(0.2f, 0.75f, 0.3f, 1f), (t - 0.4f) / 0.6f);
    }

    private static void SetLabel(Text label, string text)
    {
        if (label != null) label.text = text;
    }

    // ------------------------------------------------------------------
    // UI building utilities
    // ------------------------------------------------------------------

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Text CreateLabel(Transform parent, string name, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject obj = CreateUIElement(name, parent);
        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Text CreateLayoutLabel(Transform parent, string name, int fontSize, FontStyle style, Color color, float preferredHeight)
    {
        GameObject obj = CreateUIElement(name, parent);
        Text text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.UpperLeft;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = preferredHeight;

        return text;
    }

    private static void CreateReadOnlyLabel(RectTransform parent, string text, int fontSize, Color color)
    {
        CreateReadOnlyLabel(parent, text, fontSize, FontStyle.Normal, color);
    }

    private static void CreateReadOnlyLabel(RectTransform parent, string text, int fontSize, FontStyle style, Color color)
    {
        GameObject obj = CreateUIElement("Label", parent);
        Text txt = obj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.alignment = TextAnchor.UpperLeft;
        txt.color = color;
        txt.text = text;
        txt.raycastTarget = false;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        ContentSizeFitter csf = obj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize * 1.4f + 4f;
        le.flexibleWidth = 1f;
    }

    private static GameObject CreateButton(Transform parent, string name, string label, int fontSize, Color bgColor)
    {
        GameObject obj = CreateUIElement(name, parent);
        Image img = obj.AddComponent<Image>();
        img.color = bgColor;

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = bgColor * 1.2f;
        cb.pressedColor = bgColor * 0.8f;
        btn.colors = cb;

        Text txt = CreateLabel(obj.transform, "Label", fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        RectTransform txtRect = txt.GetComponent<RectTransform>();
        StretchFull(txtRect);
        txt.text = label;

        return obj;
    }

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        // ScrollView root
        GameObject scrollRoot = CreateUIElement(name, parent);
        RectTransform scrollRect = scrollRoot.GetComponent<RectTransform>();
        StretchFull(scrollRect);

        Image scrollBg = scrollRoot.AddComponent<Image>();
        scrollBg.color = new Color(PersonaUIStyle.DeepNavy.r, PersonaUIStyle.DeepNavy.g, PersonaUIStyle.DeepNavy.b, 0.5f);

        ScrollRect scroll = scrollRoot.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Viewport
        GameObject viewport = CreateUIElement("Viewport", scrollRoot.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        StretchFull(viewportRect);
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.clear;
        scroll.viewport = viewportRect;

        // Mask
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(10f, 0f);
        contentRect.offsetMax = new Vector2(-10f, 0f);
        scroll.content = contentRect;

        VerticalLayoutGroup contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.spacing = 8f;
        contentVlg.padding = new RectOffset(4, 4, 8, 8);
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;
        contentVlg.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter contentCsf = content.AddComponent<ContentSizeFitter>();
        contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return scrollRoot;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
