using System;
using UnityEngine;

[DisallowMultipleComponent]
public class WorldSaveService : MonoBehaviour
{
    public static WorldSaveService Instance { get; private set; }

    [SerializeField] private string playerPrefsKey = DefaultPlayerPrefsKey;
    [SerializeField] private bool enablePersistentSaveData = true;
    [SerializeField] private int saveSchemaVersion = SaveJsonValidator.CurrentSchemaVersion;
    [SerializeField] private int maxRetryAttempts = 2;

    private const string DefaultPlayerPrefsKey = "TIDE_WORLD_STATE_V2";
    private const string BackupKeySuffix = "_backup";

    public string PlayerPrefsKey => string.IsNullOrWhiteSpace(playerPrefsKey) ? DefaultPlayerPrefsKey : playerPrefsKey;
    public bool EnablePersistentSaveData => enablePersistentSaveData;
    public bool HasPersistedData => PlayerPrefs.HasKey(PlayerPrefsKey) || PlayerPrefs.HasKey(PlayerPrefsKey + BackupKeySuffix);
    public int SaveSchemaVersion => Mathf.Clamp(saveSchemaVersion, 1, SaveJsonValidator.CurrentSchemaVersion);

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
        if (Instance == this) Instance = null;
    }

    public bool TryWriteJson(string json)
    {
        if (!enablePersistentSaveData || !ValidateSaveJson(json, out int sourceVersion)) return false;

        string existing = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (SaveJsonValidator.TryValidateWorldState(existing, int.MaxValue, out int existingVersion)
            && existingVersion > SaveSchemaVersion)
        {
            Debug.LogWarning("[WorldSaveService] A newer save schema is present; preserving it.");
            OnSaveFailed?.Invoke(PlayerPrefsKey);
            return false;
        }

        string versionedJson = sourceVersion == 0 ? InjectSchemaVersion(json) : json;
        string backupKey = PlayerPrefsKey + BackupKeySuffix;

        // Preserve the previous validated generation once, before touching the
        // primary. Retries must never copy an unverified write over the backup,
        // and identical autosaves must retain the previous distinct generation.
        string previousBackup = PlayerPrefs.GetString(backupKey, string.Empty);
        bool needsBackup = !string.Equals(existing, versionedJson, StringComparison.Ordinal)
            || !ValidateSaveJson(previousBackup, out _);
        if (ValidateSaveJson(existing, out _) && needsBackup && !TryStore(backupKey, existing))
        {
            OnSaveFailed?.Invoke(PlayerPrefsKey);
            return false;
        }

        int retryCount = Mathf.Clamp(maxRetryAttempts, 0, 5);
        for (int attempt = 0; attempt <= retryCount; attempt++)
        {
            if (TryStore(PlayerPrefsKey, versionedJson))
            {
                OnSavePersisted?.Invoke(PlayerPrefsKey);
                return true;
            }
            Debug.LogWarning($"[WorldSaveService] Save attempt {attempt + 1} failed.");
        }

        string backup = PlayerPrefs.GetString(backupKey, string.Empty);
        if (ValidateSaveJson(backup, out _))
        {
            if (!TryStore(PlayerPrefsKey, backup))
            {
                Debug.LogWarning("[WorldSaveService] Primary repair failed; the validated backup remains available.");
            }
        }
        OnSaveFailed?.Invoke(PlayerPrefsKey);
        return false;
    }

    public string ReadJson()
    {
        return enablePersistentSaveData ? PlayerPrefs.GetString(PlayerPrefsKey, string.Empty) : null;
    }

    /// <summary>Checks for a usable generation without repairing storage or emitting load events.</summary>
    public bool TryPeekJson(out string json)
    {
        json = null;
        if (!enablePersistentSaveData) return false;
        string primary = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (ValidateSaveJson(primary, out _))
        {
            json = primary;
            return true;
        }
        string backup = PlayerPrefs.GetString(PlayerPrefsKey + BackupKeySuffix, string.Empty);
        if (!ValidateSaveJson(backup, out _)) return false;
        json = backup;
        return true;
    }

    public bool TryLoadJson(out string json)
    {
        if (!TryPeekJson(out json)) return false;
        string primary = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
        if (!string.Equals(primary, json, StringComparison.Ordinal))
        {
            // Missing/empty primaries recover too, but an older build may not
            // overwrite a newer schema while loading a supported older backup.
            bool futureSchema = SaveJsonValidator.TryValidateWorldState(primary, int.MaxValue, out int version)
                && version > SaveSchemaVersion;
            if (!futureSchema && !TryStore(PlayerPrefsKey, json))
            {
                Debug.LogWarning("[WorldSaveService] Loaded the backup; primary repair could not be persisted.");
            }
            Debug.Log("[WorldSaveService] Loaded the validated backup.");
        }
        OnSaveLoaded?.Invoke(PlayerPrefsKey);
        return true;
    }

    public void Clear()
    {
        string backupKey = PlayerPrefsKey + BackupKeySuffix;
        bool removedData = PlayerPrefs.HasKey(PlayerPrefsKey) || PlayerPrefs.HasKey(backupKey);
        if (!removedData) return;
        PlayerPrefs.DeleteKey(PlayerPrefsKey);
        PlayerPrefs.DeleteKey(backupKey);
        PlayerPrefs.Save();
        OnSaveCleared?.Invoke();
    }

    public void SetPersistentSaveEnabled(bool enabled) { enablePersistentSaveData = enabled; }

    private bool ValidateSaveJson(string json, out int version)
    {
        return SaveJsonValidator.TryValidateWorldState(json, SaveSchemaVersion, out version);
    }

    private bool TryStore(string key, string json)
    {
        // PlayerPrefs.SetString can throw when the platform storage quota is
        // exhausted. This is an external storage boundary, not gameplay logic.
        try
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
            return string.Equals(PlayerPrefs.GetString(key, string.Empty), json, StringComparison.Ordinal);
        }
        catch (PlayerPrefsException exception)
        {
            Debug.LogWarning($"[WorldSaveService] Save storage rejected the write: {exception.Message}");
            return false;
        }
    }

    private string InjectSchemaVersion(string json)
    {
        string trimmed = json.Trim();
        return "{\"saveSchemaVersion\":" + SaveSchemaVersion + "," + trimmed.Substring(1);
    }
}
