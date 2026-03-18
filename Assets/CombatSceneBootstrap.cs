using UnityEngine;

public class CombatSceneBootstrap : MonoBehaviour
{
    [Header("Environment")]
    [SerializeField] private Color battlefieldColor = new Color(0.23f, 0.26f, 0.31f);
    [SerializeField] private Color cameraBackground = new Color(0.1f, 0.12f, 0.16f);
    [SerializeField] private Color playerMarkerColor = new Color(0.21f, 0.73f, 0.84f);
    [SerializeField] private Color enemyMarkerColor = new Color(0.89f, 0.38f, 0.25f);
    [SerializeField] private float lightIntensity = 1.15f;

    [Header("Layout")]
    [SerializeField] private float playerSideX = -4f;
    [SerializeField] private float enemySideX = 4f;
    [SerializeField] private float slotSpacing = 2f;
    [SerializeField] private Vector3 battlefieldScale = new Vector3(1.6f, 1f, 1.2f);

    [Header("Camera")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 8f, -10f);
    [SerializeField] private Vector3 cameraLookTarget = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float cameraFieldOfView = 45f;

    [Header("Markers")]
    [SerializeField] private Vector3 markerScale = new Vector3(0.75f, 0.08f, 0.75f);
    [SerializeField] private float markerYOffset = 0.08f;

    private void Awake()
    {
        EnsureGameManager();
        EnsureDirectionalLight();
        EnsureBattlefield();
        EnsureCombatCamera();
        EnsureSpawnPoints();
    }

    private void EnsureGameManager()
    {
        if (GameStateManager.Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("GameManager");
        managerObject.AddComponent<GameStateManager>();
    }

    private void EnsureDirectionalLight()
    {
        Light directionalLight = null;
        bool createdLight = false;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                directionalLight = lights[i];
                break;
            }
        }

        if (directionalLight == null)
        {
            Transform lightTransform = GetOrCreateChild(transform, "Directional Light");
            directionalLight = lightTransform.GetComponent<Light>();
            if (directionalLight == null)
            {
                directionalLight = lightTransform.gameObject.AddComponent<Light>();
            }

            createdLight = true;
        }

        directionalLight.type = LightType.Directional;
        directionalLight.enabled = true;
        directionalLight.intensity = lightIntensity;
        if (createdLight)
        {
            directionalLight.transform.SetParent(transform, true);
        }

        directionalLight.transform.position = new Vector3(0f, 8f, 0f);
        directionalLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
    }

    private void EnsureBattlefield()
    {
        Transform environmentRoot = GetOrCreateChild(transform, "Environment");
        Transform groundTransform = environmentRoot.Find("CombatGround");

        if (groundTransform == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "CombatGround";
            groundTransform = ground.transform;
            groundTransform.SetParent(environmentRoot, false);
        }

        groundTransform.localPosition = Vector3.zero;
        groundTransform.localRotation = Quaternion.identity;
        groundTransform.localScale = battlefieldScale;

        Renderer groundRenderer = groundTransform.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.material.color = battlefieldColor;
        }
    }

    private void EnsureCombatCamera()
    {
        Camera combatCamera = Camera.main;
        if (combatCamera == null)
        {
            combatCamera = FindFirstObjectByType<Camera>();
        }

        if (combatCamera == null)
        {
            Transform cameraTransform = GetOrCreateChild(transform, "Combat Camera");
            cameraTransform.gameObject.tag = "MainCamera";

            combatCamera = cameraTransform.GetComponent<Camera>();
            if (combatCamera == null)
            {
                combatCamera = cameraTransform.gameObject.AddComponent<Camera>();
            }

            if (cameraTransform.GetComponent<AudioListener>() == null)
            {
                cameraTransform.gameObject.AddComponent<AudioListener>();
            }
        }
        else if (!combatCamera.CompareTag("MainCamera"))
        {
            combatCamera.tag = "MainCamera";
        }

        if (combatCamera.GetComponent<AudioListener>() == null)
        {
            combatCamera.gameObject.AddComponent<AudioListener>();
        }

        ConfigureCamera(combatCamera);
    }

    private void ConfigureCamera(Camera combatCamera)
    {
        combatCamera.enabled = true;
        combatCamera.transform.position = cameraPosition;
        combatCamera.transform.rotation = Quaternion.LookRotation(cameraLookTarget - cameraPosition);
        combatCamera.orthographic = false;
        combatCamera.fieldOfView = cameraFieldOfView;
        combatCamera.nearClipPlane = 0.1f;
        combatCamera.farClipPlane = 100f;
        combatCamera.clearFlags = CameraClearFlags.SolidColor;
        combatCamera.backgroundColor = cameraBackground;
    }

    private void EnsureSpawnPoints()
    {
        Transform playerRoot = GetOrCreateChild(transform, "PlayerSpawnPoints");
        Transform enemyRoot = GetOrCreateChild(transform, "EnemySpawnPoints");

        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            float zOffset = (slotIndex - 1) * slotSpacing;
            EnsureSlot(playerRoot, $"PlayerSlot_{slotIndex + 1}", new Vector3(playerSideX, 0f, zOffset), playerMarkerColor);
            EnsureSlot(enemyRoot, $"EnemySlot_{slotIndex + 1}", new Vector3(enemySideX, 0f, zOffset), enemyMarkerColor);
        }
    }

    private void EnsureSlot(Transform parent, string slotName, Vector3 localPosition, Color markerColor)
    {
        Transform slotTransform = parent.Find(slotName);
        if (slotTransform == null)
        {
            GameObject slotObject = new GameObject(slotName);
            slotTransform = slotObject.transform;
            slotTransform.SetParent(parent, false);
        }

        slotTransform.localPosition = localPosition;
        slotTransform.localRotation = Quaternion.identity;
        slotTransform.localScale = Vector3.one;

        EnsureSlotMarker(slotTransform, markerColor);
    }

    private void EnsureSlotMarker(Transform slotTransform, Color markerColor)
    {
        Transform markerTransform = slotTransform.Find("Marker");
        if (markerTransform == null)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            markerTransform = marker.transform;
            markerTransform.SetParent(slotTransform, false);
        }

        markerTransform.localPosition = new Vector3(0f, markerYOffset, 0f);
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = markerScale;

        Renderer markerRenderer = markerTransform.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.material.color = markerColor;
        }
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform childTransform = parent.Find(childName);
        if (childTransform != null)
        {
            return childTransform;
        }

        GameObject childObject = new GameObject(childName);
        childTransform = childObject.transform;
        childTransform.SetParent(parent, false);
        return childTransform;
    }
}
