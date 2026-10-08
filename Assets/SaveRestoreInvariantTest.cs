#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Behavioural coverage for save/restore of durable quest state.
///
/// Two real defects motivated this file. StoryProgressionService keeps quest
/// flags in a plain in-memory dictionary, and the world save had no field for
/// them at all, so every flag was lost on reload and its quest condition
/// re-evaluated as unmet. Separately, restoring a party composition nulled out
/// any slot whose hero id could not be resolved, silently shrinking the active
/// party below its minimum.
/// </summary>
[DisallowMultipleComponent]
public class SaveRestoreInvariantTest : MonoBehaviour
{
    private GameObject serviceObject;
    private GameObject managerObject;
    private List<GameObject> spawned;

    [ContextMenu("Run Save Restore Invariant Tests")]
    public void RunTests()
    {
        System.Action[] tests =
        {
            QuestFlagsSurviveTheWorldStateRoundTrip,
            FlagCopyAndRestoreAreSymmetric,
            UnresolvedPartyIdKeepsTheExistingHeroInTheSlot,
            PartyRestoreDoesNotResurrectAnEmptySlot,
            PartyRestorePreservesInteriorEmptySlots,
            PartyRestoreKeepsFixedSlotsWhenEverySavedSlotIsEmpty,
            MissingPartyIdsAndFallbackAreSafe
        };
        foreach (System.Action test in tests)
        {
            SetUp();
            try { test(); }
            finally { TearDown(); }
        }
        Debug.Log("[SaveRestoreInvariantTest] All save restore invariant tests passed.");
    }

    [SetUp]
    public void SetUp()
    {
        Cleanup();
        spawned = new List<GameObject>();
    }

    [TearDown]
    public void TearDown()
    {
        Cleanup();
    }

