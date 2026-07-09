using System.Text;
using UnityEngine;
using System.Collections.Generic;

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

    public static string GoodEndingBeatIdPublic => GoodEndingBeatId;
    public static string BadEndingBeatIdPublic => BadEndingBeatId;
    public static string ActThreeBeatIdPublic => ActThreeBeatId;

    [Header("Timing")]
    [SerializeField] private float introDelaySeconds = 1.2f;
    [SerializeField] private bool introRequiresPlayerMovement = true;
    [SerializeField] private float minimumPlayerTravelBeforeIntroBeat = 0.75f;
    [SerializeField] private float beatRepeatCooldown = 6f;
    [SerializeField] private string primaryIslandId = "island_lust";
    [SerializeField] private float preCombatTriggerDistance = 6f;
    [SerializeField] private float minimumPlayerTravelBeforePreCombatBeat = 1.5f;
    [SerializeField] private float postRestorationTriggerPercent = 60f;

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
        hasExplorationStartPosition = false;
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

    public bool ForceShowGoodEndingBeatForDebug()
    {
        return ShowBeat(GoodEndingBeatId, BuildGoodEndingBeatTitle(), BuildGoodEndingBeatBody());
    }

    public bool ForceShowBadEndingBeatForDebug()
    {
        return ShowBeat(BadEndingBeatId, BuildBadEndingBeatTitle(), BuildBadEndingBeatBody());
    }

    public bool ForceShowActThreeBeatForDebug()
    {
        return ShowBeat(ActThreeBeatId, BuildActThreeBeatTitle(), BuildActThreeBeatBody());
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
            if (introTimer <= 0f
                && (!introRequiresPlayerMovement || HasPlayerMovedAtLeast(minimumPlayerTravelBeforeIntroBeat)))
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
            logObject.transform.SetParent(transform);
            logUi = logObject.AddComponent<AncientTextLogUI>();
        }

        if (logUi != null)
        {
            logUi.ShowEntry(beatId, title, body, true);
        }

        beatCooldownTimer = Mathf.Max(2f, beatRepeatCooldown);

        // Wire dialogue trees to beat triggers
        TryPlayLinkedDialogueTree(beatId);

        return true;
    }

    // ================================================================== //
    //  Dialogue Tree Integration
    // ================================================================== //

    /// <summary>
    /// Maps beat IDs to dialogue tree factory methods. When a beat fires,
    /// the associated dialogue tree is played if available.
    /// </summary>
    private static readonly Dictionary<string, System.Func<DialogueTree>> BeatToDialogueMap =
        new Dictionary<string, System.Func<DialogueTree>>
    {
        { IntroBeatId, HeroDialogueContent.CeremonyDialogue },
        { PostRestorationBeatId, HeroDialogueContent.AncientTextReactionActI },
        { ActTwoBeatId, HeroDialogueContent.AncientTextReactionActII },
        { ActThreeBeatId, HeroDialogueContent.AncientTextReactionActIII },
    };

    /// <summary>
    /// Maps island IDs to pre-boss dialogue tree factory methods.
    /// </summary>
    private static readonly Dictionary<string, System.Func<DialogueTree>> IslandToPreBossDialogue =
        new Dictionary<string, System.Func<DialogueTree>>
    {
        { "island_greed", HeroDialogueContent.PreBossGreedDialogue },
        { "island_desire", HeroDialogueContent.PreBossAttachmentDialogue },
        { "island_envy", HeroDialogueContent.PreBossJealousyDialogue },
        { "island_lust", HeroDialogueContent.PreBossLustDialogue },
        { "island_anger", HeroDialogueContent.PreBossAngerDialogue },
        { "island_ego", HeroDialogueContent.PreBossEgoDialogue },
    };

    /// <summary>
    /// Attempts to play a dialogue tree linked to the given beat ID.
    /// </summary>
    private void TryPlayLinkedDialogueTree(string beatId)
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        if (BeatToDialogueMap.TryGetValue(beatId, out System.Func<DialogueTree> treeFactory))
        {
            DialogueTree tree = treeFactory();
            if (tree != null)
            {
                DialogueSystem.Instance.StartDialogueTree(tree);
            }
        }
    }

    /// <summary>
    /// Plays the pre-boss dialogue for the given island.
    /// Call from IslandFlowController or DialogueTrigger when approaching a boss.
    /// </summary>
    public static void PlayPreBossDialogue(string islandId)
    {
        if (DialogueSystem.Instance == null || DialogueSystem.Instance.IsDialogueActive)
        {
            return;
        }

        if (IslandToPreBossDialogue.TryGetValue(islandId, out System.Func<DialogueTree> treeFactory))
        {
            DialogueTree tree = treeFactory();
            if (tree != null)
            {
                DialogueSystem.Instance.StartDialogueTree(tree);
            }
        }
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
        return HasPlayerMovedAtLeast(minimumPlayerTravelBeforePreCombatBeat);
    }

    private bool HasPlayerMovedAtLeast(float minimumDistance)
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
        return delta.magnitude >= Mathf.Max(0.1f, minimumDistance);
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
        if (string.IsNullOrEmpty(islandId))
        {
            IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
            islandId = progressionManager != null
                ? progressionManager.ActiveIslandId
                : IslandThemeRegistry.GetActiveIslandId();
        }

        if (string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        float restorationPercent = IslandRestorationTracker.Instance.GetRestorationPercent(islandId);
        return restorationPercent >= postRestorationTriggerPercent;
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
