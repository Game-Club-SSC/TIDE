using System.Collections.Generic;

/// <summary>
/// Static catalog of all TideBreak ability definitions across all elements.
/// Provides runtime definitions that TideBreakData ScriptableObjects can reference.
/// </summary>
public static class TideBreakCatalog
{
    /// <summary>
    /// A single TideBreak ability definition.
    /// </summary>
    public struct TideBreakDefinition
    {
        public string name;
        public CombatUnit.Element element;
        public int unlockLevel;
        public float damageMultiplier;
        public SkillTarget targetType;
        public string description;
        public string unlockDescription;
        public bool isHidden;

        public TideBreakDefinition(
            string name,
            CombatUnit.Element element,
            int unlockLevel,
            float damageMultiplier,
            SkillTarget targetType,
            string description,
            bool isHidden = false)
        {
            this.name = name;
            this.element = element;
            this.unlockLevel = unlockLevel;
            this.damageMultiplier = damageMultiplier;
            this.targetType = targetType;
            this.description = description;
            this.isHidden = isHidden;
            this.unlockDescription = isHidden
                ? $"Revealed by an ancient text"
                : $"Reach Level {unlockLevel}";
        }
    }

    private static List<TideBreakDefinition> allEntries;

    /// <summary>
    /// The complete catalog of TideBreak abilities.
    /// </summary>
    public static IReadOnlyList<TideBreakDefinition> All
    {
        get
        {
            if (allEntries == null)
            {
                BuildCatalog();
            }
            return allEntries;
        }
    }