    [Test]
    public void QuestFlagsSurviveTheWorldStateRoundTrip()
    {
        // The defect: StoryProgressionSaveData carried act and ending data but
        // no flags, so a set flag vanished on every load.
        GameStateManager gsm = CreateGameState();
        StoryProgressionService service = CreateStoryService();
        try
        {
            service.SetFlag("quest_slime_cleared", true);
            service.SetFlag("quest_treasure_found", false);
            Assert.IsTrue(service.GetFlag("quest_slime_cleared"), "Setup: the flag should be set.");

            gsm.SaveWorldState();

            // Prove the payload really carries the flags, not just the object.
            string payload = PlayerPrefs.GetString(WorldStateSaveKey, string.Empty);
            Assert.IsNotEmpty(payload, "Setup: the save must have been written.");
            StringAssert.Contains("quest_slime_cleared", payload,
                "The save payload must carry story flags, or they are lost on every load.");

            // Wipe the live state the way a fresh session would.
            service.ResetForDebug();
            Assert.IsFalse(service.GetFlag("quest_slime_cleared"),
                "Setup: the flag should be gone before the load restores it.");

            gsm.LoadWorldState();

            Assert.IsTrue(service.GetFlag("quest_slime_cleared"),
                "A quest flag set before saving must survive a save/load round trip.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void FlagCopyAndRestoreAreSymmetric()
    {
        // The parallel-list round trip that JsonUtility needs, exercised
        // directly so a key/value misalignment is caught.
        StoryProgressionService service = CreateStoryService();
        try
        {
            service.SetFlag("alpha", true);
            service.SetFlag("beta", false);
            service.SetFlag("gamma", true);

            List<string> keys = new List<string>();
            List<bool> values = new List<bool>();
            service.CopyFlagsTo(keys, values);

            Assert.AreEqual(3, keys.Count, "Every flag must be copied.");
            Assert.AreEqual(keys.Count, values.Count, "The parallel lists must stay the same length.");
            StringAssert.Contains("alpha", string.Join(",", keys.ToArray()), "Copied keys must match the set flags.");

            service.ResetForDebug();
            service.RestoreFlagsFrom(keys, values);

            Assert.IsTrue(service.GetFlag("alpha"), "alpha must restore as true.");
            Assert.IsFalse(service.GetFlag("beta"), "beta must restore as false, not default to true.");
            Assert.IsTrue(service.GetFlag("gamma"), "gamma must restore as true.");
        }
        finally
        {
            Cleanup();
        }
    }

    [Test]
    public void UnresolvedPartyIdKeepsTheExistingHeroInTheSlot()
    {
        // An unknown id must use an unused authored hero. Copying the old
        // slot blindly can duplicate hero_fire when a later saved slot also
        // resolves to hero_fire.
        MethodInfo resolve = typeof(HeroProgressionManager).GetMethod(
            "ResolveHeroDataSlots", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolve, "ResolveHeroDataSlots should exist as a static helper.");

        HeroData[] authored = AuthoredParty();
        HeroData[] result = (HeroData[])resolve.Invoke(
            null,
            new object[] { new List<string> { "hero_does_not_exist", "hero_fire", "hero_water" }, 3, authored });

        Assert.IsNotNull(result, "The resolved array must not be null.");
        Assert.IsNotNull(result[0],
            "An unresolvable id must keep the existing hero in its slot rather than emptying the party.");
        Assert.IsNotNull(result[1], "A resolvable id must still be applied to its slot.");
        Assert.AreEqual("hero_fire", result[1].heroId, "hero_fire must land in slot 1.");
        Assert.IsNotNull(result[2], "A resolvable id must still be applied to its slot.");
        Assert.AreEqual("hero_water", result[2].heroId, "hero_water must land in slot 2.");
        Assert.AreNotEqual(result[0].heroId, result[1].heroId,
            "An unresolved-id fallback cannot duplicate a hero saved in a later slot.");
        Assert.AreNotEqual(result[0].heroId, result[2].heroId,
            "Every restored active hero must be unique.");
    }

    [Test]
    public void PartyRestoreDoesNotResurrectAnEmptySlot()
    {
        // The save records a compact list of non-empty slots, so a two-entry
        // list means the third active slot was empty. Seeding every slot from
        // the live party would put the benched hero back.
        MethodInfo resolve = typeof(HeroProgressionManager).GetMethod(
            "ResolveHeroDataSlots", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(resolve, "ResolveHeroDataSlots should exist as a static helper.");

        HeroData[] authored = AuthoredParty();
        HeroData[] result = (HeroData[])resolve.Invoke(
            null,
            new object[] { new List<string> { "hero_fire", "hero_water" }, 3, authored });

        Assert.IsNotNull(result, "The resolved array must not be null.");
        Assert.AreEqual(2, CountNonNull(result),
            "Only the two recorded ids may occupy slots; a slot the save left empty must stay empty.");
    }

    [Test]
    public void PartyRestorePreservesInteriorEmptySlots()
    {
        MethodInfo resolve = typeof(HeroProgressionManager).GetMethod(
            "ResolveHeroDataSlots", BindingFlags.Static | BindingFlags.NonPublic);
        HeroData[] result = (HeroData[])resolve.Invoke(null,
            new object[] { new List<string> { "hero_fire", string.Empty, "hero_water" }, 3, AuthoredParty() });
        Assert.AreEqual(3, result.Length, "The active slot layout stays fixed.");
        Assert.IsNull(result[1], "An explicitly empty middle slot cannot resurrect an authored hero.");
        Assert.AreEqual("hero_water", result[2].heroId, "A hole cannot move later saved heroes left.");
    }

    [Test]
    public void PartyRestoreKeepsFixedSlotsWhenEverySavedSlotIsEmpty()
    {
        MethodInfo resolve = typeof(HeroProgressionManager).GetMethod(
            "ResolveHeroDataSlots", BindingFlags.Static | BindingFlags.NonPublic);
        HeroData[] result = (HeroData[])resolve.Invoke(null,
            new object[] { new List<string>(), 3, AuthoredParty() });
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual(0, CountNonNull(result), "An explicit empty roster cannot inherit the live party.");
    }

    [Test]
    public void MissingPartyIdsAndFallbackAreSafe()
    {
        MethodInfo resolve = typeof(HeroProgressionManager).GetMethod(
            "ResolveHeroDataSlots", BindingFlags.Static | BindingFlags.NonPublic);
        HeroData[] result = null;
        Assert.DoesNotThrow(() => result = (HeroData[])resolve.Invoke(null, new object[] { null, 3, null }));
        Assert.AreEqual(3, result.Length);
        Assert.AreEqual(0, CountNonNull(result));
    }

    /// <summary>
    /// The authored DefaultParty roster, used as the pre-restore fallback so
    /// the assertion about an unresolvable id is meaningful.
    /// </summary>
    private HeroData[] AuthoredParty()
    {
        PartyData authored = Resources.Load<PartyData>("PartyData/DefaultParty");
        Assert.IsNotNull(authored, "The DefaultParty asset should exist.");

        HeroData[] copy = new HeroData[3];
        for (int i = 0; i < copy.Length && i < authored.activeSlots.Length; i++)
        {
            copy[i] = authored.activeSlots[i];
        }

        Assert.IsTrue(copy[0] != null, "Setup: the authored party should have an active hero.");
        return copy;
    }

    private static int CountNonNull(HeroData[] slots)
    {
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    // ------------------------------------------------------------------ //
    //  Helpers
    // ------------------------------------------------------------------ //

    private const string WorldStateSaveKey = "TIDE_WORLD_STATE_V1";

    private GameStateManager CreateGameState()
    {
        if (GameStateManager.Instance != null)
        {
            Object.DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        managerObject = new GameObject("TestGameStateManager_SaveRestore");
        GameStateManager gsm = managerObject.AddComponent<GameStateManager>();
        try
        {
            InvokePrivate(gsm, "Awake");
        }
        catch (System.Exception)
        {
            // Expected outside play mode: Awake calls DontDestroyOnLoad.
        }

        if (GameStateManager.Instance != gsm)
        {
            SetInstance(gsm);
        }

        return gsm;
    }

    private StoryProgressionService CreateStoryService()
    {
        if (StoryProgressionService.Instance != null)
        {
            Object.DestroyImmediate(StoryProgressionService.Instance.gameObject);
        }

        serviceObject = new GameObject("TestStoryProgression_SaveRestore");
        StoryProgressionService service = serviceObject.AddComponent<StoryProgressionService>();
        try
        {
            InvokePrivate(service, "Awake");
        }
        catch (System.Exception)
        {
            // Expected outside play mode.
        }

        if (StoryProgressionService.Instance != service)
        {
            SetInstance(service);
        }

        return service;
    }

    private static void SetInstance<T>(T manager) where T : MonoBehaviour
    {
        PropertyInfo property = typeof(T).GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
        Assert.IsNotNull(property, $"{typeof(T).Name}.Instance should exist.");
        property.GetSetMethod(true).Invoke(manager, new object[] { manager });
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"{target.GetType().Name}.{methodName} should exist.");
        method.Invoke(target, null);
    }

    private void Cleanup()
    {
        if (spawned != null)
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null)
                {
                    Object.DestroyImmediate(spawned[i]);
                }
            }

            spawned.Clear();
        }

        if (GameStateManager.Instance != null)
        {
            Object.DestroyImmediate(GameStateManager.Instance.gameObject);
        }

        if (StoryProgressionService.Instance != null)
        {
            Object.DestroyImmediate(StoryProgressionService.Instance.gameObject);
        }

        if (HeroProgressionManager.Instance != null)
        {
            Object.DestroyImmediate(HeroProgressionManager.Instance.gameObject);
        }

        if (managerObject != null)
        {
            Object.DestroyImmediate(managerObject);
            managerObject = null;
        }

        if (serviceObject != null)
        {
            Object.DestroyImmediate(serviceObject);
            serviceObject = null;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
#endif
