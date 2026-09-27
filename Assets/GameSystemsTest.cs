using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class GameSystemsTest : MonoBehaviour
{
    [ContextMenu("Run Game Systems Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Game Systems Tests ===");

        TestPartySwapService();
        TestMobileTouchController();
        TestDifficultyModeService();
        TestNewGamePlusService();
        TestLocalizationService();

        Debug.Log("=== All Game Systems Tests Passed ===");
    }

    private void Cleanup<T>(ref T singleton) where T : Component
    {
        if (singleton != null)
        {
            Object.DestroyImmediate(singleton.gameObject);
            singleton = null;
        }
    }

    private void TestPartySwapService()
    {
        Debug.Log("Testing PartySwapService...");

        PartyManager partyManager = null;
        try
        {
            // TryQueueSwap validates against the live party, so the suite
            // needs a real active/reserve split rather than empty singletons.
            GameObject host = new GameObject("Test_PartyManager");
            partyManager = host.AddComponent<PartyManager>();

            // AddComponent does not run OnEnable in edit mode, so PartyManager
            // never publishes its Instance without this.
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            PartyData data = ScriptableObject.CreateInstance<PartyData>();
            PartyData authored = Resources.Load<PartyData>("PartyData/DefaultParty");
            Assert.IsNotNull(authored, "DefaultParty asset must exist.");
            data.activeSlots = (HeroData[])authored.activeSlots.Clone();
            data.reserveSlots = (HeroData[])authored.reserveSlots.Clone();
            partyManager.Initialize(data, null);

            string activeHeroId = data.activeSlots[0] != null ? data.activeSlots[0].heroId : null;
            string reserveHeroId = data.reserveSlots[0] != null ? data.reserveSlots[0].heroId : null;
            Assert.IsNotNull(activeHeroId, "Default party should have an active hero.");
            Assert.IsNotNull(reserveHeroId, "Default party should have a reserve hero.");

            string reason;
            Assert.IsTrue(PartySwapService.TryQueueSwap(activeHeroId, reserveHeroId, out reason), $"Valid swap should succeed. {reason}");
            Assert.IsFalse(PartySwapService.TryQueueSwap(activeHeroId, activeHeroId, out reason), "Self swap should fail.");
            Assert.IsFalse(PartySwapService.TryQueueSwap("", "", out reason), "Empty ids should fail.");
            Assert.IsFalse(PartySwapService.TryQueueSwap(reserveHeroId, activeHeroId, out reason), "Reserve-to-active request should fail.");
            Assert.IsFalse(PartySwapService.TryQueueSwap("hero_does_not_exist", activeHeroId, out reason), "Unknown hero should fail.");

            Object.DestroyImmediate(data);
        }
        finally
        {
            Cleanup(ref partyManager);
        }
    }

    private void TestMobileTouchController()
    {
        Debug.Log("Testing MobileTouchController...");
        MobileTouchController controller = null;
        try
        {
            GameObject host = new GameObject("Test_MobileTouch");
            controller = host.AddComponent<MobileTouchController>();

            // AddComponent does not run OnEnable in edit mode, so the
            // singleton would never be published.
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);

            Assert.IsNotNull(MobileTouchController.Instance, "Instance should be set.");
            controller.SetDpadInput(new Vector2(2f, 0f));
            Assert.AreEqual(1f, controller.DpadInput.magnitude, 0.001f, "Dpad should clamp to unit.");
            controller.SimulateActionButtonPress(MobileTouchController.ActionButtonId.Interact);
            Assert.IsTrue(controller.IsActionButtonHeld(MobileTouchController.ActionButtonId.Interact), "Button should be held.");
            controller.SimulateActionButtonRelease(MobileTouchController.ActionButtonId.Interact);
            Assert.IsFalse(controller.IsActionButtonHeld(MobileTouchController.ActionButtonId.Interact), "Button should release.");
        }
        finally
        {
            Cleanup(ref controller);
        }
    }

    private void TestDifficultyModeService()
    {
        Debug.Log("Testing DifficultyModeService...");
        DifficultyModeService service = null;
        try
        {
            GameObject host = new GameObject("Test_Difficulty");
            service = host.AddComponent<DifficultyModeService>();
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            Assert.IsNotNull(DifficultyModeService.Instance, "Instance should be set.");
            Assert.AreEqual(DifficultyModeService.Difficulty.Standard, service.CurrentDifficulty, "Default should be Standard.");
            service.SetDifficulty(DifficultyModeService.Difficulty.Hardcore);
            Assert.IsTrue(service.IsHardcore, "Hardcore should be set.");
            Assert.IsFalse(service.AllowsFleeInCombat(), "Hardcore should disallow flee.");
            Assert.Greater(service.GetXpMultiplier(), 1f, "Hardcore XP multiplier should be > 1.");
            service.SetDifficulty(DifficultyModeService.Difficulty.Story);
            Assert.IsTrue(service.IsStory, "Story should be set.");
            Assert.Less(service.GetDamageMultiplierForEnemy(), 1f, "Story enemy damage should be reduced.");
        }
        finally
        {
            Cleanup(ref service);
        }
    }

    private void TestNewGamePlusService()
    {
        Debug.Log("Testing NewGamePlusService...");
        NewGamePlusService service = null;
        try
        {
            GameObject host = new GameObject("Test_NGPlus");
            service = host.AddComponent<NewGamePlusService>();
            host.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
            Assert.IsNotNull(NewGamePlusService.Instance, "Instance should be set.");
            Assert.IsFalse(service.CanStartNewGamePlus(), "Should not be allowed before any completions.");
            service.RegisterCompletion();
            Assert.IsTrue(service.CanStartNewGamePlus(), "Should be allowed after 1 completion.");
            Assert.IsTrue(service.StartNewGamePlus(), "Start should succeed.");
            Assert.IsTrue(service.IsInNewGamePlus, "Should be in NG+ after start.");
            Assert.AreEqual(1, service.LoopIndex, "Loop index should be 1.");
            service.EndNewGamePlus();
            Assert.IsFalse(service.IsInNewGamePlus, "Should not be in NG+ after end.");
        }
        finally
        {
            Cleanup(ref service);
        }
    }

    private void TestLocalizationService()
    {
        Debug.Log("Testing LocalizationService...");
        Assert.IsTrue(LocalizationService.HasKey("ui.play"), "Should have ui.play key.");
        LocalizationService.SetLanguage(LocalizationService.Language.English);
        Assert.AreEqual("Play", LocalizationService.Get("ui.play"), "English should return 'Play'.");
        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        Assert.AreEqual("Jugar", LocalizationService.Get("ui.play"), "Spanish should return 'Jugar'.");
        LocalizationService.SetLanguage(LocalizationService.Language.English);
    }
}
