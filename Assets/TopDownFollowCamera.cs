using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class TopDownFollowCamera : MonoBehaviour
{
    private enum FollowPlane
    {
        XY = 0,
        XZ = 1
    }

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindTarget = true;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private float targetSearchInterval = 0.5f;

    [Header("Follow")]
    [SerializeField] private FollowPlane followPlane = FollowPlane.XZ;
    [SerializeField] private bool preserveInitialOffset = true;
    [SerializeField] private Vector2 followOffset = Vector2.zero;
    [SerializeField] private bool snapOnStart = true;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float maxFollowSpeed = 100f;
    [SerializeField] private float snapDistance = 8f;

    [Header("Bounds")]
    [SerializeField] private bool constrainToBounds;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private Quaternion fixedRotation;
    private float fixedOrthogonalAxis;
    private Vector2 currentVelocity;
    private bool offsetInitialized;
    private Vector2 cachedDefaultOffset;
    private bool hasCachedDefaultOffset;
    private float nextTargetSearchTime;

    private void Awake()
    {
        fixedRotation = transform.rotation;
        fixedOrthogonalAxis = GetOrthogonalAxis(transform.position);
    }

    private void Start()
    {
        TryResolveTarget();

        if (target != null && snapOnStart)
        {
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        transform.rotation = fixedRotation;

        if (!EnsureTarget())
        {
            return;
        }

        Vector3 desiredPosition = BuildDesiredPosition();
        float planarDistance = Vector2.Distance(GetPlanarPosition(transform.position), GetPlanarPosition(desiredPosition));

        if (snapDistance > 0f && planarDistance >= snapDistance)
        {
            ApplyPosition(desiredPosition);
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 currentPlanar = GetPlanarPosition(transform.position);
        Vector2 desiredPlanar = GetPlanarPosition(desiredPosition);
        float appliedMaxSpeed = maxFollowSpeed <= 0f ? Mathf.Infinity : maxFollowSpeed;

        Vector2 nextPlanar = smoothTime <= 0f
            ? desiredPlanar
            : Vector2.SmoothDamp(
                currentPlanar,
                desiredPlanar,
                ref currentVelocity,
                smoothTime,
                appliedMaxSpeed,
                Time.deltaTime);

        ApplyPlanarPosition(nextPlanar);
    }

    public void SetTarget(Transform newTarget, bool snapImmediately = false)
    {
        target = newTarget;
        offsetInitialized = false;
        currentVelocity = Vector2.zero;

        if (target == null)
        {
            return;
        }

        InitializeOffset();

        if (snapImmediately)
        {
            SnapToTarget();
        }
    }

    public void SnapToCurrentTarget()
    {
        if (!EnsureTarget())
        {
            return;
        }

        SnapToTarget();
    }

    private bool EnsureTarget()
    {
        if (target != null)
        {
            InitializeOffset();
            return true;
        }

        return TryResolveTarget();
    }

    private bool TryResolveTarget()
    {
        if (!autoFindTarget || Time.unscaledTime < nextTargetSearchTime)
        {
            return target != null;
        }

        nextTargetSearchTime = Time.unscaledTime + Mathf.Max(0.1f, targetSearchInterval);

        if (!string.IsNullOrWhiteSpace(targetTag))
        {
            GameObject taggedTarget = GameObject.FindGameObjectWithTag(targetTag);
            if (taggedTarget != null)
            {
                SetTarget(taggedTarget.transform, snapOnStart);
                return true;
            }
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        if (player != null)
        {
            SetTarget(player.transform, snapOnStart);
            return true;
        }

        return false;
    }

    private void InitializeOffset()
    {
        if (offsetInitialized || target == null)
        {
            return;
        }

        if (preserveInitialOffset)
        {
            if (hasCachedDefaultOffset)
            {
                followOffset = cachedDefaultOffset;
            }
            else
            {
                followOffset = GetPlanarPosition(transform.position) - GetPlanarPosition(target.position);
                cachedDefaultOffset = followOffset;
                hasCachedDefaultOffset = true;
            }
        }

        offsetInitialized = true;
    }

    private Vector3 BuildDesiredPosition()
    {
        Vector2 targetPlanar = GetPlanarPosition(target.position) + followOffset;
        targetPlanar = ClampToBounds(targetPlanar);
        return ComposeWorldPosition(targetPlanar, fixedOrthogonalAxis);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        ApplyPosition(BuildDesiredPosition());
        currentVelocity = Vector2.zero;
    }

    private void ApplyPosition(Vector3 position)
    {
        transform.position = position;
        transform.rotation = fixedRotation;
    }

    private void ApplyPlanarPosition(Vector2 planarPosition)
    {
        Vector2 clampedPlanar = ClampToBounds(planarPosition);
        ApplyPosition(ComposeWorldPosition(clampedPlanar, fixedOrthogonalAxis));
    }

    private Vector2 ClampToBounds(Vector2 planarPosition)
    {
        if (!constrainToBounds)
        {
            return planarPosition;
        }

        Vector2 lowerBounds = Vector2.Min(minBounds, maxBounds);
        Vector2 upperBounds = Vector2.Max(minBounds, maxBounds);

        return new Vector2(
            Mathf.Clamp(planarPosition.x, lowerBounds.x, upperBounds.x),
            Mathf.Clamp(planarPosition.y, lowerBounds.y, upperBounds.y));
    }

    private Vector2 GetPlanarPosition(Vector3 worldPosition)
    {
        return followPlane == FollowPlane.XY
            ? new Vector2(worldPosition.x, worldPosition.y)
            : new Vector2(worldPosition.x, worldPosition.z);
    }

    private float GetOrthogonalAxis(Vector3 worldPosition)
    {
        return followPlane == FollowPlane.XY ? worldPosition.z : worldPosition.y;
    }

    private Vector3 ComposeWorldPosition(Vector2 planarPosition, float orthogonalAxis)
    {
        return followPlane == FollowPlane.XY
            ? new Vector3(planarPosition.x, planarPosition.y, orthogonalAxis)
            : new Vector3(planarPosition.x, orthogonalAxis, planarPosition.y);
    }

    public void ResetToDefaultOffset()
    {
        if (hasCachedDefaultOffset)
        {
            followOffset = cachedDefaultOffset;
            offsetInitialized = true;
            currentVelocity = Vector2.zero;
        }
    }

    public void CaptureCurrentOffsetAsDefault()
    {
        if (!EnsureTarget())
        {
            return;
        }

        followOffset = GetPlanarPosition(transform.position) - GetPlanarPosition(target.position);
        cachedDefaultOffset = followOffset;
        hasCachedDefaultOffset = true;
        offsetInitialized = true;
        currentVelocity = Vector2.zero;
    }
}
