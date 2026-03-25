using UnityEngine;

public static class ElementalCharacterFactory
{
    public const string PlayerModelRootName = "ElementalPlayerModel";
    public const string EnemyModelRootName = "ElementalEnemyModel";
    public const string BattleModelRootName = "ElementalBattleModel";

    public static Transform BuildExplorationPlayerModel(
        Transform parent,
        CombatUnit.Element element,
        Color primary,
        Color accent,
        Color glow,
        Vector3 localOffset,
        Vector3 localScale)
    {
        return BuildCharacter(
            parent,
            PlayerModelRootName,
            element,
            false,
            primary,
            accent,
            glow,
            localOffset,
            localScale,
            false,
            false);
    }

    public static Transform BuildExplorationEnemyModel(
        Transform parent,
        CombatUnit.Element element,
        Vector3 localOffset,
        Vector3 localScale)
    {
        Color primary = GetElementPrimaryColor(element);
        Color accent = Color.Lerp(primary, Color.white, 0.3f);
        Color glow = Color.Lerp(primary, Color.white, 0.55f);

        return BuildCharacter(
            parent,
            EnemyModelRootName,
            element,
            true,
            primary,
            accent,
            glow,
            localOffset,
            localScale,
            false,
            false);
    }

    public static Transform BuildBattleAllyModel(
        Transform parent,
        CombatUnit.Element element,
        Color primary,
        Color accent,
        Color glow,
        Vector3 localOffset,
        Vector3 localScale)
    {
        return BuildCharacter(
            parent,
            BattleModelRootName,
            element,
            false,
            primary,
            accent,
            glow,
            localOffset,
            localScale,
            true,
            true);
    }

    public static Transform BuildBattleEnemyModel(
        Transform parent,
        CombatUnit.Element element,
        Vector3 localOffset,
        Vector3 localScale)
    {
        Color primary = GetElementPrimaryColor(element);
        Color accent = Color.Lerp(primary, Color.white, 0.3f);
        Color glow = Color.Lerp(primary, Color.white, 0.55f);

        return BuildCharacter(
            parent,
            BattleModelRootName,
            element,
            true,
            primary,
            accent,
            glow,
            localOffset,
            localScale,
            true,
            true);
    }

    public static Color GetElementPrimaryColor(CombatUnit.Element element)
    {
        switch (element)
        {
            case CombatUnit.Element.Fire:
                return new Color(0.92f, 0.36f, 0.26f, 1f);
            case CombatUnit.Element.Water:
                return new Color(0.28f, 0.61f, 0.95f, 1f);
            case CombatUnit.Element.Earth:
                return new Color(0.39f, 0.67f, 0.34f, 1f);
            case CombatUnit.Element.Air:
                return new Color(0.73f, 0.86f, 0.96f, 1f);
            case CombatUnit.Element.Space:
                return new Color(0.51f, 0.43f, 0.78f, 1f);
            default:
                return new Color(0.85f, 0.85f, 0.85f, 1f);
        }
    }

    private static Transform BuildCharacter(
        Transform parent,
        string rootName,
        CombatUnit.Element element,
        bool isEnemy,
        Color primary,
        Color accent,
        Color glow,
        Vector3 localOffset,
        Vector3 localScale,
        bool battleVariant,
        bool includeVisualPivot)
    {
        if (parent == null)
        {
            return null;
        }

        Transform root = CreateRoot(parent, rootName, localOffset, localScale);

        Color secondary = Color.Lerp(primary, Color.black, isEnemy ? 0.42f : 0.35f);
        Color trim = Color.Lerp(accent, Color.white, 0.22f);

        Transform modelRoot = root;
        Transform renderRoot = root;
        if (includeVisualPivot)
        {
            Transform visualPivot = CreatePivot(parent, "BattleSpriteVisual", new Vector3(0f, 1.08f, 0f), Vector3.one);
            root.SetParent(visualPivot, false);
            root.localPosition = Vector3.zero;
            root.localRotation = Quaternion.identity;
            root.localScale = localScale;

            Transform shadowPivot = CreatePivot(parent, "BattleSpriteShadow", new Vector3(0f, 0.03f, 0f), new Vector3(0.9f, 0.48f, 1f));
            CreatePart(PrimitiveType.Cylinder, "ShadowDisc", shadowPivot, Vector3.zero, new Vector3(0.72f, 0.02f, 0.72f), new Color(0.06f, 0.06f, 0.08f, 1f), Vector3.zero);

            modelRoot = visualPivot;
            renderRoot = root;
        }

        BuildCoreBody(renderRoot, isEnemy, primary, secondary, trim, glow, battleVariant);
        BuildElementFeatures(renderRoot, element, isEnemy, accent, glow, battleVariant);

        return modelRoot;
    }

