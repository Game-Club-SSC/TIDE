using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum EnemyState
{
    Idle,
    Roaming,
    Chasing,
    Returning
}

public enum EnemyBehaviorType
{
    Aggressive,
    Passive
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class OverworldEnemy : MonoBehaviour
{
    [Header("Roaming")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float roamSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("Aggro")]
    [SerializeField] private float proximityAggroRange = 7.5f;
    [SerializeField] private float chaseBreakMultiplier = 1.5f;

    [Header("Detection")]
    [SerializeField] private bool requireLineOfSightForAggro;
    [SerializeField] private float aggroLineOfSightHeight = 0.6f;
    [SerializeField] private LayerMask aggroLineOfSightMask = ~0;

    [Header("Stealth")]
    [Tooltip("Enemy behavior type: Passive = can be sneaked past, Aggressive = always chases on sight")]
    [SerializeField] private EnemyBehaviorType behaviorType = EnemyBehaviorType.Aggressive;
    [Tooltip("Angle of vision cone in degrees (180 = half-circle, 360 = all around)")]
    [SerializeField] private float visionConeAngle = 180f;
    [Tooltip("Player speed threshold - below this speed, stealth detection is harder")]
    [SerializeField] private float stealthSpeedThreshold = 2f;
    [Tooltip("Multiplier to aggro range when player is moving fast")]
    [SerializeField] private float chaseSpeedAggroMultiplier = 1.3f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Combat")]
    [SerializeField] private EncounterConfig encounterConfig;
    [SerializeField] private string islandId = "";
    [SerializeField] private string encounterIdOverride = "";
    [SerializeField] private float restorationValue = 0.001f;

    [Header("Puzzle Guard")]
    [SerializeField] private bool isPuzzleGuard;
    [SerializeField] private float puzzleGuardLeashRadius = 8f;
    [SerializeField] private float puzzleGuardLeashReengageBuffer = 1f;
    [SerializeField] private float puzzleGuardReturnChaseDelay = 0.35f;
    [SerializeField] private float puzzleGuardReturnStuckTimeout = 1.2f;
    [SerializeField] private float puzzleGuardReturnProgressThreshold = 0.05f;
    [SerializeField] private float puzzleGuardReturnEnterBuffer = 0.35f;
    [SerializeField] private float puzzleGuardPostSnapReturnDelay = 0.75f;
    [SerializeField] private float puzzleGuardFallbackLogCooldown = 5f;

    [Header("Visual")]
    [SerializeField] private IslandEnemyVisualProfile visualProfile;
    [SerializeField] private Color enemyColor = new Color(0.89f, 0.38f, 0.25f);
    [SerializeField] private Color alertIndicatorColor = Color.yellow;
    [SerializeField] private Vector3 indicatorWorldOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private Vector3 exclamationScale = new Vector3(0.3f, 0.6f, 1f);
    [SerializeField] private float arrowScale = 0.35f;
    [SerializeField] private Vector3 characterModelLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 characterModelLocalScale = Vector3.one;

    private EnemyState currentState = EnemyState.Idle;
    private int patrolIndex;
    private float stateTimer;
    private Transform playerTransform;
    private Rigidbody rb;
    private Camera mainCamera;
    private GameObject exclamationObject;
    private SpriteRenderer exclamationRenderer;
    private GameObject facingArrowObject;
    private SpriteRenderer arrowRenderer;
    private Transform characterModelRoot;
    private Vector3 guardAnchorPosition;
    private string puzzleGuardIslandId = "";
    private string puzzleGuardEncounterId;
    private float puzzleGuardRestorationValue = 0.001f;
    private bool hasTriggeredCombat;
    private CombatUnit.Element visualElement = CombatUnit.Element.Fire;
    private bool startupClearCheckComplete;
    private bool isRebuildingModel;
    private bool isFloating;
    private float floatingBaseY;
    private float puzzleGuardChaseLockUntilTime;
    private float puzzleGuardReturnStuckTimer;
    private float puzzleGuardLastReturnDistance;
    private float puzzleGuardReturnLockUntilTime;
    private float puzzleGuardNextFallbackLogTime;
    private Vector3 planarVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        mainCamera = Camera.main;
    }

