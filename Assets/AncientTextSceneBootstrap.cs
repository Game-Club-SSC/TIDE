using UnityEngine;

[DisallowMultipleComponent]
public class AncientTextSceneBootstrap : MonoBehaviour
{
    [Header("Auto Spawn")]
    [SerializeField] private bool spawnDefaultTextNode = true;
    [SerializeField] private Vector3 defaultNodePosition = new Vector3(8f, 32f, 6f);
    [SerializeField] private string defaultNodeObjectName = "AncientTextNode_IslandIntro";

    [Header("Default Content")]
    [SerializeField] private string defaultTextId = "island1_intro_fragment";
    [SerializeField] private string defaultTitle = "Fragment I: The Fifth Cycle";
    [TextArea(6, 16)]
    [SerializeField] private string defaultBody =
        "The chosen are not conquerors.\n" +
        "They are balancing hands.\n\n" +
        "When one side is lifted too high, rot begins.\n" +
        "When one side is crushed too low, hope fades.\n\n" +
        "Hold both. Do not cling to either.";

    [Header("Visual")]
    [SerializeField] private Color markerColor = new Color(0.86f, 0.75f, 0.47f, 1f);
    [SerializeField] private Vector3 markerScale = new Vector3(1.3f, 2f, 1.3f);

    private void OnEnable()
    {
        EnsureAncientTextLogUi();

        if (!spawnDefaultTextNode)
        {
            return;
        }

        if (FindNodeByName(defaultNodeObjectName) != null)
        {
            return;
        }

        SpawnDefaultNode();
    }

    private static void EnsureAncientTextLogUi()
    {
        if (FindFirstObjectByType<AncientTextLogUI>() != null)
        {
            return;
        }

        GameObject logObject = new GameObject("AncientTextLogUI");
        logObject.AddComponent<AncientTextLogUI>();
    }

    private GameObject FindNodeByName(string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            return null;
        }

        AncientTextInteractable[] nodes = FindObjectsByType<AncientTextInteractable>(FindObjectsSortMode.None);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].name == nodeName)
            {
                return nodes[i].gameObject;
            }
        }

        return null;
    }

    private void SpawnDefaultNode()
    {
        if (FindNodeByName(defaultNodeObjectName) != null)
        {
            return;
        }

        AncientTextData data = ScriptableObject.CreateInstance<AncientTextData>();
        data.textId = string.IsNullOrEmpty(defaultTextId) ? "island1_intro_fragment" : defaultTextId;
        data.title = string.IsNullOrEmpty(defaultTitle) ? "Fragment" : defaultTitle;
        data.body = defaultBody;

        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        node.name = string.IsNullOrEmpty(defaultNodeObjectName) ? "AncientTextNode" : defaultNodeObjectName;
        node.transform.position = defaultNodePosition;
        node.transform.localScale = markerScale;

        Renderer renderer = node.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = markerColor;
        }

        AncientTextInteractable interactable = node.AddComponent<AncientTextInteractable>();
        interactable.ConfigureRuntimeData(data);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterAncientText(data.textId, data.title, data.body);
        }

        Debug.Log($"[AncientTextSceneBootstrap] Spawned default ancient text node '{node.name}'.");
    }
}