    private static Transform CreateRoot(Transform parent, string rootName, Vector3 localOffset, Vector3 localScale)
    {
        Transform existing = parent.Find(rootName);
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        GameObject root = new GameObject(rootName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localOffset;
        root.transform.localRotation = Quaternion.identity;

        Vector3 validatedScale = localScale;
        validatedScale.x = Mathf.Max(0.1f, validatedScale.x);
        validatedScale.y = Mathf.Max(0.1f, validatedScale.y);
        validatedScale.z = Mathf.Max(0.1f, validatedScale.z);
        root.transform.localScale = validatedScale;

        return root.transform;
    }

    private static Transform CreatePivot(Transform parent, string pivotName, Vector3 localPosition, Vector3 localScale)
    {
        Transform existing = parent.Find(pivotName);
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        GameObject pivot = new GameObject(pivotName);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = localPosition;
        pivot.transform.localRotation = Quaternion.identity;
        pivot.transform.localScale = localScale;
        return pivot.transform;
    }

    private static void BuildCoreBody(
        Transform root,
        bool isEnemy,
        Color primary,
        Color secondary,
        Color trim,
        Color glow,
        bool battleVariant)
    {
        float bodyLift = battleVariant ? 0.03f : 0f;
        float torsoWidth = isEnemy ? 0.58f : 0.5f;
        float shoulderWidth = isEnemy ? 0.46f : 0.38f;

        CreatePart(PrimitiveType.Capsule, "LegLeft", root, new Vector3(-0.13f, 0.37f + bodyLift, 0f), new Vector3(0.2f, 0.37f, 0.2f), secondary, Vector3.zero);
        CreatePart(PrimitiveType.Capsule, "LegRight", root, new Vector3(0.13f, 0.37f + bodyLift, 0f), new Vector3(0.2f, 0.37f, 0.2f), secondary, Vector3.zero);

        CreatePart(PrimitiveType.Cube, "BootLeft", root, new Vector3(-0.13f, 0.06f + bodyLift, 0.05f), new Vector3(0.2f, 0.11f, 0.3f), Color.Lerp(secondary, Color.black, 0.25f), Vector3.zero);
        CreatePart(PrimitiveType.Cube, "BootRight", root, new Vector3(0.13f, 0.06f + bodyLift, 0.05f), new Vector3(0.2f, 0.11f, 0.3f), Color.Lerp(secondary, Color.black, 0.25f), Vector3.zero);

        CreatePart(PrimitiveType.Capsule, "Torso", root, new Vector3(0f, 1f + bodyLift, 0f), new Vector3(torsoWidth, 0.47f, 0.36f), primary, Vector3.zero);
        CreatePart(PrimitiveType.Cube, "ChestPlate", root, new Vector3(0f, 1.02f + bodyLift, 0.18f), new Vector3(0.34f, 0.28f, 0.09f), trim, Vector3.zero);
        CreatePart(PrimitiveType.Cube, "WaistPlate", root, new Vector3(0f, 0.77f + bodyLift, 0.13f), new Vector3(0.36f, 0.14f, 0.08f), secondary, Vector3.zero);

        CreatePart(PrimitiveType.Capsule, "ArmLeft", root, new Vector3(-shoulderWidth, 0.98f + bodyLift, 0f), new Vector3(0.15f, 0.29f, 0.15f), secondary, new Vector3(0f, 0f, 10f));
        CreatePart(PrimitiveType.Capsule, "ArmRight", root, new Vector3(shoulderWidth, 0.98f + bodyLift, 0f), new Vector3(0.15f, 0.29f, 0.15f), secondary, new Vector3(0f, 0f, -10f));

        CreatePart(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 1.62f + bodyLift, 0f), new Vector3(0.36f, 0.36f, 0.36f), Color.Lerp(primary, Color.white, 0.12f), Vector3.zero);
        CreatePart(PrimitiveType.Cube, "Visor", root, new Vector3(0f, 1.62f + bodyLift, 0.16f), new Vector3(0.25f, 0.11f, 0.06f), glow, Vector3.zero);

        if (isEnemy)
        {
            CreatePart(PrimitiveType.Cube, "Jaw", root, new Vector3(0f, 1.42f + bodyLift, 0.17f), new Vector3(0.22f, 0.09f, 0.08f), Color.Lerp(primary, Color.black, 0.4f), Vector3.zero);
            CreatePart(PrimitiveType.Cylinder, "HornLeft", root, new Vector3(-0.18f, 1.85f + bodyLift, 0.02f), new Vector3(0.05f, 0.14f, 0.05f), Color.Lerp(trim, Color.white, 0.2f), new Vector3(25f, 0f, 18f));
            CreatePart(PrimitiveType.Cylinder, "HornRight", root, new Vector3(0.18f, 1.85f + bodyLift, 0.02f), new Vector3(0.05f, 0.14f, 0.05f), Color.Lerp(trim, Color.white, 0.2f), new Vector3(25f, 0f, -18f));
        }
    }

