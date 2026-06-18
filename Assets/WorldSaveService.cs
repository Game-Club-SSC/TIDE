using UnityEngine;

[DisallowMultipleComponent]
public class WorldSaveService : MonoBehaviour
{
    public static WorldSaveService Instance { get; private set; }

    [SerializeField] private string playerPrefsKey = "TIDE_WORLD_STATE_V1";
    [SerializeField] private bool enablePersistentSaveData = true;

    public string PlayerPrefsKey => playerPrefsKey;
    public bool EnablePersistentSaveData => enablePersistentSaveData;
    public bool HasPersistedData => PlayerPrefs.HasKey(playerPrefsKey);

    public event System.Action<string> OnSavePersisted;
    public event System.Action<string> OnSaveLoaded;
    public event System.Action OnSaveCleared;

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool TryWriteJson(string json)
    {
        if (!enablePersistentSaveData)
        {
            return false;
        }

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            PlayerPrefs.SetString(playerPrefsKey, json);
            PlayerPrefs.Save();
            OnSavePersisted?.Invoke(playerPrefsKey);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[WorldSaveService] Write failed: {ex.Message}");
            return false;
        }
    }

    public string ReadJson()
    {
        if (!HasPersistedData)
        {
            return null;
        }

        return PlayerPrefs.GetString(playerPrefsKey, string.Empty);
    }

    public bool TryLoadJson(out string json)
    {
        json = ReadJson();
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        OnSaveLoaded?.Invoke(playerPrefsKey);
        return true;
    }

    public void Clear()
    {
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            PlayerPrefs.Save();
            OnSaveCleared?.Invoke();
        }
    }

    public void SetPersistentSaveEnabled(bool enabled)
    {
        enablePersistentSaveData = enabled;
    }
}
