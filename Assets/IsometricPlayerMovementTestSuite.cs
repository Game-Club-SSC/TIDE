using System.Reflection;
using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class IsometricPlayerMovementTestSuite : MonoBehaviour
{
    [ContextMenu("Run Movement System Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Isometric Player Movement Tests ===");

        TestDashStartsAndLocksCooldown();
        TestDashStopsCleanly();
        TestMovementLockCancelsDash();
        TestInteractionAssistSelectsNearestTarget();

        Debug.Log("=== All Isometric Player Movement Tests Passed ===");
    }

    private IsometricPlayer CreatePlayer()
    {
        GameObject playerObject = new GameObject("MovementTestPlayer");
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        return playerObject.AddComponent<IsometricPlayer>();
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist.");
        field.SetValue(target, value);
    }

    private static object InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' should exist.");
        return method.Invoke(target, args);
    }

    private void TestDashStartsAndLocksCooldown()
    {
        GameObject playerObject = new GameObject("MovementTestPlayer_Dash");
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        IsometricPlayer player = playerObject.AddComponent<IsometricPlayer>();

        SetPrivateField(player, "canMove", true);
        SetPrivateField(player, "inputVector", new Vector3(0f, 0f, 1f));
        SetPrivateField(player, "dashSpeed", 14f);
        SetPrivateField(player, "dashDuration", 0.2f);
        SetPrivateField(player, "dashCooldown", 0.8f);
        SetPrivateField(player, "nextDashAllowedAt", 0f);

        InvokePrivateMethod(player, "StartDash");

        Assert.IsTrue(player.DebugIsDashing, "Dash should begin immediately.");

        bool canStartDash = (bool)InvokePrivateMethod(player, "CanStartDash");
        Assert.IsFalse(canStartDash, "Dash should not be available again during cooldown.");

        Object.DestroyImmediate(playerObject);
    }

    private void TestDashStopsCleanly()
    {
        GameObject playerObject = new GameObject("MovementTestPlayer_StopDash");
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        IsometricPlayer player = playerObject.AddComponent<IsometricPlayer>();

        SetPrivateField(player, "isDashing", true);
        InvokePrivateMethod(player, "StopDash");

        Assert.IsFalse(player.DebugIsDashing, "StopDash should clear dash state.");

        Object.DestroyImmediate(playerObject);
    }

    private void TestMovementLockCancelsDash()
    {
        GameObject playerObject = new GameObject("MovementTestPlayer_Lock");
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        IsometricPlayer player = playerObject.AddComponent<IsometricPlayer>();

        SetPrivateField(player, "canMove", false);
        SetPrivateField(player, "isDashing", true);

        MethodInfo method = typeof(IsometricPlayer).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "Update should exist.");
        method.Invoke(player, null);

        Assert.IsFalse(player.DebugIsDashing, "Movement lock should cancel any active dash.");

        Object.DestroyImmediate(playerObject);
    }

    private void TestInteractionAssistSelectsNearestTarget()
    {
        GameObject playerObject = new GameObject("MovementTestPlayer_Assist");
        playerObject.tag = "Player";
        playerObject.AddComponent<Rigidbody>();
        IsometricPlayer player = playerObject.AddComponent<IsometricPlayer>();

        SetPrivateField(player, "interactionAssistRadius", 5f);
        SetPrivateField(player, "interactKey", KeyCode.Return);

        GameObject nearTargetObject = new GameObject("NearAssistTarget");
        nearTargetObject.AddComponent<BoxCollider>().isTrigger = true;
        nearTargetObject.transform.position = new Vector3(1f, 0f, 0f);
        DummyAssistTarget nearTarget = nearTargetObject.AddComponent<DummyAssistTarget>();
        nearTarget.SetActive(true);

        GameObject farTargetObject = new GameObject("FarAssistTarget");
        farTargetObject.AddComponent<BoxCollider>().isTrigger = true;
        farTargetObject.transform.position = new Vector3(4f, 0f, 0f);
        DummyAssistTarget farTarget = farTargetObject.AddComponent<DummyAssistTarget>();
        farTarget.SetActive(true);

        MethodInfo method = typeof(IsometricPlayer).GetMethod("TryGetInteractionAssistTarget", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "TryGetInteractionAssistTarget should exist.");

        object[] parameters = { true, null };
        bool found = (bool)method.Invoke(player, parameters);

        Assert.IsTrue(found, "Interaction assist should find a nearby target.");
        Assert.AreEqual(nearTarget, parameters[1], "Nearest active target should be selected.");

        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(nearTargetObject);
        Object.DestroyImmediate(farTargetObject);
    }

    private sealed class DummyAssistTarget : MonoBehaviour, IPlayerInteractionAssistTarget
    {
        private bool active = true;

        public void SetActive(bool isActive)
        {
            active = isActive;
        }

        public Vector3 GetInteractionAssistPosition()
        {
            return transform.position;
        }

        public float GetInteractionAssistRadius()
        {
            return 2f;
        }

        public bool IsInteractionAssistActive()
        {
            return active;
        }
    }
}
