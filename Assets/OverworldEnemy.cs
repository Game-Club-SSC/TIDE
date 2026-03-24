using UnityEngine;
using UnityEngine.Rendering;

public enum EnemyState
{
    Idle,
    Roaming,
    Alert,
    Chasing,
    Returning
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class OverworldEnemy : MonoBehaviour
{
    [Header("Roaming")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float roamSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 1.5f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("Vision")]
    [SerializeField] private float visionRange = 6f;
    [SerializeField] private float visionHalfAngle = 30f;
    [SerializeField] private LayerMask visionBlockMask;
    [SerializeField] private float eyeHeight = 0.5f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Returning Recovery")]
    [SerializeField] private float stuckCheckInterval = 0.35f;
    [SerializeField] private float minimumReturnMovement = 0.08f;
    [SerializeField] private float recoveryBypassDistance = 1.4f;
    [SerializeField] private float recoveryDuration = 1.1f;
    [SerializeField] private float recoverySpeedMultiplier = 1.2f;
    [SerializeField] private float recoveryRetryCooldown = 0.4f;
    [SerializeField] private int maxRecoveryAttemptsBeforeEscalation = 3;

    [Header("Combat")]
    [SerializeField] private EncounterConfig encounterConfig;
    [SerializeField] private string islandId = "default";
    [SerializeField] private string encounterIdOverride = "";
    [SerializeField] private float restorationValue = 0.001f;

    [Header("Puzzle Guard")]
    [SerializeField] private bool isPuzzleGuard;
    [SerializeField] private float puzzleGuardRoamRadius = 1.5f;
    [SerializeField] private float puzzleGuardLeashRadius = 8f;

    [Header("Visual")]
    [SerializeField] private Color enemyColor = new Color(0.89f, 0.38f, 0.25f);
    [SerializeField] private Color alertIndicatorColor = Color.yellow;
    [SerializeField] private Vector3 indicatorWorldOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private Vector3 exclamationScale = new Vector3(0.3f, 0.6f, 1f);
    [SerializeField] private float arrowScale = 0.35f;

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
    private Vector3 guardAnchorPosition;
    private Vector3 guardRoamTarget;
    private float guardRoamWaitTimer;
    private bool hasGuardRoamTarget;
    private string puzzleGuardIslandId = "default";
    private string puzzleGuardEncounterId;
    private float puzzleGuardRestorationValue = 0.001f;
    private Vector2 returnLastPlanarPosition;
    private float returnStuckCheckTimer;
    private bool returnStuckTrackingInitialized;
    private Vector3 returnRecoveryTarget;
    private float returnRecoveryTimer;
    private float returnRecoveryRetryCooldownTimer;
    private bool hasReturnRecoveryTarget;
    private int returnRecoveryAttempts;

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

        if (ShouldDespawnAsClearedEncounter())
        {
            Destroy(gameObject);
            return;
        }

        if (isPuzzleGuard)
        {
            if (guardAnchorPosition == Vector3.zero)
            {
                guardAnchorPosition = transform.position;
            }

            transform.position = guardAnchorPosition;
            hasGuardRoamTarget = false;
            guardRoamWaitTimer = 0f;
        }

        CreateExclamationIndicator();
        CreateFacingArrow();
        TransitionToState(EnemyState.Roaming);
        Debug.Log($"[OverworldEnemy] Initialized: {name} with {(patrolPoints != null ? patrolPoints.Length : 0)} patrol points.");
    }

    public void ConfigureAsPuzzleGuard(
        EncounterConfig config,
        string islandId,
        string encounterId,
        float restorationValue,
        Vector3 anchorPosition,
        float microRoamRadius,
        float leashRadius,
        float roamSpeedOverride,
        float chaseSpeedOverride)
    {
        encounterConfig = config;
        isPuzzleGuard = true;
        guardAnchorPosition = anchorPosition;
        puzzleGuardRoamRadius = Mathf.Max(0.25f, microRoamRadius);
        puzzleGuardLeashRadius = Mathf.Max(1f, leashRadius);
        puzzleGuardIslandId = string.IsNullOrEmpty(islandId) ? "default" : islandId;
        puzzleGuardEncounterId = encounterId;
        puzzleGuardRestorationValue = Mathf.Max(0.001f, restorationValue);
        roamSpeed = Mathf.Max(0.1f, roamSpeedOverride);
        chaseSpeed = Mathf.Max(roamSpeed, chaseSpeedOverride);
        patrolPoints = null;
        transform.position = guardAnchorPosition;
    }

