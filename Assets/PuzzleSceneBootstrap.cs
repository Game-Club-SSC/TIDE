using UnityEngine;

public class PuzzleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private Color groundColor = new Color(0.22f, 0.24f, 0.28f);
    [SerializeField] private Color cameraBackground = new Color(0.11f, 0.12f, 0.15f);

    private void Awake()
    {
        EnsureGameManager();
        EnsureDirectionalLight();
        EnsureGround();
        EnsurePuzzleCamera();
        EnsurePuzzleManager();
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
        if (FindFirstObjectByType<Light>() != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("Directional Light");
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private void EnsureGround()
    {
        if (GameObject.Find("PuzzleGround") != null)
        {
            return;
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "PuzzleGround";
        ground.transform.position = new Vector3(0f, -0.2f, 0f);
        ground.transform.localScale = new Vector3(2.2f, 1f, 2.2f);

        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.material.color = groundColor;
        }
    }

    private void EnsurePuzzleCamera()
    {
        if (Camera.main != null)
        {
            ConfigureCamera(Camera.main);
            return;
        }

        GameObject cameraObject = new GameObject("Puzzle Camera");
        cameraObject.tag = "MainCamera";

        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        ConfigureCamera(cameraComponent);
    }

    private void ConfigureCamera(Camera cameraComponent)
    {
        cameraComponent.transform.position = new Vector3(0f, 14f, 0f);
        cameraComponent.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cameraComponent.orthographic = true;
        cameraComponent.orthographicSize = 4.5f;
        cameraComponent.nearClipPlane = 0.1f;
        cameraComponent.farClipPlane = 100f;
        cameraComponent.clearFlags = CameraClearFlags.SolidColor;
        cameraComponent.backgroundColor = cameraBackground;
    }

    private void EnsurePuzzleManager()
    {
        if (GetComponent<TideManager>() == null)
        {
            gameObject.AddComponent<TideManager>();
        }
    }
}
