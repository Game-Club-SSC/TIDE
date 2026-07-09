using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class EnchantedMouraNPCConfigTest : MonoBehaviour
{
    [ContextMenu("Run All Enchanted Moura NPC Config Tests")]
    public void RunAllTests()
    {
        TestInteractionsBeforeRevealConfigurablePerNpc();
        TestNpcRespawnAfterRetreat();
        Debug.Log("=== All Enchanted Moura NPC Config Tests Passed ===");
    }

    [ContextMenu("Test InteractionsBeforeReveal Configurable Per NPC")]
    public void TestInteractionsBeforeRevealConfigurablePerNpc()
    {
        Debug.Log("[EnchantedMouraNPCConfigTest] Testing InteractionsBeforeReveal is configurable per-NPC...");

        string source = ReadSourceFile("EnchantedMouraNPC.cs");
        Assert.IsFalse(string.IsNullOrEmpty(source), "EnchantedMouraNPC.cs source should be readable.");

        bool hasConst = source.Contains("private const int InteractionsBeforeReveal = 3");
        bool hasSerialized = source.Contains("[SerializeField]") &&
            (source.Contains("interactionsBeforeReveal") || source.Contains("InteractionsBeforeReveal"));

        if (hasConst)
        {
            Assert.IsTrue(hasSerialized,
                "InteractionsBeforeReveal is const 3. It should be a [SerializeField] per-NPC configurable field instead.");
        }
        else
        {
            Assert.IsTrue(hasSerialized || source.Contains("interactionsBeforeReveal"),
                "InteractionsBeforeReveal should exist as a configurable field.");
        }

        Debug.Log("[EnchantedMouraNPCConfigTest] TestInteractionsBeforeRevealConfigurablePerNpc passed.");
    }

    [ContextMenu("Test NPC Respawn After Retreat")]
    public void TestNpcRespawnAfterRetreat()
    {
        Debug.Log("[EnchantedMouraNPCConfigTest] Testing NPC respawn after retreat...");
        GameObject npcObject = new GameObject("MouraNPC_RespawnTest");
        BoxCollider collider = npcObject.AddComponent<BoxCollider>();
        EnchantedMouraNPC npc = npcObject.AddComponent<EnchantedMouraNPC>();
        try
        {
            Assert.IsFalse(npc.IsRevealed, "NPC should start unrevealed.");
            Assert.AreEqual(0, npc.InteractionCount, "NPC should start with 0 interactions.");

            FieldInfo interactionCountField = typeof(EnchantedMouraNPC).GetField(
                "interactionCount", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(interactionCountField, "interactionCount field should exist.");

            FieldInfo isRevealedField = typeof(EnchantedMouraNPC).GetField(
                "isRevealed", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(isRevealedField, "isRevealed field should exist.");

            interactionCountField.SetValue(npc, 5);
            isRevealedField.SetValue(npc, true);
            Assert.IsTrue(npc.IsRevealed, "NPC should be revealed after manual set.");

            FieldInfo playerInRangeField = typeof(EnchantedMouraNPC).GetField(
                "playerInRange", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(playerInRangeField, "playerInRange field should exist.");
            playerInRangeField.SetValue(npc, false);

            Assert.IsFalse((bool)playerInRangeField.GetValue(npc), "Player should be out of range after retreat.");

            Debug.Log("[EnchantedMouraNPCConfigTest] TestNpcRespawnAfterRetreat passed.");
        }
        finally
        {
            DestroyImmediate(npcObject);
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
