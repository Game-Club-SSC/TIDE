using System.Collections.Generic;
using UnityEngine;

public class TideManager : MonoBehaviour
{
    [Header("Interaction")]
    public LayerMask tileLayer = ~0;
    [SerializeField] private int maxCarrySteps = 2;

    [Header("Board Generation")]
    [SerializeField] private Vector3 boardCenter = new Vector3(20.56f, 19.86f, 4.35f);
    [SerializeField] private float tileSpacing = 2.25f;
    [SerializeField] private Vector3 tileScale = new Vector3(1.9f, 0.35f, 1.9f);
    [SerializeField] private bool anchorToExistingSceneTile = true;
    [SerializeField] private bool replaceExistingSceneTiles = true;

    private readonly TideTile[,] activeTiles = new TideTile[3, 3];

    private TutorialLesson[] lessons;
    private Transform runtimeBoardRoot;
    private TideTile hoveredTile;
    private TideTile selectedSource;
    private TideTile carryingSource;
    private int selectedTakeAmount;
    private int carriedAmount;
    private int currentLessonIndex;
    private bool lowSourcePreviewSeen;
    private bool hasPlacedFirstMove;
    private bool lessonComplete;
    private bool tutorialComplete;
    private string statusMessage = string.Empty;
    private float statusMessageExpiresAt;

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle hintStyle;
    private GUIStyle messageStyle;

    private void Start()
    {
        BuildLessons();
        PrepareBoardAnchor();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterPuzzle();
        }