    private void Start()
    {
        playerTransform = FindPlayer();

        if (TryDespawnAsClearedEncounter())
        {
            return;
        }

        ResolveVisualElement();

        if (visualProfile != null)
        {
            ApplyVisualProfile(visualProfile);
        }

        Ensure3DVisualSetup();

        if (isPuzzleGuard)
        {
            if (guardAnchorPosition == Vector3.zero)
            {
                guardAnchorPosition = transform.position;
            }

            transform.position = guardAnchorPosition;
        }

        CreateExclamationIndicator();
        CreateFacingArrow();
        ApplyEnemyColor();
        TransitionToState(EnemyState.Roaming);
        Debug.Log($"[OverworldEnemy] Initialized: {name} with {(patrolPoints != null ? patrolPoints.Length : 0)} patrol points.");
    }

    public void ConfigureAsPuzzleGuard(
        EncounterConfig config,
        string islandId,
        string encounterId,
        float restorationValue,
        Vector3 anchorPosition,
        float leashRadius,
        float roamSpeedOverride,
        float chaseSpeedOverride)
    {
        encounterConfig = config;
        isPuzzleGuard = true;
        guardAnchorPosition = anchorPosition;
        puzzleGuardLeashRadius = Mathf.Max(1f, leashRadius);
        puzzleGuardIslandId = IslandThemeRegistry.ResolveIslandId(islandId);
        puzzleGuardEncounterId = encounterId;
        puzzleGuardRestorationValue = Mathf.Max(0.001f, restorationValue);
        roamSpeed = Mathf.Max(0.1f, roamSpeedOverride);
        chaseSpeed = Mathf.Max(roamSpeed, chaseSpeedOverride);
        requireLineOfSightForAggro = true;
        patrolPoints = null;
        transform.position = guardAnchorPosition;
    }

    private void Update()
    {
        if (!startupClearCheckComplete && TryDespawnAsClearedEncounter())
        {
            return;
        }

        if (!CanOperate()) return;
        if (playerTransform == null) playerTransform = FindPlayer();

        if (playerTransform != null)
        {
            float touchDistance = GetPlanarDistance(transform.position, playerTransform.position);
            if (touchDistance <= arrivalThreshold)
            {
                TryTriggerCombat();
                return;
            }
        }

        switch (currentState)
        {
            case EnemyState.Roaming:
                UpdateRoaming();
                break;
            case EnemyState.Chasing:
                UpdateChasing();
                break;
            case EnemyState.Returning:
                UpdateReturning();
                break;
        }

        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (!CanOperate())
        {
            StopMovement();
            return;
        }

        switch (currentState)
        {
            case EnemyState.Roaming:
                FixedRoaming();
                break;
            case EnemyState.Chasing:
                FixedChasing();
                break;
            case EnemyState.Returning:
                FixedReturning();
                break;
            case EnemyState.Idle:
                StopMovement();
                break;
        }
    }

    // ========== STATE MACHINE ==========

    private void TransitionToState(EnemyState newState)
    {
        EnemyState previousState = currentState;
        currentState = newState;

        switch (newState)
        {
            case EnemyState.Roaming:
                stateTimer = 0f;
                SetExclamationVisible(false);
                break;
            case EnemyState.Chasing:
                SetExclamationVisible(true);
                break;
            case EnemyState.Returning:
                SetExclamationVisible(false);

                if (isPuzzleGuard)
                {
                    puzzleGuardChaseLockUntilTime = Time.time + Mathf.Max(0f, puzzleGuardReturnChaseDelay);
                    puzzleGuardReturnStuckTimer = 0f;
                    puzzleGuardLastReturnDistance = GetPlanarDistance(transform.position, guardAnchorPosition);
                    break;
                }

                patrolIndex = FindNearestPatrolIndex();
                break;
        }

#if UNITY_EDITOR
        Debug.Log($"[OverworldEnemy] {name}: {previousState} -> {newState}");
#endif
    }

    // ========== ROAMING ==========

    private void UpdateRoaming()
    {
        if (ShouldStartChase())
        {
            TransitionToState(EnemyState.Chasing);
            return;
        }

        if (isPuzzleGuard)
        {
            UpdatePuzzleGuardRoamState();
            return;
        }

        if (!HasPatrolPoints())
        {
            return;
        }

        if (stateTimer > 0f)
        {
            stateTimer -= Time.deltaTime;
        }
    }

