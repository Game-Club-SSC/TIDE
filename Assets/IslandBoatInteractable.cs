using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class IslandBoatInteractable : MonoBehaviour, IPlayerInteractionAssistTarget
{
    private const string DefaultPromptResourceName = "PuzzlePrompt";
    private const float DefaultPromptPixelsPerUnit = 360f;
    private const string TravelCanvasName = "BoatTravelCanvas";
    private const string TravelPanelName = "BoatTravelPanel";
    private const string TravelLabelName = "BoatTravelLabel";
    private static readonly Vector3 DefaultFallbackSpawnPosition = new Vector3(12f, 31.54f, 2.69f);

    [Serializable]
    private sealed class IslandDestination
    {
        public string islandId = IslandThemeRegistry.DefaultIslandId;
        public string displayName = string.Empty;
        public bool useCustomSpawnPosition;
        public Vector3 customSpawnPosition = DefaultFallbackSpawnPosition;
        public Vector3 boatOffset = new Vector3(6f, 0f, 0f);
    }

    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.72f, 0.72f, 1f);
    [SerializeField] private Sprite promptSprite;
    [SerializeField] private Color promptTint = Color.white;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.Return;
    [SerializeField] private KeyCode closePanelKey = KeyCode.Escape;
    [SerializeField] private Vector3 triggerSize = new Vector3(3.5f, 2.5f, 3.5f);
    [SerializeField] private Color boatColor = new Color(0.18f, 0.36f, 0.52f, 1f);

    [Header("Travel Destinations")]
    [SerializeField] private List<IslandDestination> destinations = new List<IslandDestination>();
    [SerializeField] private Vector3 defaultTravelOffset = new Vector3(6f, 0f, 0f);
    [SerializeField] private Vector3 fallbackSpawnPosition = DefaultFallbackSpawnPosition;

    [Header("Travel Panel")]
    [SerializeField] private Color panelColor = new Color(0.06f, 0.1f, 0.16f, 0.94f);
    [SerializeField] private Color textColor = new Color(0.95f, 0.97f, 1f, 1f);
    [SerializeField] private Color selectedTextColor = new Color(0.97f, 0.86f, 0.54f, 1f);
    [SerializeField] private int panelFontSize = 22;

    private readonly List<string> orderedDestinationIds = new List<string>();

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private Sprite runtimePromptSprite;
    private bool playerInRange;
    private bool panelOpen;
    private int selectedIndex;

    private Canvas travelCanvas;
    private RectTransform travelPanel;
    private Text travelLabel;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplyBoatColor();
        EnsureDestinationList();
        RefreshDestinationOrder();
        SetPromptVisible(false);

        Debug.Log($"[IslandBoatInteractable] Initialized on '{name}' at {transform.position}.");
    }

    private void Update()
    {
        UpdatePromptFacing();

        if (panelOpen)
        {
            if (!CanInteractWithBoat())
            {
                CloseTravelPanel();
                return;
            }

            HandleTravelPanelInput();
            RefreshTravelPanelText();
            return;
        }

        bool canInteract = CanInteractWithBoat();
        SetPromptVisible(canInteract);
        if (!canInteract)
        {
            return;
        }

        if (!IsInteractPressed())
        {
            return;
        }

        OpenTravelPanel();
    }

    public bool TryGetSpawnPositionForIsland(string islandId, out Vector3 spawnPosition)
    {
        spawnPosition = ResolveSafeSpawnPosition(fallbackSpawnPosition);

        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        IslandDestination destination = FindDestination(resolvedIslandId);
        if (destination == null)
        {
            return false;
        }

        if (destination.useCustomSpawnPosition && IsFiniteVector(destination.customSpawnPosition))
        {
            spawnPosition = ResolveSafeSpawnPosition(destination.customSpawnPosition);
            return true;
        }

        Vector3 offsetSpawn = transform.position + destination.boatOffset;
        if (!IsFiniteVector(offsetSpawn))
        {
            return false;
        }

        spawnPosition = ResolveSafeSpawnPosition(offsetSpawn);
        return true;
    }

    private bool CanInteractWithBoat()
    {
        GameStateManager manager = GameStateManager.Instance;
        return playerInRange
            && manager != null
            && manager.currentState == GameStateManager.GameState.Exploration
            && !manager.IsTransitioning;
    }

    private bool IsInteractPressed()
    {
        if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return true;
        }

        if (interactKey != KeyCode.Return && Input.GetKeyDown(KeyCode.Return))
        {
            return true;
        }

        return false;
    }

    private void OpenTravelPanel()
    {
        EnsureDestinationList();
        RefreshDestinationOrder();
        if (orderedDestinationIds.Count == 0)
        {
            Debug.LogWarning("[IslandBoatInteractable] No valid island destinations are configured.");
            return;
        }

        EnsureTravelCanvas();

        selectedIndex = ResolveInitialSelectionIndex();
        panelOpen = true;
        if (travelCanvas != null)
        {
            travelCanvas.gameObject.SetActive(true);
        }

        SetPromptVisible(false);
        RefreshTravelPanelText();
    }

    private void CloseTravelPanel()
    {
        panelOpen = false;
        if (travelCanvas != null)
        {
            travelCanvas.gameObject.SetActive(false);
        }

        SetPromptVisible(CanInteractWithBoat());
    }

    private void HandleTravelPanelInput()
    {
        if (Input.GetKeyDown(closePanelKey))
        {
            CloseTravelPanel();
            return;
        }

        if (orderedDestinationIds.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            selectedIndex = (selectedIndex - 1 + orderedDestinationIds.Count) % orderedDestinationIds.Count;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            selectedIndex = (selectedIndex + 1) % orderedDestinationIds.Count;
        }

        int numericSelection = ReadNumericSelection();
        if (numericSelection >= 0)
        {
            selectedIndex = numericSelection;
        }

        if (IsInteractPressed())
        {
            TryTravelToSelectedDestination();
        }
    }

    private int ReadNumericSelection()
    {
        int maxEntries = Mathf.Min(orderedDestinationIds.Count, 9);
        for (int i = 0; i < maxEntries; i++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha1 + i);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                return i;
            }
        }

        return -1;
    }

    private void TryTravelToSelectedDestination()
    {
        if (selectedIndex < 0 || selectedIndex >= orderedDestinationIds.Count)
        {
            return;
        }

        string destinationIslandId = orderedDestinationIds[selectedIndex];
        if (string.IsNullOrEmpty(destinationIslandId))
        {
            return;
        }

        if (IsMainSceneActive()
            && string.Equals(destinationIslandId, IslandThemeRegistry.GetActiveIslandId(), StringComparison.Ordinal))
        {
            CloseTravelPanel();
            return;
        }

        GameStateManager gameStateManager = GameStateManager.Instance;
        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        if (gameStateManager == null || progressionManager == null)
        {
            Debug.LogWarning("[IslandBoatInteractable] Missing manager references. Cannot complete travel.");
            return;
        }

        // Validate via TravelValidationService (unlocked + dock exists)
        string fromIslandId = progressionManager.ActiveIslandId;
        TravelValidationService.ValidationResult validation = TravelValidationService.ValidateTravel(fromIslandId, destinationIslandId);
        if (!validation.CanTravel)
        {
            Debug.LogWarning($"[IslandBoatInteractable] Travel validation failed: {validation.FailureReason}");
            return;
        }

        // Backtracking check (not covered by TravelValidationService)
        IslandBacktrackingManager backtrackingManager = IslandBacktrackingManager.Instance;
        if (backtrackingManager != null && !backtrackingManager.CanVisitIsland(destinationIslandId))
        {
            Debug.LogWarning($"[IslandBoatInteractable] Island '{destinationIslandId}' is not yet accessible. Complete more islands to unlock backtracking.");
            return;
        }

        // Resolve spawn position: prefer TeleportAnchor dock, fall back to configured offsets
        Vector3 destinationSpawn = ResolveDestinationSpawn(destinationIslandId, progressionManager, validation.Destination);

        // Delegate to GameStateManager's fade pipeline (fade → teleport → snap camera → fade back)
        if (!gameStateManager.TravelToIsland(destinationIslandId, destinationSpawn))
        {
            Debug.LogWarning($"[IslandBoatInteractable] GameStateManager rejected travel to '{destinationIslandId}'.");
            return;
        }

        Debug.Log($"[IslandBoatInteractable] Traveled from '{fromIslandId}' to '{destinationIslandId}' at {destinationSpawn}.");
        PlayTravelFanfare();
        CloseTravelPanel();
    }

    private static bool IsMainSceneActive()
    {
        return SceneManager.GetActiveScene().name == GameStateManager.MainSceneName;
    }

    private Vector3 ResolveDestinationSpawn(
        string destinationIslandId,
        IslandProgressionManager progressionManager,
        TeleportAnchor dockAnchor)
    {
        // 1. Prefer TeleportAnchor boat dock if available
        if (dockAnchor != null && IsFiniteVector(dockAnchor.SpawnPosition))
        {
            return ResolveSafeSpawnPosition(dockAnchor.SpawnPosition);
        }

        // 2. Return to previously recorded position on this island
        if (progressionManager != null
            && progressionManager.TryGetIslandReturnPosition(destinationIslandId, out Vector3 returnPosition)
            && IsFiniteVector(returnPosition))
        {
            return ResolveSafeSpawnPosition(returnPosition);
        }

        // 3. Configured per-destination spawn
        if (TryGetSpawnPositionForIsland(destinationIslandId, out Vector3 configuredSpawn)
            && IsFiniteVector(configuredSpawn))
        {
            return ResolveSafeSpawnPosition(configuredSpawn);
        }

        // 4. Computed fallback
        return ResolveFallbackSpawn(destinationIslandId);
    }

    private Vector3 ResolveFallbackSpawn(string destinationIslandId)
    {
        int progressionIndex = GetProgressionIndex(destinationIslandId);
        Vector3 basePosition = IsFiniteVector(transform.position)
            ? transform.position
            : ResolveSafeSpawnPosition(fallbackSpawnPosition);

        float lateralOffset = defaultTravelOffset.x + Mathf.Max(0f, progressionIndex) * 1.8f;
        float depthOffset = defaultTravelOffset.z;
        if (progressionIndex > 0)
        {
            depthOffset += (progressionIndex % 2 == 0 ? 1f : -1f) * (1.5f + (progressionIndex / 2) * 0.75f);
        }

        Vector3 fallback = new Vector3(
            basePosition.x + lateralOffset,
            basePosition.y + defaultTravelOffset.y,
            basePosition.z + depthOffset);
        return ResolveSafeSpawnPosition(fallback);
    }

    private int ResolveInitialSelectionIndex()
    {
        if (orderedDestinationIds.Count == 0)
        {
            return 0;
        }

        string activeIslandId = IslandProgressionManager.Instance != null
            ? IslandProgressionManager.Instance.ActiveIslandId
            : IslandThemeRegistry.GetActiveIslandId();

        for (int i = 0; i < orderedDestinationIds.Count; i++)
        {
            if (string.Equals(orderedDestinationIds[i], activeIslandId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        for (int i = 0; i < orderedDestinationIds.Count; i++)
        {
            if (IslandProgressionManager.Instance != null
                && IslandProgressionManager.Instance.IsIslandUnlocked(orderedDestinationIds[i]))
            {
                IslandBacktrackingManager backtrackingManager = IslandBacktrackingManager.Instance;
                if (backtrackingManager == null || backtrackingManager.CanVisitIsland(orderedDestinationIds[i]))
                {
                    return i;
                }
            }
        }

        return 0;
    }

    private void RefreshDestinationOrder()
    {
        orderedDestinationIds.Clear();

        for (int i = 0; i < destinations.Count; i++)
        {
            IslandDestination destination = destinations[i];
            if (destination == null)
            {
                continue;
            }

            string islandId = IslandThemeRegistry.ResolveIslandId(destination.islandId);
            if (string.IsNullOrEmpty(islandId) || !IslandThemeRegistry.IsKnownIslandId(islandId))
            {
                continue;
            }

            if (!orderedDestinationIds.Contains(islandId))
            {
                orderedDestinationIds.Add(islandId);
            }
        }

        if (selectedIndex >= orderedDestinationIds.Count)
        {
            selectedIndex = Mathf.Max(0, orderedDestinationIds.Count - 1);
        }
    }

    private void EnsureDestinationList()
    {
        HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = destinations.Count - 1; i >= 0; i--)
        {
            IslandDestination destination = destinations[i];
            if (destination == null)
            {
                destinations.RemoveAt(i);
                continue;
            }

            string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(destination.islandId);
            if (string.IsNullOrEmpty(resolvedIslandId)
                || !IslandThemeRegistry.IsKnownIslandId(resolvedIslandId)
                || seenIds.Contains(resolvedIslandId))
            {
                destinations.RemoveAt(i);
                continue;
            }

            destination.islandId = resolvedIslandId;
            if (string.IsNullOrEmpty(destination.displayName))
            {
                destination.displayName = BuildIslandDisplayName(resolvedIslandId);
            }

            if (!IsFiniteVector(destination.boatOffset))
            {
                destination.boatOffset = defaultTravelOffset;
            }

            if (destination.useCustomSpawnPosition)
            {
                destination.customSpawnPosition = ResolveSafeSpawnPosition(destination.customSpawnPosition);
            }

            seenIds.Add(resolvedIslandId);
        }

        IReadOnlyList<string> progressionOrder = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < progressionOrder.Count; i++)
        {
            string islandId = progressionOrder[i];
            if (!IslandThemeRegistry.IsKnownIslandId(islandId) || seenIds.Contains(islandId))
            {
                continue;
            }

            destinations.Add(CreateDefaultDestination(islandId, i));
            seenIds.Add(islandId);
        }

        destinations.Sort((left, right) => GetProgressionIndex(left.islandId).CompareTo(GetProgressionIndex(right.islandId)));
    }

    private IslandDestination CreateDefaultDestination(string islandId, int progressionIndex)
    {
        Vector3 offset = defaultTravelOffset;
        offset.x += progressionIndex * 1.8f;
        if (progressionIndex > 0)
        {
            offset.z += (progressionIndex % 2 == 0 ? 1f : -1f) * (1.5f + (progressionIndex / 2) * 0.75f);
        }

        return new IslandDestination
        {
            islandId = islandId,
            displayName = BuildIslandDisplayName(islandId),
            useCustomSpawnPosition = false,
            customSpawnPosition = ResolveSafeSpawnPosition(fallbackSpawnPosition),
            boatOffset = offset
        };
    }

    private IslandDestination FindDestination(string islandId)
    {
        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        for (int i = 0; i < destinations.Count; i++)
        {
            IslandDestination destination = destinations[i];
            if (destination == null)
            {
                continue;
            }

            if (string.Equals(destination.islandId, resolvedIslandId, StringComparison.Ordinal))
            {
                return destination;
            }
        }

        return null;
    }

    private int GetProgressionIndex(string islandId)
    {
        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        IReadOnlyList<string> order = IslandThemeRegistry.ProgressionOrder;
        for (int i = 0; i < order.Count; i++)
        {
            if (string.Equals(order[i], resolvedIslandId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return int.MaxValue / 2;
    }

    private void EnsureTravelCanvas()
    {
        if (travelCanvas != null && travelPanel != null && travelLabel != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(TravelCanvasName, typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        travelCanvas = canvasObject.AddComponent<Canvas>();
        travelCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        travelCanvas.sortingOrder = 220;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject(TravelPanelName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        travelPanel = panelObject.GetComponent<RectTransform>();
        travelPanel.anchorMin = new Vector2(0.5f, 0.5f);
        travelPanel.anchorMax = new Vector2(0.5f, 0.5f);
        travelPanel.pivot = new Vector2(0.5f, 0.5f);
        travelPanel.sizeDelta = new Vector2(900f, 560f);
        travelPanel.anchoredPosition = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = panelColor;
        panelImage.raycastTarget = false;

        GameObject labelObject = new GameObject(TravelLabelName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(panelObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(36f, 32f);
        labelRect.offsetMax = new Vector2(-36f, -32f);

        travelLabel = labelObject.GetComponent<Text>();
Font boatFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
if (boatFont == null)
{
    Debug.LogError("[IslandBoatInteractable] LegacyRuntime.ttf not found. Travel label will use Unity default font.");
}
else
{
    travelLabel.font = boatFont;
}
        travelLabel.fontSize = Mathf.Max(14, panelFontSize);
        travelLabel.fontStyle = FontStyle.Normal;
        travelLabel.alignment = TextAnchor.UpperLeft;
        travelLabel.color = textColor;
        travelLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        travelLabel.verticalOverflow = VerticalWrapMode.Overflow;
        travelLabel.raycastTarget = false;

        canvasObject.SetActive(false);
    }

    private void RefreshTravelPanelText()
    {
        if (!panelOpen || travelLabel == null)
        {
            return;
        }

        IslandProgressionManager progressionManager = IslandProgressionManager.Instance;
        string activeIslandId = progressionManager != null
            ? progressionManager.ActiveIslandId
            : IslandThemeRegistry.GetActiveIslandId();

        string text = "BOAT TRAVEL\n";
        text += $"Current Island: {BuildIslandDisplayName(activeIslandId)}\n";
        text += "Use [W/S] or [Up/Down] to select destination. [Enter] travel. [Esc] close.\n\n";

        if (orderedDestinationIds.Count == 0)
        {
            text += "No destinations configured.";
            travelLabel.text = text;
            return;
        }

        for (int i = 0; i < orderedDestinationIds.Count; i++)
        {
            string islandId = orderedDestinationIds[i];
            bool isSelected = i == selectedIndex;
            bool isActive = string.Equals(activeIslandId, islandId, StringComparison.Ordinal);
            bool isUnlocked = progressionManager == null || progressionManager.IsIslandUnlocked(islandId);
            bool isRestored = IsIslandTravelEligible(progressionManager, islandId);
            IslandBacktrackingManager backtrackingManager = IslandBacktrackingManager.Instance;
            bool isBacktrackingAccessible = backtrackingManager == null || backtrackingManager.CanVisitIsland(islandId);
            bool isFullyAccessible = isUnlocked && isBacktrackingAccessible;
            bool isLockedSelection = isSelected && !isFullyAccessible;
            bool isUnrestoredSelection = isSelected && isUnlocked && !isRestored && !isActive;

            string pointer = isSelected ? ">" : " ";
            string indexText = (i + 1).ToString();
            string lockTag = isFullyAccessible ? string.Empty : (isUnlocked ? "[RESTRICTED] " : "[LOCKED] ");
            string unrestoredTag = isUnlocked && !isRestored && !isActive ? "[UNRESTORED] " : string.Empty;
            string activeTag = isActive ? "[ACTIVE] " : string.Empty;
            string line = $"{pointer} {indexText}. {activeTag}{lockTag}{unrestoredTag}{BuildIslandDisplayName(islandId)}";

            if (isSelected)
            {
                text += $"<color=#{ColorUtility.ToHtmlStringRGB(selectedTextColor)}>{line}</color>\n";
            }
            else
            {
                text += line + "\n";
            }

            if (isLockedSelection)
            {
                if (!isUnlocked)
                {
                    text += "    Complete the current island restoration to unlock this destination.\n";
                }
                else if (!isBacktrackingAccessible)
                {
                    text += "    Not yet accessible. Restore more islands to unlock backtracking.\n";
                }
            }

            if (isUnrestoredSelection)
            {
                text += "    This island has unfinished restoration work.\n";
            }
        }

        travelLabel.text = text;
    }

    private string BuildIslandDisplayName(string islandId)
    {
        string resolvedIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        if (string.IsNullOrEmpty(resolvedIslandId))
        {
            return "Island";
        }

        IslandDestination destination = FindDestination(resolvedIslandId);
        if (destination != null && !string.IsNullOrEmpty(destination.displayName))
        {
            return destination.displayName;
        }

        IslandConfig config = IslandThemeRegistry.GetConfig(resolvedIslandId);
        if (config != null && !string.IsNullOrEmpty(config.viceName))
        {
            return config.viceName;
        }

        const string islandPrefix = "island_";
        string trimmed = resolvedIslandId.StartsWith(islandPrefix, StringComparison.Ordinal)
            ? resolvedIslandId.Substring(islandPrefix.Length)
            : resolvedIslandId;

        if (string.IsNullOrEmpty(trimmed))
        {
            return "Island";
        }

        string[] words = trimmed.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (string.IsNullOrEmpty(words[i]))
            {
                continue;
            }

            words[i] = words[i].Length == 1
                ? char.ToUpperInvariant(words[i][0]).ToString()
                : char.ToUpperInvariant(words[i][0]) + words[i].Substring(1).ToLowerInvariant();
        }

        return words.Length == 0 ? "Island" : string.Join(" ", words);
    }

    private Vector3 ResolveSafeSpawnPosition(Vector3 candidatePosition)
    {
        Vector3 fallback = IsFiniteVector(fallbackSpawnPosition)
            ? fallbackSpawnPosition
            : DefaultFallbackSpawnPosition;

        if (!IsFiniteVector(candidatePosition))
        {
            return fallback;
        }

        float resolvedY = candidatePosition.y;
        if (Mathf.Abs(resolvedY) < 0.001f)
        {
            float fallbackY = Mathf.Abs(fallback.y) > 0.001f ? fallback.y : 1f;
            resolvedY = fallbackY;
        }

        return new Vector3(candidatePosition.x, resolvedY, candidatePosition.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        playerInRange = false;
        CloseTravelPanel();
        SetPromptVisible(false);
    }

    private void ApplyBoatColor()
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = boatColor;
        }
    }

    private void EnsureSolidCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (!colliders[i].isTrigger)
            {
                return;
            }
        }

        BoxCollider solidCollider = gameObject.AddComponent<BoxCollider>();
        solidCollider.isTrigger = false;
        solidCollider.size = triggerSize;
        solidCollider.center = Vector3.zero;
    }

    private void CreatePromptVisual()
    {
        promptRoot = new GameObject("BoatPrompt");
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptLocalOffset;

        GameObject spriteObject = new GameObject("PromptImage");
        spriteObject.transform.SetParent(promptRoot.transform, false);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localRotation = Quaternion.identity;
        spriteObject.transform.localScale = promptScale;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetPromptSprite();
        spriteRenderer.color = promptTint;
        spriteRenderer.shadowCastingMode = ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;
        spriteRenderer.sortingOrder = 10;
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 facingDirection = promptRoot.transform.position - mainCamera.transform.position;
        if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        promptRoot.transform.rotation = Quaternion.LookRotation(facingDirection.normalized, mainCamera.transform.up);
    }

    private Sprite GetPromptSprite()
    {
        if (promptSprite != null)
        {
            return promptSprite;
        }

        if (runtimePromptSprite != null)
        {
            return runtimePromptSprite;
        }

        Texture2D promptTexture = Resources.Load<Texture2D>(DefaultPromptResourceName);
        if (promptTexture == null)
        {
            return null;
        }

        runtimePromptSprite = Sprite.Create(
            promptTexture,
            new Rect(0f, 0f, promptTexture.width, promptTexture.height),
            new Vector2(0.5f, 0.5f),
            DefaultPromptPixelsPerUnit);
        runtimePromptSprite.name = $"{DefaultPromptResourceName}_Runtime";
        return runtimePromptSprite;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (promptRoot != null && promptRoot.activeSelf != isVisible)
        {
            promptRoot.SetActive(isVisible);
        }
    }

    private void OnDestroy()
    {
        if (runtimePromptSprite != null)
        {
            Destroy(runtimePromptSprite);
            runtimePromptSprite = null;
        }

        if (travelCanvas != null)
        {
            Destroy(travelCanvas.gameObject);
            travelCanvas = null;
            travelPanel = null;
            travelLabel = null;
        }
    }

    public Vector3 GetInteractionAssistPosition()
    {
        return transform.position;
    }

    public float GetInteractionAssistRadius()
    {
        return Mathf.Max(triggerSize.x, triggerSize.z);
    }

    public bool IsInteractionAssistActive()
    {
        GameStateManager manager = GameStateManager.Instance;
        return playerInRange
            && manager != null
            && manager.currentState == GameStateManager.GameState.Exploration
            && !manager.IsTransitioning;
    }

    private static bool IsPlayerCollider(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider.CompareTag("Player"))
        {
            return true;
        }

        return collider.GetComponentInParent<IsometricPlayer>() != null;
    }

    private static bool IsFiniteVector(Vector3 value)
    {
        return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static void PlayTravelFanfare()
    {
        AudioManager audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.HandleTravel();
            audioManager.HandleBoatDepart();
        }
    }

    private static bool IsIslandTravelEligible(IslandProgressionManager progressionManager, string islandId)
    {
        if (progressionManager == null || string.IsNullOrEmpty(islandId))
        {
            return false;
        }

        if (string.Equals(progressionManager.ActiveIslandId, islandId, StringComparison.Ordinal))
        {
            return true;
        }

        IslandRestorationTracker tracker = IslandRestorationTracker.Instance;
        if (tracker == null)
        {
            return true;
        }

        IslandRestorationState state = tracker.GetRestorationState(islandId);
        if (state == null)
        {
            return true;
        }

        return state.IsIslandRestored;
    }
}
