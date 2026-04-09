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
    private DevMenuUI menuUi;

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
            menuUi?.Toggle();
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

        for (int i = 0; i < KonamiSequence.Length; i++)
        {
            if (!Input.GetKeyDown(KonamiSequence[i]))
            {
                continue;
            }

            if (KonamiSequence[konamiIndex] == KonamiSequence[i])
            {
                konamiIndex++;
                konamiTimer = SequenceTimeout;
                if (konamiIndex >= KonamiSequence.Length)
                {
                    konamiIndex = 0;
                    isUnlocked = !isUnlocked;
                    if (menuUi != null)
                    {
                        menuUi.SetVisible(isUnlocked);
                    }
                }
            }
            else
            {
                konamiIndex = KonamiSequence[0] == KonamiSequence[i] ? 1 : 0;
                konamiTimer = konamiIndex > 0 ? SequenceTimeout : 0f;
            }
        }
    }

    private void EnsureDependencies()
    {
        if (DevCheatService.Instance == null)
        {
            GameObject serviceObject = new GameObject("DevCheatService");
            serviceObject.AddComponent<DevCheatService>();
        }

        if (menuUi == null)
        {
            menuUi = FindFirstObjectByType<DevMenuUI>();
        }

        if (menuUi == null)
        {
            GameObject menuObject = new GameObject("DevMenuUI");
            menuUi = menuObject.AddComponent<DevMenuUI>();
        }
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