    private static void BuildElementFeatures(Transform root, CombatUnit.Element element, bool isEnemy, Color accent, Color glow, bool battleVariant)
    {
        float heightOffset = battleVariant ? 0.03f : 0f;

        switch (element)
        {
            case CombatUnit.Element.Fire:
                CreatePart(PrimitiveType.Cylinder, "FlameCore", root, new Vector3(0f, 1.95f + heightOffset, 0f), new Vector3(0.08f, 0.22f, 0.08f), glow, Vector3.zero);
                CreatePart(PrimitiveType.Cylinder, "FlameTip", root, new Vector3(0f, 2.2f + heightOffset, 0f), new Vector3(0.045f, 0.12f, 0.045f), Color.Lerp(glow, Color.white, 0.4f), Vector3.zero);
                CreatePart(PrimitiveType.Cube, "FireShoulderLeft", root, new Vector3(-0.37f, 1.24f + heightOffset, 0f), new Vector3(0.14f, 0.16f, 0.18f), accent, new Vector3(0f, 0f, 20f));
                CreatePart(PrimitiveType.Cube, "FireShoulderRight", root, new Vector3(0.37f, 1.24f + heightOffset, 0f), new Vector3(0.14f, 0.16f, 0.18f), accent, new Vector3(0f, 0f, -20f));
                break;

            case CombatUnit.Element.Water:
                CreatePart(PrimitiveType.Cube, "WaterFinBack", root, new Vector3(0f, 1.25f + heightOffset, -0.2f), new Vector3(0.12f, 0.45f, 0.08f), accent, new Vector3(20f, 0f, 0f));
                CreatePart(PrimitiveType.Cube, "WaterFinLeft", root, new Vector3(-0.33f, 1.05f + heightOffset, 0f), new Vector3(0.05f, 0.24f, 0.22f), glow, new Vector3(0f, 0f, 14f));
                CreatePart(PrimitiveType.Cube, "WaterFinRight", root, new Vector3(0.33f, 1.05f + heightOffset, 0f), new Vector3(0.05f, 0.24f, 0.22f), glow, new Vector3(0f, 0f, -14f));
                break;

            case CombatUnit.Element.Earth:
                CreatePart(PrimitiveType.Cube, "EarthPauldronLeft", root, new Vector3(-0.34f, 1.23f + heightOffset, 0f), new Vector3(0.24f, 0.19f, 0.24f), accent, Vector3.zero);
                CreatePart(PrimitiveType.Cube, "EarthPauldronRight", root, new Vector3(0.34f, 1.23f + heightOffset, 0f), new Vector3(0.24f, 0.19f, 0.24f), accent, Vector3.zero);
                CreatePart(PrimitiveType.Cube, "EarthCore", root, new Vector3(0f, 0.9f + heightOffset, -0.14f), new Vector3(0.3f, 0.25f, 0.16f), Color.Lerp(accent, Color.black, 0.15f), Vector3.zero);
                break;

            case CombatUnit.Element.Air:
                CreatePart(PrimitiveType.Cube, "AirWingLeft", root, new Vector3(-0.46f, 1.16f + heightOffset, -0.12f), new Vector3(0.1f, 0.34f, 0.46f), accent, new Vector3(0f, 0f, 24f));
                CreatePart(PrimitiveType.Cube, "AirWingRight", root, new Vector3(0.46f, 1.16f + heightOffset, -0.12f), new Vector3(0.1f, 0.34f, 0.46f), accent, new Vector3(0f, 0f, -24f));
                CreatePart(PrimitiveType.Cube, "AirCrest", root, new Vector3(0f, 1.98f + heightOffset, 0f), new Vector3(0.08f, 0.2f, 0.08f), glow, Vector3.zero);
                break;

            case CombatUnit.Element.Space:
                CreatePart(PrimitiveType.Cylinder, "SpaceHalo", root, new Vector3(0f, 1.93f + heightOffset, 0f), new Vector3(0.38f, 0.02f, 0.38f), glow, new Vector3(90f, 0f, 0f));
                CreatePart(PrimitiveType.Cube, "SpaceCrystalLeft", root, new Vector3(-0.27f, 1.48f + heightOffset, 0.14f), new Vector3(0.09f, 0.17f, 0.09f), accent, new Vector3(35f, 35f, 0f));
                CreatePart(PrimitiveType.Cube, "SpaceCrystalRight", root, new Vector3(0.27f, 1.48f + heightOffset, 0.14f), new Vector3(0.09f, 0.17f, 0.09f), accent, new Vector3(-35f, 35f, 0f));
                break;

            default:
                CreatePart(PrimitiveType.Cube, "Core", root, new Vector3(0f, 1.18f + heightOffset, -0.16f), new Vector3(0.16f, 0.16f, 0.1f), accent, Vector3.zero);
                break;
        }

        if (isEnemy)
        {
            CreatePart(PrimitiveType.Cube, "EnemyClawLeft", root, new Vector3(-0.5f, 0.8f + heightOffset, 0.14f), new Vector3(0.14f, 0.08f, 0.2f), Color.Lerp(accent, Color.black, 0.2f), new Vector3(0f, 0f, 20f));
            CreatePart(PrimitiveType.Cube, "EnemyClawRight", root, new Vector3(0.5f, 0.8f + heightOffset, 0.14f), new Vector3(0.14f, 0.08f, 0.2f), Color.Lerp(accent, Color.black, 0.2f), new Vector3(0f, 0f, -20f));
        }
    }

    private static Transform CreatePart(
        PrimitiveType primitiveType,
        string partName,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        Vector3 localEulerAngles)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        part.transform.localScale = localScale;

        Collider collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }

        Renderer renderer = part.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
            renderer.enabled = true;
        }

        return part.transform;
    }
}
