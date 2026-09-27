using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Batch-mode verification for the suites that cover the bug-fix commits but
/// are not part of the required set: party composition, dialogue trees, island
/// flow, and gear progression. Complements RequiredVerificationRunner.
/// </summary>
public static class GoalVerificationRunner
{
    public static void RunGoalVerificationSuitesBatch()
    {
        int passed = 0;
        int failed = 0;

        RunContextSuite<IslandFlowControllerTest>(nameof(IslandFlowControllerTest.RunTests), ref passed, ref failed);
        RunContextSuite<DialogueTreeRunnerTest>(nameof(DialogueTreeRunnerTest.RunTests), ref passed, ref failed);
        RunContextSuite<GearProgressionTest>(nameof(GearProgressionTest.RunAllTests), ref passed, ref failed);
        RunContextSuite<GameSystemsTest>(nameof(GameSystemsTest.RunTests), ref passed, ref failed);
        RunContextSuite<PartySwapServiceTest>(nameof(PartySwapServiceTest.RunTests), ref passed, ref failed);
        RunContextSuite<RestorationTrackerTest>(nameof(RestorationTrackerTest.RunTests), ref passed, ref failed);
        RunContextSuite<IslandContentVerificationTest>(nameof(IslandContentVerificationTest.RunTests), ref passed, ref failed);
        RunNUnitSuite<DialogueEffectVerificationTest>(ref passed, ref failed);
        RunNUnitSuite<PreBossDialogueFlowTest>(ref passed, ref failed);
        RunNUnitSuite<MobileBattleTapTest>(ref passed, ref failed);
        RunNUnitSuite<CombatBalanceInvariantTest>(ref passed, ref failed);
        RunNUnitSuite<AudioMixingInvariantTest>(ref passed, ref failed);
        RunNUnitSuite<MobileTouchInputInvariantTest>(ref passed, ref failed);
        RunNUnitSuite<SaveRestoreInvariantTest>(ref passed, ref failed);
        RunNUnitSuite<TideBreakTargetingTest>(ref passed, ref failed);
        RunContextSuite<MobileTouchControllerTest>(nameof(MobileTouchControllerTest.RunTests), ref passed, ref failed);
        RunContextSuite<MobileTouchInputManagerTest>(nameof(MobileTouchInputManagerTest.RunAllTests), ref passed, ref failed);
        RunNUnitSuite<HeroDataTestSuite>(ref passed, ref failed);
        RunNUnitSuite<PartySetupTestSuite>(ref passed, ref failed);
        RunNUnitSuite<HeroProgressionTest>(ref passed, ref failed);
        RunNUnitSuite<EndingEvaluatorTest>(ref passed, ref failed);
        RunNUnitSuite<StoryProgressionTest>(ref passed, ref failed);
        RunNUnitSuite<IslandRestorationStateEdgeTest>(ref passed, ref failed);
        RunNUnitSuite<IslandProgressionTravelTest>(ref passed, ref failed);
        RunNUnitSuite<IslandBacktrackingManagerTest>(ref passed, ref failed);
        RunNUnitSuite<GameStateManagerRefactorTest>(ref passed, ref failed);

        string summary = $"Goal verification complete. Suites/tests passed: {passed}. Failed: {failed}.";
        if (failed == 0)
        {
            Debug.Log($"[GoalVerificationRunner] {summary}");
        }
        else
        {
            Debug.LogError($"[GoalVerificationRunner] {summary}");
        }

        if (failed > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    private static void RunContextSuite<T>(string methodName, ref int passed, ref int failed) where T : MonoBehaviour
    {
        GameObject testObject = new GameObject($"GoalVerification_{typeof(T).Name}");
        try
        {
            T component = testObject.AddComponent<T>();
            MethodInfo method = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new MissingMethodException(typeof(T).Name, methodName);
            }

            method.Invoke(component, null);
            passed++;
            Debug.Log($"[GoalVerificationRunner] PASS {typeof(T).Name}.{methodName}");
        }
        catch (Exception ex)
        {
            failed++;
            Exception root = Unwrap(ex);
            Debug.LogError($"[GoalVerificationRunner] FAIL {typeof(T).Name}.{methodName}: {root.GetType().Name}: {root.Message}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    private static void RunNUnitSuite<T>(ref int passed, ref int failed) where T : class
    {
        MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo setUp = FindAttributedMethod<SetUpAttribute>(methods);
        MethodInfo tearDown = FindAttributedMethod<TearDownAttribute>(methods);

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo test = methods[i];
            if (!Attribute.IsDefined(test, typeof(TestAttribute)))
            {
                continue;
            }

            // A MonoBehaviour fixture needs a real backing GameObject:
            // `new T()` yields a component with no host, so any access to
            // gameObject or a component lookup would throw and be misreported
            // as a test failure. Plain classes are constructed directly.
            GameObject host = null;
            T instance;
            try
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                {
                    host = new GameObject($"GoalVerification_{typeof(T).Name}_{test.Name}");
                    instance = (T)(object)host.AddComponent(typeof(T));
                }
                else
                {
                    instance = Activator.CreateInstance<T>();
                }
            }
            catch (Exception ex)
            {
                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }

                failed++;
                Exception root = Unwrap(ex);
                Debug.LogError($"[GoalVerificationRunner] FAIL {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
                continue;
            }

            bool tearDownAttempted = false;
            try
            {
                setUp?.Invoke(instance, null);
                test.Invoke(instance, null);
                tearDownAttempted = true;
                tearDown?.Invoke(instance, null);
                passed++;
                Debug.Log($"[GoalVerificationRunner] PASS {typeof(T).Name}.{test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Exception root = Unwrap(ex);
                Debug.LogError($"[GoalVerificationRunner] FAIL {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
            }
            finally
            {
                if (!tearDownAttempted && tearDown != null)
                {
                    try
                    {
                        tearDownAttempted = true;
                        tearDown.Invoke(instance, null);
                    }
                    catch (Exception ex)
                    {
                        Exception root = Unwrap(ex);
                        Debug.LogError($"[GoalVerificationRunner] Teardown failed for {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
                    }
                }

                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }
        }
    }

    private static MethodInfo FindAttributedMethod<TAttribute>(MethodInfo[] methods) where TAttribute : Attribute
    {
        for (int i = 0; i < methods.Length; i++)
        {
            if (Attribute.IsDefined(methods[i], typeof(TAttribute)))
            {
                return methods[i];
            }
        }

        return null;
    }

    private static Exception Unwrap(Exception ex)
    {
        Exception current = ex;
        while (current is TargetInvocationException invocation && invocation.InnerException != null)
        {
            current = invocation.InnerException;
        }

        return current;
    }
}
