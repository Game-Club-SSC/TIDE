using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// Serializes the current game state into JSON for the phone web controller's
/// stats dashboard. This runs on the main thread and produces a snapshot
/// that can be safely read from the HTTP listener thread.
/// </summary>
public static class GameStateSerializer
{
    public static string BuildFullStateJson()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append('{');

        // Game state
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            AppendJsonField(sb, "gameState", gsm.currentState.ToString(), true);
            AppendJsonField(sb, "storyAct", gsm.CurrentStoryAct.ToString(), false);
            AppendJsonField(sb, "highestAct", gsm.HighestStoryActReached.ToString(), false);
            AppendJsonField(sb, "endingBranch", gsm.ResolvedEndingBranch.ToString(), false);
            AppendJsonField(sb, "isEndingTriggered", gsm.IsEndingTriggered.ToString().ToLowerInvariant(), false);
            AppendJsonField(sb, "isTransitioning", gsm.IsTransitioning.ToString().ToLowerInvariant(), false);
        }

        // Active island
        string activeIsland = IslandThemeRegistry.GetActiveIslandId();
        AppendJsonField(sb, "activeIsland", activeIsland, false);

        // Player position
        IsometricPlayer player = Object.FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            sb.Append("\"playerPos\":{");
            AppendJsonFieldRaw(sb, "x", pos.x.ToString("F2", CultureInfo.InvariantCulture), true);
            AppendJsonFieldRaw(sb, "y", pos.y.ToString("F2", CultureInfo.InvariantCulture), false);
            AppendJsonFieldRaw(sb, "z", pos.z.ToString("F2", CultureInfo.InvariantCulture), false);
            sb.Append("}");

            // Player movement state
            AppendJsonField(sb, "autoRun", player.AutoRunEnabled.ToString().ToLowerInvariant(), false);
            AppendJsonField(sb, "allowHop", player.AllowHop.ToString().ToLowerInvariant(), false);
        }
        else
        {
            AppendJsonField(sb, "playerPos", "null", false);
        }

        // Island restoration percentages
        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker != null)
        {
            sb.Append("\"islandRestorations\":{");
            bool first = true;
            foreach (string islandId in IslandThemeRegistry.ProgressionOrder)
            {
                if (!first) sb.Append(',');
                first = false;
                float pct = tracker.GetRestorationPercent(islandId);
                sb.Append($"\"{EscapeJson(islandId)}\":{pct.ToString("F1", CultureInfo.InvariantCulture)}";
            }
            sb.Append('}');
        }

        // Battle state
        BattleManager battle = Object.FindFirstObjectByType<BattleManager>();
        if (battle != null)
        {
            AppendJsonField(sb, "battlePhase", battle.CurrentPhase.ToString(), false);
        }

        // Flow controller state
        IslandFlowController flow = Object.FindFirstObjectByType<IslandFlowController>();
        if (flow != null)
        {
            AppendJsonField(sb, "flowActive", flow.IsActive.ToString().ToLowerInvariant(), false);
        }

        // Phone controller state
        PhoneInputBridge phone = PhoneInputBridge.Instance;
        if (phone != null)
        {
            AppendJsonField(sb, "phonePaired", phone.IsPaired.ToString().ToLowerInvariant(), false);
            AppendJsonField(sb, "phoneInputActive", phone.IsPhoneInputActive.ToString().ToLowerInvariant(), false);
        }

        sb.Append('}');
        return sb.ToString();
    }

    private static void AppendJsonField(StringBuilder sb, string key, string value, bool isFirst)
    {
        if (!isFirst)
        {
            sb.Append(',');
        }
        sb.Append($"\"{EscapeJson(key)}\":\"{EscapeJson(value)}\"");
    }

    private static void AppendJsonFieldRaw(StringBuilder sb, string key, string value, bool isFirst)
    {
        if (!isFirst)
        {
            sb.Append(',');
        }
        sb.Append($"\"{EscapeJson(key)}\":{value}");
    }

    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }
}
