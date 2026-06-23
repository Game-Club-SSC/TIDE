using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the unique Fate boss encounter in Chapter Seven, Act 3 Finale.
/// Presents philosophical questions that determine the ending path, then
/// optionally transitions into the hardest combat encounter in the game.
///
/// Call <see cref="StartFateEncounter"/> from IslandFlowController when
/// the party reaches the final island. The dialogue plays first; if the
/// player chooses to fight, combat is initiated via BattleManager.
/// </summary>
[DisallowMultipleComponent]
public class FateEncounterDirector : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────────────────────────

    public static FateEncounterDirector Instance { get; private set; }

    // ──────────────────────────────────────────────────────────────────
    //  Events
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Fired after all fate questions are answered.
    /// acceptedFate is true when the player mostly chose acceptance answers.</summary>
    public event Action<bool> OnFateDialogueComplete;

    /// <summary>Fired after the combat phase resolves.
    /// playerWon is true when the player defeats Fate.</summary>
    public event Action<bool> OnFateCombatComplete;

    // ──────────────────────────────────────────────────────────────────
    //  Serialized Inspector Fields
    // ──────────────────────────────────────────────────────────────────

    [Header("UI References (auto-built if null)")]
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text questionText;
    [SerializeField] private Transform answerButtonContainer;
    [SerializeField] private GameObject answerButtonPrefab;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private Text narratorConcludingText;

    [Header("Typewriter")]
    [SerializeField] private float typewriterCharInterval = 0.03f;
    [SerializeField] private float postQuestionDelay = 0.4f;

    [Header("Combat")]
    [SerializeField] private GameObject fateBossPrefab;
    [SerializeField] private Transform fateBossSpawnPoint;
    [SerializeField] private float preCombatFadeDuration = 1.2f;
    [SerializeField] private float postCombatFadeDuration = 1.5f;

    [Header("Book Closing")]
    [SerializeField] private float bookCloseDuration = 2.5f;
    [SerializeField] private float narratorLineDelay = 3f;

    [Header("Fate Boss Stats")]
    [SerializeField] private int fateMaxHp = 9999;
    [SerializeField] private int fateAttack = 85;
    [SerializeField] private int fateDefense = 40;
    [SerializeField] private int fateSpeed = 30;
    [SerializeField] private Element fateBaseElement = Element.None;

    [Header("Narrator Conclusion Lines")]
    [SerializeField] private string[] narratorConcludingLines = new string[]
    {
        "And so the tale draws to its close, dear children.",
        "Whether fate was embraced or defied, the ending remains the same.",
        "The book closes, but the story lives on in those who listen."
    };

    [Header("Custom Questions (optional override)")]
    [SerializeField] private FateQuestion[] fateQuestions;

    // ──────────────────────────────────────────────────────────────────
    //  Fate Questions Data
    // ──────────────────────────────────────────────────────────────────

    [Serializable]
    public class FateQuestion
    {
        [TextArea(2, 4)]
        public string questionText;

        public FateAnswer[] answers;
    }

    [Serializable]
    public class FateAnswer
    {
        [TextArea(1, 3)]
        public string answerText;

        /// <summary>Positive = defiance, negative = acceptance.
        /// The magnitude indicates strength of the conviction.</summary>
        public int defianceWeight;

        /// <summary>Optional narrator reply shown after selection.</summary>
        [TextArea(1, 2)]
        public string narratorReply;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Internal State
    // ──────────────────────────────────────────────────────────────────

    private enum EncounterPhase
    {
        Idle,
        Dialogue,
        DialogueComplete,
        CombatFadeIn,
        Combat,
        PostCombatFadeOut,
        NarratorConclusion,
        Complete
    }

    private EncounterPhase currentPhase = EncounterPhase.Idle;
    private bool dialogueAcceptedFate;
    private int totalDefianceScore;
    private int questionCount;
    private bool skipTypewriter;
    private bool waitingForAnswer;
    private Coroutine activeRoutine;
    private GameObject spawnedFateBoss;

    // ──────────────────────────────────────────────────────────────────
    //  Default Questions (used when none are assigned via Inspector)
    // ──────────────────────────────────────────────────────────────────

    private static FateQuestion[] BuildDefaultQuestions()
    {
        return new FateQuestion[]
        {
            new FateQuestion
            {
                questionText = "Have you come to peace with your fate?",
                answers = new FateAnswer[]
                {
                    new FateAnswer
                    {
                        answerText = "Yes. I accept what comes.",
                        defianceWeight = -2,
                        narratorReply = "Acceptance is its own kind of strength... or surrender."
                    },
                    new FateAnswer
                    {
                        answerText = "No. I refuse to yield.",
                        defianceWeight = 2,
                        narratorReply = "Defiance burns bright, but even fire must eventually rest."
                    },
                    new FateAnswer
                    {
                        answerText = "I don't know yet.",
                        defianceWeight = 0,
                        narratorReply = "Uncertainty is honest. But honesty alone does not chart a course."
                    }
                }
            },
            new FateQuestion
            {
                questionText = "Would you sacrifice your friends to save the world?",
                answers = new FateAnswer[]
                {
                    new FateAnswer
                    {
                        answerText = "If there is no other way, yes.",
                        defianceWeight = -2,
                        narratorReply = "A terrible calculus. The world endures; the heart does not."
                    },
                    new FateAnswer
                    {
                        answerText = "Never. We find another way or fall together.",
                        defianceWeight = 2,
                        narratorReply = "Loyalty above all. Admirable... and perhaps foolish."
                    },
                    new FateAnswer
                    {
                        answerText = "I would ask them to choose for themselves.",
                        defianceWeight = 0,
                        narratorReply = "Freedom of choice. A rare gift in a world of predetermined ends."
                    }
                }
            },
            new FateQuestion
            {
                questionText = "Is a purpose chosen for you still your own?",
                answers = new FateAnswer[]
                {
                    new FateAnswer
                    {
                        answerText = "Yes. Purpose is purpose, regardless of its origin.",
                        defianceWeight = -2,
                        narratorReply = "To find meaning in the assigned path is wisdom, or perhaps resignation."
                    },
                    new FateAnswer
                    {
                        answerText = "No. A chain with a gilded label is still a chain.",
                        defianceWeight = 2,
                        narratorReply = "To name the cage is the first step toward leaving it."
                    },
                    new FateAnswer
                    {
                        answerText = "It becomes my own the moment I choose to walk it.",
                        defianceWeight = 0,
                        narratorReply = "Agency through action. A middle path between fate and rebellion."
                    }
                }
            },
            new FateQuestion
            {
                questionText = "Does knowing the end make the journey meaningless?",
                answers = new FateAnswer[]
                {
                    new FateAnswer
                    {
                        answerText = "Yes. The destination overshadows the path.",
                        defianceWeight = -2,
                        narratorReply = "An ending written before the first step. How heavy that must feel."
                    },
                    new FateAnswer
                    {
                        answerText = "No. The journey is the meaning, always.",
                        defianceWeight = 2,
                        narratorReply = "To find meaning in the walk, not the destination. A rebel's creed."
                    },
                    new FateAnswer
                    {
                        answerText = "The end gives the journey its weight.",
                        defianceWeight = 0,
                        narratorReply = "Both true and tragic. The knowledge of ending shapes every step."
                    }
                }
            },
            new FateQuestion
            {
                questionText = "Can acceptance coexist with grief?",
                answers = new FateAnswer[]
                {
                    new FateAnswer
                    {
                        answerText = "Yes. Grief is how acceptance earns its depth.",
                        defianceWeight = -2,
                        narratorReply = "To weep and still move forward... that is the hardest courage."
                    },
                    new FateAnswer
                    {
                        answerText = "No. One must eventually silence the other.",
                        defianceWeight = 2,
                        narratorReply = "To choose one over the other. Perhaps necessary. Perhaps a loss."
                    },
                    new FateAnswer
                    {
                        answerText = "They are the same thing, seen from different angles.",
                        defianceWeight = 0,
                        narratorReply = "A philosopher's answer. The boundary between surrender and resolve blurs."
                    }
                }
            }
        };
    }

    // ──────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public Entry Point
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from IslandFlowController when the party reaches the final island.
    /// Starts the Fate dialogue phase. Combat only begins if the player defies fate.
    /// </summary>
    public void StartFateEncounter()
    {
        if (currentPhase != EncounterPhase.Idle)
        {
            Debug.LogWarning("[FateEncounterDirector] Encounter already in progress.");
            return;
        }

        EnsureGameStateReady();

        totalDefianceScore = 0;
        dialogueAcceptedFate = false;

        activeRoutine = StartCoroutine(RunFateDialogue());
    }

    // ──────────────────────────────────────────────────────────────────
    //  Dialogue Phase
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RunFateDialogue()
    {
        currentPhase = EncounterPhase.Dialogue;

        FateQuestion[] questions = LoadQuestions();
        questionCount = questions.Length;

        EnsureDialogueUI();
        ShowDialogueCanvas(true);
        SetFadeAlpha(0f);

        for (int i = 0; i < questions.Length; i++)
        {
            FateQuestion question = questions[i];

            // Typewrite the question
            yield return StartCoroutine(TypewriteText(questionText, question.questionText));

            yield return new WaitForSeconds(postQuestionDelay);

            // Present answer buttons and wait for selection
            int selectedIndex = -1;
            waitingForAnswer = true;
            yield return StartCoroutine(WaitForAnswer(question.answers, (index) => selectedIndex = index));
            waitingForAnswer = false;

            if (selectedIndex < 0 || selectedIndex >= question.answers.Length)
            {
                selectedIndex = 0;
            }

            FateAnswer chosen = question.answers[selectedIndex];
            totalDefianceScore += chosen.defianceWeight;

            // Show narrator reply if any
            if (!string.IsNullOrEmpty(chosen.narratorReply))
            {
                yield return StartCoroutine(TypewriteText(questionText, chosen.narratorReply));
                yield return new WaitForSeconds(1.2f);
            }

            ClearAnswerButtons();
        }

        dialogueAcceptedFate = totalDefianceScore < 0;

        // Transition dialogue to completion
        currentPhase = EncounterPhase.DialogueComplete;

        Debug.Log($"[FateEncounterDirector] Dialogue complete. Defiance score: {totalDefianceScore}. Accepted fate: {dialogueAcceptedFate}.");

        OnFateDialogueComplete?.Invoke(dialogueAcceptedFate);

        if (dialogueAcceptedFate)
        {
            yield return StartCoroutine(RunAcceptedFatePath());
        }
        else
        {
            yield return StartCoroutine(RunDefiancePath());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Accepted Fate Path (Bad Ending)
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RunAcceptedFatePath()
    {
        // Fade to black, then trigger the bad ending through GameStateManager.
        // No combat occurs, so we set the ending branch directly and play
        // the ending sequence without going through the combat pipeline.
        yield return StartCoroutine(FadeToBlack(preCombatFadeDuration));

        HideDialogueCanvas();

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.ForceEndingBranchForDebug(GameStateManager.EndingBranch.Bad);
        }

        PlayEndingDirectly(GameStateManager.EndingBranch.Bad);

        yield return StartCoroutine(RunNarratorConclusion());

        currentPhase = EncounterPhase.Complete;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Defiance Path (Combat -> Good or Bittersweet Ending)
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RunDefiancePath()
    {
        // Narrator bridge line
        if (questionText != null)
        {
            questionText.text = string.Empty;
            yield return StartCoroutine(TypewriteText(questionText,
                "Then you choose to fight. Fate watches, and waits."));
            yield return new WaitForSeconds(1.5f);
        }

        // Fade to black before combat
        yield return StartCoroutine(FadeToBlack(preCombatFadeDuration));

        HideDialogueCanvas();

        // Spawn the Fate boss and configure combat
        SpawnFateBoss();
        ConfigureFateCombat();

        currentPhase = EncounterPhase.Combat;

        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm != null)
        {
            bm.ConfigureEnvyContext(true, true);
            bm.StartBattle();
        }
        else
        {
            Debug.LogError("[FateEncounterDirector] BattleManager not found. Cannot start combat.");
        }
    }

    /// <summary>
    /// Called by BattleManager (or GameStateManager.OnCombatEnded) when the
    /// Fate fight concludes. Hooks into the existing combat-end pipeline.
    /// The caller should pass true for a player victory.
    /// </summary>
    public void OnFateCombatResolved(bool playerWon)
    {
        if (currentPhase != EncounterPhase.Combat)
        {
            return;
        }

        currentPhase = EncounterPhase.PostCombatFadeOut;

        OnFateCombatComplete?.Invoke(playerWon);

        Debug.Log($"[FateEncounterDirector] Combat resolved. Player won: {playerWon}.");

        if (playerWon)
        {
            StartCoroutine(RunVictoryEnding());
        }
        else
        {
            StartCoroutine(RunDefeatEnding());
        }
    }

    private IEnumerator RunVictoryEnding()
    {
        // Good ending: the party defeats fate and fades together.
        // Combat already went through BattleManager -> GameStateManager.OnCombatEnded,
        // which calls ResolveFinalEndingAfterBossVictory and triggers the ending.
        // We only need to wait for the fade and run the narrator conclusion.
        yield return StartCoroutine(FadeToBlack(postCombatFadeDuration));

        CleanupFateBoss();

        yield return StartCoroutine(RunNarratorConclusion());

        currentPhase = EncounterPhase.Complete;
    }

    private IEnumerator RunDefeatEnding()
    {
        // Defeat in combat also leads to bad ending.
        // Same as victory -- the normal combat pipeline handles the ending.
        yield return StartCoroutine(FadeToBlack(postCombatFadeDuration));

        CleanupFateBoss();

        yield return StartCoroutine(RunNarratorConclusion());

        currentPhase = EncounterPhase.Complete;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Ending Playback Helper
    // ──────────────────────────────────────────────────────────────────

    private static void PlayEndingDirectly(GameStateManager.EndingBranch branch)
    {
        EndingSequenceDirector director = EndingSequenceDirector.Instance;
        if (director != null)
        {
            director.PlayEnding(branch);
        }
        else
        {
            // EndingSequenceDirector not yet in scene -- create it
            GameObject go = new GameObject("EndingSequenceDirector");
            director = go.AddComponent<EndingSequenceDirector>();
            director.PlayEnding(branch);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Narrator Conclusion (book closing)
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator RunNarratorConclusion()
    {
        currentPhase = EncounterPhase.NarratorConclusion;

        // Build narrator UI if needed
        EnsureNarratorConcludingUI();

        if (narratorConcludingText == null)
        {
            yield break;
        }

        narratorConcludingText.gameObject.SetActive(true);

        string[] lines = narratorConcludingLines != null && narratorConcludingLines.Length > 0
            ? narratorConcludingLines
            : new string[]
            {
                "And so the tale draws to its close, dear children.",
                "The book closes, but the story lives on in those who listen."
            };

        for (int i = 0; i < lines.Length; i++)
        {
            yield return StartCoroutine(TypewriteText(narratorConcludingText, lines[i]));
            yield return new WaitForSeconds(narratorLineDelay);
        }

        // Final fade to full black -- the book closing
        yield return StartCoroutine(FadeToBlack(bookCloseDuration));

        if (narratorConcludingText != null)
        {
            narratorConcludingText.gameObject.SetActive(false);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Answer Button Handling
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator WaitForAnswer(FateAnswer[] answers, Action<int> onSelected)
    {
        ClearAnswerButtons();
        int selectedIndex = -1;

        for (int i = 0; i < answers.Length; i++)
        {
            int capturedIndex = i;
            GameObject buttonObj = CreateAnswerButton(answers[i].answerText);

            if (buttonObj != null)
            {
                Button btn = buttonObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() =>
                    {
                        selectedIndex = capturedIndex;
                    });
                }
            }
        }

        while (selectedIndex < 0)
        {
            yield return null;
        }

        // Small delay so the player sees their button press register
        yield return new WaitForSeconds(0.15f);

        onSelected?.Invoke(selectedIndex);
    }

    private void ClearAnswerButtons()
    {
        if (answerButtonContainer == null)
        {
            return;
        }

        for (int i = answerButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(answerButtonContainer.GetChild(i).gameObject);
        }
    }

    private GameObject CreateAnswerButton(string label)
    {
        GameObject buttonObj;

        if (answerButtonPrefab != null)
        {
            buttonObj = Instantiate(answerButtonPrefab, answerButtonContainer);
        }
        else
        {
            buttonObj = new GameObject("FateAnswer_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(answerButtonContainer, false);

            RectTransform rt = buttonObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600f, 60f);

            Image img = buttonObj.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            Button btn = buttonObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.3f, 0.3f, 0.4f);
            cb.pressedColor = new Color(0.5f, 0.4f, 0.2f);
            btn.colors = cb;
        }

        // Ensure label text exists
        Text btnText = buttonObj.GetComponentInChildren<Text>();
        if (btnText == null)
        {
            GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 4f);
            textRt.offsetMax = new Vector2(-16f, -4f);

            btnText = textObj.GetComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (btnText.font == null)
            {
                btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            btnText.fontSize = 20;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = new Color(0.9f, 0.9f, 0.85f);
        }

        btnText.text = label;

        return buttonObj;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Typewriter Effect
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator TypewriteText(Text target, string fullText)
    {
        if (target == null || string.IsNullOrEmpty(fullText))
        {
            yield break;
        }

        target.text = string.Empty;
        skipTypewriter = false;

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipTypewriter)
            {
                target.text = fullText;
                yield break;
            }

            target.text += fullText[i];
            yield return new WaitForSeconds(typewriterCharInterval);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Fade Helpers
    // ──────────────────────────────────────────────────────────────────

    private IEnumerator FadeToBlack(float duration)
    {
        EnsureFadeOverlay();

        if (fadeOverlay == null)
        {
            yield break;
        }

        fadeOverlay.gameObject.SetActive(true);
        Color c = fadeOverlay.color;
        float startAlpha = c.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeOverlay.color = c;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        EnsureFadeOverlay();

        if (fadeOverlay == null)
        {
            yield break;
        }

        fadeOverlay.gameObject.SetActive(true);
        Color c = fadeOverlay.color;
        float startAlpha = c.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            fadeOverlay.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeOverlay.color = c;
        fadeOverlay.gameObject.SetActive(false);
    }

    private void SetFadeAlpha(float alpha)
    {
        EnsureFadeOverlay();

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = alpha;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(alpha > 0.001f);
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Fate Boss Setup
    // ──────────────────────────────────────────────────────────────────

    private void SpawnFateBoss()
    {
        if (spawnedFateBoss != null)
        {
            return;
        }

        if (fateBossPrefab != null)
        {
            Vector3 spawnPos = fateBossSpawnPoint != null ? fateBossSpawnPoint.position : Vector3.zero;
            spawnedFateBoss = Instantiate(fateBossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Build a minimal CombatUnit at runtime
            spawnedFateBoss = new GameObject("Fate_The_Inevitable", typeof(CombatUnit));
            if (fateBossSpawnPoint != null)
            {
                spawnedFateBoss.transform.position = fateBossSpawnPoint.position;
            }
        }

        spawnedFateBoss.name = "Fate_The_Inevitable";
    }

    private void ConfigureFateCombat()
    {
        // Configure the BattleManager for a fate encounter
        BattleManager bm = FindObjectOfType<BattleManager>();
        if (bm != null)
        {
            bm.ConfigureEnvyContext(true, true);
        }

        // Register fate boss if it has a CombatUnit
        CombatUnit fateUnit = spawnedFateBoss.GetComponent<CombatUnit>();
        if (fateUnit != null && bm != null)
        {
            bm.RegisterUnit(fateUnit);
        }
    }

    private void CleanupFateBoss()
    {
        if (spawnedFateBoss != null)
        {
            Destroy(spawnedFateBoss);
            spawnedFateBoss = null;
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  UI Construction (procedural fallback)
    // ──────────────────────────────────────────────────────────────────

    private void EnsureDialogueUI()
    {
        if (dialogueCanvas != null && questionText != null && answerButtonContainer != null)
        {
            return;
        }

        // Create a dedicated canvas
        if (dialogueCanvas == null)
        {
            GameObject canvasGo = new GameObject("FateDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            dialogueCanvas = canvasGo.GetComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // Dialogue panel
        if (dialoguePanel == null)
        {
            dialoguePanel = CreatePanel(dialogueCanvas.transform, "FateDialoguePanel",
                new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f),
                new Color(0.05f, 0.05f, 0.1f, 0.9f));
        }

        // Question text
        if (questionText == null)
        {
            GameObject textGo = CreateTextChild(dialoguePanel.transform, "FateQuestionText",
                new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f), 28,
                TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f));
            questionText = textGo.GetComponent<Text>();
        }

        // Answer button container
        if (answerButtonContainer == null)
        {
            GameObject containerGo = new GameObject("AnswerButtonContainer", typeof(RectTransform));
            containerGo.transform.SetParent(dialoguePanel.transform, false);

            RectTransform rt = containerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.05f);
            rt.anchorMax = new Vector2(0.9f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = containerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 8, 8);

            answerButtonContainer = containerGo.transform;
        }

        ShowDialogueCanvas(false);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeOverlay != null)
        {
            return;
        }

        // Try to find an existing one
        fadeOverlay = FindObjectOfType<FateFadeOverlayTag>()?.GetComponent<Image>();

        if (fadeOverlay != null)
        {
            return;
        }

        // Build one
        GameObject overlayGo = new GameObject("FateFadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayGo.AddComponent<FateFadeOverlayTag>();

        Canvas parentCanvas = dialogueCanvas != null ? dialogueCanvas : FindObjectOfType<Canvas>();
        if (parentCanvas == null)
        {
            GameObject canvasGo = new GameObject("FateOverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            parentCanvas = canvasGo.GetComponent<Canvas>();
            parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            parentCanvas.sortingOrder = 200;
        }

        overlayGo.transform.SetParent(parentCanvas.transform, false);

        RectTransform rt = overlayGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        fadeOverlay = overlayGo.GetComponent<Image>();
        fadeOverlay.color = Color.black;
        fadeOverlay.raycastTarget = false;
        fadeOverlay.gameObject.SetActive(false);
    }

    private void EnsureNarratorConcludingUI()
    {
        if (narratorConcludingText != null)
        {
            return;
        }

        Canvas canvas = dialogueCanvas;
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("NarratorCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
        }

        // Dark backdrop
        GameObject backdrop = CreatePanel(canvas.transform, "NarratorBackdrop",
            Vector2.zero, Vector2.one, new Color(0.02f, 0.02f, 0.04f, 1f));
        Image backdropImg = backdrop.GetComponent<Image>();
        if (backdropImg != null)
        {
            backdropImg.raycastTarget = false;
        }

        // Text
        GameObject textGo = CreateTextChild(backdrop.transform, "NarratorConcludingText",
            new Vector2(0.1f, 0.3f), new Vector2(0.9f, 0.7f), 30,
            TextAnchor.MiddleCenter, new Color(0.85f, 0.8f, 0.7f));
        narratorConcludingText = textGo.GetComponent<Text>();
        narratorConcludingText.gameObject.SetActive(false);
    }

    // ──────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ──────────────────────────────────────────────────────────────────

    private void ShowDialogueCanvas(bool visible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(visible);
        }
    }

    private void HideDialogueCanvas()
    {
        ShowDialogueCanvas(false);
    }

    private static GameObject CreatePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
    {
        GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(parent, false);

        RectTransform rt = panelGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panelGo.GetComponent<Image>();
        img.color = bgColor;

        return panelGo;
    }

    private static GameObject CreateTextChild(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize,
        TextAnchor alignment, Color color)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(parent, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;

        return textGo;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Data Loading
    // ──────────────────────────────────────────────────────────────────

    private FateQuestion[] LoadQuestions()
    {
        // If questions are assigned via inspector, use them
        if (fateQuestions != null && fateQuestions.Length > 0)
        {
            return fateQuestions;
        }

        return BuildDefaultQuestions();
    }

    // ──────────────────────────────────────────────────────────────────
    //  GameStateManager Integration
    // ──────────────────────────────────────────────────────────────────

    private void EnsureGameStateReady()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            Debug.LogWarning("[FateEncounterDirector] GameStateManager not found. Encounter may not integrate with save system.");
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public Query API
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Whether the player chose to accept fate (bad ending path).</summary>
    public bool DidAcceptFate => dialogueAcceptedFate;

    /// <summary>Total defiance score across all questions.</summary>
    public int TotalDefianceScore => totalDefianceScore;

    /// <summary>Whether the encounter is currently running.</summary>
    public bool IsActive => currentPhase != EncounterPhase.Idle && currentPhase != EncounterPhase.Complete;

    /// <summary>
    /// Determines the ending branch based on the dialogue answers.
    /// Call after dialogue completes but before the ending plays.
    /// </summary>
    public GameStateManager.EndingBranch ResolveEndingFromAnswers()
    {
        if (dialogueAcceptedFate)
        {
            return GameStateManager.EndingBranch.Bad;
        }

        // Mixed answers (score near zero) produce a bittersweet variant
        // that still routes through Good but with different narration
        // handled by the score magnitude in the ending sequence.
        return GameStateManager.EndingBranch.Good;
    }

    // ──────────────────────────────────────────────────────────────────
    //  Marker Tag for Fade Overlay
    // ──────────────────────────────────────────────────────────────────

    private class FateFadeOverlayTag : MonoBehaviour { }
}