        LoadLesson(0);
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.currentState == GameStateManager.GameState.Puzzle)
        {
            GameStateManager.Instance.ExitPuzzle();
        }
    }

    private void Update()
    {
        hoveredTile = GetHoveredTile();

        HandleGlobalInput();
        if (tutorialComplete || lessonComplete)
        {
            UpdateTileVisuals();
            return;
        }

        TrackLowSourcePreview();

        if (carriedAmount > 0)
        {
            HandleDestinationSelection();
        }
        else
        {
            HandleSourceSelection();
        }

        UpdateTileVisuals();
    }

    private void OnGUI()
    {
        EnsureGuiStyles();

        Rect panelRect = new Rect(16f, 16f, 430f, 280f);
        GUI.Box(panelRect, GUIContent.none);

        float y = panelRect.y + 12f;
        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 28f),
            $"Tide Tutorial {currentLessonIndex + 1}/{lessons.Length}", titleStyle);
        y += 28f;

        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 24f),
            CurrentLesson.Title, bodyStyle);
        y += 28f;

        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 110f),
            GetInstructionText(), bodyStyle);
        y += 116f;

        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 42f),
            GetCarryText(), hintStyle);
        y += 44f;

        GUI.Label(new Rect(panelRect.x + 12f, y, panelRect.width - 24f, 60f),
            "Controls: left click to select, mouse wheel or Q/E to adjust amount, Enter or click the source again to take, R to restart the lesson.",
            hintStyle);

        if (!string.IsNullOrEmpty(statusMessage) && Time.time <= statusMessageExpiresAt)
        {
            GUI.Label(new Rect(panelRect.x + 12f, panelRect.yMax - 34f, panelRect.width - 24f, 24f),
                statusMessage, messageStyle);
        }
    }

    private TutorialLesson CurrentLesson => lessons[currentLessonIndex];

    private void BuildLessons()
    {
        lessons = new[]
        {
            CreateLesson(
                "Lesson 1: Restore the obvious imbalance",
                "Balanced Tide rests at 5. Start by drawing excess from the highlighted bright tile and placing it onto the highlighted low tile.",
                new[,]
                {
                    { 7, 5, 3 },
                    { 5, 5, 5 },
                    { 5, 5, 5 }
                },
                new Vector2Int(0, 0),
                new Vector2Int(2, 0),
                false),
            CreateLesson(
                "Lesson 2: Low tiles can also release Tide",
                "New rule: tiles below 5 can also be sources. Hover a low tile to preview how much Tide it can release down to 1, then solve the corrupted island layout.",
                new[,]
                {
                    { 9, 1, 10 },
                    { 7, 5, 2 },
                    { 5, 3, 3 }
                },
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                true,
                new Vector2Int(1, 1))
        };
    }

    private TutorialLesson CreateLesson(
        string title,
        string intro,
        int[,] values,
        Vector2Int recommendedSource,
        Vector2Int recommendedDestination,
        bool requiresLowSourcePreview,
        params Vector2Int[] sealedTiles)
    {
        bool[,] sealedMap = new bool[3, 3];
        for (int i = 0; i < sealedTiles.Length; i++)
        {
            Vector2Int sealedCoordinate = sealedTiles[i];
            sealedMap[sealedCoordinate.y, sealedCoordinate.x] = true;
        }

        return new TutorialLesson
        {
            Title = title,
            Intro = intro,
            Values = values,
            Sealed = sealedMap,
            RecommendedSource = recommendedSource,
            RecommendedDestination = recommendedDestination,
            RequiresLowSourcePreview = requiresLowSourcePreview
        };
    }

    private void PrepareBoardAnchor()
    {
        TideTile[] sceneTiles = FindObjectsByType<TideTile>(FindObjectsSortMode.None);
        if (sceneTiles.Length > 0 && anchorToExistingSceneTile)
        {
            boardCenter = sceneTiles[0].transform.position;
        }

        if (!replaceExistingSceneTiles)
        {
            return;
        }

        for (int i = 0; i < sceneTiles.Length; i++)
        {
            Destroy(sceneTiles[i].gameObject);
        }
    }

    private void LoadLesson(int lessonIndex)
    {
        currentLessonIndex = Mathf.Clamp(lessonIndex, 0, lessons.Length - 1);
        selectedSource = null;
        carryingSource = null;
        selectedTakeAmount = 0;
        carriedAmount = 0;
        lowSourcePreviewSeen = false;
        hasPlacedFirstMove = false;
        lessonComplete = false;
        tutorialComplete = false;
        statusMessage = string.Empty;
        statusMessageExpiresAt = 0f;

        if (runtimeBoardRoot != null)
        {
            Destroy(runtimeBoardRoot.gameObject);
        }

        runtimeBoardRoot = new GameObject($"RuntimeTideBoard_{currentLessonIndex + 1}").transform;
        runtimeBoardRoot.SetParent(transform, false);

        GenerateBoard(CurrentLesson);
        UpdateTileVisuals();
    }

    private void GenerateBoard(TutorialLesson lesson)
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObject.name = $"TideTile_{row}_{col}";
                tileObject.transform.SetParent(runtimeBoardRoot, false);
                tileObject.transform.position = GetWorldPosition(row, col);
                tileObject.transform.localScale = tileScale;
                tileObject.layer = gameObject.layer;

                TideTile tile = tileObject.AddComponent<TideTile>();
                tile.Configure(new Vector2Int(col, row), lesson.Values[row, col], lesson.Sealed[row, col]);
                activeTiles[row, col] = tile;
            }
        }
    }

    private Vector3 GetWorldPosition(int row, int col)
    {
        float xOffset = (col - 1) * tileSpacing;
        float zOffset = (1 - row) * tileSpacing;
        return boardCenter + new Vector3(xOffset, 0f, zOffset);
    }

    private TideTile GetHoveredTile()
    {
        if (Camera.main == null)
        {
            return null;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayer))
        {
            return hit.collider.GetComponent<TideTile>();
        }

        return null;
    }

    private void HandleGlobalInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            LoadLesson(currentLessonIndex);
            return;
        }

        if (!lessonComplete)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
        {
            return;
        }

        if (currentLessonIndex < lessons.Length - 1)
        {
            LoadLesson(currentLessonIndex + 1);
            return;
        }

        tutorialComplete = true;
    }

    private void HandleSourceSelection()
    {
        if (selectedSource == null)
        {
            if (!Input.GetMouseButtonDown(0) || hoveredTile == null)
            {
                return;
            }

            if (hoveredTile.IsSealed)
            {
                SetStatus("Sealed tiles cannot be interacted with.");
                return;
            }

            if (hoveredTile.GetMaxTake() <= 0)
            {
                SetStatus("That tile cannot release any Tide.");
                return;
            }

            selectedSource = hoveredTile;
            selectedTakeAmount = GetSuggestedTakeAmount(selectedSource);
            SetStatus($"Selected {selectedSource.CurrentTideValue}. Adjust the amount, then confirm the take.");
            return;
        }

        int maxTake = selectedSource.GetMaxTake();
        if (maxTake <= 0)
        {
            selectedSource = null;
            selectedTakeAmount = 0;
            return;
        }

        float scrollDelta = Input.mouseScrollDelta.y;
        int amountDelta = 0;
        if (scrollDelta > 0.05f || Input.GetKeyDown(KeyCode.E))
        {
            amountDelta = 1;
        }
        else if (scrollDelta < -0.05f || Input.GetKeyDown(KeyCode.Q))
        {
            amountDelta = -1;
        }

        if (amountDelta != 0)
        {
            selectedTakeAmount = Mathf.Clamp(selectedTakeAmount + amountDelta, 1, maxTake);
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            selectedSource = null;
            selectedTakeAmount = 0;
            SetStatus("Source selection cleared.");
            return;
        }

        if (Input.GetMouseButtonDown(0) && hoveredTile != null && hoveredTile != selectedSource)
        {
            if (hoveredTile.IsSealed || hoveredTile.GetMaxTake() <= 0)
            {
                SetStatus("Pick a tile that can legally release Tide.");
                return;
            }

            selectedSource = hoveredTile;
            selectedTakeAmount = GetSuggestedTakeAmount(selectedSource);
            SetStatus($"Selected {selectedSource.CurrentTideValue}. Adjust the amount, then confirm the take.");
            return;
        }

        bool confirmedByClick = Input.GetMouseButtonDown(0) && hoveredTile == selectedSource;
        bool confirmedByKey = Input.GetKeyDown(KeyCode.Return);
        if (!confirmedByClick && !confirmedByKey)
        {
            return;
        }

        selectedTakeAmount = Mathf.Clamp(selectedTakeAmount, 1, maxTake);
        carryingSource = selectedSource;
        carriedAmount = selectedTakeAmount;
        carryingSource.ApplyTake(carriedAmount);
        selectedSource = null;
        SetStatus($"Carrying {carriedAmount} Tide. Choose a reachable normal tile to place it.");
    }

    private void HandleDestinationSelection()
    {
        if (!Input.GetMouseButtonDown(0) || hoveredTile == null)
        {
            return;
        }

        if (hoveredTile == carryingSource)
        {
            SetStatus("Choose a different destination tile.");
            return;
        }

        if (hoveredTile.IsSealed)
        {
            SetStatus("Sealed tiles block placement.");
            return;
        }

        if (!hoveredTile.CanReceive(carriedAmount))
        {
            SetStatus("That tile cannot hold the full bundle.");
            return;
        }

        if (!CanReachWithinCarrySteps(carryingSource, hoveredTile))
        {
            SetStatus("That destination is outside the two-step carry range.");
            return;
        }

        hoveredTile.ApplyPlace(carriedAmount);
        carriedAmount = 0;
        carryingSource = null;
        hasPlacedFirstMove = true;
        SetStatus("Move completed.");
        EvaluateLessonCompletion();
    }

    private void TrackLowSourcePreview()
    {
        if (!CurrentLesson.RequiresLowSourcePreview || lowSourcePreviewSeen || hoveredTile == null)
        {
            return;
        }

        if (hoveredTile.IsSealed || hoveredTile.CurrentTideValue >= 5 || hoveredTile.CurrentTideValue <= 1)
        {
            return;
        }

        lowSourcePreviewSeen = true;
        SetStatus($"{hoveredTile.CurrentTideValue} can release {hoveredTile.GetMaxTake()} Tide by dropping to 1.");
    }

    private void EvaluateLessonCompletion()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                TideTile tile = activeTiles[row, col];
                if (tile.IsSealed)
                {
                    continue;
                }

                if (tile.CurrentTideValue != 5)
                {
                    return;
                }
            }
        }

        lessonComplete = true;
        if (currentLessonIndex >= lessons.Length - 1)
        {
            tutorialComplete = true;
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ExitPuzzle();
            }

            SetStatus("Tutorial complete. Puzzle flow unlocked.");
            return;
        }

        SetStatus("Lesson complete. Press Space to continue.");
    }

    private int GetSuggestedTakeAmount(TideTile source)
    {
        int maxTake = source.GetMaxTake();
        if (maxTake <= 0)
        {
            return 0;
        }

        if (!hasPlacedFirstMove && source == GetTile(CurrentLesson.RecommendedSource))
        {
            TideTile destination = GetTile(CurrentLesson.RecommendedDestination);
            if (destination != null)
            {
                int amountNeeded = destination.CurrentTideValue < 5
                    ? 5 - destination.CurrentTideValue
                    : Mathf.Abs(destination.CurrentTideValue - 5);

                if (amountNeeded > 0)
                {
                    return Mathf.Clamp(amountNeeded, 1, maxTake);
                }
            }
        }

        return maxTake;
    }

    private bool CanReachWithinCarrySteps(TideTile startTile, TideTile endTile)
    {
        if (startTile == null || endTile == null || endTile.IsSealed)
        {
            return false;
        }

        Queue<PathNode> frontier = new Queue<PathNode>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Vector2Int start = startTile.GridPosition;
        Vector2Int goal = endTile.GridPosition;

        frontier.Enqueue(new PathNode(start, 0));
        visited.Add(start);

        while (frontier.Count > 0)
        {
            PathNode current = frontier.Dequeue();
            if (current.Position == goal && current.StepsTaken > 0)
            {
                return true;
            }

            if (current.StepsTaken >= maxCarrySteps)
            {
                continue;
            }

            for (int rowOffset = -1; rowOffset <= 1; rowOffset++)
            {
                for (int colOffset = -1; colOffset <= 1; colOffset++)
                {
                    if (rowOffset == 0 && colOffset == 0)
                    {
                        continue;
                    }

                    int nextRow = current.Position.y + rowOffset;
                    int nextCol = current.Position.x + colOffset;
                    if (nextRow < 0 || nextRow >= 3 || nextCol < 0 || nextCol >= 3)
                    {
                        continue;
                    }

                    TideTile candidate = activeTiles[nextRow, nextCol];
                    if (candidate == null || candidate.IsSealed)
                    {
                        continue;
                    }

                    if (Mathf.Abs(rowOffset) == 1 && Mathf.Abs(colOffset) == 1)
                    {
                        TideTile sideA = activeTiles[current.Position.y, nextCol];
                        TideTile sideB = activeTiles[nextRow, current.Position.x];
                        if ((sideA != null && sideA.IsSealed) || (sideB != null && sideB.IsSealed))
                        {
                            continue;
                        }
                    }

                    Vector2Int nextPosition = new Vector2Int(nextCol, nextRow);
                    if (visited.Contains(nextPosition))
                    {
                        continue;
                    }

                    visited.Add(nextPosition);
                    frontier.Enqueue(new PathNode(nextPosition, current.StepsTaken + 1));
                }
            }
        }

        return false;
    }

    private TideTile GetTile(Vector2Int coordinates)
    {
        if (coordinates.y < 0 || coordinates.y >= 3 || coordinates.x < 0 || coordinates.x >= 3)
        {
            return null;
        }

        return activeTiles[coordinates.y, coordinates.x];
    }

    private void UpdateTileVisuals()
    {
        TideTile recommendedSource = GetTile(CurrentLesson.RecommendedSource);
        TideTile recommendedDestination = GetTile(CurrentLesson.RecommendedDestination);

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                TideTile tile = activeTiles[row, col];
                bool isSelected = tile == selectedSource || tile == carryingSource;
                bool isHovered = tile == hoveredTile;
                bool isSuggested = false;
                bool isReachable = false;

                if (CurrentLesson.RequiresLowSourcePreview &&
                    !lowSourcePreviewSeen &&
                    !tile.IsSealed &&
                    tile.CurrentTideValue > 1 &&
                    tile.CurrentTideValue < 5)
                {
                    isSuggested = true;
                }

                if (!hasPlacedFirstMove)
                {
                    if (carriedAmount > 0 || selectedSource != null)
                    {
                        isSuggested |= tile == recommendedDestination;
                    }
                    else
                    {
                        isSuggested |= tile == recommendedSource;
                    }
                }

                if (selectedSource != null && tile != selectedSource && tile.CanReceive(selectedTakeAmount))
                {
                    isReachable = CanReachWithinCarrySteps(selectedSource, tile);
                }
                else if (carriedAmount > 0 && tile != carryingSource && tile.CanReceive(carriedAmount))
                {
                    isReachable = CanReachWithinCarrySteps(carryingSource, tile);
                }

                tile.SetVisualState(isSelected, isSuggested, isReachable, isHovered);
            }
        }
    }

    private string GetInstructionText()
    {
        if (tutorialComplete)
        {
            return "Both tutorial lessons are complete. Press R to replay the current lesson.";
        }

        if (lessonComplete)
        {
            if (currentLessonIndex < lessons.Length - 1)
            {
                return "Lesson complete. Press Space to load the next tutorial board.";
            }

            return "Tutorial complete. Press R if you want to replay the final lesson.";
        }

        if (CurrentLesson.RequiresLowSourcePreview && !lowSourcePreviewSeen)
        {
            return CurrentLesson.Intro;
        }

        if (selectedSource != null)
        {
            return $"Selected source value: {selectedSource.CurrentTideValue}. This tile can release up to {selectedSource.GetMaxTake()} Tide. Adjust the take amount, then confirm the take.";
        }

        if (carriedAmount > 0)
        {
            return $"You are carrying {carriedAmount} Tide. Place it on any reachable normal tile that can hold the full bundle. Sealed tiles block routes.";
        }

        if (!hasPlacedFirstMove)
        {
            return CurrentLesson.Intro;
        }

        return "Restore every normal tile to 5 to finish the lesson.";
    }

    private string GetCarryText()
    {
        if (selectedSource != null)
        {
            return $"Take amount: {selectedTakeAmount}/{selectedSource.GetMaxTake()} from tile {selectedSource.CurrentTideValue}.";
        }

        if (carriedAmount > 0 && carryingSource != null)
        {
            return $"Held bundle: {carriedAmount} Tide from source {carryingSource.GridPosition.x + 1},{carryingSource.GridPosition.y + 1}.";
        }

        return "Held bundle: none.";
    }

    private void SetStatus(string message)
    {
        statusMessage = message;
        statusMessageExpiresAt = Time.time + 4f;
    }

    private void EnsureGuiStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            wordWrap = true
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true,
            normal = { textColor = new Color(0.8f, 0.85f, 0.9f) }
        };

        messageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.96f, 0.7f) }
        };
    }

    private sealed class TutorialLesson
    {
        public string Title;
        public string Intro;
        public int[,] Values;
        public bool[,] Sealed;
        public Vector2Int RecommendedSource;
        public Vector2Int RecommendedDestination;
        public bool RequiresLowSourcePreview;
    }

    private readonly struct PathNode
    {
        public PathNode(Vector2Int position, int stepsTaken)
        {
            Position = position;
            StepsTaken = stepsTaken;
        }

        public Vector2Int Position { get; }
        public int StepsTaken { get; }
    }
}
