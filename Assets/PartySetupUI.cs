using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PartySetupUI : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    [SerializeField] private KeyCode gearCycleKey = KeyCode.Return;

    [Header("Layout")]
    [SerializeField] private float panelWidth = 400f;
    [SerializeField] private float panelHeight = 520f;
    [SerializeField] private float heroRowHeight = 72f;
    [SerializeField] private float padding = 16f;

    [Header("Colors")]
    [SerializeField] private Color panelBackground = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    [SerializeField] private Color activeSlotColor = new Color(0.21f, 0.73f, 0.84f, 0.8f);
    [SerializeField] private Color reserveSlotColor = new Color(0.45f, 0.45f, 0.5f, 0.6f);
    [SerializeField] private Color titleColor = new Color(0.95f, 0.9f, 0.7f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color activeBadgeColor = new Color(0.3f, 0.85f, 0.4f);
    [SerializeField] private Color reserveBadgeColor = new Color(0.6f, 0.6f, 0.65f);

    private bool isOpen;
    private Canvas menuCanvas;
    private GameObject panelRoot;
    private string selectedHeroId;

    public bool IsOpen => isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (CanToggle())
            {
                ToggleMenu();
            }
        }

        if (isOpen && Input.GetKeyDown(gearCycleKey) && !string.IsNullOrEmpty(selectedHeroId))
        {
            CycleGearForHero(selectedHeroId);
        }
    }

    private bool CanToggle()
    {
        if (GameStateManager.Instance == null)
        {
            return true;
        }

        return GameStateManager.Instance.currentState == GameStateManager.GameState.Exploration
            && !GameStateManager.Instance.IsTransitioning;
    }

    public void ToggleMenu()
    {
        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isOpen) return;

        if (PartyManager.Instance == null || PartyManager.Instance.PartyData == null)
        {
            Debug.LogWarning("[PartySetupUI] Cannot open: no PartyManager or PartyData found.");
            return;
        }

        isOpen = true;
        EnsureCanvas();
        RebuildPanel();
    }

    public void CloseMenu()
    {
        if (!isOpen) return;

        isOpen = false;
        if (panelRoot != null)
        {
            Destroy(panelRoot);
            panelRoot = null;
        }

        if (menuCanvas != null)
        {
            Destroy(menuCanvas.gameObject);
            menuCanvas = null;
        }
    }

    private void EnsureCanvas()
    {
        if (menuCanvas != null) return;

        GameObject canvasObject = new GameObject("PartySetupCanvas");
        canvasObject.transform.SetParent(transform, false);

        menuCanvas = canvasObject.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void RebuildPanel()
    {
        if (panelRoot != null)
        {
            Destroy(panelRoot);
        }

        PartyData party = PartyManager.Instance.PartyData;
        HeroDatabase db = PartyManager.Instance.HeroDatabase;
        if (db == null)
        {
            Debug.LogWarning("[PartySetupUI] HeroDatabase not assigned to PartyManager.");
            return;
        }

        HeroData[] allHeroes = db.GetAllHeroes();
        if (allHeroes == null || allHeroes.Length == 0)
        {
            Debug.LogWarning("[PartySetupUI] No heroes found in HeroDatabase.");
            return;
        }

        float totalHeight = panelHeight;
        panelRoot = new GameObject("PartyPanel");
        panelRoot.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, totalHeight);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = panelBackground;

        float currentY = -padding;

        CreateLabel(panelRoot, "PARTY SELECTION", new Vector2(padding, currentY), new Vector2(panelWidth - padding * 2, 30f), titleColor, 20, FontStyle.Bold);
        currentY += 36f;

        CreateLabel(panelRoot, "ACTIVE", new Vector2(padding, currentY), new Vector2(panelWidth - padding * 2, 20f), activeBadgeColor, 14, FontStyle.Bold);
        currentY += 24f;

        int activeCount = 0;
        for (int i = 0; i < allHeroes.Length; i++)
        {
            HeroData hero = allHeroes[i];
            if (hero == null) continue;

            bool isActive = party.IsHeroActive(hero.heroId);
            if (!isActive) continue;

            CreateHeroRow(panelRoot, hero, isActive, currentY, party);
            currentY += heroRowHeight;
            activeCount++;
        }

        currentY += 6f;
        CreateLabel(panelRoot, "RESERVE", new Vector2(padding, currentY), new Vector2(panelWidth - padding * 2, 20f), reserveBadgeColor, 14, FontStyle.Bold);
        currentY += 24f;

        for (int i = 0; i < allHeroes.Length; i++)
        {
            HeroData hero = allHeroes[i];
            if (hero == null) continue;

            bool isActive = party.IsHeroActive(hero.heroId);
            if (isActive) continue;

            CreateHeroRow(panelRoot, hero, isActive, currentY, party);
            currentY += heroRowHeight;
        }

        currentY += 10f;
        CreateLabel(panelRoot, $"Press {toggleKey} to close", new Vector2(padding, currentY), new Vector2(panelWidth - padding * 2, 20f), textColor, 12, FontStyle.Italic);
    }

    private void CreateHeroRow(GameObject parent, HeroData hero, bool isActive, float yPos, PartyData party)
    {
        GameObject rowObject = new GameObject($"HeroRow_{hero.heroId}");
        rowObject.transform.SetParent(parent.transform, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.offsetMin = new Vector2(padding, -(yPos + heroRowHeight - 4f));
        rowRect.offsetMax = new Vector2(-padding, -yPos);

        Image rowBg = rowObject.AddComponent<Image>();
        rowBg.color = isActive ? activeSlotColor : reserveSlotColor;

        Button button = rowObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = isActive ? activeSlotColor : reserveSlotColor;
        colors.highlightedColor = isActive ? new Color(0.25f, 0.78f, 0.88f, 0.9f) : new Color(0.55f, 0.55f, 0.6f, 0.7f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.3f, 0.9f);
        button.colors = colors;

        string heroId = hero.heroId;
        button.onClick.AddListener(() => OnHeroClicked(heroId));

        float textX = 10f;
        float textWidth = panelWidth - padding * 2 - 20f;

        string elementName = ResolveElementName(hero);
        string statusLabel = isActive ? "[ACTIVE]" : "[RESERVE]";
        string displayName = $"{hero.displayName} - {elementName}  {statusLabel}";

        CreateLabel(rowObject, displayName, new Vector2(textX, 4f), new Vector2(textWidth, 20f), textColor, 14, FontStyle.Bold);

        string stats = BuildStatsLine(hero);
        CreateLabel(rowObject, stats, new Vector2(textX, 24f), new Vector2(textWidth, 14f), new Color(0.7f, 0.7f, 0.7f), 11, FontStyle.Normal);

        string gearText = BuildGearLine(hero);
        CreateLabel(rowObject, gearText, new Vector2(textX, 38f), new Vector2(textWidth, 14f), new Color(0.6f, 0.85f, 0.6f), 10, FontStyle.Italic);
    }

    private void OnHeroClicked(string heroId)
    {
        selectedHeroId = heroId;

        if (PartyManager.Instance == null) return;

        PartyData party = PartyManager.Instance.PartyData;
        if (party == null) return;

        bool wasActive = PartyManager.Instance.IsHeroActive(heroId);

        if (wasActive)
        {
            if (party.GetActiveCount() <= 1)
            {
                Debug.Log("[PartySetupUI] Cannot remove the last active hero.");
                return;
            }

            PartyManager.Instance.ToggleHeroActive(heroId);
            RebuildPanel();
            return;
        }

        if (party.GetActiveCount() >= 3)
        {
            Debug.Log("[PartySetupUI] Active party is full (3/3). Remove a hero first.");
            return;
        }

        PartyManager.Instance.ToggleHeroActive(heroId);
        RebuildPanel();
    }

    private string ResolveElementName(HeroData hero)
    {
        if (PartyManager.Instance != null)
        {
            CombatUnit.Element resolved = PartyManager.Instance.ResolveElement(hero);
            if (resolved != CombatUnit.Element.None)
            {
                return resolved.ToString();
            }
        }

        return hero.element.ToString();
    }

    private static void CreateLabel(GameObject parent, string text, Vector2 position, Vector2 size, Color color, int fontSize, FontStyle fontStyle)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = position;
        labelRect.sizeDelta = size;

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
    }

    private string BuildStatsLine(HeroData hero)
    {
        if (HeroProgressionManager.Instance != null)
        {
            int level = HeroProgressionManager.Instance.GetLevel(hero.heroId);
            int bonus = level - 1;
            LevelingConfig config = HeroProgressionManager.Instance.LevelingConfig;
            GearSetData gear = HeroProgressionManager.Instance.GetEquippedGearSet(hero.heroId);

            int baseHp = hero.baseMaxHP;
            int baseAtk = hero.baseAttack;
            int baseDef = hero.baseDefense;
            int baseSpd = hero.baseSpeed;

            if (config != null && bonus > 0)
            {
                baseHp += bonus * config.hpPerLevel;
                baseAtk += bonus * config.attackPerLevel;
                baseDef += bonus * config.defensePerLevel;
                baseSpd += bonus * config.speedPerLevel;
            }

            float atkPct = gear != null ? gear.TotalAttackPercent : 0f;
            float defPct = gear != null ? gear.TotalDefensePercent : 0f;
            float hpPct = gear != null ? gear.TotalHpPercent : 0f;

            int finalHp = baseHp + Mathf.RoundToInt(baseHp * hpPct);
            int finalAtk = baseAtk + Mathf.RoundToInt(baseAtk * atkPct);
            int finalDef = baseDef + Mathf.RoundToInt(baseDef * defPct);

            return $"Lv.{level}  HP {finalHp}  ATK {finalAtk}  DEF {finalDef}  SPD {baseSpd}";
        }

        return $"HP {hero.baseMaxHP}  ATK {hero.baseAttack}  DEF {hero.baseDefense}  SPD {hero.baseSpeed}";
    }

    private string BuildGearLine(HeroData hero)
    {
        if (HeroProgressionManager.Instance != null)
        {
            GearSetData equipped = HeroProgressionManager.Instance.GetEquippedGearSet(hero.heroId);
            if (equipped != null)
            {
                string tag = hero.heroId == selectedHeroId ? " [Enter: cycle]" : "";
                return $"Gear: {equipped.displayName}{tag}";
            }
        }

        if (hero.heroId == selectedHeroId)
        {
            return "Gear: None (Enter to equip)";
        }

        return "Gear: None";
    }

    private void CycleGearForHero(string heroId)
    {
        if (HeroProgressionManager.Instance == null)
        {
            Debug.LogWarning("[PartySetupUI] HeroProgressionManager not found.");
            return;
        }

        GearSetData[] available = HeroProgressionManager.Instance.AvailableGearSets;
        if (available == null || available.Length == 0)
        {
            Debug.Log("[PartySetupUI] No gear sets available.");
            return;
        }

        GearSetData current = HeroProgressionManager.Instance.GetEquippedGearSet(heroId);
        if (current == null)
        {
            HeroProgressionManager.Instance.EquipGearSet(heroId, available[0]);
        }
        else
        {
            int currentIndex = -1;
            for (int i = 0; i < available.Length; i++)
            {
                if (available[i] != null && available[i].setId == current.setId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % available.Length;
            if (nextIndex < available.Length && available[nextIndex] != null)
            {
                HeroProgressionManager.Instance.EquipGearSet(heroId, available[nextIndex]);
            }
            else
            {
                HeroProgressionManager.Instance.UnequipGearSet(heroId);
            }
        }

        RebuildPanel();
    }

    private void OnDestroy()
    {
        CloseMenu();
    }
}