    private void FixedRoaming()
    {
        if (isPuzzleGuard)
        {
            FixedPuzzleGuardRoam();
            return;
        }

        if (!HasPatrolPoints())
        {
            StopMovement();
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            StopMovement();
            return;
        }

        float distance = GetPlanarDistance(transform.position, target.position);

        if (distance <= arrivalThreshold)
        {
            StopMovement();

            if (stateTimer <= 0f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                stateTimer = waitTimeAtPoint;
            }

            return;
        }

        MoveToward(target.position, roamSpeed);
    }

    // ========== CHASING ==========

    private void UpdateChasing()
    {
        if (playerTransform == null)
        {
            TransitionToState(EnemyState.Returning);
            return;
        }

        float distanceToPlayer = GetPlanarDistance(transform.position, playerTransform.position);

        if (distanceToPlayer <= arrivalThreshold)
        {
            TryTriggerCombat();
            return;
        }

        if (ShouldStopChasing(distanceToPlayer))
        {
            TransitionToState(EnemyState.Returning);
        }
    }

    private void FixedChasing()
    {
        if (playerTransform == null)
        {
            StopMovement();
            return;
        }

        MoveToward(playerTransform.position, chaseSpeed);
    }

    // ========== RETURNING ==========

    private void UpdateReturning()
    {
        if (isPuzzleGuard)
        {
            UpdatePuzzleGuardReturning();
            return;
        }

        if (ShouldStartChase())
        {
            TransitionToState(EnemyState.Chasing);
            return;
        }

        if (!HasPatrolPoints())
        {
            TransitionToState(EnemyState.Roaming);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            TransitionToState(EnemyState.Roaming);
            return;
        }

        float distance = GetPlanarDistance(transform.position, target.position);
        if (distance <= arrivalThreshold)
        {
            TransitionToState(EnemyState.Roaming);
        }
    }

    private void FixedReturning()
    {
        if (isPuzzleGuard)
        {
            MoveToward(guardAnchorPosition, roamSpeed);
            return;
        }

        if (!HasPatrolPoints())
        {
            StopMovement();
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            StopMovement();
            return;
        }

        MoveToward(target.position, roamSpeed);
    }

    private bool ShouldStartChase()
    {
        if (playerTransform == null)
        {
            return false;
        }

        if (isPuzzleGuard && currentState == EnemyState.Returning)
        {
            if (Time.time < puzzleGuardChaseLockUntilTime)
            {
                return false;
            }

            float distanceFromAnchor = GetPlanarDistance(transform.position, guardAnchorPosition);
            float reengageRadius = Mathf.Max(
                arrivalThreshold + 0.5f,
                puzzleGuardLeashRadius - Mathf.Max(0.25f, puzzleGuardLeashReengageBuffer));
            if (distanceFromAnchor > reengageRadius)
            {
                return false;
            }

            float playerDistanceFromAnchor = GetPlanarDistance(playerTransform.position, guardAnchorPosition);
            if (playerDistanceFromAnchor > reengageRadius)
            {
                return false;
            }
        }

        float distance = GetPlanarDistance(transform.position, playerTransform.position);
        float effectiveAggroRange = proximityAggroRange;

        // Adjust aggro range based on player speed for stealth mechanics
        if (behaviorType == EnemyBehaviorType.Passive)
        {
            float playerSpeed = GetPlayerSpeed();
            if (playerSpeed > stealthSpeedThreshold)
            {
                effectiveAggroRange *= chaseSpeedAggroMultiplier;
            }
            else
            {
                // When player is moving slowly, reduce detection range for stealth
                effectiveAggroRange *= 0.7f;
            }
        }

        if (distance <= Mathf.Max(arrivalThreshold, effectiveAggroRange))
        {
            // Check vision cone for passive enemies
            if (behaviorType == EnemyBehaviorType.Passive && !IsPlayerInVisionCone())
            {
                return false;
            }

            return HasAggroLineOfSightToPlayer();
        }

        return false;
    }

    private bool IsPlayerInVisionCone()
    {
        if (playerTransform == null)
        {
            return false;
        }

        Vector3 directionToPlayer = (playerTransform.position - transform.position);
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        directionToPlayer.Normalize();

        Vector3 forward = transform.forward;
        forward.y = 0f;

        float angle = Vector3.Angle(forward, directionToPlayer);
        return angle <= visionConeAngle * 0.5f;
    }

