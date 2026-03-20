using UnityEngine;

[CreateAssetMenu(fileName = "HeroDatabase", menuName = "TIDE/Hero Database")]
public class HeroDatabase : ScriptableObject
{
    [Header("All Heroes")]
    [Tooltip("All 5 playable heroes. Must contain exactly 5 entries.")]
    public HeroData[] allHeroes = new HeroData[5];

    [Header("Default Party")]
    [Tooltip("Default party configuration for new games.")]
    public PartyData defaultParty;

    public HeroData GetHero(string heroId)
    {
        if (string.IsNullOrEmpty(heroId) || allHeroes == null)
        {
            return null;
        }

        for (int i = 0; i < allHeroes.Length; i++)
        {
            if (allHeroes[i] != null && allHeroes[i].heroId == heroId)
            {
                return allHeroes[i];
            }
        }

        return null;
    }

    public HeroData[] GetAllHeroes()
    {
        if (allHeroes == null)
        {
            return System.Array.Empty<HeroData>();
        }

        return allHeroes;
    }

    public bool IsValid()
    {
        if (allHeroes == null || allHeroes.Length != 5)
        {
            Debug.LogError("[HeroDatabase] Must contain exactly 5 hero entries.");
            return false;
        }

        for (int i = 0; i < allHeroes.Length; i++)
        {
            if (allHeroes[i] == null)
            {
                Debug.LogError($"[HeroDatabase] Hero slot {i} is null.");
                return false;
            }

            if (!allHeroes[i].IsValid())
            {
                Debug.LogError($"[HeroDatabase] Hero '{allHeroes[i].heroId}' failed validation.");
                return false;
            }
        }

        if (defaultParty == null)
        {
            Debug.LogError("[HeroDatabase] Default party is not assigned.");
            return false;
        }

        if (!defaultParty.IsValid())
        {
            Debug.LogError("[HeroDatabase] Default party failed validation.");
            return false;
        }

        return true;
    }
}
