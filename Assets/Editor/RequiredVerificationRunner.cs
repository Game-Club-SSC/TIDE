using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public static class RequiredVerificationRunner
{
    [MenuItem("TIDE/Run Required Verification Suites")]
    public static void RunRequiredVerificationSuites()
    {
        int passed = 0;
        int failed = 0;

        RunContextSuite<PostDeferralVerticalSliceRegressionRunner>(nameof(PostDeferralVerticalSliceRegressionRunner.RunMatrix), ref passed, ref failed);
        RunContextSuite<VerticalSliceRegressionRunnerTest>(nameof(VerticalSliceRegressionRunnerTest.RunTests), ref passed, ref failed);
        RunContextSuite<AncientTextRevealDirectorTest>(nameof(AncientTextRevealDirectorTest.RunTests), ref passed, ref failed);
        RunContextSuite<MobileTouchInputManagerTest>(nameof(MobileTouchInputManagerTest.RunAllTests), ref passed, ref failed);
        RunNUnitSuite<BattleFlowTestSuite>(ref passed, ref failed);

        string summary = $"Required verification complete. Suites passed: {passed}. Tests or suites failed: {failed}.";
        if (failed == 0)
        {
            Debug.Log($"[RequiredVerificationRunner] {summary}");
        }
        else
        {
            Debug.LogError($"[RequiredVerificationRunner] {summary}");
        }

        EditorUtility.DisplayDialog("TIDE Verification", summary, "OK");
    }

    private static void RunContextSuite<T>(string methodName, ref int passed, ref int failed) where T : MonoBehaviour
    {
        GameObject testObject = new GameObject($"RequiredVerification_{typeof(T).Name}");
        try
        {
            T component = testObject.AddComponent<T>();
            MethodInfo method = typeof(T).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
            {
                throw new MissingMethodException(typeof(T).Name, methodName);
            }

            method.Invoke(component, null);
            if (component is PostDeferralVerticalSliceRegressionRunner matrix && matrix.FailedCount > 0)
            {
                throw new AssertionException($"Post-deferral matrix reported {matrix.FailedCount} failed steps.");
            }

            passed++;
            Debug.Log($"[RequiredVerificationRunner] PASS {typeof(T).Name}.{methodName}");
        }
        catch (Exception ex)
        {
            failed++;
            Exception root = Unwrap(ex);
            Debug.LogError($"[RequiredVerificationRunner] FAIL {typeof(T).Name}.{methodName}: {root.GetType().Name}: {root.Message}");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    private static void RunNUnitSuite<T>(ref int passed, ref int failed) where T : new()
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

            T instance = new T();
            bool tearDownAttempted = false;
            try
            {
                setUp?.Invoke(instance, null);
                test.Invoke(instance, null);
                tearDownAttempted = true;
                tearDown?.Invoke(instance, null);
                passed++;
                Debug.Log($"[RequiredVerificationRunner] PASS {typeof(T).Name}.{test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Exception root = Unwrap(ex);
                Debug.LogError($"[RequiredVerificationRunner] FAIL {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
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
                        Debug.LogError($"[RequiredVerificationRunner] Teardown failed for {typeof(T).Name}.{test.Name}: {root.GetType().Name}: {root.Message}");
                    }
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
