using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BadEndingReactionsTest : MonoBehaviour
{
    [ContextMenu("Run Bad Ending Reactions Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Bad Ending Reactions Tests ===");

        TestGetReactionByHeroIdEarth();
        TestGetReactionByHeroIdAir();
        TestGetReactionByHeroIdFire();
        TestGetReactionByHeroIdWater();
        TestGetReactionByHeroIdMC();
        TestGetReactionByHeroIdUnknownFallsBackToMC();
        TestGetReactionByElementEarth();
        TestGetReactionByElementAir();
        TestGetReactionByElementFire();
        TestGetReactionByElementWater();
        TestGetReactionByElementSpaceMapsToMC();
        TestGetReactionByElementNoneMapsToMC();
        TestElementToHeroIdSpaceDoesNotHaveExplicitMapping();
        TestGetAllReactionsReturnsFour();
        TestGetAllReactionsIncludingMCReturnsFive();
        TestGetMCReactionReturnsMCData();
        TestBuildBadEndingDialogueNotEmpty();
        TestGetRandomLineReturnsNonEmpty();

        Debug.Log("=== All Bad Ending Reactions Tests Passed ===");
    }

    private void TestGetReactionByHeroIdEarth()
    {
        Debug.Log("Testing GetReaction for Earth hero...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_earth");

        Assert.AreEqual("hero_earth", reaction.heroId, "Earth reaction heroId should be hero_earth.");
        Assert.AreEqual("Freida", reaction.heroName, "Earth reaction heroName should be Freida.");
        Assert.AreEqual("Earth", reaction.element, "Earth reaction element should be Earth.");
        Assert.GreaterOrEqual(reaction.despairLines.Length, 1, "Earth should have despair lines.");
        Assert.IsNotNull(reaction.finalWords, "Earth should have final words.");

        Debug.Log("✓ GetReaction Earth test passed");
    }

    private void TestGetReactionByHeroIdAir()
    {
        Debug.Log("Testing GetReaction for Air hero...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_air");

        Assert.AreEqual("hero_air", reaction.heroId, "Air reaction heroId should be hero_air.");
        Assert.AreEqual("Briar", reaction.heroName, "Air reaction heroName should be Briar.");
        Assert.AreEqual("Air", reaction.element, "Air reaction element should be Air.");

        Debug.Log("✓ GetReaction Air test passed");
    }

    private void TestGetReactionByHeroIdFire()
    {
        Debug.Log("Testing GetReaction for Fire hero...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_fire");

        Assert.AreEqual("hero_fire", reaction.heroId, "Fire reaction heroId should be hero_fire.");
        Assert.AreEqual("Killian", reaction.heroName, "Fire reaction heroName should be Killian.");
        Assert.AreEqual("Fire", reaction.element, "Fire reaction element should be Fire.");

        Debug.Log("✓ GetReaction Fire test passed");
    }

    private void TestGetReactionByHeroIdWater()
    {
        Debug.Log("Testing GetReaction for Water hero...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_water");

        Assert.AreEqual("hero_water", reaction.heroId, "Water reaction heroId should be hero_water.");
        Assert.AreEqual("Merrick", reaction.heroName, "Water reaction heroName should be Merrick.");
        Assert.AreEqual("Water", reaction.element, "Water reaction element should be Water.");

        Debug.Log("✓ GetReaction Water test passed");
    }

    private void TestGetReactionByHeroIdMC()
    {
        Debug.Log("Testing GetReaction for MC hero...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_mc");

        Assert.AreEqual("hero_mc", reaction.heroId, "MC reaction heroId should be hero_mc.");
        Assert.AreEqual("The Chosen", reaction.heroName, "MC reaction heroName should be The Chosen.");

        Debug.Log("✓ GetReaction MC test passed");
    }

    private void TestGetReactionByHeroIdUnknownFallsBackToMC()
    {
        Debug.Log("Testing GetReaction for unknown heroId falls back to MC...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction("hero_unknown");

        Assert.AreEqual("hero_mc", reaction.heroId, "Unknown heroId should fall back to MC reaction.");
        Assert.AreEqual("The Chosen", reaction.heroName, "Fallback should be The Chosen.");

        Debug.Log("✓ GetReaction unknown fallback test passed");
    }

    private void TestGetReactionByElementEarth()
    {
        Debug.Log("Testing GetReaction by Element.Earth...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.Earth);

        Assert.AreEqual("hero_earth", reaction.heroId, "Element.Earth should map to hero_earth.");
        Assert.AreEqual("Freida", reaction.heroName, "Element.Earth should resolve to Freida.");

        Debug.Log("✓ GetReaction Element.Earth test passed");
    }

    private void TestGetReactionByElementAir()
    {
        Debug.Log("Testing GetReaction by Element.Air...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.Air);

        Assert.AreEqual("hero_air", reaction.heroId, "Element.Air should map to hero_air.");
        Assert.AreEqual("Briar", reaction.heroName, "Element.Air should resolve to Briar.");

        Debug.Log("✓ GetReaction Element.Air test passed");
    }

    private void TestGetReactionByElementFire()
    {
        Debug.Log("Testing GetReaction by Element.Fire...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.Fire);

        Assert.AreEqual("hero_fire", reaction.heroId, "Element.Fire should map to hero_fire.");
        Assert.AreEqual("Killian", reaction.heroName, "Element.Fire should resolve to Killian.");

        Debug.Log("✓ GetReaction Element.Fire test passed");
    }

    private void TestGetReactionByElementWater()
    {
        Debug.Log("Testing GetReaction by Element.Water...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.Water);

        Assert.AreEqual("hero_water", reaction.heroId, "Element.Water should map to hero_water.");
        Assert.AreEqual("Merrick", reaction.heroName, "Element.Water should resolve to Merrick.");

        Debug.Log("✓ GetReaction Element.Water test passed");
    }

    private void TestGetReactionByElementSpaceMapsToMC()
    {
        Debug.Log("Testing GetReaction by Element.Space maps to MC (not a dedicated hero)...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.Space);

        Assert.AreEqual("hero_mc", reaction.heroId,
            "Element.Space should fall through to default and map to hero_mc, not a dedicated Space hero.");
        Assert.AreEqual("The Chosen", reaction.heroName,
            "Element.Space reaction should resolve to The Chosen (MC).");

        Debug.Log("✓ GetReaction Element.Space maps to MC test passed");
    }

    private void TestGetReactionByElementNoneMapsToMC()
    {
        Debug.Log("Testing GetReaction by Element.None maps to MC...");

        BadEndingReactions.CharacterReaction reaction = BadEndingReactions.GetReaction(CombatUnit.Element.None);

        Assert.AreEqual("hero_mc", reaction.heroId, "Element.None should fall through to default and map to hero_mc.");

        Debug.Log("✓ GetReaction Element.None maps to MC test passed");
    }

    private void TestElementToHeroIdSpaceDoesNotHaveExplicitMapping()
    {
        Debug.Log("Testing ElementToHeroId has no explicit Space mapping in source...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "BadEndingReactions.cs"));

        Assert.IsTrue(sourceCode.Contains("case CombatUnit.Element.Earth"),
            "Should have explicit Earth case.");
        Assert.IsTrue(sourceCode.Contains("case CombatUnit.Element.Air"),
            "Should have explicit Air case.");
        Assert.IsTrue(sourceCode.Contains("case CombatUnit.Element.Fire"),
            "Should have explicit Fire case.");
        Assert.IsTrue(sourceCode.Contains("case CombatUnit.Element.Water"),
            "Should have explicit Water case.");
        Assert.IsTrue(sourceCode.Contains("case CombatUnit.Element.Space"),
            "Should have explicit Space case mapping to hero_space.");

        Debug.Log("✓ ElementToHeroId no explicit Space mapping test passed");
    }

    private void TestGetAllReactionsReturnsFour()
    {
        Debug.Log("Testing GetAllReactions returns exactly 4 companion reactions...");

        BadEndingReactions.CharacterReaction[] reactions = BadEndingReactions.GetAllReactions();

        Assert.AreEqual(4, reactions.Length, "GetAllReactions should return exactly 4 companion reactions.");

        Debug.Log("✓ GetAllReactions count test passed");
    }

    private void TestGetAllReactionsIncludingMCReturnsFive()
    {
        Debug.Log("Testing GetAllReactionsIncludingMC returns exactly 5 reactions...");

        BadEndingReactions.CharacterReaction[] reactions = BadEndingReactions.GetAllReactionsIncludingMC();

        Assert.AreEqual(5, reactions.Length, "GetAllReactionsIncludingMC should return exactly 5 reactions.");

        bool foundMC = false;
        for (int i = 0; i < reactions.Length; i++)
        {
            if (string.Equals(reactions[i].heroId, "hero_mc", System.StringComparison.Ordinal))
            {
                foundMC = true;
                break;
            }
        }

        Assert.IsTrue(foundMC, "MC reaction should be included in the full set.");

        Debug.Log("✓ GetAllReactionsIncludingMC count test passed");
    }

    private void TestGetMCReactionReturnsMCData()
    {
        Debug.Log("Testing GetMCReaction returns MC-specific data...");

        BadEndingReactions.CharacterReaction mc = BadEndingReactions.GetMCReaction();

        Assert.AreEqual("hero_mc", mc.heroId, "MC reaction heroId should be hero_mc.");
        Assert.AreEqual("The Chosen", mc.heroName, "MC reaction heroName should be The Chosen.");
        Assert.AreEqual("Varies", mc.element, "MC reaction element should be Varies.");
        Assert.GreaterOrEqual(mc.despairLines.Length, 1, "MC should have despair lines.");
        Assert.GreaterOrEqual(mc.blameLines.Length, 1, "MC should have blame lines.");
        Assert.GreaterOrEqual(mc.isolationLines.Length, 1, "MC should have isolation lines.");
        Assert.IsNotNull(mc.finalWords, "MC should have final words.");

        Debug.Log("✓ GetMCReaction test passed");
    }

    private void TestBuildBadEndingDialogueNotEmpty()
    {
        Debug.Log("Testing BuildBadEndingDialogue returns non-empty sequence...");

        var entries = BadEndingReactions.BuildBadEndingDialogue();

        Assert.IsNotNull(entries, "BuildBadEndingDialogue should not return null.");
        Assert.Greater(entries.Count, 0, "BuildBadEndingDialogue should return at least one entry.");

        bool foundCompanion = false;
        bool foundMC = false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].speakerName, "The Chosen", System.StringComparison.Ordinal))
            {
                foundMC = true;
            }
            else
            {
                foundCompanion = true;
            }
        }

        Assert.IsTrue(foundCompanion, "Dialogue should include companion lines.");
        Assert.IsTrue(foundMC, "Dialogue should include MC lines.");

        Debug.Log("✓ BuildBadEndingDialogue test passed");
    }

    private void TestGetRandomLineReturnsNonEmpty()
    {
        Debug.Log("Testing GetRandomLine returns non-empty strings for each category...");

        BadEndingReactions.ReactionCategory[] categories = new[]
        {
            BadEndingReactions.ReactionCategory.Despair,
            BadEndingReactions.ReactionCategory.Blame,
            BadEndingReactions.ReactionCategory.Isolation,
            BadEndingReactions.ReactionCategory.Final
        };

        for (int i = 0; i < categories.Length; i++)
        {
            string line = BadEndingReactions.GetRandomLine("hero_fire", categories[i]);
            Assert.IsFalse(string.IsNullOrEmpty(line),
                $"GetRandomLine for {categories[i]} should return non-empty string.");
        }

        Debug.Log("✓ GetRandomLine test passed");
    }
}
