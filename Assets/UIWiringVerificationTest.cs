using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using NUnit.Framework;

/// <summary>
/// Edit Mode tests for the title screen + exploration pause menu (issue #294).
/// Verifies that the title buttons exist and are wired to actions, the Continue
/// button is disabled without a save and enabled with one, the pause menu
/// toggles, and each pause-menu action calls the correct service
/// (GameStateManager save/load, GearInventoryUI, PartySetupUI, AudioSettingsUI).
/// </summary>
public class UIWiringVerificationTest
{
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private float previousTimeScale = 1f;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        CleanupSingletons();
    }

    [TearDown]
    public void TearDown()
    {
        CleanupSingletons();
        Time.timeScale = previousTimeScale;

        // Remove any save data the tests wrote so the dev environment's
        // persisted world state is not polluted by verification runs.
        PlayerPrefs.DeleteKey("TIDE_WORLD_STATE_V1");
        PlayerPrefs.DeleteKey("TIDE_WORLD_STATE_V1_backup");
        PlayerPrefs.DeleteKey("TIDE_WORLD_STATE_V2");
        PlayerPrefs.DeleteKey("TIDE_WORLD_STATE_V2_backup");
        PlayerPrefs.DeleteKey("TIDE_FINAL_BOSS_DEFEATS_V1");
        PlayerPrefs.DeleteKey(PartyManager.MainCharacterElementPreferenceKey);
        PlayerPrefs.Save();
    }

    private void CleanupSingletons()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();

        if (GameStateManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (WorldSaveService.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(WorldSaveService.Instance.gameObject);
        }

        if (DialogueSystem.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(DialogueSystem.Instance.gameObject);
        }

        if (IslandRestorationTracker.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(IslandRestorationTracker.Instance.gameObject);
        }

        if (IslandProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(IslandProgressionManager.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (PartyManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(PartyManager.Instance.gameObject);
        }

        if (PlayerGearInventory.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(PlayerGearInventory.Instance.gameObject);
        }

        if (TideBreakProgressionManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(TideBreakProgressionManager.Instance.gameObject);
        }

        if (AncientTextRevealDirector.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(AncientTextRevealDirector.Instance.gameObject);
        }

        if (GamepadInputManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(GamepadInputManager.Instance.gameObject);
        }

        if (MobileTouchInputManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(MobileTouchInputManager.Instance.gameObject);
        }

        if (AudioManager.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(AudioManager.Instance.gameObject);
        }

        if (DevCheatService.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(DevCheatService.Instance.gameObject);
        }

        if (EndingEvaluator.Instance != null)
        {
            UnityEngine.Object.DestroyImmediate(EndingEvaluator.Instance.gameObject);
        }

        // Procedural UI creation may have spawned an EventSystem; remove any
        // leftovers so tests stay isolated.
        UnityEngine.EventSystems.EventSystem[] eventSystems =
            UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (eventSystems[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(eventSystems[i].gameObject);
            }
        }
    }

    // ------------------------------------------------------------------
    //  Title screen
    // ------------------------------------------------------------------

    [Test]
    public void TitleScreenBuildsFourWiredButtons()
    {
        GameObject titleObject = new GameObject("TitleScreenUI_Test");
        spawnedObjects.Add(titleObject);
        TitleScreenUI titleUI = titleObject.AddComponent<TitleScreenUI>();
        titleUI.EnsureUI();

        Assert.IsTrue(titleUI.IsReady, "Title UI should be built after EnsureUI.");

        Assert.IsNotNull(titleUI.NewGameButton, "New Game button should exist.");
        Assert.IsNotNull(titleUI.ContinueButton, "Continue button should exist.");
        Assert.IsNotNull(titleUI.SettingsButton, "Settings button should exist.");
        Assert.IsNotNull(titleUI.QuitButton, "Quit button should exist.");

        AssertHasClickHandler(titleUI.NewGameButton, "New Game");
        AssertHasClickHandler(titleUI.ContinueButton, "Continue");
        AssertHasClickHandler(titleUI.SettingsButton, "Settings");
        AssertHasClickHandler(titleUI.QuitButton, "Quit");
    }

    [Test]
    public void ContinueDisabledWithoutSave_EnabledWithSave()
    {
        GameObject titleObject = new GameObject("TitleScreenUI_ContinueTest");
        spawnedObjects.Add(titleObject);
        TitleScreenUI titleUI = titleObject.AddComponent<TitleScreenUI>();
        titleUI.EnsureUI();

        GameStateManager manager = CreateIsolatedGameStateManager();
        manager.ClearPersistentWorldStateForDebug(true);
        titleUI.RefreshContinueButton();
        Assert.IsFalse(titleUI.ContinueButton.interactable,
            "Continue must be disabled when no save data exists.");

        manager.SaveWorldState();
        titleUI.RefreshContinueButton();
        Assert.IsTrue(titleUI.ContinueButton.interactable,
            "Continue must be enabled once a world state has been saved.");

        Assert.IsTrue(titleUI.HasPersistedSave(), "HasPersistedSave should reflect the saved world state.");
    }

    [Test]
    public void ContinueDisabledWithCorruptSave()
    {
        GameObject titleObject = new GameObject("TitleScreenUI_CorruptTest");
        spawnedObjects.Add(titleObject);
        TitleScreenUI titleUI = titleObject.AddComponent<TitleScreenUI>();
        titleUI.EnsureUI();

        // A real GameStateManager validates the payload; garbage under the save
        // key must not enable Continue because the load path would fail.
        GameStateManager manager = CreateIsolatedGameStateManager();
        PlayerPrefs.SetString("TIDE_WORLD_STATE_V1", "{not valid json");
        PlayerPrefs.Save();

        titleUI.RefreshContinueButton();
        Assert.IsFalse(titleUI.ContinueButton.interactable,
            "Continue must stay disabled when the persisted save is corrupt.");
        Assert.IsFalse(manager.HasLoadableWorldState(),
            "HasLoadableWorldState must reject corrupt JSON.");

        manager.ClearPersistentWorldStateForDebug(true);
    }

    [Test]
    public void ContinueIgnoresOrphanWorldSaveServiceEnvelope()
    {
        GameObject titleObject = new GameObject("TitleScreenUI_OrphanV2Test");
        spawnedObjects.Add(titleObject);
        TitleScreenUI titleUI = titleObject.AddComponent<TitleScreenUI>();
        titleUI.EnsureUI();

        GameStateManager manager = CreateIsolatedGameStateManager();
        manager.ClearPersistentWorldStateForDebug(true);
        PlayerPrefs.SetString("TIDE_WORLD_STATE_V2", "{\"saveSchemaVersion\":1,\"payload\":{}}");
        PlayerPrefs.Save();

        titleUI.RefreshContinueButton();
        Assert.IsFalse(titleUI.ContinueButton.interactable,
            "An orphan V2 envelope must not enable Continue until the runtime can restore it.");
        Assert.IsFalse(titleUI.HasPersistedSave(),
            "Title save availability must follow GameStateManager's real load source.");
    }

    [Test]
    public void NewGameButtonResetsWorldState()
    {
        GameObject titleObject = new GameObject("TitleScreenUI_NewGameTest");
        spawnedObjects.Add(titleObject);
        TitleScreenUI titleUI = titleObject.AddComponent<TitleScreenUI>();
        titleUI.EnsureUI();

        GameStateManager manager = CreateIsolatedGameStateManager();
        manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActII);
        manager.MarkNarrativeBeatCompleted("test_beat_294");
        manager.SaveWorldState();

        Assert.AreEqual(GameStateManager.StoryAct.ActII, manager.CurrentStoryAct,
            "Test setup should advance the story act.");
        Assert.IsTrue(manager.IsNarrativeBeatCompleted("test_beat_294"),
            "Test setup should complete a narrative beat.");

        titleUI.NewGameButton.onClick.Invoke();

        Assert.IsTrue(titleUI.IsChoosingElement,
            "New Game should ask the player to choose the main character's affinity first.");
        Assert.AreEqual(5, titleUI.ElementButtons.Length,
            "The title screen should offer all five GDD elemental affinities.");

        titleUI.ElementButtons[0].onClick.Invoke();

        Assert.IsTrue(titleUI.DebugNewGameRequested, "New Game action should have run.");
        Assert.AreEqual(GameStateManager.StoryAct.ActI, manager.CurrentStoryAct,
            "New Game must reset the story act to Act I.");
        Assert.IsFalse(manager.IsNarrativeBeatCompleted("test_beat_294"),
            "New Game must clear completed narrative beats.");
        Assert.AreEqual((int)CombatUnit.Element.Fire,
            PlayerPrefs.GetInt(PartyManager.MainCharacterElementPreferenceKey),
            "New Game must persist the selected main-character element before loading exploration.");
    }

    // ------------------------------------------------------------------
    //  Pause menu
    // ------------------------------------------------------------------

    [Test]
    public void PauseMenuTogglesOpenClosed()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject pauseObject = new GameObject("PauseMenuUI_ToggleTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        Assert.IsFalse(pauseUI.IsOpen, "Pause menu should start closed.");
        pauseUI.OpenMenu();
        Assert.IsTrue(pauseUI.IsOpen, "OpenMenu should open the pause menu.");
        pauseUI.CloseMenu();
        Assert.IsFalse(pauseUI.IsOpen, "CloseMenu should close the pause menu.");
        pauseUI.ToggleMenu();
        Assert.IsTrue(pauseUI.IsOpen, "ToggleMenu should open a closed pause menu.");
        pauseUI.ToggleMenu();
        Assert.IsFalse(pauseUI.IsOpen, "ToggleMenu should close an open pause menu.");
    }

    [Test]
    public void DisablingOpenPauseMenuRestoresTime()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseDisableTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject pauseObject = new GameObject("PauseMenuUI_DisableTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();

        pauseUI.OpenMenu();
        Assert.AreEqual(0f, Time.timeScale, "Opening pause must stop scaled time.");

        pauseUI.enabled = false;
        pauseObject.SendMessage("OnDisable", SendMessageOptions.DontRequireReceiver);

        Assert.IsFalse(pauseUI.IsOpen, "Disabling the pause component must clear its open state.");
        Assert.AreEqual(1f, Time.timeScale, "Disabling an open pause menu must restore scaled time.");
    }

    [Test]
    public void ReenablingPauseMenuRestoresMobilePauseButton()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseMobileReenableTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject mobileObject = new GameObject("MobileTouchInputManager_PauseReenableTest");
        spawnedObjects.Add(mobileObject);
        MobileTouchInputManager mobileInput = mobileObject.AddComponent<MobileTouchInputManager>();
        PropertyInfo mobileProperty = typeof(MobileTouchInputManager).GetProperty("IsMobilePlatform");
        Assert.IsNotNull(mobileProperty, "Mobile platform property should exist.");
        mobileProperty.SetValue(mobileInput, true);
        SetAutoPropertyBackingField(typeof(MobileTouchInputManager), "Instance", mobileInput);

        GameObject pauseObject = new GameObject("PauseMenuUI_MobileReenableTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        FieldInfo buttonField = typeof(PauseMenuUI).GetField(
            "mobilePauseButton", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(buttonField, "Pause menu mobile button field should exist.");
        GameObject mobilePauseButton = (GameObject)buttonField.GetValue(pauseUI);
        Assert.IsNotNull(mobilePauseButton, "Pause menu should build its mobile pause button.");
        Assert.IsTrue(mobilePauseButton.activeSelf,
            "Mobile pause button should be visible while the pause menu is closed.");

        pauseUI.OpenMenu();
        pauseUI.enabled = false;
        pauseObject.SendMessage("OnDisable", SendMessageOptions.DontRequireReceiver);
        Assert.IsFalse(mobilePauseButton.activeSelf,
            "Disabling the pause component should hide its mobile button.");

        pauseUI.enabled = true;
        pauseObject.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        Assert.IsTrue(mobilePauseButton.activeSelf,
            "Re-enabling the pause component should restore its mobile button.");
    }

    [Test]
    public void PauseMenuBuildsAllActionButtons()
    {
        GameObject pauseObject = new GameObject("PauseMenuUI_ButtonsTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        Assert.IsNotNull(pauseUI.ResumeButton, "Resume button should exist.");
        Assert.IsNotNull(pauseUI.PartyButton, "Party button should exist.");
        Assert.IsNotNull(pauseUI.InventoryButton, "Inventory button should exist.");
        Assert.IsNotNull(pauseUI.SaveButton, "Save button should exist.");
        Assert.IsNotNull(pauseUI.LoadButton, "Load button should exist.");
        Assert.IsNotNull(pauseUI.SettingsButton, "Settings button should exist.");
        Assert.IsNotNull(pauseUI.QuitToMenuButton, "Quit to Menu button should exist.");

        AssertHasClickHandler(pauseUI.ResumeButton, "Resume");
        AssertHasClickHandler(pauseUI.PartyButton, "Party");
        AssertHasClickHandler(pauseUI.InventoryButton, "Inventory");
        AssertHasClickHandler(pauseUI.SaveButton, "Save");
        AssertHasClickHandler(pauseUI.LoadButton, "Load");
        AssertHasClickHandler(pauseUI.SettingsButton, "Settings");
        AssertHasClickHandler(pauseUI.QuitToMenuButton, "Quit to Menu");
    }

    [Test]
    public void PauseSaveActionPersistsWorldState()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseSaveTest");
        spawnedObjects.Add(managerObject);
        GameStateManager manager = AddGameStateManagerTo(managerObject);
        manager.ClearPersistentWorldStateForDebug(true);

        GameObject pauseObject = new GameObject("PauseMenuUI_SaveTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        Assert.IsFalse(manager.HasPersistedWorldState, "No save should exist before Save is pressed.");
        pauseUI.SaveButton.onClick.Invoke();
        Assert.IsTrue(manager.HasPersistedWorldState,
            "The Save button must invoke GameStateManager.SaveWorldState.");
    }

    [Test]
    public void PauseLoadActionConfirmsAndRestores()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseLoadTest");
        spawnedObjects.Add(managerObject);
        GameStateManager manager = AddGameStateManagerTo(managerObject);
        manager.ForceStoryActForDebug(GameStateManager.StoryAct.ActII);
        manager.SaveWorldState();

        GameObject pauseObject = new GameObject("PauseMenuUI_LoadTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();
        pauseUI.OpenMenu();

        pauseUI.LoadButton.onClick.Invoke();
        Assert.IsTrue(pauseUI.ConfirmLoadVisible,
            "Load must show a confirmation prompt before restoring.");
        Assert.IsNotNull(pauseUI.ConfirmYesButton, "Load confirm should offer a Yes button.");
        Assert.IsNotNull(pauseUI.ConfirmNoButton, "Load confirm should offer a No button.");

        pauseUI.ConfirmNoButton.onClick.Invoke();
        Assert.IsFalse(pauseUI.ConfirmLoadVisible, "Cancelling the load must hide the confirm prompt.");
        Assert.IsFalse(pauseUI.DebugLoadConfirmed, "Cancelling must not confirm the load.");

        pauseUI.LoadButton.onClick.Invoke();
        pauseUI.ConfirmYesButton.onClick.Invoke();
        Assert.IsTrue(pauseUI.DebugLoadConfirmed, "Confirming must trigger the load restore path.");
        Assert.IsFalse(pauseUI.ConfirmLoadVisible, "Confirming must dismiss the prompt.");
        Assert.IsFalse(pauseUI.IsOpen, "Confirming load must close the pause menu through its normal cleanup path.");
        Assert.AreEqual(1f, Time.timeScale, "Confirming load must restore scaled time.");
    }

    [Test]
    public void PauseLoadRejectsCorruptSaveBeforeConfirmation()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseCorruptLoadTest");
        spawnedObjects.Add(managerObject);
        GameStateManager manager = AddGameStateManagerTo(managerObject);
        PlayerPrefs.SetString("TIDE_WORLD_STATE_V1", "{not valid json");
        PlayerPrefs.Save();

        GameObject pauseObject = new GameObject("PauseMenuUI_CorruptLoadTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();
        pauseUI.OnLoadClicked();

        Assert.IsFalse(pauseUI.ConfirmLoadVisible,
            "Corrupt save data must not open a confirmation that can never load.");
        Assert.IsFalse(manager.LoadWorldStateAndRestoreScene(),
            "The shared load entrypoint must fail closed for corrupt data.");
    }

    [Test]
    public void PauseSettingsActionOpensAudioSettings()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseSettingsTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject pauseObject = new GameObject("PauseMenuUI_SettingsTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        pauseUI.SettingsButton.onClick.Invoke();

        AudioSettingsUI settingsUI = UnityEngine.Object.FindFirstObjectByType<AudioSettingsUI>();
        Assert.IsNotNull(settingsUI, "Settings action must create an AudioSettingsUI.");
        Assert.IsTrue(settingsUI.IsVisible, "Settings action must open the audio settings panel.");
    }

    [Test]
    public void PauseInventoryActionOpensGearInventory()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseInventoryTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject pauseObject = new GameObject("PauseMenuUI_InventoryTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        pauseUI.InventoryButton.onClick.Invoke();

        GearInventoryUI gearUI = UnityEngine.Object.FindFirstObjectByType<GearInventoryUI>();
        Assert.IsNotNull(gearUI, "Inventory action must create a GearInventoryUI.");
        Assert.IsTrue(gearUI.IsOpen, "Inventory action must open the gear inventory.");
    }

    [Test]
    public void PausePartyActionOpensPartySetup()
    {
        GameObject managerObject = new GameObject("GameStateManager_PausePartyTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        SetupPartyManager();

        GameObject pauseObject = new GameObject("PauseMenuUI_PartyTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        pauseUI.PartyButton.onClick.Invoke();

        PartySetupUI partyUI = UnityEngine.Object.FindFirstObjectByType<PartySetupUI>();
        Assert.IsNotNull(partyUI, "Party action must create a PartySetupUI.");
        Assert.IsTrue(partyUI.IsOpen, "Party action must open the party setup UI.");
    }

    [Test]
    public void PauseQuitToMenuClosesPause()
    {
        GameObject managerObject = new GameObject("GameStateManager_PauseQuitTest");
        spawnedObjects.Add(managerObject);
        AddGameStateManagerTo(managerObject);

        GameObject pauseObject = new GameObject("PauseMenuUI_QuitTest");
        spawnedObjects.Add(pauseObject);
        PauseMenuUI pauseUI = pauseObject.AddComponent<PauseMenuUI>();
        pauseUI.EnsureUI();

        pauseUI.OpenMenu();
        Assert.IsTrue(pauseUI.IsOpen, "Pause menu should be open before Quit to Menu.");
        pauseUI.QuitToMenuButton.onClick.Invoke();
        Assert.IsFalse(pauseUI.IsOpen, "Quit to Menu must close the pause menu.");
    }

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Asserts a button has at least one runtime onClick listener. Button.onClick
    /// keeps dynamic (AddListener) callbacks in an internal InvokableCallList, so
    /// count them via reflection instead of GetInvocationList (not public on
    /// UnityEvent in this Unity version).
    /// </summary>
    private static void AssertHasClickHandler(Button button, string label)
    {
        Assert.IsNotNull(button, $"{label} button should exist.");

        FieldInfo callsField = typeof(UnityEngine.Events.UnityEventBase).GetField(
            "m_Calls", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(callsField, "UnityEventBase.m_Calls field should exist for listener inspection.");

        object calls = callsField.GetValue(button.onClick);
        Assert.IsNotNull(calls, $"{label} button onClick should have a call registry.");

        FieldInfo runtimeField = calls.GetType().GetField(
            "m_RuntimeCalls", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(runtimeField, "InvokableCallList.m_RuntimeCalls field should exist.");

        IList runtimeCalls = runtimeField.GetValue(calls) as IList;
        Assert.IsNotNull(runtimeCalls, $"{label} button should expose its runtime listener list.");
        Assert.Greater(runtimeCalls.Count, 0, $"{label} button must be wired to at least one action.");
    }

    private static GameStateManager CreateIsolatedGameStateManager()
    {
        GameObject managerObject = new GameObject("GameStateManager_UIWiringTest");
        return AddGameStateManagerTo(managerObject);
    }

    private static GameStateManager AddGameStateManagerTo(GameObject host)
    {
        GameStateManager manager = host.AddComponent<GameStateManager>();

        // In edit-mode batchmode, AddComponent does not run Awake, so the
        // singleton Instance property is not assigned. Set it via the
        // auto-property backing field (repo pattern: SetPrivateField in
        // HubVerificationTest) so UI actions that resolve GameStateManager.Instance work.
        SetAutoPropertyBackingField(typeof(GameStateManager), "Instance", manager);

        return manager;
    }

    private static void SetupPartyManager()
    {
        PartyData party = ScriptableObject.CreateInstance<PartyData>();
        party.activeSlots = new HeroData[3];
        party.reserveSlots = new HeroData[2];
        party.activeSlots[0] = CreateTestHero("hero_fire", "Hero Fire", CombatUnit.Element.Fire);
        party.activeSlots[1] = CreateTestHero("hero_water", "Hero Water", CombatUnit.Element.Water);
        party.activeSlots[2] = CreateTestHero("hero_earth", "Hero Earth", CombatUnit.Element.Earth);

        HeroDatabase database = ScriptableObject.CreateInstance<HeroDatabase>();
        database.allHeroes = new HeroData[]
        {
            party.activeSlots[0],
            party.activeSlots[1],
            party.activeSlots[2]
        };

        GameObject partyObject = new GameObject("PartyManager_UIWiringTest");
        PartyManager partyManager = partyObject.AddComponent<PartyManager>();
        partyManager.Initialize(party, database);

        // OnEnable does not run in edit mode; assign the singleton so
        // PartySetupUI.OpenMenu resolves PartyManager.Instance.
        SetAutoPropertyBackingField(typeof(PartyManager), "Instance", partyManager);
    }

    private static void SetAutoPropertyBackingField(System.Type type, string propertyName, object value)
    {
        string backingFieldName = "<" + propertyName + ">k__BackingField";
        FieldInfo field = type.GetField(backingFieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field,
            $"Auto-property backing field '{backingFieldName}' should exist on {type.Name}.");
        field.SetValue(null, value);
    }

    private static HeroData CreateTestHero(string id, string name, CombatUnit.Element element)
    {
        HeroData hero = ScriptableObject.CreateInstance<HeroData>();
        hero.heroId = id;
        hero.displayName = name;
        hero.element = element;
        hero.isMainCharacter = id == "hero_fire";
        hero.baseMaxHP = 100;
        return hero;
    }
}
