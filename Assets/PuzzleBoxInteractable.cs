using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class PuzzleBoxInteractable : MonoBehaviour
{
    [Header("Prompt Layout")]
    [SerializeField] private Vector3 promptLocalOffset = new Vector3(1.45f, 1.15f, 0f);
    [SerializeField] private Vector3 promptScale = new Vector3(1.8f, 0.85f, 1f);

    [Header("Interaction")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3.25f, 2.25f, 3.25f);
    [SerializeField] private Color boxColor = new Color(1f, 0.45f, 0.12f);

    private Collider interactionTrigger;
    private Renderer cachedRenderer;
    private GameObject promptRoot;
    private bool playerInRange;

    private void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        interactionTrigger = GetComponent<Collider>();

        if (interactionTrigger is BoxCollider triggerBox)
        {
            triggerBox.isTrigger = true;
            triggerBox.size = triggerSize;
        }

        EnsureSolidCollider();
        CreatePromptVisual();
        ApplyBoxColor();
        SetPromptVisible(false);
    }

    private void Start()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.PuzzleSolved)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && GameStateManager.Instance.PuzzleSolved)
        {
            Destroy(gameObject);
            return;
        }

        UpdatePromptFacing();

        bool canInteract = playerInRange &&
                           GameStateManager.Instance != null &&
                           GameStateManager.Instance.CanEnterPuzzle();

        SetPromptVisible(canInteract);

        if (!canInteract)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            return;
        }

        IsometricPlayer player = FindFirstObjectByType<IsometricPlayer>();
        Vector3 returnPosition = player != null ? player.transform.position : transform.position + Vector3.back * 2f;
        GameStateManager.Instance.EnterPuzzleScene(returnPosition);
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
        solidCollider.size = Vector3.one;
        solidCollider.center = Vector3.zero;
    }

    private void CreatePromptVisual()
    {
        promptRoot = new GameObject("ExaminePrompt");
        promptRoot.transform.SetParent(transform, false);
        promptRoot.transform.localPosition = promptLocalOffset;

        GameObject accentObject = CreatePromptQuad("Accent", new Color(0.16f, 0.32f, 0.95f), new Vector3(0.1f, -0.05f, 0.03f), new Vector3(promptScale.x * 1.1f, promptScale.y, 1f));
        accentObject.transform.SetParent(promptRoot.transform, false);
        accentObject.transform.localRotation = Quaternion.Euler(0f, 0f, -9f);

        GameObject panelObject = CreatePromptQuad("Panel", Color.white, Vector3.zero, promptScale);
        panelObject.transform.SetParent(promptRoot.transform, false);

        GameObject textObject = new GameObject("PromptText");
        textObject.transform.SetParent(promptRoot.transform, false);
        textObject.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        textObject.transform.localScale = Vector3.one * 0.28f;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = "examine";
        text.fontSize = 12f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.color = Color.black;
    }

    private GameObject CreatePromptQuad(string objectName, Color color, Vector3 localPosition, Vector3 localScale)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        quad.transform.localPosition = localPosition;
        quad.transform.localScale = localScale;

        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null)
        {
            Destroy(quadCollider);
        }

        Renderer quadRenderer = quad.GetComponent<Renderer>();
        quadRenderer.material.color = color;
        quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        return quad;
    }

    private void UpdatePromptFacing()
    {
        if (promptRoot == null || Camera.main == null)
        {
            return;
        }

        promptRoot.transform.rotation = Quaternion.LookRotation(-Camera.main.transform.forward, Camera.main.transform.up);
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (promptRoot != null && promptRoot.activeSelf != isVisible)
        {
            promptRoot.SetActive(isVisible);
        }
    }
}
