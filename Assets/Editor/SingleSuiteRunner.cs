using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>Batch entry point that runs a single NUnit suite in isolation.</summary>
public static class SingleSuiteRunner
{
    public static void RunDialogueEffectOnly()
    {
        Run<DialogueEffectVerificationTest>();
    }

    public static void RunAllDialogueEffect()
    {
        Run<DialogueEffectVerificationTest>();
    }

    private static void Run<T>() where T : new()
    {
        MethodInfo[] methods = typeof(T).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo setUp = Find<SetUpAttribute>(methods);
        MethodInfo tearDown = Find<TearDownAttribute>(methods);
        int passed = 0;
        int failed = 0;

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo test = methods[i];
            if (!Attribute.IsDefined(test, typeof(TestAttribute)))
            {
                continue;
            }

            T instance = new T();
            try
            {
                setUp?.Invoke(instance, null);
                test.Invoke(instance, null);
                passed++;
                Debug.Log($"[SingleSuiteRunner] PASS {typeof(T).Name}.{test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Exception root = ex;
                while (root is TargetInvocationException tie && tie.InnerException != null)
                {
                    root = tie.InnerException;
                }

                Debug.LogError($"[SingleSuiteRunner] FAIL {typeof(T).Name}.{test.Name}: {root.Message}");
            }
            finally
            {
                try
                {
                    tearDown?.Invoke(instance, null);
                }
                catch (Exception teardownError)
                {
                    Debug.LogError($"[SingleSuiteRunner] Teardown error: {teardownError.Message}");
                }
            }
        }

        Debug.Log($"[SingleSuiteRunner] {typeof(T).Name}: passed {passed}, failed {failed}.");
        if (failed > 0)
        {
            EditorApplication.Exit(1);
        }
    }

    private static MethodInfo Find<TAttribute>(MethodInfo[] methods) where TAttribute : Attribute
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
}
