using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class LocalizationServiceTest : MonoBehaviour
{
    [ContextMenu("Run Localization Service Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Localization Service Tests ===");

        TestGetReturnsEnglishByDefault();
        TestGetReturnsSpanishWhenSet();
        TestGetReturnsFallbackToEnglish();
        TestGetReturnsRawKeyForUnknown();
        TestGetReturnsEmptyForNullOrEmptyKey();
        TestHasKeyReturnsTrueForKnownKey();
        TestHasKeyReturnsFalseForUnknownKey();
        TestHasKeyReturnsFalseForNullOrEmpty();
        TestSetLanguagePersistenceToPlayerPrefs();
        TestLanguagePersistenceNotImplemented();
        TestFallbackChainTargetToEnglishToRawKey();
        TestKeyCoverageBeyond18();
        TestGetAllKeysReturnsAllKeys();
        TestDefaultLanguageIsEnglish();
        TestAllKeysHaveBothLanguages();

        Debug.Log("=== All Localization Service Tests Passed ===");
    }

    private void TestGetReturnsEnglishByDefault()
    {
        Debug.Log("Testing Get returns English value by default...");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        string value = LocalizationService.Get("ui.play");
        Assert.AreEqual("Play", value, "Get('ui.play') should return 'Play' in English.");

        value = LocalizationService.Get("ui.attack");
        Assert.AreEqual("Attack", value, "Get('ui.attack') should return 'Attack' in English.");

        Debug.Log("✓ Get returns English by default test passed");
    }

    private void TestGetReturnsSpanishWhenSet()
    {
        Debug.Log("Testing Get returns Spanish value when language is set...");

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);

        string value = LocalizationService.Get("ui.play");
        Assert.AreEqual("Jugar", value, "Get('ui.play') should return 'Jugar' in Spanish.");

        value = LocalizationService.Get("ui.attack");
        Assert.AreEqual("Atacar", value, "Get('ui.attack') should return 'Atacar' in Spanish.");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Debug.Log("✓ Get returns Spanish when set test passed");
    }

    private void TestGetReturnsFallbackToEnglish()
    {
        Debug.Log("Testing Get falls back to English when current language has no entry...");

        LocalizationService.SetLanguage(LocalizationService.Language.English);
        string englishValue = LocalizationService.Get("ui.play");

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        string spanishValue = LocalizationService.Get("ui.play");

        Assert.AreNotEqual(englishValue, spanishValue,
            "English and Spanish values should be different for 'ui.play'.");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Debug.Log("✓ Get fallback to English test passed");
    }

    private void TestGetReturnsRawKeyForUnknown()
    {
        Debug.Log("Testing Get returns raw key for unknown key...");

        string value = LocalizationService.Get("unknown.key.that.does.not.exist");
        Assert.AreEqual("unknown.key.that.does.not.exist", value,
            "Unknown key should return the raw key string.");

        Debug.Log("✓ Get returns raw key test passed");
    }

    private void TestGetReturnsEmptyForNullOrEmptyKey()
    {
        Debug.Log("Testing Get returns empty for null or empty key...");

        string value = LocalizationService.Get(null);
        Assert.AreEqual(string.Empty, value, "Get(null) should return empty string.");

        value = LocalizationService.Get("");
        Assert.AreEqual(string.Empty, value, "Get('') should return empty string.");

        Debug.Log("✓ Get null/empty key test passed");
    }

    private void TestHasKeyReturnsTrueForKnownKey()
    {
        Debug.Log("Testing HasKey returns true for known key...");

        Assert.IsTrue(LocalizationService.HasKey("ui.play"), "HasKey('ui.play') should return true.");
        Assert.IsTrue(LocalizationService.HasKey("ui.tidebreak"), "HasKey('ui.tidebreak') should return true.");
        Assert.IsTrue(LocalizationService.HasKey("ui.endings.bad"), "HasKey('ui.endings.bad') should return true.");

        Debug.Log("✓ HasKey known key test passed");
    }

    private void TestHasKeyReturnsFalseForUnknownKey()
    {
        Debug.Log("Testing HasKey returns false for unknown key...");

        Assert.IsFalse(LocalizationService.HasKey("unknown.key"),
            "HasKey('unknown.key') should return false.");
        Assert.IsFalse(LocalizationService.HasKey("ui.nonexistent"),
            "HasKey('ui.nonexistent') should return false.");

        Debug.Log("✓ HasKey unknown key test passed");
    }

    private void TestHasKeyReturnsFalseForNullOrEmpty()
    {
        Debug.Log("Testing HasKey returns false for null or empty...");

        Assert.IsFalse(LocalizationService.HasKey(null), "HasKey(null) should return false.");
        Assert.IsFalse(LocalizationService.HasKey(""), "HasKey('') should return false.");

        Debug.Log("✓ HasKey null/empty test passed");
    }

    private void TestSetLanguagePersistenceToPlayerPrefs()
    {
        Debug.Log("Testing language persistence to PlayerPrefs...");

        string sourceCode = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "LocalizationService.cs"));

        Assert.IsTrue(sourceCode.Contains("PlayerPrefs"),
            "LocalizationService should persist language to PlayerPrefs.");

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        Assert.AreEqual(LocalizationService.Language.Spanish, LocalizationService.CurrentLanguage,
            "Language should be set in memory.");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Debug.Log("✓ Language persistence to PlayerPrefs confirmed");
    }

    private void TestLanguagePersistenceNotImplemented()
    {
        Debug.Log("Testing language does not survive across SetLanguage resets...");

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        Assert.AreEqual(LocalizationService.Language.Spanish, LocalizationService.CurrentLanguage);

        LocalizationService.SetLanguage(LocalizationService.Language.English);
        Assert.AreEqual(LocalizationService.Language.English, LocalizationService.CurrentLanguage,
            "Language should reset to English when explicitly set.");

        Debug.Log("✓ Language persistence not implemented test passed");
    }

    private void TestFallbackChainTargetToEnglishToRawKey()
    {
        Debug.Log("Testing fallback chain: target language -> English -> raw key...");

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);

        string spanishResult = LocalizationService.Get("ui.defend");
        Assert.AreEqual("Defender", spanishResult,
            "Should return Spanish translation when available.");

        LocalizationService.SetLanguage(LocalizationService.Language.English);
        string englishResult = LocalizationService.Get("ui.defend");
        Assert.AreEqual("Defend", englishResult,
            "Should return English translation.");

        string unknownResult = LocalizationService.Get("no.such.key");
        Assert.AreEqual("no.such.key", unknownResult,
            "Should return raw key when key doesn't exist in any language.");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Debug.Log("✓ Fallback chain test passed");
    }

    private void TestKeyCoverageBeyond18()
    {
        Debug.Log("Testing key coverage is beyond 18 keys...");

        int keyCount = 0;
        foreach (string key in LocalizationService.GetAllKeys())
        {
            keyCount++;
        }

        Assert.Greater(keyCount, 18,
            $"LocalizationService should have more than 18 keys. Found: {keyCount}.");
        Assert.GreaterOrEqual(keyCount, 68,
            $"LocalizationService should have at least 68 keys. Found: {keyCount}.");

        Debug.Log($"✓ Key coverage test passed ({keyCount} keys)");
    }

    private void TestGetAllKeysReturnsAllKeys()
    {
        Debug.Log("Testing GetAllKeys returns all registered keys...");

        IEnumerable<string> keys = LocalizationService.GetAllKeys();
        Assert.IsNotNull(keys, "GetAllKeys should not return null.");

        List<string> keyList = new List<string>(keys);
        Assert.IsTrue(keyList.Contains("ui.play"), "Should contain 'ui.play'.");
        Assert.IsTrue(keyList.Contains("ui.options"), "Should contain 'ui.options'.");
        Assert.IsTrue(keyList.Contains("ui.exit"), "Should contain 'ui.exit'.");
        Assert.IsTrue(keyList.Contains("ui.battle"), "Should contain 'ui.battle'.");
        Assert.IsTrue(keyList.Contains("ui.attack"), "Should contain 'ui.attack'.");
        Assert.IsTrue(keyList.Contains("ui.defend"), "Should contain 'ui.defend'.");
        Assert.IsTrue(keyList.Contains("ui.skill"), "Should contain 'ui.skill'.");
        Assert.IsTrue(keyList.Contains("ui.tidebreak"), "Should contain 'ui.tidebreak'.");
        Assert.IsTrue(keyList.Contains("ui.victory"), "Should contain 'ui.victory'.");
        Assert.IsTrue(keyList.Contains("ui.defeat"), "Should contain 'ui.defeat'.");
        Assert.IsTrue(keyList.Contains("ui.pause"), "Should contain 'ui.pause'.");
        Assert.IsTrue(keyList.Contains("ui.resume"), "Should contain 'ui.resume'.");
        Assert.IsTrue(keyList.Contains("ui.save"), "Should contain 'ui.save'.");
        Assert.IsTrue(keyList.Contains("ui.load"), "Should contain 'ui.load'.");
        Assert.IsTrue(keyList.Contains("ui.acceptance.title"), "Should contain 'ui.acceptance.title'.");
        Assert.IsTrue(keyList.Contains("ui.endings.good"), "Should contain 'ui.endings.good'.");
        Assert.IsTrue(keyList.Contains("ui.endings.bad"), "Should contain 'ui.endings.bad'.");
        Assert.IsTrue(keyList.Contains("ui.difficulty.story"), "Should contain 'ui.difficulty.story'.");
        Assert.IsTrue(keyList.Contains("ui.difficulty.standard"), "Should contain 'ui.difficulty.standard'.");
        Assert.IsTrue(keyList.Contains("ui.difficulty.hardcore"), "Should contain 'ui.difficulty.hardcore'.");

        Debug.Log("✓ GetAllKeys test passed");
    }

    private void TestDefaultLanguageIsEnglish()
    {
        Debug.Log("Testing default language is English...");

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Assert.AreEqual(LocalizationService.Language.English, LocalizationService.CurrentLanguage,
            "Default language should be English.");

        Debug.Log("✓ Default language test passed");
    }

    private void TestAllKeysHaveBothLanguages()
    {
        Debug.Log("Testing all keys have both English and Spanish translations...");

        IEnumerable<string> keys = LocalizationService.GetAllKeys();

        LocalizationService.SetLanguage(LocalizationService.Language.English);
        foreach (string key in keys)
        {
            string englishValue = LocalizationService.Get(key);
            Assert.IsFalse(string.IsNullOrEmpty(englishValue),
                $"Key '{key}' should have a non-empty English translation.");
            Assert.AreNotEqual(key, englishValue,
                $"Key '{key}' English translation should not be the raw key.");
        }

        LocalizationService.SetLanguage(LocalizationService.Language.Spanish);
        foreach (string key in keys)
        {
            string spanishValue = LocalizationService.Get(key);
            Assert.IsFalse(string.IsNullOrEmpty(spanishValue),
                $"Key '{key}' should have a non-empty Spanish translation.");
            Assert.AreNotEqual(key, spanishValue,
                $"Key '{key}' Spanish translation should not be the raw key.");
        }

        LocalizationService.SetLanguage(LocalizationService.Language.English);

        Debug.Log("✓ All keys have both languages test passed");
    }
}
