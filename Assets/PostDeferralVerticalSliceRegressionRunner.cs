using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class PostDeferralVerticalSliceRegressionRunner : MonoBehaviour
{
    [Serializable]
    private struct RegressionResult
    {
        public string matrixId;
        public string scene;
        public string script;
        public bool passed;
        public string details;
    }

    private readonly List<RegressionResult> lastResults = new List<RegressionResult>();

    [SerializeField]
    [TextArea(8, 24)]
    private string latestReport;

    [ContextMenu("Run Post-Deferral Vertical Slice Regression Matrix")]
    public void RunMatrix()
    {
        lastResults.Clear();

        Debug.Log("=== Starting Post-Deferral Vertical Slice Regression Matrix ===");

        RunStep("VSR-001", "level_1.unity", "Core content + ordering", "IslandContentVerificationTest.RunTests", () =>
            RunContextMenuSuite<IslandContentVerificationTest>(nameof(IslandContentVerificationTest.RunTests)));
        RunStep("VSR-002", "level_1.unity", "Restoration accounting", "RestorationTrackerTest.RunTests", () =>
            RunContextMenuSuite<RestorationTrackerTest>(nameof(RestorationTrackerTest.RunTests)));
        RunStep("VSR-003", "level_1.unity", "Boss gate unlock", "BossEncounterGateTest.RunTests", () =>
            RunContextMenuSuite<BossEncounterGateTest>(nameof(BossEncounterGateTest.RunTests)));
        RunStep("VSR-004", "level_1.unity", "Threshold gate startup + transitions", "RestorationThresholdGateTest.RunTests", () =>
            RunContextMenuSuite<RestorationThresholdGateTest>(nameof(RestorationThresholdGateTest.RunTests)));
        RunStep("VSR-005", "level_1.unity", "Inter-island progression + travel", "IslandProgressionTravelTest.RunTests", () =>
            RunContextMenuSuite<IslandProgressionTravelTest>(nameof(IslandProgressionTravelTest.RunTests)));
        RunStep("VSR-006", "CombatScene.unity", "Hero XP + level progression", "HeroProgressionTest.RunAllTests", () =>
            RunContextMenuSuite<HeroProgressionTest>(nameof(HeroProgressionTest.RunAllTests)));
        RunStep("VSR-007", "CombatScene.unity", "Gear equip/stat effects", "GearSystemTest.RunAllTests", () =>
            RunContextMenuSuite<GearSystemTest>(nameof(GearSystemTest.RunAllTests)));
        RunStep("VSR-008", "CombatScene.unity", "Gear XP + duplication gates", "GearProgressionTest.RunAllTests", () =>
            RunContextMenuSuite<GearProgressionTest>(nameof(GearProgressionTest.RunAllTests)));
        RunStep("VSR-009", "level_1.unity", "Deferral guardrail debug controls", "DevGodModeStateTest.RunTests", () =>
            RunContextMenuSuite<DevGodModeStateTest>(nameof(DevGodModeStateTest.RunTests)));

        latestReport = BuildReport();
        Debug.Log(latestReport);
        Debug.Log("=== Completed Post-Deferral Vertical Slice Regression Matrix ===");
    }

    private void RunStep(string matrixId, string scene, string coverage, string script, Action action)
    {
        try
        {
            action?.Invoke();
            lastResults.Add(new RegressionResult
            {
                matrixId = matrixId,
                scene = scene,
                script = script,
                passed = true,
                details = coverage
            });
            Debug.Log($"[PostDeferralVerticalSliceRegressionRunner] {matrixId} PASS - {script}");
        }
        catch (Exception ex)
        {
            Exception root = Unwrap(ex);
            string details = $"{coverage} | {root.GetType().Name}: {root.Message}";
            lastResults.Add(new RegressionResult
            {
                matrixId = matrixId,
                scene = scene,
                script = script,
                passed = false,
                details = details
            });
            Debug.LogError($"[PostDeferralVerticalSliceRegressionRunner] {matrixId} FAIL - {script} | {details}");
        }
    }

    private static void RunContextMenuSuite<T>(string methodName) where T : MonoBehaviour
    {
        GameObject testObject = new GameObject($"MatrixRunner_{typeof(T).Name}");
        try
        {
            T component = testObject.AddComponent<T>();
            MethodInfo method = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new MissingMethodException(typeof(T).Name, methodName);
            }

            method.Invoke(component, null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    private string BuildReport()
    {
        int passCount = 0;
        for (int i = 0; i < lastResults.Count; i++)
        {
            if (lastResults[i].passed)
            {
                passCount++;
            }
        }

        int failCount = lastResults.Count - passCount;
        List<string> lines = new List<string>
        {
            "Post-Deferral Vertical Slice Regression Matrix Results",
            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"Totals: Pass={passCount} Fail={failCount}",
            ""
        };

        for (int i = 0; i < lastResults.Count; i++)
        {
            RegressionResult result = lastResults[i];
            string status = result.passed ? "PASS" : "FAIL";
            lines.Add($"{result.matrixId} | {status} | {result.scene} | {result.script} | {result.details}");
        }

        return string.Join("\n", lines);
    }

    private static Exception Unwrap(Exception exception)
    {
        if (exception is TargetInvocationException invocationException && invocationException.InnerException != null)
        {
            return Unwrap(invocationException.InnerException);
        }

        return exception;
    }
}
