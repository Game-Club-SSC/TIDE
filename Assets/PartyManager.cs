using UnityEngine;

[DisallowMultipleComponent]
public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }

    [Header("Party Configuration")]
    [SerializeField] private PartyData partyData;
    [SerializeField] private HeroDatabase heroDatabase;

    private CombatUnit.Element chosenMainCharacterElement = CombatUnit.Element.None;
    private bool hasChosenElement;

    public PartyData PartyData => partyData;
    public HeroDatabase HeroDatabase => heroDatabase;
    public bool HasChosenElement => hasChosenElement;

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

    private void OnDisable()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize(PartyData party, HeroDatabase database)
    {
        partyData = party;
        heroDatabase = database;
    }

    public void SetMainCharacterElement(CombatUnit.Element element)
    {
        chosenMainCharacterElement = element;
        hasChosenElement = true;
        Debug.Log($"[PartyManager] Main character element set to: {element}");
    }

    public CombatUnit.Element GetMainCharacterElement()
    {
        return hasChosenElement ? chosenMainCharacterElement : CombatUnit.Element.None;
    }

    public CombatUnit.Element ResolveElement(HeroData hero)
    {
        if (hero == null)
        {
            return CombatUnit.Element.None;
        }

        if (hero.isMainCharacter && hasChosenElement)
        {
            return chosenMainCharacterElement;
        }

        return hero.element;
    }

    public HeroData[] GetActiveParty()
    {
        if (partyData == null)
        {
            return System.Array.Empty<HeroData>();
        }

        return partyData.activeSlots;
    }

    public HeroData[] GetReserveParty()
    {
        if (partyData == null)
        {
            return System.Array.Empty<HeroData>();
        }

        return partyData.reserveSlots;
    }

    public bool SwapActiveReserve(int activeIndex, int reserveIndex)
    {
        if (partyData == null)
        {
            Debug.LogWarning("[PartyManager] No party data assigned.");
            return false;
        }

        bool swapped = partyData.SwapActiveReserve(activeIndex, reserveIndex);
        if (swapped)
        {
            Debug.Log($"[PartyManager] Swapped active[{activeIndex}] with reserve[{reserveIndex}].");
        }

        return swapped;
    }

    public HeroData GetHero(string heroId)
    {
        if (heroDatabase == null)
        {
            return null;
        }

        return heroDatabase.GetHero(heroId);
    }

    public void ApplyHeroToUnit(CombatUnit unit, HeroData hero)
    {
        if (unit == null || hero == null)
        {
            Debug.LogWarning("[PartyManager] Cannot apply hero data: unit or hero is null.");
            return;
        }

        unit.UnitName = hero.displayName;
        unit.ElementType = ResolveElement(hero);
        unit.MaxHP = hero.baseMaxHP;
        unit.HP = hero.baseMaxHP;
        unit.MaxMP = hero.baseMaxMP;
        unit.MP = hero.baseMaxMP;
        unit.Attack = hero.baseAttack;
        unit.Defense = hero.baseDefense;
        unit.Speed = hero.baseSpeed;

        if (hero.starterSkills != null)
        {
            unit.SetSkills(hero.starterSkills);
        }

        Debug.Log($"[PartyManager] Applied hero '{hero.displayName}' ({unit.ElementType}) to unit.");
    }

    public static void ApplyHeroToUnitStatic(CombatUnit unit, HeroData hero)
    {
        if (unit == null || hero == null)
        {
            return;
        }

        unit.UnitName = hero.displayName;
        unit.ElementType = hero.element;
        unit.MaxHP = hero.baseMaxHP;
        unit.HP = hero.baseMaxHP;
        unit.MaxMP = hero.baseMaxMP;
        unit.MP = hero.baseMaxMP;
        unit.Attack = hero.baseAttack;
        unit.Defense = hero.baseDefense;
        unit.Speed = hero.baseSpeed;

        if (hero.starterSkills != null)
        {
            unit.SetSkills(hero.starterSkills);
        }
    }

    public static void ApplyHeroToUnitWithElement(CombatUnit unit, HeroData hero, CombatUnit.Element element)
    {
        if (unit == null || hero == null)
        {
            return;
        }

        unit.UnitName = hero.displayName;
        unit.ElementType = element;
        unit.MaxHP = hero.baseMaxHP;
        unit.HP = hero.baseMaxHP;
        unit.MaxMP = hero.baseMaxMP;
        unit.MP = hero.baseMaxMP;
        unit.Attack = hero.baseAttack;
        unit.Defense = hero.baseDefense;
        unit.Speed = hero.baseSpeed;

        if (hero.starterSkills != null)
        {
            unit.SetSkills(hero.starterSkills);
        }
    }
}