    /// <summary>
    /// Returns all non-hidden TideBreaks for the given element.
    /// </summary>
    public static List<TideBreakDefinition> GetForElement(CombatUnit.Element element)
    {
        List<TideBreakDefinition> result = new List<TideBreakDefinition>();
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].element == element && !All[i].isHidden)
            {
                result.Add(All[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// Returns all non-hidden TideBreaks for the given element that are unlocked at or below the given level.
    /// </summary>
    public static List<TideBreakDefinition> GetUnlockedForElement(CombatUnit.Element element, int heroLevel)
    {
        List<TideBreakDefinition> result = new List<TideBreakDefinition>();
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].element == element && !All[i].isHidden && All[i].unlockLevel <= heroLevel)
            {
                result.Add(All[i]);
            }
        }
        return result;
    }

    /// <summary>
    /// Looks up a single TideBreak definition by name.
    /// Returns null if not found.
    /// </summary>
    public static TideBreakDefinition? FindByName(string abilityName)
    {
        if (string.IsNullOrEmpty(abilityName))
        {
            return null;
        }

        for (int i = 0; i < All.Count; i++)
        {
            if (string.Equals(All[i].name, abilityName, System.StringComparison.Ordinal))
            {
                return All[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Returns all hidden TideBreaks (those revealed only via ancient texts).
    /// </summary>
    public static List<TideBreakDefinition> GetHidden()
    {
        List<TideBreakDefinition> result = new List<TideBreakDefinition>();
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].isHidden)
            {
                result.Add(All[i]);
            }
        }
        return result;
    }

    private static void BuildCatalog()
    {
        allEntries = new List<TideBreakDefinition>();

        // ==================== FIRE ====================
        allEntries.Add(new TideBreakDefinition(
            "Flame Surge",
            CombatUnit.Element.Fire,
            unlockLevel: 1,
            damageMultiplier: 2.0f,
            targetType: SkillTarget.SingleEnemy,
            description: "A focused burst of searing flame strikes a single foe."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Inferno Wave",
            CombatUnit.Element.Fire,
            unlockLevel: 5,
            damageMultiplier: 1.8f,
            targetType: SkillTarget.AllEnemies,
            description: "A rolling wall of fire sweeps across all enemies."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Ember Shield",
            CombatUnit.Element.Fire,
            unlockLevel: 10,
            damageMultiplier: 0f,
            targetType: SkillTarget.Self,
            description: "Wrap yourself in living embers, restoring 30% of max HP."
        ));

        // Hidden: Fire
        allEntries.Add(new TideBreakDefinition(
            "Phoenix Rebirth",
            CombatUnit.Element.Fire,
            unlockLevel: 15,
            damageMultiplier: 3.0f,
            targetType: SkillTarget.SingleEnemy,
            description: "Call upon the immortal flame to scorch a single foe, then heal yourself for 50% of max HP.",
            isHidden: true
        ));

        // ==================== WATER ====================
        allEntries.Add(new TideBreakDefinition(
            "Tidal Crush",
            CombatUnit.Element.Water,
            unlockLevel: 1,
            damageMultiplier: 2.0f,
            targetType: SkillTarget.SingleEnemy,
            description: "A concentrated torrent slams into a single enemy."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Tsunami",
            CombatUnit.Element.Water,
            unlockLevel: 5,
            damageMultiplier: 1.8f,
            targetType: SkillTarget.AllEnemies,
            description: "A massive wave crashes over all enemies."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Healing Rain",
            CombatUnit.Element.Water,
            unlockLevel: 10,
            damageMultiplier: 0f,
            targetType: SkillTarget.AllAllies,
            description: "A gentle rain falls on allies, restoring 25% of max HP to each."
        ));

        // Hidden: Water
        allEntries.Add(new TideBreakDefinition(
            "Abyssal Drain",
            CombatUnit.Element.Water,
            unlockLevel: 15,
            damageMultiplier: 2.5f,
            targetType: SkillTarget.SingleEnemy,
            description: "Channel the deep ocean to drain life from a single foe, stealing HP equal to damage dealt.",
            isHidden: true
        ));

        // ==================== EARTH ====================
        allEntries.Add(new TideBreakDefinition(
            "Stone Hammer",
            CombatUnit.Element.Earth,
            unlockLevel: 1,
            damageMultiplier: 2.2f,
            targetType: SkillTarget.SingleEnemy,
            description: "A colossal stone fist crashes down on a single enemy. Slow but devastating."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Earthquake",
            CombatUnit.Element.Earth,
            unlockLevel: 5,
            damageMultiplier: 1.6f,
            targetType: SkillTarget.AllEnemies,
            description: "The ground splits apart, damaging all enemies."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Fortify",
            CombatUnit.Element.Earth,
            unlockLevel: 10,
            damageMultiplier: 0f,
            targetType: SkillTarget.AllAllies,
            description: "Encase all allies in protective stone, increasing their defense by 40%."
        ));

        // ==================== AIR ====================
        allEntries.Add(new TideBreakDefinition(
            "Gale Slash",
            CombatUnit.Element.Air,
            unlockLevel: 1,
            damageMultiplier: 2.0f,
            targetType: SkillTarget.SingleEnemy,
            description: "A razor-sharp gust of wind cuts into a single foe."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Cyclone",
            CombatUnit.Element.Air,
            unlockLevel: 5,
            damageMultiplier: 1.7f,
            targetType: SkillTarget.AllEnemies,
            description: "A spinning vortex of wind batters all enemies."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Tailwind",
            CombatUnit.Element.Air,
            unlockLevel: 10,
            damageMultiplier: 0f,
            targetType: SkillTarget.AllAllies,
            description: "A favorable wind surrounds all allies, increasing their speed by 30%."
        ));

        // ==================== SPACE ====================
        allEntries.Add(new TideBreakDefinition(
            "Void Rend",
            CombatUnit.Element.Space,
            unlockLevel: 1,
            damageMultiplier: 2.1f,
            targetType: SkillTarget.SingleEnemy,
            description: "Tear a rift in space to strike a single enemy with void energy."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Dimensional Rift",
            CombatUnit.Element.Space,
            unlockLevel: 5,
            damageMultiplier: 1.9f,
            targetType: SkillTarget.AllEnemies,
            description: "Fracture reality itself, dealing void damage to all enemies."
        ));

        allEntries.Add(new TideBreakDefinition(
            "Time Warp",
            CombatUnit.Element.Space,
            unlockLevel: 10,
            damageMultiplier: 0f,
            targetType: SkillTarget.SingleAlly,
            description: "Bend the flow of time to grant a single ally an extra turn."
        ));
    }
}
