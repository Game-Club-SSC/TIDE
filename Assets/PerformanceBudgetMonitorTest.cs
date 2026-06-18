using NUnit.Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class PerformanceBudgetMonitorTest : MonoBehaviour
{
    [ContextMenu("Run Performance Budget Monitor Tests")]
    public void RunTests()
    {
        Debug.Log("=== Starting Performance Budget Monitor Tests ===");

        TestMonitorSingletonExists();
        TestIsWithinUnitCap();
        TestFrameRecordingComputesAverage();
        TestResetClearsState();
        TestBudgetDefaults();

        Debug.Log("=== All Performance Budget Monitor Tests Passed ===");
    }

    private void TestMonitorSingletonExists()
    {
        GameObject host = new GameObject("Test_Perf");
        PerformanceBudgetMonitor monitor = host.AddComponent<PerformanceBudgetMonitor>();
        try
        {
            Assert.IsNotNull(PerformanceBudgetMonitor.Instance, "Instance should be set.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestIsWithinUnitCap()
    {
        GameObject host = new GameObject("Test_Perf2");
        PerformanceBudgetMonitor monitor = host.AddComponent<PerformanceBudgetMonitor>();
        try
        {
            Assert.IsTrue(monitor.IsWithinUnitCap(6, 6), "6+6 should be within cap.");
            Assert.IsFalse(monitor.IsWithinUnitCap(7, 6), "7 heroes should exceed cap.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestFrameRecordingComputesAverage()
    {
        GameObject host = new GameObject("Test_Perf3");
        PerformanceBudgetMonitor monitor = host.AddComponent<PerformanceBudgetMonitor>();
        try
        {
            monitor.RecordFrame(10f);
            monitor.RecordFrame(20f);
            monitor.RecordFrame(30f);
            Assert.AreEqual(20f, monitor.CurrentAverageFrameMs, 0.001f, "Average should be 20.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestResetClearsState()
    {
        GameObject host = new GameObject("Test_Perf4");
        PerformanceBudgetMonitor monitor = host.AddComponent<PerformanceBudgetMonitor>();
        try
        {
            monitor.RecordFrame(20f);
            monitor.ResetForDebug();
            Assert.AreEqual(0f, monitor.CurrentAverageFrameMs, "Average should reset to 0.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    private void TestBudgetDefaults()
    {
        GameObject host = new GameObject("Test_Perf5");
        PerformanceBudgetMonitor monitor = host.AddComponent<PerformanceBudgetMonitor>();
        try
        {
            Assert.AreEqual(60, monitor.TargetFrameRate, "Default target should be 60fps.");
            Assert.GreaterOrEqual(monitor.MaxFrameMs, 16f, "Default max frame ms should be ~16.67.");
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }
}
