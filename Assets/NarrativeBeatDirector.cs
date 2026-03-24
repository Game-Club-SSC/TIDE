using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class NarrativeBeatDirector : MonoBehaviour
{
    private const string IntroBeatId = "beat_intro_tension";
    private const string PreCombatBeatId = "beat_pre_guard_combat";
    private const string PostRestorationBeatId = "beat_post_restoration_reflection";

    [Header("Timing")]
    [SerializeField] private float introDelaySeconds = 1.2f;
    [SerializeField] private float beatRepeatCooldown = 6f;
    [SerializeField] private string primaryIslandId = "default";

    private float introTimer;
    private float beatCooldownTimer;
    private bool introQueued;

    private void OnEnable()
    {
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

    private static bool TryShouldTriggerPreCombatBeat()
    {
        OverworldEnemy[] enemies = FindObjectsByType<OverworldEnemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
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

        string islandId = string.IsNullOrEmpty(primaryIslandId) ? "default" : primaryIslandId;
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
}