    private float GetPlayerSpeed()
    {
        if (playerTransform == null)
        {
            return 0f;
        }

        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 velocity = playerRb.linearVelocity;
            velocity.y = 0f;
            return velocity.magnitude;
        }

        return 0f;
    }

    private bool ShouldStopChasing(float distanceToPlayer)
    {
        float breakDistance = Mathf.Max(proximityAggroRange + 1f, proximityAggroRange * Mathf.Max(1.1f, chaseBreakMultiplier));
        if (distanceToPlayer > breakDistance)
        {
            return true;
        }

        if (!isPuzzleGuard)
        {
            return false;
        }

        float distanceFromAnchor = GetPlanarDistance(transform.position, guardAnchorPosition);
        return distanceFromAnchor > Mathf.Max(arrivalThreshold + 0.5f, puzzleGuardLeashRadius);
    }

    // ========== MOVEMENT ==========

    private void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();
        planarVelocity = new Vector3(direction.x * speed, 0f, direction.z * speed);

        if (rb.isKinematic)
        {
            rb.MovePosition(rb.position + planarVelocity * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = new Vector3(planarVelocity.x, rb.linearVelocity.y, planarVelocity.z);
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
    }

    private void StopMovement()
    {
        planarVelocity = Vector3.zero;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        }
    }

    private void ResetMotion()
    {
        StopMovement();

        if (rb != null && !rb.isKinematic)
        {
            rb.angularVelocity = Vector3.zero;
        }
    }

    // ========== VISUALS ==========

    private void UpdateVisuals()
    {
        if (exclamationObject != null)
        {
            UpdateBillboard(exclamationObject.transform);
        }

        if (facingArrowObject != null)
        {
            UpdateBillboard(facingArrowObject.transform);

            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }

                if (mainCamera == null)
                {
                    return;
                }

                Vector3 toCamera = mainCamera.transform.position - facingArrowObject.transform.position;
                toCamera.y = 0f;
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    Quaternion billboardRotation = Quaternion.LookRotation(toCamera.normalized);
                    Vector3 euler = billboardRotation.eulerAngles;
                    facingArrowObject.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
                }
            }
        }

        if (isFloating)
        {
            float bobOffset = Mathf.Sin(Time.time * 1.5f) * 0.08f;
            Vector3 pos = transform.position;
            pos.y = floatingBaseY + (visualProfile != null ? visualProfile.floatHeight : 0.3f) + bobOffset;
            transform.position = pos;
        }
    }

    private void UpdateBillboard(Transform indicator)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 toCamera = mainCamera.transform.position - indicator.position;
        if (toCamera.sqrMagnitude <= Mathf.Epsilon) return;

        indicator.rotation = Quaternion.LookRotation(toCamera.normalized, mainCamera.transform.up);
    }

    private void CreateExclamationIndicator()
    {
        exclamationObject = new GameObject("ExclamationMark");
        exclamationObject.transform.SetParent(transform, false);
        exclamationObject.transform.localPosition = indicatorWorldOffset;
        exclamationObject.transform.localScale = exclamationScale;

        exclamationRenderer = exclamationObject.AddComponent<SpriteRenderer>();
        exclamationRenderer.sprite = ExclamationMarkSprite.GetSprite();
        TideRuntimeVisualUtility.ApplySpriteColor(exclamationRenderer, alertIndicatorColor);
        exclamationRenderer.shadowCastingMode = ShadowCastingMode.Off;
        exclamationRenderer.receiveShadows = false;
        exclamationRenderer.sortingOrder = 10;

        exclamationObject.SetActive(false);
    }

    private void ResolveVisualElement()
    {
        CombatUnit.Element resolved = CombatUnit.Element.Fire;

        if (encounterConfig != null)
        {
            EnemyData firstEnemy = encounterConfig.GetEnemy(0);
            if (firstEnemy != null && firstEnemy.element != CombatUnit.Element.None)
            {
                resolved = firstEnemy.element;
            }
        }

        visualElement = resolved;
    }

    private void Ensure3DVisualSetup()
    {
        RemoveLegacySpriteVisuals();
        RebuildElementalEnemyModel();
    }

    private void RemoveLegacySpriteVisuals()
    {
        Transform oldFallback = transform.Find("Enemy3DVisual");
        if (oldFallback != null)
        {
            Destroy(oldFallback.gameObject);
        }

        Transform oldBody = transform.Find("FuturisticEnemyBody");
        if (oldBody != null)
        {
            Destroy(oldBody.gameObject);
        }

        Transform oldShadow = transform.Find("FuturisticEnemyShadow");
        if (oldShadow != null)
        {
            Destroy(oldShadow.gameObject);
        }

        Transform battleSprite = transform.Find("BattleSpriteVisual");
        if (battleSprite != null)
        {
            Destroy(battleSprite.gameObject);
        }

        Transform battleShadow = transform.Find("BattleSpriteShadow");
        if (battleShadow != null)
        {
            Destroy(battleShadow.gameObject);
        }
    }

    /// <summary>
    /// Applies an island-specific visual profile to this enemy, overriding the
    /// default element-derived colors and behavior. Call before Ensure3DVisualSetup
    /// for full effect, or at runtime to re-theme the enemy.
    /// </summary>
    public void ApplyVisualProfile(IslandEnemyVisualProfile profile)
    {
        if (profile == null)
        {
            return;
        }

        visualProfile = profile;

        // Override enemyColor from profile
        enemyColor = profile.primaryColor;

        // Map island to a CombatUnit.Element for the model factory
        visualElement = ResolveVisualElementForIsland(profile.islandId);

        // Apply scale to the character model if it already exists
        if (characterModelRoot != null)
        {
            ApplyProfileScale();
            ApplyProfileColorsToModel();
        }

        // Apply floating behavior
        isFloating = profile.floatsAboveGround;
        if (isFloating)
        {
            floatingBaseY = transform.position.y;
        }

        // Recolor indicators
        if (arrowRenderer != null)
        {
            arrowRenderer.color = enemyColor;
        }

        if (exclamationRenderer != null)
        {
            exclamationRenderer.color = profile.eyeColor;
        }

        Debug.Log($"[OverworldEnemy] {name}: Applied visual profile for '{profile.islandId}'.");
    }

    private static CombatUnit.Element ResolveVisualElementForIsland(string islandId)
    {
        if (string.IsNullOrEmpty(islandId))
        {
            return CombatUnit.Element.Fire;
        }

        switch (islandId)
        {
            case "island_lust":     return CombatUnit.Element.Water;
            case "island_greed":    return CombatUnit.Element.Earth;
            case "island_desire":    return CombatUnit.Element.Air;
            case "island_anger":    return CombatUnit.Element.Fire;
            case "island_envy":     return CombatUnit.Element.Space;
            case "island_ego":    return CombatUnit.Element.Space;
            default:                return CombatUnit.Element.Fire;
        }
    }

    private void ApplyEnemyColor()
    {
        if (visualProfile != null)
        {
            enemyColor = visualProfile.primaryColor;
        }
        else
        {
            enemyColor = ElementalCharacterFactory.GetElementPrimaryColor(visualElement);
        }

        if (arrowRenderer != null)
        {
            arrowRenderer.color = enemyColor;
        }

        if (exclamationRenderer != null && visualProfile != null)
        {
            exclamationRenderer.color = visualProfile.eyeColor;
        }
    }

    private void RebuildElementalEnemyModel()
    {
        if (isRebuildingModel)
        {
            return;
        }

        isRebuildingModel = true;
        characterModelRoot = ElementalCharacterFactory.BuildExplorationEnemyModel(
            transform,
            visualElement,
            characterModelLocalOffset,
            characterModelLocalScale);

        if (characterModelRoot != null)
        {
            characterModelRoot.name = ElementalCharacterFactory.EnemyModelRootName;
        }
        else
        {
            Debug.LogWarning($"[OverworldEnemy] Could not build model root for {name}.");
        }

        if (visualProfile != null && characterModelRoot != null)
        {
            ApplyProfileColorsToModel();
            ApplyProfileScale();
        }

        ConfigureModelRendererVisibility();
        ApplyEnemyColor();
        isRebuildingModel = false;
    }

    private void ApplyProfileColorsToModel()
    {
        if (visualProfile == null || characterModelRoot == null)
        {
            return;
        }

        Color primary = visualProfile.primaryColor;
        Color secondary = visualProfile.secondaryColor;
        Color eye = visualProfile.eyeColor;

        Renderer[] renderers = characterModelRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                continue;
            }

            string partName = renderer.gameObject.name;

            if (partName.Contains("Head") || partName.Contains("Torso"))
            {
                TideRuntimeVisualUtility.ApplyMeshColor(renderer, Color.Lerp(primary, Color.white, 0.12f));
            }
            else if (partName.Contains("Visor") || partName.Contains("Eye"))
            {
                TideRuntimeVisualUtility.ApplyMeshColor(renderer, eye);
            }
            else if (partName.Contains("Arm") || partName.Contains("Leg")
                     || partName.Contains("Waist") || partName.Contains("Claw"))
            {
                TideRuntimeVisualUtility.ApplyMeshColor(renderer, secondary);
            }
            else if (partName.Contains("Horn") || partName.Contains("Chest")
                     || partName.Contains("Boot"))
            {
                TideRuntimeVisualUtility.ApplyMeshColor(renderer, Color.Lerp(secondary, Color.black, 0.25f));
            }
            else
            {
                TideRuntimeVisualUtility.ApplyMeshColor(renderer, primary);
            }
        }
    }

    private void ApplyProfileScale()
    {
        if (visualProfile == null || characterModelRoot == null)
        {
            return;
        }

        characterModelLocalScale = visualProfile.baseScale;
        characterModelRoot.localScale = visualProfile.baseScale;
    }

    private void ConfigureModelRendererVisibility()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            bool belongsToModel = characterModelRoot != null && renderer.transform.IsChildOf(characterModelRoot);
            bool isIndicatorRenderer = (exclamationObject != null && renderer.transform.IsChildOf(exclamationObject.transform))
                                     || (facingArrowObject != null && renderer.transform.IsChildOf(facingArrowObject.transform));
            renderer.enabled = belongsToModel || isIndicatorRenderer;
        }
    }

    private void CreateFacingArrow()
    {
        facingArrowObject = new GameObject("FacingArrow");
        facingArrowObject.transform.SetParent(transform, false);
        facingArrowObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        facingArrowObject.transform.localScale = new Vector3(arrowScale, arrowScale, 1f);

        arrowRenderer = facingArrowObject.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = CreateArrowSprite();
        TideRuntimeVisualUtility.ApplySpriteColor(arrowRenderer, enemyColor);
        arrowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        arrowRenderer.receiveShadows = false;
        arrowRenderer.sortingOrder = 5;
    }

    private Sprite CreateArrowSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Draw a triangle pointing down (forward direction)
        for (int y = 2; y <= 12; y++)
        {
            int halfWidth = (y * 3) / 12;
            int xStart = 8 - halfWidth;
            int xEnd = 8 + halfWidth;
            for (int x = xStart; x <= xEnd; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void SetExclamationVisible(bool visible)
    {
        if (exclamationObject != null && exclamationObject.activeSelf != visible)
        {
            exclamationObject.SetActive(visible);
        }
    }

    // ========== COMBAT TRIGGER ==========

    private void TryTriggerCombat()
    {
        if (hasTriggeredCombat)
        {
            return;
        }

        if (GameStateManager.Instance == null)
        {
            Debug.LogWarning("[OverworldEnemy] GameStateManager.Instance is null. Cannot trigger combat.");
            return;
        }

        if (!GameStateManager.Instance.CanEnterCombatScene()) return;

        if (encounterConfig == null)
        {
            Debug.LogWarning($"[OverworldEnemy] {name} has no EncounterConfig assigned. Cannot trigger combat.");
            return;
        }

        hasTriggeredCombat = true;

        GameStateManager.Instance.PendingEnemyComposition =
            EnemyComposition.FromEncounterConfig(encounterConfig);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (isPuzzleGuard)
        {
            Vector3 returnPosition = playerTransform != null ? playerTransform.position : transform.position;
            Debug.Log($"[OverworldEnemy] Puzzle guard '{name}' initiating combat for encounter '{puzzleGuardEncounterId}'.");
            GameStateManager.Instance.EnterCombatSceneFromExploration(
                puzzleGuardIslandId,
                puzzleGuardEncounterId,
                puzzleGuardRestorationValue,
                returnPosition);
        }
        else
        {
            string scopedEncounterId = ResolveTrackingEncounterId();
            if (!string.IsNullOrEmpty(scopedEncounterId))
            {
                Vector3 returnPosition = playerTransform != null ? playerTransform.position : transform.position;
                GameStateManager.Instance.EnterCombatSceneFromExploration(
                    ResolveTrackingIslandId(),
                    scopedEncounterId,
                    Mathf.Max(0.001f, restorationValue),
                    returnPosition,
                    IsBossEncounterId(scopedEncounterId));
                Debug.Log($"[OverworldEnemy] {name} initiating tracked combat '{scopedEncounterId}'.");
            }
            else
            {
                Debug.Log($"[OverworldEnemy] {name} initiating combat with '{encounterConfig.displayName}'.");
                GameStateManager.Instance.EnterCombatScene();
            }
        }

        Destroy(gameObject, 0.1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        if (!IsPlayerCollider(collision.collider))
        {
            return;
        }

        TryTriggerCombat();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        TryTriggerCombat();
    }

    private void UpdatePuzzleGuardRoamState()
    {
        if (Time.time < puzzleGuardReturnLockUntilTime)
        {
            return;
        }

        float returnEnterDistance = Mathf.Max(
            arrivalThreshold + 0.05f,
            arrivalThreshold + Mathf.Max(0.01f, puzzleGuardReturnEnterBuffer));

        if (GetPlanarDistance(transform.position, guardAnchorPosition) > returnEnterDistance)
        {
            TransitionToState(EnemyState.Returning);
        }
    }

    private void UpdatePuzzleGuardReturning()
    {
        float distanceToAnchor = GetPlanarDistance(transform.position, guardAnchorPosition);
        if (distanceToAnchor <= arrivalThreshold)
        {
            StopMovement();
            puzzleGuardReturnLockUntilTime = Time.time + 0.1f;
            TransitionToState(EnemyState.Roaming);
            return;
        }

        if (distanceToAnchor < puzzleGuardLastReturnDistance - Mathf.Max(0.01f, puzzleGuardReturnProgressThreshold))
        {
            puzzleGuardReturnStuckTimer = 0f;
        }
        else if (distanceToAnchor < puzzleGuardLastReturnDistance)
        {
            // Moving toward anchor but not enough progress — reset stuck timer but track distance
            puzzleGuardReturnStuckTimer = 0f;
        }
        else
        {
            puzzleGuardReturnStuckTimer += Time.deltaTime;
        }
        puzzleGuardLastReturnDistance = distanceToAnchor;

        if (puzzleGuardReturnStuckTimer >= Mathf.Max(0.1f, puzzleGuardReturnStuckTimeout))
        {
            if (Time.time >= puzzleGuardNextFallbackLogTime)
            {
                Debug.LogWarning($"[OverworldEnemy] {name} return fallback: snapping puzzle guard to anchor.");
                puzzleGuardNextFallbackLogTime = Time.time + Mathf.Max(0.5f, puzzleGuardFallbackLogCooldown);
            }

            rb.position = guardAnchorPosition;
            ResetMotion();
            puzzleGuardReturnLockUntilTime = Time.time + Mathf.Max(0.1f, puzzleGuardPostSnapReturnDelay);
            TransitionToState(EnemyState.Roaming);
            return;
        }

        if (ShouldStartChase())
        {
            float distanceToPlayer = playerTransform != null
                ? GetPlanarDistance(transform.position, playerTransform.position)
                : float.MaxValue;

            if (!ShouldStopChasing(distanceToPlayer))
            {
                TransitionToState(EnemyState.Chasing);
            }
        }
    }

    private void FixedPuzzleGuardRoam()
    {
        float distanceToAnchor = GetPlanarDistance(transform.position, guardAnchorPosition);
        if (distanceToAnchor <= arrivalThreshold)
        {
            StopMovement();
            return;
        }

        MoveToward(guardAnchorPosition, roamSpeed);
    }

    // ========== HELPERS ==========

    private bool CanOperate()
    {
        if (GameStateManager.Instance == null) return true;
        if (GameStateManager.Instance.currentState != GameStateManager.GameState.Exploration) return false;
        if (GameStateManager.Instance.IsTransitioning) return false;
        return true;
    }

    private Transform FindPlayer()
    {
        if (playerTransform != null) return playerTransform;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) return playerObj.transform;

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null) return player.transform;

        return null;
    }

    private bool HasPatrolPoints()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return false;
        return patrolPoints[patrolIndex] != null;
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

    private bool HasAggroLineOfSightToPlayer()
    {
        if (!requireLineOfSightForAggro || playerTransform == null)
        {
            return true;
        }

        Vector3 origin = transform.position + (Vector3.up * Mathf.Max(0.1f, aggroLineOfSightHeight));
        Vector3 target = playerTransform.position + (Vector3.up * Mathf.Max(0.1f, aggroLineOfSightHeight));
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f)
        {
            return true;
        }

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            direction / distance,
            distance,
            aggroLineOfSightMask,
            QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return true;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null)
            {
                continue;
            }

            if (hitCollider.transform == transform || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            return IsPlayerCollider(hitCollider);
        }

        return true;
    }

    private int FindNearestPatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return 0;

        int nearest = 0;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            float dist = GetPlanarDistance(transform.position, patrolPoints[i].position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = i;
            }
        }

        return nearest;
    }

    private static float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    private bool ShouldDespawnAsClearedEncounter()
    {
        string scopedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(scopedEncounterId))
        {
            return false;
        }

        if (IslandRestorationTracker.Instance == null)
        {
            return false;
        }

        string scopedIslandId = ResolveTrackingIslandId();
        if (IslandRestorationTracker.Instance.HasClearedEncounter(scopedIslandId, scopedEncounterId))
        {
            Debug.Log($"[OverworldEnemy] Removing '{name}' because encounter '{scopedEncounterId}' is already cleared.");
            return true;
        }

        return false;
    }

    private bool TryDespawnAsClearedEncounter()
    {
        string scopedEncounterId = ResolveTrackingEncounterId();
        if (string.IsNullOrEmpty(scopedEncounterId))
        {
            startupClearCheckComplete = true;
            return false;
        }

        if (IslandRestorationTracker.Instance == null)
        {
            // Don't mark as complete — retry next frame when tracker may be ready
            return false;
        }

        startupClearCheckComplete = true;
        if (!ShouldDespawnAsClearedEncounter())
        {
            return false;
        }

        Destroy(gameObject);
        return true;
    }

    private string ResolveTrackingEncounterId()
    {
        if (isPuzzleGuard)
        {
            return puzzleGuardEncounterId;
        }

        if (!string.IsNullOrEmpty(encounterIdOverride))
        {
            return encounterIdOverride;
        }

        if (encounterConfig != null && !string.IsNullOrEmpty(encounterConfig.encounterId))
        {
            return encounterConfig.encounterId;
        }

        return string.Empty;
    }

    private string ResolveTrackingIslandId()
    {
        string fallbackIslandId = IslandThemeRegistry.GetActiveIslandId();

        if (isPuzzleGuard)
        {
            if (string.IsNullOrEmpty(puzzleGuardIslandId))
            {
                return fallbackIslandId;
            }

            return IslandThemeRegistry.ResolveIslandId(puzzleGuardIslandId);
        }

        if (string.IsNullOrEmpty(islandId))
        {
            return fallbackIslandId;
        }

        return IslandThemeRegistry.ResolveIslandId(islandId);
    }

    private static bool IsBossEncounterId(string encounterId)
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        return encounterId.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ========== LIFECYCLE ==========

    private void OnDestroy()
    {
        if (exclamationObject != null)
        {
            Destroy(exclamationObject);
        }

        if (facingArrowObject != null)
        {
            Destroy(facingArrowObject);
        }
    }

    // ========== GIZMOS ==========

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);

                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[next] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
                }
            }
        }

        // Aggro range
        Gizmos.color = currentState == EnemyState.Chasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(arrivalThreshold, proximityAggroRange));

        // Vision cone for passive enemies
        if (behaviorType == EnemyBehaviorType.Passive)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            float halfAngle = visionConeAngle * 0.5f;
            float coneLength = Mathf.Max(arrivalThreshold, proximityAggroRange);

            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;

            Gizmos.DrawLine(transform.position, transform.position + leftDir * coneLength);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * coneLength);

            // Draw arc
            int segments = 20;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = -halfAngle + (visionConeAngle * i / segments);
                float angle2 = -halfAngle + (visionConeAngle * (i + 1) / segments);

                Vector3 point1 = transform.position + Quaternion.Euler(0, angle1, 0) * transform.forward * coneLength;
                Vector3 point2 = transform.position + Quaternion.Euler(0, angle2, 0) * transform.forward * coneLength;
                Gizmos.DrawLine(point1, point2);
            }
        }
    }
#endif
}
