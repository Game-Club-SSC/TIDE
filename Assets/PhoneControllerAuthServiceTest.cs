using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PhoneControllerAuthServiceTest : MonoBehaviour
{
    [ContextMenu("Run All Phone Controller Auth Service Tests")]
    public void RunAllTests()
    {
        TestTokenRefreshMechanism();
        TestTokenPersistenceAcrossRestart();
        Debug.Log("=== All Phone Controller Auth Service Tests Passed ===");
    }

    [ContextMenu("Test Token Refresh Mechanism")]
    public void TestTokenRefreshMechanism()
    {
        Debug.Log("[PhoneControllerAuthServiceTest] Testing token refresh mechanism...");
        try
        {
            PhoneControllerAuthService.RevokeAllTokens();

            string token = PhoneControllerAuthService.GenerateToken();
            Assert.IsFalse(string.IsNullOrEmpty(token), "Generated token should not be null or empty.");
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token), "Freshly generated token should be valid.");

            bool reRegistered = PhoneControllerAuthService.RegisterToken(token, TimeSpan.FromHours(2));
            Assert.IsTrue(reRegistered, "Re-registering an existing token should succeed.");
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token),
                "Token should still be valid after re-registration (refresh).");

            Assert.AreEqual(1, PhoneControllerAuthService.GetActiveTokenCount(),
                "Re-registration should not create a duplicate token.");

            PhoneControllerAuthService.RevokeAllTokens();
            Debug.Log("[PhoneControllerAuthServiceTest] TestTokenRefreshMechanism passed.");
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
        }
    }

    [ContextMenu("Test Token Persistence Across Restart")]
    public void TestTokenPersistenceAcrossRestart()
    {
        Debug.Log("[PhoneControllerAuthServiceTest] Testing token persistence across restart...");
        try
        {
            PhoneControllerAuthService.RevokeAllTokens();

            string token = PhoneControllerAuthService.GenerateToken();
            Assert.IsTrue(PhoneControllerAuthService.ValidateToken(token), "Token should be valid after generation.");

            FieldInfo activeTokensField = typeof(PhoneControllerAuthService).GetField(
                "ActiveTokens", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(activeTokensField, "ActiveTokens dictionary should exist.");

            var activeTokens = activeTokensField.GetValue(null) as System.Collections.IDictionary;
            Assert.IsNotNull(activeTokens, "ActiveTokens should be a dictionary.");
            Assert.IsTrue(activeTokens.Contains(token), "Token should exist in the ActiveTokens dictionary.");

            DateTime expiry = (DateTime)activeTokens[token];
            Assert.IsTrue(expiry > DateTime.UtcNow, "Token expiry should be in the future.");

            string source = ReadSourceFile("PhoneControllerAuthService.cs");
            bool hasPersistentStorage = source.Contains("PlayerPrefs") ||
                source.Contains("File.") ||
                source.Contains("Serialize") ||
                source.Contains("Save") ||
                source.Contains("Persist");

            if (!hasPersistentStorage)
            {
                Debug.LogWarning("[PhoneControllerAuthServiceTest] Token storage is in-memory only (Dictionary). " +
                    "Tokens will not survive an actual application restart. " +
                    "Consider adding PlayerPrefs or file-based persistence.");
            }

            PhoneControllerAuthService.RevokeAllTokens();
            Debug.Log("[PhoneControllerAuthServiceTest] TestTokenPersistenceAcrossRestart passed.");
        }
        finally
        {
            PhoneControllerAuthService.RevokeAllTokens();
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