    private void Update()
    {
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
            case EnemyState.Alert:
                UpdateAlert();
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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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
            case EnemyState.Alert:
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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
            case EnemyState.Alert:
                stateTimer = 0.5f;
                SetExclamationVisible(true);
                break;
            case EnemyState.Chasing:
                SetExclamationVisible(true);
                break;
            case EnemyState.Returning:
                SetExclamationVisible(false);
                ResetReturnRecoveryTracking();

                if (isPuzzleGuard)
                {
                    hasGuardRoamTarget = false;
                    guardRoamWaitTimer = 0f;
                    break;
                }

                patrolIndex = FindNearestPatrolIndex();
                break;
        }

        Debug.Log($"[OverworldEnemy] {name}: {previousState} -> {newState}");
    }

    // ========== ROAMING ==========

    private void UpdateRoaming()
    {
        if (playerTransform != null && CanSeePlayer())
        {
            TransitionToState(EnemyState.Alert);
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
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float distance = GetPlanarDistance(transform.position, target.position);

        if (distance <= arrivalThreshold)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (stateTimer <= 0f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                stateTimer = waitTimeAtPoint;
            }

            return;
        }

        MoveToward(target.position, roamSpeed);
    }

    // ========== ALERT ==========

    private void UpdateAlert()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            TransitionToState(EnemyState.Chasing);
        }
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
    }

    private void FixedChasing()
    {
        if (playerTransform == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        MoveToward(playerTransform.position, chaseSpeed);
    }

    // ========== RETURNING ==========

    private void UpdateReturning()
    {
        UpdateReturnRecoveryTimer();

        if (isPuzzleGuard)
        {
            EvaluateReturningStuck(guardAnchorPosition);

            float distanceToAnchor = GetPlanarDistance(transform.position, guardAnchorPosition);
            if (distanceToAnchor <= arrivalThreshold)
            {
                TransitionToState(EnemyState.Roaming);
            }
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

        EvaluateReturningStuck(target.position);

        float distance = GetPlanarDistance(transform.position, target.position);
        if (distance <= arrivalThreshold)
        {
            TransitionToState(EnemyState.Roaming);
        }
    }

    private void FixedReturning()
    {
        if (hasReturnRecoveryTarget)
        {
            MoveToward(returnRecoveryTarget, roamSpeed * Mathf.Max(1f, recoverySpeedMultiplier));

            float recoveryDistance = GetPlanarDistance(transform.position, returnRecoveryTarget);
            if (recoveryDistance <= Mathf.Max(arrivalThreshold, 0.2f))
            {
                hasReturnRecoveryTarget = false;
                returnRecoveryTimer = 0f;
            }

            return;
        }

        if (isPuzzleGuard)
        {
            MoveToward(guardAnchorPosition, roamSpeed);
            return;
        }

        if (!HasPatrolPoints())
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        if (target == null)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        MoveToward(target.position, roamSpeed);
    }

    // ========== VISION ==========

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        if (distance > visionRange) return false;

        if (distance < 0.01f) return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        float angle = Vector3.Angle(forward, toPlayer.normalized);
        if (angle > visionHalfAngle) return false;

        Vector3 eyePos = transform.position + Vector3.up * eyeHeight;
        Vector3 playerCenter = playerTransform.position + Vector3.up * eyeHeight;
        Vector3 direction = playerCenter - eyePos;

        if (Physics.Raycast(eyePos, direction.normalized, distance, visionBlockMask))
        {
            return false;
        }

        return true;
    }

    // ========== MOVEMENT ==========

    private void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        direction.Normalize();
        rb.linearVelocity = new Vector3(direction.x * speed, rb.linearVelocity.y, direction.z * speed);

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 10f);
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

            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            if (vel.sqrMagnitude > 0.01f)
            {
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
        exclamationRenderer.color = alertIndicatorColor;
        exclamationRenderer.shadowCastingMode = ShadowCastingMode.Off;
        exclamationRenderer.receiveShadows = false;
        exclamationRenderer.sortingOrder = 10;

        exclamationObject.SetActive(false);
    }

    private void CreateFacingArrow()
    {
        facingArrowObject = new GameObject("FacingArrow");
        facingArrowObject.transform.SetParent(transform, false);
        facingArrowObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        facingArrowObject.transform.localScale = new Vector3(arrowScale, arrowScale, 1f);

        arrowRenderer = facingArrowObject.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = CreateArrowSprite();
        arrowRenderer.color = enemyColor;
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
                    returnPosition);
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

    private void UpdatePuzzleGuardRoamState()
    {
        if (guardRoamWaitTimer > 0f)
        {
            guardRoamWaitTimer -= Time.deltaTime;
            return;
        }

        if (!hasGuardRoamTarget)
        {
            AssignNewGuardRoamTarget();
            return;
        }

        float distance = GetPlanarDistance(transform.position, guardRoamTarget);
        if (distance <= arrivalThreshold)
        {
            guardRoamWaitTimer = Mathf.Max(0.25f, waitTimeAtPoint * 0.8f);
            AssignNewGuardRoamTarget();
        }
    }

    private void FixedPuzzleGuardRoam()
    {
        if (!hasGuardRoamTarget || guardRoamWaitTimer > 0f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        MoveToward(guardRoamTarget, roamSpeed);
    }

    private void AssignNewGuardRoamTarget()
    {
        Vector2 offset = Random.insideUnitCircle * puzzleGuardRoamRadius;
        guardRoamTarget = new Vector3(
            guardAnchorPosition.x + offset.x,
            transform.position.y,
            guardAnchorPosition.z + offset.y);
        hasGuardRoamTarget = true;
    }

    private void ResetReturnRecoveryTracking()
    {
        returnLastPlanarPosition = GetPlanarPosition(transform.position);
        returnStuckCheckTimer = 0f;
        returnStuckTrackingInitialized = true;
        returnRecoveryTimer = 0f;
        returnRecoveryRetryCooldownTimer = 0f;
        hasReturnRecoveryTarget = false;
        returnRecoveryAttempts = 0;
    }

    private void UpdateReturnRecoveryTimer()
    {
        if (!hasReturnRecoveryTarget)
        {
            if (returnRecoveryRetryCooldownTimer > 0f)
            {
                returnRecoveryRetryCooldownTimer -= Time.deltaTime;
            }

            return;
        }

        returnRecoveryTimer -= Time.deltaTime;
        if (returnRecoveryTimer <= 0f)
        {
            hasReturnRecoveryTarget = false;
            returnRecoveryTimer = 0f;
        }
    }

    private void EvaluateReturningStuck(Vector3 finalTarget)
    {
        if (!returnStuckTrackingInitialized)
        {
            returnLastPlanarPosition = GetPlanarPosition(transform.position);
            returnStuckTrackingInitialized = true;
        }

        if (hasReturnRecoveryTarget)
        {
            return;
        }

        if (returnRecoveryRetryCooldownTimer > 0f)
        {
            return;
        }

        float interval = Mathf.Max(0.1f, stuckCheckInterval);
        returnStuckCheckTimer += Time.deltaTime;
        if (returnStuckCheckTimer < interval)
        {
            return;
        }

        Vector2 currentPlanarPosition = GetPlanarPosition(transform.position);
        float movedDistance = Vector2.Distance(currentPlanarPosition, returnLastPlanarPosition);
        float distanceToGoal = GetPlanarDistance(transform.position, finalTarget);

        returnLastPlanarPosition = currentPlanarPosition;
        returnStuckCheckTimer = 0f;

        if (distanceToGoal <= Mathf.Max(arrivalThreshold * 2f, 0.5f))
        {
            returnRecoveryAttempts = 0;
            return;
        }

        float expectedMovement = Mathf.Max(0.01f, roamSpeed) * interval;
        float requiredMovement = Mathf.Min(
            Mathf.Max(0.01f, minimumReturnMovement),
            Mathf.Max(0.01f, expectedMovement * 0.6f));

        if (movedDistance >= requiredMovement)
        {
            returnRecoveryAttempts = 0;
            return;
        }

        BeginReturnRecovery(finalTarget);
    }

    private void BeginReturnRecovery(Vector3 finalTarget)
    {
        Vector3 toTarget = finalTarget - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.001f)
        {
            return;
        }

        returnRecoveryAttempts++;
        float scaledBypassDistance = Mathf.Max(0.5f, recoveryBypassDistance) * (1f + Mathf.Max(0, returnRecoveryAttempts - 1) * 0.45f);

        if (returnRecoveryAttempts >= Mathf.Max(1, maxRecoveryAttemptsBeforeEscalation))
        {
            Vector3 fallbackTarget = transform.position + (toTarget.normalized * scaledBypassDistance);
            returnRecoveryTarget = new Vector3(fallbackTarget.x, transform.position.y, fallbackTarget.z);
            returnRecoveryTimer = Mathf.Max(0.2f, recoveryDuration * 0.8f);
            hasReturnRecoveryTarget = true;
            returnRecoveryRetryCooldownTimer = Mathf.Max(0.1f, recoveryRetryCooldown);
            Debug.LogWarning($"[OverworldEnemy] {name} required repeated return recovery. Escalating direct step toward patrol target.");
            return;
        }

        Vector3 forward = toTarget.normalized;
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        float sideSign = Random.value < 0.5f ? -1f : 1f;
        float bypassDistance = scaledBypassDistance;
        Vector3 candidate = transform.position + (side * sideSign * bypassDistance) + (forward * (bypassDistance * 0.5f));

        if (isPuzzleGuard)
        {
            Vector3 fromAnchor = candidate - guardAnchorPosition;
            fromAnchor.y = 0f;
            float maxLeash = Mathf.Max(1f, puzzleGuardLeashRadius * 0.9f);
            if (fromAnchor.magnitude > maxLeash)
            {
                candidate = guardAnchorPosition + fromAnchor.normalized * maxLeash;
            }
        }

        returnRecoveryTarget = new Vector3(candidate.x, transform.position.y, candidate.z);
        returnRecoveryTimer = Mathf.Max(0.2f, recoveryDuration);
        returnRecoveryRetryCooldownTimer = Mathf.Max(0.1f, recoveryRetryCooldown);
        hasReturnRecoveryTarget = true;
        Debug.Log($"[OverworldEnemy] {name} detected as stuck while returning. Applying bypass movement.");
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

    private static Vector2 GetPlanarPosition(Vector3 position)
    {
        return new Vector2(position.x, position.z);
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
        if (isPuzzleGuard)
        {
            return string.IsNullOrEmpty(puzzleGuardIslandId) ? "default" : puzzleGuardIslandId;
        }

        return string.IsNullOrEmpty(islandId) ? "default" : islandId;
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

        // Vision cone
        Gizmos.color = currentState == EnemyState.Alert || currentState == EnemyState.Chasing
            ? Color.red
            : Color.yellow;

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 forward = Application.isPlaying ? transform.forward : Vector3.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 leftBound = Quaternion.Euler(0f, -visionHalfAngle, 0f) * forward;
        Vector3 rightBound = Quaternion.Euler(0f, visionHalfAngle, 0f) * forward;

        Gizmos.DrawRay(origin, leftBound * visionRange);
        Gizmos.DrawRay(origin, rightBound * visionRange);
        Gizmos.DrawWireSphere(origin, visionRange);

    }
#endif
}
