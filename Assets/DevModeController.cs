using UnityEngine;

[DisallowMultipleComponent]
public class DevModeController : MonoBehaviour
{
    private static readonly KeyCode[] KonamiSequence =
    {
        KeyCode.UpArrow,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.B,
        KeyCode.A
    };

    private const float SequenceTimeout = 4f;

    private int konamiIndex;
    private float konamiTimer;
    private bool isUnlocked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private DevMenuUI menuUi;
#endif

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureDependencies();
    }

    private void Update()
    {
        if (!IsAllowed())
        {
            return;
        }

        EnsureDependencies();
        HandleKonamiInput();

        if (isUnlocked && Input.GetKeyDown(KeyCode.F10))
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            menuUi?.Toggle();
#endif
        }

        if (DevCheatService.Instance != null)
        {
            DevCheatService.Instance.ApplyContinuousCheats();
        }
    }

    private void OnGUI()
    {
        if (!isUnlocked || DevCheatService.Instance == null || !DevCheatService.Instance.ShowDebugOverlay)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (menuUi != null && menuUi.IsVisible)
        {
            return;
        }
#endif

        GUI.Box(new Rect(16f, 16f, 620f, 38f), "DEV GOD MODE ACTIVE - Konami unlocked (F10 toggles menu)");
        string summary = DevCheatService.Instance.BuildDebugSummary();
        GUI.Box(new Rect(16f, 60f, 620f, 290f), summary);
    }

    private void HandleKonamiInput()
    {
        if (konamiIndex > 0)
        {
            konamiTimer -= Time.unscaledDeltaTime;
            if (konamiTimer <= 0f)
            {
                konamiIndex = 0;
                konamiTimer = 0f;
            }
        }

        if (!TryReadKonamiKeyDown(out KeyCode pressedKey))
        {
            return;
        }

        AdvanceKonamiSequence(pressedKey);
    }

    private void AdvanceKonamiSequence(KeyCode key)
    {
        if (konamiIndex < KonamiSequence.Length && KonamiSequence[konamiIndex] == key)
        {
            konamiIndex++;
            konamiTimer = SequenceTimeout;
        }
        else if (KonamiSequence[0] == key)
        {
            konamiIndex = 1;
            konamiTimer = SequenceTimeout;
        }
        else
        {
            konamiIndex = 0;
            konamiTimer = 0f;
        }

        if (konamiIndex < KonamiSequence.Length)
        {
            return;
        }

        konamiIndex = 0;
        isUnlocked = !isUnlocked;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (menuUi != null)
        {
            menuUi.SetVisible(isUnlocked);
        }
#endif
    }

    private static bool TryReadKonamiKeyDown(out KeyCode pressedKey)
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            pressedKey = KeyCode.UpArrow;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            pressedKey = KeyCode.DownArrow;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            pressedKey = KeyCode.LeftArrow;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            pressedKey = KeyCode.RightArrow;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            pressedKey = KeyCode.B;
            return true;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            pressedKey = KeyCode.A;
            return true;
        }

        pressedKey = KeyCode.None;
        return false;
    }

    private void EnsureDependencies()
    {
        if (DevCheatService.Instance == null)
        {
            GameObject serviceObject = new GameObject("DevCheatService");
            serviceObject.AddComponent<DevCheatService>();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (menuUi == null)
        {
            menuUi = FindFirstObjectByType<DevMenuUI>();
        }

        if (menuUi == null)
        {
            GameObject menuObject = new GameObject("DevMenuUI");
            menuUi = menuObject.AddComponent<DevMenuUI>();
        }
#endif

        EnsurePhoneWebController();
    }

    private void EnsurePhoneWebController()
    {
        if (PhoneWebController.Instance != null)
        {
            // Ensure the bridge is connected to the existing server
            if (PhoneInputBridge.Instance != null)
            {
                PhoneInputBridge.Instance.ReconnectToServer();
            }
            return;
        }

        // Create the input bridge first (so it exists when the server starts)
        if (PhoneInputBridge.Instance == null)
        {
            GameObject bridgeObject = new GameObject("PhoneInputBridge");
            bridgeObject.AddComponent<PhoneInputBridge>();
        }

        // Then create the server
        GameObject phoneControllerObject = new GameObject("PhoneWebController");
        phoneControllerObject.AddComponent<PhoneWebController>();
    }

    private static bool IsAllowed()
    {
        GameStateManager gsm = GameStateManager.Instance;
        if (gsm == null)
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        return gsm.IsDeveloperGodModeAllowed();
    }
}
