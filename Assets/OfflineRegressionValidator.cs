using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Offline validation of the Post-Deferral Vertical Slice Regression Matrix.
/// Run from the Unity menu (no scene required) or via static CLI entry point.
/// Checks script existence, component availability, ContextMenu wiring,
/// and per-row acceptance-criteria readiness without executing actual tests.
/// </summary>
public static class OfflineRegressionValidator
{
    // ───────────────────────── Matrix definition ─────────────────────────

    private struct MatrixRow
    {
        public string id;
        public string scene;
        public string scriptName;
        public string entryMethod;
        public string acceptanceSummary;
    }

    private static readonly MatrixRow[] Rows =
    {
        new MatrixRow
        {
            id = "VSR-001", scene = "level_1.unity",
            scriptName = "IslandContentVerificationTest", entryMethod = "RunTests",
            acceptanceSummary = "Combat system regression: island content set, encounter ordering, restoration budgets, boss-threshold budgeting"
        },
        new MatrixRow
        {
            id = "VSR-002", scene = "level_1.unity",
            scriptName = "RestorationTrackerTest", entryMethod = "RunTests",
            acceptanceSummary = "Restoration accounting: duplicate protection, threshold checks, reset/multi-island isolation"
        },
        new MatrixRow
        {
            id = "VSR-003", scene = "level_1.unity",
            scriptName = "BossEncounterGateTest", entryMethod = "RunTests",
            acceptanceSummary = "Boss unlock at threshold/above-threshold, event behavior, island filtering"
        },
        new MatrixRow
        {
            id = "VSR-004", scene = "level_1.unity",
            scriptName = "RestorationThresholdGateTest", entryMethod = "RunTests",
            acceptanceSummary = "Restoration threshold gate startup sync and threshold transitions"
        },
        new MatrixRow
        {
            id = "VSR-005", scene = "level_1.unity",
            scriptName = "IslandProgressionTravelTest", entryMethod = "RunTests",
            acceptanceSummary = "Exploration progression and travel unlocks after restoration completion"
        },
        new MatrixRow
        {
            id = "VSR-006", scene = "CombatScene.unity",
            scriptName = "HeroProgressionTest", entryMethod = "RunAllTests",
            acceptanceSummary = "Combat-driven hero XP and leveling behaviors"
        },
        new MatrixRow
        {
            id = "VSR-007", scene = "CombatScene.unity",
            scriptName = "GearSystemTest", entryMethod = "RunAllTests",
            acceptanceSummary = "Gear equip/unequip/full-set effects with level growth application"
        },
        new MatrixRow
        {
            id = "VSR-008", scene = "CombatScene.unity",
            scriptName = "GearProgressionTest", entryMethod = "RunAllTests",
            acceptanceSummary = "Gear progression milestones, random slot rules, duplication/finalization guardrails"
        },
        new MatrixRow
        {
            id = "VSR-009", scene = "level_1.unity",
            scriptName = "DevGodModeStateTest", entryMethod = "RunTests",
            acceptanceSummary = "Post-deferral debug state controls for tracker/progression consistency"
        },
    };

    // ─────────────── Required production components per VSR row ───────────

    private static readonly Dictionary<string, string[]> RequiredComponents = new Dictionary<string, string[]>
    {
        { "VSR-001", new[] { "IslandRestorationTracker", "BossEncounterGate", "IslandThemeRegistry" } },
        { "VSR-002", new[] { "IslandRestorationTracker" } },
        { "VSR-003", new[] { "IslandRestorationTracker", "BossEncounterGate" } },
        { "VSR-004", new[] { "IslandRestorationTracker", "RestorationThresholdGate" } },
        { "VSR-005", new[] { "IslandProgressionManager", "IslandRestorationTracker" } },
        { "VSR-006", new[] { "HeroProgressionManager", "LevelingConfig" } },
        { "VSR-007", new[] { "HeroProgressionManager", "LevelingConfig" } },
        { "VSR-008", new[] { "GearInstance", "GearSetData", "GearBonusStatType" } },
        { "VSR-009", new[] { "IslandRestorationTracker", "IslandProgressionManager", "DevModeController" } },
    };

    // ─────────────────────── Validation result types ─────────────────────

    private enum Status { Pass, Fail, Warn }

