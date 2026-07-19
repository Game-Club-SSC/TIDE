using System;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldSaveService : MonoBehaviour
{
    public static WorldSaveService Instance { get; private set; }

    [SerializeField] private string playerPrefsKey = "TIDE_WORLD_STATE_V1";
    [SerializeField] private bool enablePersistentSaveData = true;
    [SerializeField] private int saveSchemaVersion = 1;
    [SerializeField] private int maxRetryAttempts = 2;

    private const string BackupKeySuffix = "_backup";
    private const string VersionFieldName = "\"saveSchemaVersion\"";

    public string PlayerPrefsKey => playerPrefsKey;
    public bool EnablePersistentSaveData => enablePersistentSaveData;
    public bool HasPersistedData => PlayerPrefs.HasKey(playerPrefsKey);
    public int SaveSchemaVersion => saveSchemaVersion;

    public event Action<string> OnSavePersisted;
    public event Action<string> OnSaveLoaded;
    public event Action OnSaveCleared;
    public event Action<string> OnSaveFailed;

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

        string versionedJson = InjectSchemaVersion(json);

        for (int attempt = 0; attempt <= maxRetryAttempts; attempt++)
        {
            if (TryWriteJsonInternal(versionedJson))
            {
                return true;
            }

            Debug.LogWarning($"[WorldSaveService] Save attempt {attempt + 1} failed, retrying...");
        }

        string backupKey = playerPrefsKey + BackupKeySuffix;
        if (PlayerPrefs.HasKey(backupKey))
        {
            Debug.Log("[WorldSaveService] Restoring backup after write failure.");
            string backup = PlayerPrefs.GetString(backupKey, string.Empty);
            if (!string.IsNullOrEmpty(backup))
            {
                PlayerPrefs.SetString(playerPrefsKey, backup);
                PlayerPrefs.Save();
            }
        }

        OnSaveFailed?.Invoke(playerPrefsKey);
        return false;
    }

    private bool TryWriteJsonInternal(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[WorldSaveService] Cannot write null or empty JSON");
            return false;
        }

        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            string backupKey = playerPrefsKey + BackupKeySuffix;
            string existing = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(existing))
            {
                PlayerPrefs.SetString(backupKey, existing);
            }
        }

        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();

        string verify = PlayerPrefs.GetString(playerPrefsKey, string.Empty);
        if (!string.Equals(verify, json, System.StringComparison.Ordinal))
        {
            Debug.LogWarning("[WorldSaveService] Write verification failed — saved data does not match input.");
            return false;
        }

        OnSavePersisted?.Invoke(playerPrefsKey);
        return true;
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

        if (!ValidateSaveJson(json))
        {
            Debug.LogWarning("[WorldSaveService] Save data failed validation. Attempting backup restore.");
            string backupKey = playerPrefsKey + BackupKeySuffix;
            if (PlayerPrefs.HasKey(backupKey))
            {
                string backup = PlayerPrefs.GetString(backupKey, string.Empty);
                if (!string.IsNullOrEmpty(backup) && ValidateSaveJson(backup))
                {
                    json = backup;
                    PlayerPrefs.SetString(playerPrefsKey, backup);
                    PlayerPrefs.Save();
                    Debug.Log("[WorldSaveService] Restored valid backup.");
                }
                else
                {
                    Debug.LogError("[WorldSaveService] Backup also invalid. Save data may be corrupted.");
                    return false;
                }
            }
            else
            {
                Debug.LogError("[WorldSaveService] No backup available. Save data may be corrupted.");
                return false;
            }
        }

        OnSaveLoaded?.Invoke(playerPrefsKey);
        return true;
    }

    public void Clear()
    {
        if (PlayerPrefs.HasKey(playerPrefsKey))
        {
            PlayerPrefs.DeleteKey(playerPrefsKey);
            string backupKey = playerPrefsKey + BackupKeySuffix;
            if (PlayerPrefs.HasKey(backupKey))
            {
                PlayerPrefs.DeleteKey(backupKey);
            }
            PlayerPrefs.Save();
            OnSaveCleared?.Invoke();
        }
    }

    public void SetPersistentSaveEnabled(bool enabled)
    {
        enablePersistentSaveData = enabled;
    }

    private bool ValidateSaveJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        if (!json.Contains("{") || !json.Contains("}"))
        {
            return false;
        }

        if (!json.Contains("\"puzzleStates\""))
        {
            return false;
        }

        return true;
    }

    private string InjectSchemaVersion(string json)
    {
        if (string.IsNullOrEmpty(json) || !json.StartsWith("{"))
        {
            return json;
        }

        if (json.Contains(VersionFieldName))
        {
            return json;
        }

        return "{\"saveSchemaVersion\":" + saveSchemaVersion + "," + json.Substring(1);
    }
}
