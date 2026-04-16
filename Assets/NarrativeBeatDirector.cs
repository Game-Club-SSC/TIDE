using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class NarrativeBeatDirector : MonoBehaviour
{
    private const string IntroBeatId = "beat_intro_tension";
    private const string PreCombatBeatId = "beat_pre_guard_combat";
    private const string PostRestorationBeatId = "beat_post_restoration_reflection";
    private const string ActTwoBeatId = "beat_act_two_revelation";
    private const string ActThreeBeatId = "beat_act_three_acceptance";
    private const string GoodEndingBeatId = "beat_ending_good";
    private const string BadEndingBeatId = "beat_ending_bad";

    [Header("Timing")]
    [SerializeField] private float introDelaySeconds = 1.2f;
    [SerializeField] private float beatRepeatCooldown = 6f;
    [SerializeField] private string primaryIslandId = "island_lust";
    [SerializeField] private float preCombatTriggerDistance = 6f;
    [SerializeField] private float minimumPlayerTravelBeforePreCombatBeat = 1.5f;

    private float introTimer;
    private float beatCooldownTimer;
    private bool introQueued;
    private Vector3 explorationStartPosition;
    private bool hasExplorationStartPosition;

    private void OnEnable()
    {
        ResetForDebug();
    }

    public void ResetForDebug()
    {
        CacheExplorationStartPosition();
        introTimer = Mathf.Max(0.2f, introDelaySeconds);
        introQueued = true;
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null && gsm.IsNarrativeBeatCompleted(IntroBeatId))
        {
            introQueued = false;
        }

        beatCooldownTimer = 0f;
    }

    private void Update()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return;
        }

        if (gsm.currentState != GameStateManager.GameState.Exploration || gsm.IsTransitioning)
        {
            return;
        }

        if (beatCooldownTimer > 0f)
        {
            beatCooldownTimer -= Time.deltaTime;
            return;
        }

        if (introQueued && !gsm.IsNarrativeBeatCompleted(IntroBeatId))
        {
            introTimer -= Time.deltaTime;
            if (introTimer <= 0f)
            {
                if (ShowBeat(IntroBeatId, BuildIntroBeatTitle(), BuildIntroBeatBody()))
                {
                    introQueued = false;
                }
            }

            return;
        }

        if (!gsm.IsNarrativeBeatCompleted(PreCombatBeatId) && TryShouldTriggerPreCombatBeat())
        {
            ShowBeat(PreCombatBeatId, BuildPreCombatBeatTitle(), BuildPreCombatBeatBody());
            return;
        }

        if (!gsm.IsNarrativeBeatCompleted(PostRestorationBeatId) && TryShouldTriggerPostRestorationBeat())
        {
            ShowBeat(PostRestorationBeatId, BuildPostRestorationBeatTitle(), BuildPostRestorationBeatBody());
            return;
        }

        if (gsm.IsEndingTriggered)
        {
            if (gsm.ResolvedEndingBranch == GameStateManager.EndingBranch.Good
                && !gsm.IsNarrativeBeatCompleted(GoodEndingBeatId))
            {
                ShowBeat(GoodEndingBeatId, BuildGoodEndingBeatTitle(), BuildGoodEndingBeatBody());
            }
            else if (gsm.ResolvedEndingBranch == GameStateManager.EndingBranch.Bad
                && !gsm.IsNarrativeBeatCompleted(BadEndingBeatId))
            {
                ShowBeat(BadEndingBeatId, BuildBadEndingBeatTitle(), BuildBadEndingBeatBody());
            }

            return;
        }

        if (gsm.CurrentStoryAct >= GameStateManager.StoryAct.ActII && !gsm.IsNarrativeBeatCompleted(ActTwoBeatId))
        {
            ShowBeat(ActTwoBeatId, BuildActTwoBeatTitle(), BuildActTwoBeatBody());
            return;
        }

        if (gsm.CurrentStoryAct >= GameStateManager.StoryAct.ActIII && !gsm.IsNarrativeBeatCompleted(ActThreeBeatId))
        {
            ShowBeat(ActThreeBeatId, BuildActThreeBeatTitle(), BuildActThreeBeatBody());
            return;
        }

    }

    private bool ShowBeat(string beatId, string title, string body)
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return false;
        }

        bool newlyCompleted = gsm.MarkNarrativeBeatCompleted(beatId);
        if (!newlyCompleted)
        {
            return false;
        }

        gsm.RegisterAncientText(beatId, title, body);
        gsm.DiscoverAncientText(beatId);

        AncientTextLogUI logUi = FindFirstObjectByType<AncientTextLogUI>();
        if (logUi == null)
        {
            GameObject logObject = new GameObject("AncientTextLogUI");
            logUi = logObject.AddComponent<AncientTextLogUI>();
        }

        if (logUi != null)
        {
            logUi.ShowEntry(beatId, title, body, true);
        }

        beatCooldownTimer = Mathf.Max(2f, beatRepeatCooldown);
        return true;
    }

    private void CacheExplorationStartPosition()
    {
        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return;
        }

        explorationStartPosition = player.transform.position;
        hasExplorationStartPosition = true;
    }

    private bool HasPlayerMovedEnoughForPreCombatBeat()
    {
        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return false;
        }

        if (!hasExplorationStartPosition)
        {
            explorationStartPosition = player.transform.position;
            hasExplorationStartPosition = true;
            return false;
        }

        Vector3 delta = player.transform.position - explorationStartPosition;
        delta.y = 0f;
        return delta.magnitude >= Mathf.Max(0.5f, minimumPlayerTravelBeforePreCombatBeat);
    }

    private bool TryShouldTriggerPreCombatBeat()
    {
        if (!HasPlayerMovedEnoughForPreCombatBeat())
        {
            return false;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player == null)
        {
            return false;
        }

        Vector3 playerPosition = player.transform.position;
        float triggerDistance = Mathf.Max(1.5f, preCombatTriggerDistance);

        OverworldEnemy[] enemies = FindObjectsByType<OverworldEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            OverworldEnemy enemy = enemies[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 delta = enemy.transform.position - playerPosition;
            delta.y = 0f;
            if (delta.magnitude <= triggerDistance)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryShouldTriggerPostRestorationBeat()
    {
        if (IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        string islandId = IslandThemeRegistry.ResolveIslandId(primaryIslandId);
        float restorationPercent = IslandRestorationTracker.Instance.GetRestorationPercent(islandId);
        return restorationPercent >= 60f;
    }

    private static string BuildIntroBeatTitle()
    {
        return "Campfire Friction";
    }

    private static string BuildIntroBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("Fire: We waste daylight arguing while the island rots.\n");
        body.Append("Water: And if we rush without balance, we become the same rot.\n");
        body.Append("Earth: Save it. We move together or not at all.\n");
        body.Append("Air: Then start with one section. Small. Clean.\n");
        body.Append("Space: One step in balance is still a step forward.");
        return body.ToString();
    }

    private static string BuildPreCombatBeatTitle()
    {
        return "Before the Guard";
    }

    private static string BuildPreCombatBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("Air: That guard is feeding off the sealed tile.\n");
        body.Append("Earth: Break the formation, then the lock should loosen.\n");
        body.Append("Fire: Fine. We cut through, then rebalance the field.");
        return body.ToString();
    }

    private static string BuildPostRestorationBeatTitle()
    {
        return "After the First Shift";
    }

    private static string BuildPostRestorationBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("Water: The island feels lighter... but only for now.\n");
        body.Append("Space: Balance never lasts. It must be renewed.\n");
        body.Append("Fire: Then we keep moving before it slips again.");
        return body.ToString();
    }

    private static string BuildActTwoBeatTitle()
    {
        return "What The Texts Meant";
    }

    private static string BuildActTwoBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("Earth: These records are not warnings. They are instructions.\n");
        body.Append("Water: Every century, the same march. The same ending.\n");
        body.Append("Air: Then the silence between us is not fear. It is recognition.");
        return body.ToString();
    }

    private static string BuildActThreeBeatTitle()
    {
        return "Acceptance Before The Last Shore";
    }

    private static string BuildActThreeBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("Fire: We know what waits after this.\n");
        body.Append("Space: Knowing does not empty the path. It only clarifies it.\n");
        body.Append("Earth: Then we finish what we were sent here to finish, together.");
        return body.ToString();
    }

    private static string BuildGoodEndingBeatTitle()
    {
        return "Sunset In Balance";
    }

    private static string BuildGoodEndingBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("The six enemies are gone, and with them the need for the chosen five.\n");
        body.Append("The party understands at last that they were born only to restore balance.\n");
        body.Append("Facing the sunset together, they accept that peace costs them their own fading light.");
        return body.ToString();
    }

    private static string BuildBadEndingBeatTitle()
    {
        return "Sunset Without Purpose";
    }

    private static string BuildBadEndingBeatBody()
    {
        StringBuilder body = new StringBuilder();
        body.Append("The party falls before finishing its purpose, and only the main character remains.\n");
        body.Append("Despair twists fate into meaninglessness instead of acceptance.\n");
        body.Append("On the hill at sunset, he dies believing the cycle ended in nothing.");
        return body.ToString();
    }
}
