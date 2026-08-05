using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Player gear inventory screen. Lists owned gear (name, rarity, level,
/// rolled bonus slots, equipped hero) and supports equip/unequip via mouse,
/// keyboard, and gamepad. Open from the 'I' key or via OpenMenu()/ToggleMenu()
/// (pause menu integration by issue 294).
/// </summary>
[DisallowMultipleComponent]
public class GearInventoryUI : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Layout")]
    [SerializeField] private float panelWidth = 600f;
    [SerializeField] private float panelHeight = 640f;
    [SerializeField] private float rowHeight = 58f;
    [SerializeField] private float padding = 16f;

    [Header("Colors")]
    [SerializeField] private Color panelBackground = new Color(0.10f, 0.12f, 0.16f, 0.96f);
    [SerializeField] private Color titleColor = new Color(0.95f, 0.9f, 0.7f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color rowColor = new Color(0.22f, 0.26f, 0.34f, 0.85f);
    [SerializeField] private Color selectedRowColor = new Color(0.36f, 0.44f, 0.58f, 0.9f);
    [SerializeField] private Color equippedRowColor = new Color(0.2f, 0.42f, 0.3f, 0.85f);
    [SerializeField] private Color buttonColor = new Color(0.25f, 0.55f, 0.85f, 0.9f);
    [SerializeField] private Color disabledColor = new Color(0.35f, 0.35f, 0.4f, 0.8f);
    [SerializeField] private Color newDropColor = new Color(0.55f, 0.95f, 1f);

    private const int MaxVisibleRows = 8;

    private static readonly Color CommonColor = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color UncommonColor = new Color(0.45f, 0.9f, 0.45f);
    private static readonly Color RareColor = new Color(0.45f, 0.65f, 1f);
    private static readonly Color EpicColor = new Color(0.75f, 0.5f, 1f);
    private static readonly Color LegendaryColor = new Color(1f, 0.85f, 0.35f);

    private bool isOpen;
    private Canvas menuCanvas;
    private GameObject panelRoot;
    private int selectedRowIndex;
    private int selectedHeroIndex;
    private int scrollOffset;

    public bool IsOpen => isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }

        if (!isOpen)
        {
            return;
        }

        bool navUp = Input.GetKeyDown(KeyCode.UpArrow);
        bool navDown = Input.GetKeyDown(KeyCode.DownArrow);
        bool heroLeft = Input.GetKeyDown(KeyCode.LeftArrow);
        bool heroRight = Input.GetKeyDown(KeyCode.RightArrow);
        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
        bool unequip = Input.GetKeyDown(KeyCode.X);
        bool close = Input.GetKeyDown(KeyCode.Escape);

        GamepadInputManager gamepad = GamepadInputManager.Instance;
        if (gamepad != null && gamepad.IsGamepadConnected)
        {
            navUp = navUp || gamepad.NavUpPressed;
            navDown = navDown || gamepad.NavDownPressed;
            heroLeft = heroLeft || gamepad.TabLeftPressed;
            heroRight = heroRight || gamepad.TabRightPressed;
            confirm = confirm || gamepad.ConfirmPressed;
            close = close || gamepad.BackPressed;
        }

        if (navUp)
        {
            MoveSelection(-1);
        }
        else if (navDown)
        {
            MoveSelection(1);
        }

        if (heroLeft)
        {
            CycleHero(-1);
        }
        else if (heroRight)
        {
            CycleHero(1);
        }

        if (confirm)
        {
            OnEquipSelected();
        }

        if (unequip)
        {
            OnUnequipSelectedHero();
        }

        if (close)
        {
            CloseMenu();
        }
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
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        selectedRowIndex = 0;
        scrollOffset = 0;
        EnsureCanvas();
        RebuildPanel();
    }

    public void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

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
        if (menuCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("GearInventoryCanvas");
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

    private static void EnsureEventSystem()
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

        PlayerGearInventory inventory = PlayerGearInventory.Instance;
        HeroProgressionManager progression = HeroProgressionManager.Instance;
        if (inventory == null || progression == null)
        {
            return;
        }

        IReadOnlyList<GearInstance> owned = inventory.GetOwnedGear();
        HeroData[] heroes = GetPartyHeroes(progression);
        int heroCount = heroes != null ? heroes.Length : 0;
        if (heroCount > 0 && selectedHeroIndex >= heroCount)
        {
            selectedHeroIndex = heroCount - 1;
        }

        int visibleRowCount = Mathf.Min(owned.Count, MaxVisibleRows);
        float contentHeight = 150f + visibleRowCount * rowHeight + 60f;
        float totalHeight = Mathf.Max(panelHeight, contentHeight);

        panelRoot = new GameObject("GearInventoryPanel");
        panelRoot.transform.SetParent(menuCanvas.transform, false);

        RectTransform panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelWidth, totalHeight);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = panelBackground;

        float y = padding;
        CreateLabel(panelRoot, "GEAR INVENTORY", new Vector2(padding, y), new Vector2(panelWidth - padding * 2, 30f), titleColor, 20, FontStyle.Bold);
        y += 36f;

        CreateHeroSelector(panelRoot, heroes, heroCount, inventory, y);
        y += 58f;

        CreateLegend(panelRoot, y);
        y += 26f;

        if (owned.Count == 0)
        {
            CreateLabel(panelRoot, "No gear owned yet. Defeat enemies to earn drops.", new Vector2(padding, y), new Vector2(panelWidth - padding * 2, 26f), textColor, 14, FontStyle.Italic);
            y += 34f;
        }
        else
        {
            int visibleCount = Mathf.Min(MaxVisibleRows, owned.Count);
            if (scrollOffset > owned.Count - visibleCount)
            {
                scrollOffset = Mathf.Max(0, owned.Count - visibleCount);
            }

            int shown = 0;
            for (int i = scrollOffset; i < owned.Count && shown < visibleCount; i++, shown++)
            {
                CreateGearRow(panelRoot, owned[i], i, inventory, y);
                y += rowHeight;
            }

            if (owned.Count > visibleCount)
            {
                CreateLabel(panelRoot, $"Showing {scrollOffset + 1}-{Mathf.Min(scrollOffset + visibleCount, owned.Count)} of {owned.Count} (Up/Down scroll)", new Vector2(padding, y), new Vector2(panelWidth - padding * 2, 16f), textColor, 10, FontStyle.Italic);
                y += 20f;
            }
        }

        y += 6f;
        CreateLabel(panelRoot, "Up/Down: select gear   Left/Right: hero   Enter: equip   X: unequip hero   I/ESC: close", new Vector2(padding, y), new Vector2(panelWidth - padding * 2, 20f), textColor, 11, FontStyle.Italic);
    }

    private void CreateHeroSelector(GameObject parent, HeroData[] heroes, int heroCount, PlayerGearInventory inventory, float yPos)
    {
        if (heroCount == 0)
        {
            CreateLabel(parent, "No party heroes available.", new Vector2(padding, yPos), new Vector2(panelWidth - padding * 2, 22f), textColor, 13, FontStyle.Normal);
            return;
        }

        HeroData hero = heroes[selectedHeroIndex];
        string heroName = hero != null && !string.IsNullOrEmpty(hero.displayName) ? hero.displayName : "Unknown Hero";
        GearInstance equipped = hero != null ? inventory.GetEquipped(hero.heroId) : null;
        string equippedText = equipped != null
            ? $"Equipped: {PlayerGearInventory.GetDisplayName(equipped)} ({equipped.rarity})"
            : "Equipped: none";

        CreateLabel(parent, $"Hero ({selectedHeroIndex + 1}/{heroCount}): {heroName}  [< >]", new Vector2(padding, yPos), new Vector2(panelWidth - padding * 2, 22f), titleColor, 14, FontStyle.Bold);
        CreateLabel(parent, equippedText, new Vector2(padding, yPos + 24f), new Vector2(panelWidth - padding * 2, 20f), textColor, 12, FontStyle.Normal);
    }

    private void CreateLegend(GameObject parent, float yPos)
    {
        float x = padding;
        string[] names = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        Color[] colors = { CommonColor, UncommonColor, RareColor, EpicColor, LegendaryColor };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject labelObject = new GameObject($"Legend_{i}");
            labelObject.transform.SetParent(parent.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(x, -yPos);
            labelRect.sizeDelta = new Vector2(100f, 16f);
            Text label = labelObject.AddComponent<Text>();
            label.text = names[i];
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 11;
            label.color = colors[i];
            label.alignment = TextAnchor.MiddleLeft;
            x += 100f;
        }
    }

    private void CreateGearRow(GameObject parent, GearInstance gear, int index, PlayerGearInventory inventory, float yPos)
    {
        if (gear == null)
        {
            return;
        }

        bool isSelected = index == selectedRowIndex;
        bool isEquipped = inventory.IsEquipped(gear.instanceId);
        Color bgColor = isSelected ? selectedRowColor : (isEquipped ? equippedRowColor : rowColor);

        GameObject rowObject = new GameObject($"GearRow_{index}");
        rowObject.transform.SetParent(parent.transform, false);

        RectTransform rowRect = rowObject.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.offsetMin = new Vector2(padding, -(yPos + rowHeight - 4f));
        rowRect.offsetMax = new Vector2(-padding, -yPos);

        Image rowBg = rowObject.AddComponent<Image>();
        rowBg.color = bgColor;

        Button selectButton = rowObject.AddComponent<Button>();
        ColorBlock colors = selectButton.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = selectedRowColor;
        colors.pressedColor = new Color(0.5f, 0.55f, 0.65f, 0.9f);
        selectButton.colors = colors;
        int rowIndex = index;
        selectButton.onClick.AddListener(() =>
        {
            selectedRowIndex = rowIndex;
            RebuildPanel();
        });

        float textX = 10f;
        float textWidth = panelWidth - padding * 2 - 20f;

        Color rarityColor = GetRarityColor(gear.rarity);
        string header = $"{PlayerGearInventory.GetDisplayName(gear)}  {gear.rarity}  Lv.{gear.level} [{gear.UnlockedSlotCount}/{GearInstance.MaxBonusSlots} slots]";
        CreateLabel(rowObject, header, new Vector2(textX, 3f), new Vector2(textWidth * 0.62f, 18f), rarityColor, 13, FontStyle.Bold);

        string slotText = gear.UnlockedSlotCount > 0 ? gear.GetSlotDisplayString() : "No bonus slots";
        CreateLabel(rowObject, slotText, new Vector2(textX, 21f), new Vector2(textWidth * 0.62f, 14f), new Color(0.85f, 0.85f, 0.65f), 10, FontStyle.Normal);

        bool isNewDrop = IsNewDrop(gear);
        string stateText;
        if (isEquipped)
        {
            stateText = "EQUIPPED";
        }
        else if (isNewDrop)
        {
            stateText = "NEW DROP";
        }
        else
        {
            stateText = "";
        }

        if (!string.IsNullOrEmpty(stateText))
        {
            CreateLabel(rowObject, stateText, new Vector2(textX, 37f), new Vector2(textWidth * 0.5f, 14f), isNewDrop ? newDropColor : new Color(0.6f, 0.95f, 0.6f), 11, FontStyle.Bold);
        }

        CreateEquipButton(rowObject, gear, inventory, new Vector2(textWidth * 0.66f, 8f), new Vector2(textWidth * 0.16f, 42f));
        string equipLabel = GetEquipButtonText(gear, inventory);
        CreateLabel(rowObject, equipLabel, new Vector2(textWidth * 0.66f, 2f), new Vector2(textWidth * 0.16f, 18f), Color.white, 11, FontStyle.Bold);
    }

    private void CreateEquipButton(GameObject parent, GearInstance gear, PlayerGearInventory inventory, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject("EquipBtn");
        buttonObject.transform.SetParent(parent.transform, false);

        RectTransform btnRect = buttonObject.AddComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0f, 1f);
        btnRect.anchorMax = new Vector2(0f, 1f);
        btnRect.pivot = new Vector2(0f, 1f);
        btnRect.anchoredPosition = position;
        btnRect.sizeDelta = size;

        HeroData[] heroes = GetPartyHeroes(HeroProgressionManager.Instance);
        bool hasHero = heroes != null && heroes.Length > 0;
        bool isEquipped = inventory.IsEquipped(gear.instanceId);
        bool interactable = hasHero && !isEquipped;

        Image btnBg = buttonObject.AddComponent<Image>();
        btnBg.color = interactable ? buttonColor : disabledColor;

        Button button = buttonObject.AddComponent<Button>();
        button.interactable = interactable;
        GearInstance gearCopy = gear;
        button.onClick.AddListener(() => OnEquipClicked(gearCopy));
    }

    private string GetEquipButtonText(GearInstance gear, PlayerGearInventory inventory)
    {
        if (inventory.IsEquipped(gear.instanceId))
        {
            return "Equipped";
        }

        return "Equip";
    }

    private bool IsNewDrop(GearInstance gear)
    {
        PlayerGearInventory inventory = PlayerGearInventory.Instance;
        if (inventory == null || gear == null)
        {
            return false;
        }

        IReadOnlyList<GearInstance> drops = inventory.LastBattleDrops;
        for (int i = 0; i < drops.Count; i++)
        {
            if (drops[i] != null && drops[i].instanceId == gear.instanceId)
            {
                return true;
            }
        }

        return false;
    }

    private void OnEquipClicked(GearInstance gear)
    {
        if (gear == null)
        {
            return;
        }

        OnEquipInstance(gear);
    }

    private void OnEquipSelected()
    {
        IReadOnlyList<GearInstance> owned = PlayerGearInventory.Instance != null ? PlayerGearInventory.Instance.GetOwnedGear() : null;
        if (owned == null || selectedRowIndex < 0 || selectedRowIndex >= owned.Count)
        {
            return;
        }

        OnEquipInstance(owned[selectedRowIndex]);
    }

    private void OnEquipInstance(GearInstance gear)
    {
        HeroData[] heroes = GetPartyHeroes(HeroProgressionManager.Instance);
        PlayerGearInventory inventory = PlayerGearInventory.Instance;
        if (inventory == null || gear == null || heroes == null || heroes.Length == 0)
        {
            return;
        }

        if (inventory.IsEquipped(gear.instanceId))
        {
            return;
        }

        HeroData hero = heroes[selectedHeroIndex];
        if (hero == null)
        {
            return;
        }

        if (inventory.TryEquip(hero.heroId, gear.instanceId))
        {
            Debug.Log($"[GearInventoryUI] Equipped '{gear.setId}' ({gear.rarity}) on {hero.heroId}.");
        }

        RebuildPanel();
    }

    private void OnUnequipSelectedHero()
    {
        HeroData[] heroes = GetPartyHeroes(HeroProgressionManager.Instance);
        PlayerGearInventory inventory = PlayerGearInventory.Instance;
        if (inventory == null || heroes == null || heroes.Length == 0)
        {
            return;
        }

        HeroData hero = heroes[selectedHeroIndex];
        if (hero == null)
        {
            return;
        }

        if (inventory.TryUnequip(hero.heroId))
        {
            Debug.Log($"[GearInventoryUI] Unequipped gear from {hero.heroId}.");
        }

        RebuildPanel();
    }

    private void MoveSelection(int delta)
    {
        IReadOnlyList<GearInstance> owned = PlayerGearInventory.Instance != null ? PlayerGearInventory.Instance.GetOwnedGear() : null;
        int count = owned != null ? owned.Count : 0;
        if (count == 0)
        {
            return;
        }

        selectedRowIndex = Mathf.Clamp(selectedRowIndex + delta, 0, count - 1);
        int visibleCount = Mathf.Min(MaxVisibleRows, count);
        if (selectedRowIndex < scrollOffset)
        {
            scrollOffset = selectedRowIndex;
        }
        else if (selectedRowIndex >= scrollOffset + visibleCount)
        {
            scrollOffset = selectedRowIndex - visibleCount + 1;
        }

        RebuildPanel();
    }

    private void CycleHero(int delta)
    {
        HeroData[] heroes = GetPartyHeroes(HeroProgressionManager.Instance);
        int count = heroes != null ? heroes.Length : 0;
        if (count == 0)
        {
            return;
        }

        selectedHeroIndex = (selectedHeroIndex + delta + count) % count;
        RebuildPanel();
    }

    private static HeroData[] GetPartyHeroes(HeroProgressionManager progression)
    {
        if (progression == null)
        {
            return null;
        }

        PartyManager partyManager = PartyManager.Instance;
        if (partyManager == null || partyManager.PartyData == null)
        {
            return null;
        }

        HeroData[] active = partyManager.GetActiveParty();
        HeroData[] reserve = partyManager.GetReserveParty();
        int total = (active != null ? active.Length : 0) + (reserve != null ? reserve.Length : 0);
        if (total == 0)
        {
            return null;
        }

        HeroData[] heroes = new HeroData[total];
        int index = 0;
        if (active != null)
        {
            for (int i = 0; i < active.Length; i++)
            {
                heroes[index++] = active[i];
            }
        }

        if (reserve != null)
        {
            for (int i = 0; i < reserve.Length; i++)
            {
                heroes[index++] = reserve[i];
            }
        }

        return heroes;
    }

    private static Color GetRarityColor(GearDropService.GearRarity rarity)
    {
        switch (rarity)
        {
            case GearDropService.GearRarity.Uncommon: return UncommonColor;
            case GearDropService.GearRarity.Rare: return RareColor;
            case GearDropService.GearRarity.Epic: return EpicColor;
            case GearDropService.GearRarity.Legendary: return LegendaryColor;
            default: return CommonColor;
        }
    }

    private static void CreateLabel(GameObject parent, string text, Vector2 position, Vector2 size, Color color, int fontSize, FontStyle fontStyle)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent.transform, false);

        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(position.x, -position.y);
        labelRect.sizeDelta = size;

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
    }
}