    private struct CheckResult
    {
        public string label;
        public Status status;
        public string detail;
    }

    // ──────────────────────── Public entry points ────────────────────────

    /// <summary>
    /// Unity menu entry point. Visible as Tools > Offline Regression Validator.
    /// </summary>
    [MenuItem("Tools/Offline Regression Validator/Run Full Validation")]
    public static void RunFromMenu()
    {
        string report = Run();
        Debug.Log(report);
    }

    /// <summary>
    /// CLI-safe entry point. Returns the full report string.
    /// </summary>
    public static string Run()
    {
        List<CheckResult> results = new List<CheckResult>();
        int passCount = 0;
        int failCount = 0;
        int warnCount = 0;

        // ── 1. Scan all .cs files under Assets for test scripts ──
        string assetsPath = Path.Combine(Application.dataPath);
        Dictionary<string, string> foundScripts = FindTestScripts(assetsPath);

        // ── 2. Per-row validation ──
        foreach (MatrixRow row in Rows)
        {
            // 2a. Script file exists
            string scriptKey = row.scriptName + ".cs";
            if (!foundScripts.ContainsKey(scriptKey))
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: script file",
                    status = Status.Fail,
                    detail = $"Script file '{scriptKey}' not found under Assets/."
                });
                failCount++;
            }
            else
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: script file",
                    status = Status.Pass,
                    detail = foundScripts[scriptKey]
                });
                passCount++;
            }

            // 2b. Entry method exists and is public
            Type scriptType = FindTypeByName(row.scriptName);
            if (scriptType == null)
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: type resolution",
                    status = Status.Fail,
                    detail = $"Type '{row.scriptName}' not found in loaded assemblies."
                });
                failCount++;
            }
            else
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: type resolution",
                    status = Status.Pass,
                    detail = scriptType.FullName
                });
                passCount++;

                // Entry method
                MethodInfo method = scriptType.GetMethod(
                    row.entryMethod,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: entry method '{row.entryMethod}'",
                        status = Status.Fail,
                        detail = $"Public method '{row.entryMethod}' not found on {row.scriptName}."
                    });
                    failCount++;
                }
                else
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: entry method '{row.entryMethod}'",
                        status = Status.Pass,
                        detail = $"{method.ReturnType.Name} {method.Name}({FormatParams(method)})"
                    });
                    passCount++;
                }

                // ContextMenu attribute on entry method
                bool hasContextMenu = method != null &&
                    Attribute.IsDefined(method, typeof(ContextMenuAttribute));
                if (!hasContextMenu)
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: ContextMenu on '{row.entryMethod}'",
                        status = Status.Warn,
                        detail = "Entry method does not have [ContextMenu] attribute. Matrix runner uses reflection, so this is non-blocking."
                    });
                    warnCount++;
                }
                else
                {
                    ContextMenuAttribute attr = (ContextMenuAttribute)Attribute.GetCustomAttribute(
                        method, typeof(ContextMenuAttribute));
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: ContextMenu on '{row.entryMethod}'",
                        status = Status.Pass,
                        detail = $"ContextMenu(\"{attr.menuItem}\")"
                    });
                    passCount++;
                }

                // MonoBehaviour inheritance
                bool isMonoBehaviour = typeof(MonoBehaviour).IsAssignableFrom(scriptType);
                if (!isMonoBehaviour)
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: MonoBehaviour inheritance",
                        status = Status.Fail,
                        detail = $"{row.scriptName} does not inherit from MonoBehaviour."
                    });
                    failCount++;
                }
                else
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: MonoBehaviour inheritance",
                        status = Status.Pass,
                        detail = "OK"
                    });
                    passCount++;
                }

                // DisallowMultipleComponent attribute
                bool hasDisallow = Attribute.IsDefined(scriptType, typeof(DisallowMultipleComponentAttribute));
                if (!hasDisallow)
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: DisallowMultipleComponent",
                        status = Status.Warn,
                        detail = "Missing [DisallowMultipleComponent]. Runner may attach duplicate instances."
                    });
                    warnCount++;
                }
                else
                {
                    results.Add(new CheckResult
                    {
                        label = $"{row.id}: DisallowMultipleComponent",
                        status = Status.Pass,
                        detail = "OK"
                    });
                    passCount++;
                }
            }

            // 2c. Required production components
            if (RequiredComponents.ContainsKey(row.id))
            {
                foreach (string compName in RequiredComponents[row.id])
                {
                    Type compType = FindTypeByName(compName);
                    if (compType == null)
                    {
                        results.Add(new CheckResult
                        {
                            label = $"{row.id}: component '{compName}'",
                            status = Status.Fail,
                            detail = $"Production type '{compName}' not found in assemblies."
                        });
                        failCount++;
                    }
                    else
                    {
                        results.Add(new CheckResult
                        {
                            label = $"{row.id}: component '{compName}'",
                            status = Status.Pass,
                            detail = compType.FullName
                        });
                        passCount++;
                    }
                }
            }

            // 2d. Scene file existence
            string scenePath = FindSceneFile(row.scene);
            if (string.IsNullOrEmpty(scenePath))
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: scene '{row.scene}'",
                    status = Status.Warn,
                    detail = $"Scene file '{row.scene}' not found under Assets/. Tests create isolated objects so this is non-blocking."
                });
                warnCount++;
            }
            else
            {
                results.Add(new CheckResult
                {
                    label = $"{row.id}: scene '{row.scene}'",
                    status = Status.Pass,
                    detail = scenePath
                });
                passCount++;
            }
        }

        // ── 3. Build report ──
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("================================================================");
        sb.AppendLine("  Offline Regression Validator - Readiness Report");
        sb.AppendLine($"  Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  Total checks: {passCount + failCount + warnCount}");
        sb.AppendLine($"  Pass: {passCount}  Fail: {failCount}  Warn: {warnCount}");
        sb.AppendLine("================================================================");
        sb.AppendLine();

        // Group by VSR row
        string currentRow = null;
        foreach (CheckResult r in results)
        {
            string rowId = r.label.Split(':')[0];
            if (rowId != currentRow)
            {
                if (currentRow != null) sb.AppendLine();
                MatrixRow rowDef = Array.Find(Rows, x => x.id == rowId);
                sb.AppendLine($"--- {rowId}  [{rowDef.acceptanceSummary}]  (scene: {rowDef.scene}) ---");
                currentRow = rowId;
            }

            string icon = r.status == Status.Pass ? "[PASS]"
                        : r.status == Status.Fail ? "[FAIL]"
                        : "[WARN]";
            sb.AppendLine($"  {icon} {r.label}");
            sb.AppendLine($"         {r.detail}");
        }

        sb.AppendLine();
        sb.AppendLine("================================================================");

        if (failCount == 0)
        {
            sb.AppendLine("  RESULT: ALL CHECKS PASSED - Tests are ready to execute in Unity Editor.");
            sb.AppendLine("  Next step: open target scene, attach PostDeferralVerticalSliceRegressionRunner,");
            sb.AppendLine("  invoke 'Run Post-Deferral Vertical Slice Regression Matrix' from context menu.");
        }
        else
        {
            sb.AppendLine($"  RESULT: {failCount} FAILURE(S) DETECTED - Fix issues above before running matrix.");
        }

        sb.AppendLine("================================================================");

        return sb.ToString();
    }

    // ──────────────────────── Helper methods ─────────────────────────────

    private static Dictionary<string, string> FindTestScripts(string rootPath)
    {
        Dictionary<string, string> found = new Dictionary<string, string>();
        if (!Directory.Exists(rootPath))
            return found;

        string[] files = Directory.GetFiles(rootPath, "*Test*.cs", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            found[name] = file;
        }

        return found;
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType(typeName);
            if (t != null)
                return t;
        }

        return null;
    }

    private static string FindSceneFile(string sceneName)
    {
        string[] candidates = Directory.GetFiles(
            Application.dataPath, sceneName, SearchOption.AllDirectories);
        return candidates.Length > 0 ? candidates[0] : null;
    }

    private static string FormatParams(MethodInfo method)
    {
        ParameterInfo[] ps = method.GetParameters();
        if (ps.Length == 0) return string.Empty;
        string[] parts = new string[ps.Length];
        for (int i = 0; i < ps.Length; i++)
            parts[i] = $"{ps[i].ParameterType.Name} {ps[i].Name}";
        return string.Join(", ", parts);
    }
}
