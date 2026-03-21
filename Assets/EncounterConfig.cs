using UnityEngine;

[CreateAssetMenu(fileName = "EncounterConfig", menuName = "TIDE/Encounter Config")]
public class EncounterConfig : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key for lookup (e.g. encounter_imp_trio).")]
    public string encounterId;

    [Tooltip("Human-readable display name (e.g. Imp Trio).")]
    public string displayName;

    [Header("Enemies")]
    [Tooltip("Up to 3 enemy data assets that compose this encounter.")]
    public EnemyData[] enemies = System.Array.Empty<EnemyData>();

    public int EnemyCount => enemies != null ? enemies.Length : 0;

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(encounterId))
        {
            return false;
        }

        if (enemies == null || enemies.Length == 0)
        {
            return false;
        }

        if (enemies.Length > 3)
        {
            Debug.LogWarning($"[EncounterConfig] '{encounterId}' has {enemies.Length} enemies but max is 3.");
            return false;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null || !enemies[i].IsValid())
            {
                return false;
            }
        }

        return true;
    }

    public EnemyData GetEnemy(int index)
    {
        if (enemies == null || index < 0 || index >= enemies.Length)
        {
            return null;
        }

        return enemies[index];
    }
}
