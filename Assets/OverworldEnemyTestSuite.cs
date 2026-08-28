using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class OverworldEnemyTestSuite
{
    private GameObject enemyObject;
    private GameObject playerObject;
    private OverworldEnemy enemy;

    [SetUp]
    public void SetUp()
    {
        enemyObject = new GameObject("TestOverworldEnemy");
        enemyObject.AddComponent<Rigidbody>();
        enemyObject.AddComponent<BoxCollider>();
        enemy = enemyObject.AddComponent<OverworldEnemy>();

        playerObject = new GameObject("TestPlayer");
    }

    [TearDown]
    public void TearDown()
    {
        if (enemyObject != null)
        {
            Object.DestroyImmediate(enemyObject);
        }

        if (playerObject != null)
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void PuzzleGuardReturningDoesNotReengageWhilePlayerIsStillOutsideAnchorRadius()
    {
        ConfigureReturningPuzzleGuard(enemyPosition: new Vector3(6.5f, 0f, 0f), playerPosition: new Vector3(7.6f, 0f, 0f));

        bool shouldStartChase = InvokeShouldStartChase();

        Assert.IsFalse(shouldStartChase,
            "Puzzle guards should finish returning before re-engaging when the player remains outside the anchor re-engage radius.");
    }

    [Test]
    public void PuzzleGuardReturningCanReengageOncePlayerReentersAnchorRadius()
    {
        ConfigureReturningPuzzleGuard(enemyPosition: new Vector3(6.5f, 0f, 0f), playerPosition: new Vector3(6.8f, 0f, 0f));

        bool shouldStartChase = InvokeShouldStartChase();

        Assert.IsTrue(shouldStartChase,
            "Puzzle guards should re-engage after the player comes back inside the anchor re-engage radius.");
    }

    [Test]
    public void KinematicEnemyMovementDoesNotSetLinearVelocity()
    {
        Rigidbody body = enemyObject.GetComponent<Rigidbody>();
        body.isKinematic = true;

        InvokeMoveToward(new Vector3(4f, 0f, 0f), 2f);

        Assert.AreEqual(Vector3.zero, body.linearVelocity,
            "Kinematic enemies should move with MovePosition instead of assigning linear velocity.");
    }

    private void ConfigureReturningPuzzleGuard(Vector3 enemyPosition, Vector3 playerPosition)
    {
        enemyObject.transform.position = enemyPosition;
        playerObject.transform.position = playerPosition;

        SetPrivateField("playerTransform", playerObject.transform);
        SetPrivateField("currentState", EnemyState.Returning);
        SetPrivateField("isPuzzleGuard", true);
        SetPrivateField("guardAnchorPosition", Vector3.zero);
        SetPrivateField("puzzleGuardLeashRadius", 8f);
        SetPrivateField("puzzleGuardLeashReengageBuffer", 1f);
        SetPrivateField("puzzleGuardChaseLockUntilTime", -1f);
        SetPrivateField("proximityAggroRange", 7.5f);
        SetPrivateField("arrivalThreshold", 0.3f);
        SetPrivateField("requireLineOfSightForAggro", false);
    }

    private bool InvokeShouldStartChase()
    {
        MethodInfo method = typeof(OverworldEnemy).GetMethod("ShouldStartChase", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "ShouldStartChase should exist for puzzle guard chase tests.");
        return (bool)method.Invoke(enemy, null);
    }

    private void InvokeMoveToward(Vector3 target, float speed)
    {
        MethodInfo method = typeof(OverworldEnemy).GetMethod("MoveToward", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, "MoveToward should exist for kinematic movement tests.");
        method.Invoke(enemy, new object[] { target, speed });
    }

    private void SetPrivateField(string fieldName, object value)
    {
        FieldInfo field = typeof(OverworldEnemy).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field '{fieldName}' should exist on OverworldEnemy.");
        field.SetValue(enemy, value);
    }
}
