using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Renderer))]
public class PuzzleBoxInteractable : MonoBehaviour
{
    private const string DefaultPromptResourceName = "PuzzlePrompt";
    private const float DefaultPromptPixelsPerUnit = 360f;

    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(0.72f, 0.72f, 1f);
    [SerializeField] private Sprite promptSprite;
    [SerializeField] private Color promptTint = Color.white;

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3.25f, 2.25f, 3.25f);
    [SerializeField] private Color boxColor = new Color(1f, 0.45f, 0.12f);

    [Header("Puzzle Layout")]
    [Tooltip("Puzzle data asset. Preferred over raw values.")]
    [SerializeField] private PuzzleData puzzleData;

    [Tooltip("Legacy: 3x3 grid of Tide values in row-major order. Used if Puzzle Data is not assigned.")]
    [SerializeField] private int[] puzzleValues;
    [SerializeField] private int sealedRow = 1;
    [SerializeField] private int sealedCol = 1;

    [Header("Restoration")]
    [Tooltip("Island ID this puzzle belongs to. Leave empty if not part of island restoration.")]
    [SerializeField] private string islandId = "";
    [Tooltip("Unique encounter ID for this puzzle within the island.")]
    [SerializeField] private string encounterId = "";
    [Tooltip("Stable runtime identifier for this puzzle box. Falls back to encounterId when empty.")]
    [SerializeField] private string puzzleBoxId = "";
    [Range(0f, 1f)]
    [Tooltip("Restoration contribution when puzzle is solved (0-1).")]
    [SerializeField] private float restorationValue = 0.2f;

    [Header("Locked Tile Combat")]
    [SerializeField] private float sealedTileCombatRestorationValue = 0.001f;

    private BoxCollider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;
    private Sprite runtimePromptSprite;
    private bool thisBoxSolved;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<BoxCollider>();
        interactionTrigger.isTrigger = true;
        interactionTrigger.size = triggerSize;

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplyBoxColor();
        SetPromptVisible(false);

        if (GameStateManager.Instance != null && GameStateManager.Instance.IsPuzzleBoxSolved(GetPuzzleBoxId()))
        {
            MarkSolved();
        }
    }

    private void Update()
    {
        if (thisBoxSolved) return;

        UpdatePromptFacing();

        GameStateManager gsm = GameStateManager.Instance;

        bool canInteract = playerInRange &&
                           gsm != null &&
                           gsm.CanEnterPuzzle();

        SetPromptVisible(canInteract);

        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        PuzzleOverlayController overlayController = FindFirstObjectByType<PuzzleOverlayController>();
        if (overlayController == null)
        {
            GameObject overlayObject = new GameObject("PuzzleOverlayController");
            overlayController = overlayObject.AddComponent<PuzzleOverlayController>();
        }

        if (overlayController != null)
        {
            overlayController.OpenPuzzle(this);
        }
    }

    public void MarkSolved()
    {
        if (thisBoxSolved)
        {
            return;
        }

        thisBoxSolved = true;
        SetPromptVisible(false);

        if (interactionTrigger != null)
        {
            interactionTrigger.enabled = false;
        }

        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }

        // Hide mound child object if it exists
        Transform moundTransform = transform.Find("Mound for Dig (1)");
        if (moundTransform != null)
        {
            moundTransform.gameObject.SetActive(false);
            Debug.Log("[PuzzleBoxInteractable] Mound hidden after puzzle completion.");
        }

        gameObject.SetActive(false);

        GameStateManager gsm = GameStateManager.Instance;
        if (gsm != null)
        {
            gsm.MarkPuzzleBoxSolved(GetPuzzleBoxId());
        }
    }

    public string GetPuzzleBoxId()
    {
        if (!string.IsNullOrEmpty(puzzleBoxId))
        {
            return puzzleBoxId;
        }

        if (!string.IsNullOrEmpty(encounterId))
        {
            return encounterId;
        }

        return name;
    }

    public PuzzleData GetPuzzleData()
    {
        return puzzleData;
    }

    public int[,] GetLegacyPuzzleLayout()
    {
        if (puzzleValues == null || puzzleValues.Length != 9)
        {
            return null;
        }

        int[,] grid = new int[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                grid[row, col] = Mathf.Clamp(puzzleValues[row * 3 + col], 1, 10);
            }
        }

        return grid;
    }

    public Vector2Int GetLegacySealedPosition()
    {
        return new Vector2Int(Mathf.Clamp(sealedCol, -1, 2), Mathf.Clamp(sealedRow, -1, 2));
    }

    public string GetIslandId()
    {
        return islandId;
    }

    public string GetEncounterId()
    {
        return encounterId;
    }

    public float GetRestorationValue()
    {
        return restorationValue;
    }

    public float GetSealedTileCombatRestorationValue()
    {
        return Mathf.Max(0.001f, sealedTileCombatRestorationValue);
    }

    public bool TryGetLockedTileInfo(out Vector2Int lockedTilePosition, out string lockedTileEncounterId, out string lockedTileIslandId)
    {
        lockedTilePosition = new Vector2Int(-1, -1);
        lockedTileEncounterId = string.Empty;
        lockedTileIslandId = islandId;

        if (puzzleData != null)
        {
            if (!puzzleData.HasLockedTile)
            {
                return false;
            }

            lockedTilePosition = puzzleData.lockedPosition;
            lockedTileEncounterId = puzzleData.lockedTileEncounterId;
            if (!string.IsNullOrEmpty(puzzleData.lockedTileIslandId))
            {
                lockedTileIslandId = puzzleData.lockedTileIslandId;
            }

            if (string.IsNullOrEmpty(lockedTileEncounterId))
            {
                string scopedEncounter = !string.IsNullOrEmpty(encounterId) ? encounterId : GetPuzzleBoxId();
                lockedTileEncounterId = $"{scopedEncounter}_sealed_{lockedTilePosition.x}_{lockedTilePosition.y}_guard";
            }

            return true;
        }

        Vector2Int legacyLockedPosition = GetLegacySealedPosition();
        if (legacyLockedPosition.x < 0 || legacyLockedPosition.y < 0)
        {
            return false;
        }

        lockedTilePosition = legacyLockedPosition;
        string legacyScope = !string.IsNullOrEmpty(encounterId) ? encounterId : GetPuzzleBoxId();
        lockedTileEncounterId = $"{legacyScope}_sealed_{legacyLockedPosition.x}_{legacyLockedPosition.y}_guard";
        return true;
    }

    public Vector3 GetOverlayBoardCenterWorldPosition()
    {
        return transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void ApplyBoxColor()
    {
        if (cachedRenderer != null)
        {
            cachedRenderer.material.color = boxColor;
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
        promptRoot = new GameObject("ExaminePrompt");
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

        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning($"{nameof(PuzzleBoxInteractable)} on {name} could not find a prompt sprite. Assign one in the inspector or add a PNG named {DefaultPromptResourceName} under Assets/Resources.", this);
        }
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null || Camera.main == null)
        {
            return;
        }
        Vector3 facingDirection = promptRoot.transform.position - Camera.main.transform.position;
        if (facingDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        promptRoot.transform.rotation = Quaternion.LookRotation(facingDirection.normalized, Camera.main.transform.up);
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
    }
}
