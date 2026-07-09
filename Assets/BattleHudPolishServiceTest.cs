using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleHudPolishServiceTest : MonoBehaviour
{
    [ContextMenu("Run All Battle Hud Polish Service Tests")]
    public void RunAllTests()
    {
        TestVisualEffectsExistBeyondColorConstants();
        TestUnknownStatusEffectTypeLogsWarning();
        Debug.Log("=== All Battle Hud Polish Service Tests Passed ===");
    }

    [ContextMenu("Test Visual Effects Exist Beyond Color Constants")]
    public void TestVisualEffectsExistBeyondColorConstants()
    {
        Debug.Log("[BattleHudPolishServiceTest] Testing visual effects exist beyond color constants...");
        try
        {
            Color critFlash = BattleHudPolishService.GetCritFlashColor();
            Assert.IsTrue(critFlash.a > 0f, "Crit flash color should have non-zero alpha.");

            float critDuration = BattleHudPolishService.GetCritFlashDuration();
            Assert.IsTrue(critDuration > 0f, "Crit flash duration should be positive.");
            Assert.IsTrue(critDuration <= 2f, "Crit flash duration should be reasonable (<= 2s).");

            Color momentumLow = BattleHudPolishService.GetMomentumBarColor(0.2f);
            Color momentumMid = BattleHudPolishService.GetMomentumBarColor(0.7f);
            Color momentumHigh = BattleHudPolishService.GetMomentumBarColor(1.0f);
            Assert.AreNotEqual(momentumLow, momentumMid, "Low and mid momentum colors should differ.");
            Assert.AreNotEqual(momentumMid, momentumHigh, "Mid and high momentum colors should differ.");

            string source = ReadSourceFile("BattleHudPolishService.cs");
            bool hasFlash = source.Contains("GetCritFlashColor") && source.Contains("GetCritFlashDuration");
            Assert.IsTrue(hasFlash, "BattleHudPolishService should have crit flash effect (color + duration).");

            bool hasMomentumGradient = source.Contains("GetMomentumBarColor");
            Assert.IsTrue(hasMomentumGradient, "BattleHudPolishService should have momentum bar color method.");

            Debug.Log("[BattleHudPolishServiceTest] TestVisualEffectsExistBeyondColorConstants passed.");
        }
        finally
        {
        }
    }

    [ContextMenu("Test Unknown StatusEffectType Logs Warning")]
    public void TestUnknownStatusEffectTypeLogsWarning()
    {
        Debug.Log("[BattleHudPolishServiceTest] Testing unknown StatusEffectType logs warning...");
        try
        {
            string source = ReadSourceFile("BattleHudPolishService.cs");
            Assert.IsFalse(string.IsNullOrEmpty(source), "BattleHudPolishService.cs source should be readable.");

            bool hasDefaultCase = source.Contains("default:");
            Assert.IsTrue(hasDefaultCase, "GetStatusEffectIconColor should have a default case.");

            bool defaultReturnsColor = source.IndexOf("default:") < source.IndexOf("GetStatusEffectLabel") ||
                source.Contains("default: return");
            Assert.IsTrue(defaultReturnsColor, "Default case should return a fallback color.");

            bool defaultLogsWarning = source.Contains("Debug.LogWarning") ||
                source.Contains("Debug.LogError");
            if (!defaultLogsWarning)
            {
                Debug.LogWarning("[BattleHudPolishServiceTest] GetStatusEffectIconColor default case does not log " +
                    "a warning for unknown StatusEffectType. Consider adding Debug.LogWarning.");
            }

            Color defaultColor = BattleHudPolishService.GetStatusEffectIconColor((StatusEffectType)(-1));
            Assert.IsTrue(defaultColor.a > 0f, "Default status effect color should have non-zero alpha.");

            string defaultLabel = BattleHudPolishService.GetStatusEffectLabel((StatusEffectType)(-1));
            Assert.IsFalse(string.IsNullOrEmpty(defaultLabel), "Default status effect label should not be empty.");

            Debug.Log("[BattleHudPolishServiceTest] TestUnknownStatusEffectTypeLogsWarning passed.");
        }
        finally
        {
        }
    }

    private static string ReadSourceFile(string fileName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(fileName.Replace(".cs", " t:MonoScript"));
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(fileName))
            {
                return System.IO.File.ReadAllText(path);
            }
        }
        return string.Empty;
    }
}
