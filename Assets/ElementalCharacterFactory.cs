using UnityEngine;

public static class ElementalCharacterFactory
{
    public const string PlayerModelRootName = "ElementalPlayerModel";
    public const string EnemyModelRootName = "ElementalEnemyModel";
    public const string BattleModelRootName = "ElementalBattleModel";
    public const string PlayerSpriteRootName = "ElementalPlayerSprite";
    public const string ShadowQuadName = "PlayerShadowQuad";
    public const string PlayerSpriteRendererName = "PlayerSpriteRenderer";

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

    public static Transform BuildExplorationPlayerSprite(
        Transform parent,
        string styleId,
        CombatUnit.Element element,
        Color primary,
        Color accent,
        Color glow,
        Vector3 localOffset,
        Vector3 localScale)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(PlayerSpriteRootName);
        if (existing != null)
        {
            Object.Destroy(existing.gameObject);
        }

        Transform shadowExisting = parent.Find(ShadowQuadName);
        if (shadowExisting != null)
        {
            Object.Destroy(shadowExisting.gameObject);
        }

        GameObject root = new GameObject(PlayerSpriteRootName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localOffset;
        root.transform.localRotation = Quaternion.identity;

        Vector3 validatedScale = localScale;
        validatedScale.x = Mathf.Max(0.1f, validatedScale.x);
        validatedScale.y = Mathf.Max(0.1f, validatedScale.y);
        validatedScale.z = Mathf.Max(0.1f, validatedScale.z);
        root.transform.localScale = validatedScale;

        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadow.name = ShadowQuadName;
        Collider shadowCollider = shadow.GetComponent<Collider>();
        if (shadowCollider != null)
        {
            // Destroy is deferred until the end of the frame. Disable first so a
            // dynamic player rigidbody never hits a concave Quad collider.
            shadowCollider.enabled = false;
            Object.Destroy(shadowCollider);
        }
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = new Vector3(localOffset.x, 0.04f, localOffset.z);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadow.transform.localScale = new Vector3(0.9f * validatedScale.x, 0.9f * validatedScale.z, 1f);

        Renderer shadowRenderer = shadow.GetComponent<Renderer>();
        if (shadowRenderer != null)
        {
            TideRuntimeVisualUtility.ApplyMeshColor(shadowRenderer, new Color(0f, 0f, 0f, 0.45f), true);
            shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadowRenderer.receiveShadows = false;
        }

        GameObject spriteObject = new GameObject(PlayerSpriteRendererName);
        spriteObject.transform.SetParent(root.transform, false);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localRotation = Quaternion.identity;
        spriteObject.transform.localScale = Vector3.one;

        SpriteRenderer spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        Sprite sprite = FuturisticSpriteLibrary.GetPlayerOverworldSprite(styleId);
        spriteRenderer.sprite = sprite;
        TideRuntimeVisualUtility.ApplySpriteColor(spriteRenderer, Color.white);
        spriteRenderer.sortingOrder = 100;
        spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spriteRenderer.receiveShadows = false;

        BillboardSprite billboard = spriteObject.AddComponent<BillboardSprite>();
        billboard.FaceCamera = true;
        billboard.LockYAxis = true;
        billboard.SetSortingOrder(100);

        return root.transform;
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
            TideRuntimeVisualUtility.ApplyMeshColor(renderer, color);
            renderer.enabled = true;
        }

        return part.transform;
    }
}

/// <summary>
/// Applies materials to meshes and sprites created at runtime. Unity's
/// CreatePrimitive method still creates materials with the legacy Standard
/// shader, which renders magenta in a URP player. Keeping the replacement in
/// one place makes generated visuals use the same shader on every platform.
/// </summary>
public static class TideRuntimeVisualUtility
{
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColorProperty = Shader.PropertyToID("_Color");
    private static readonly int SurfaceProperty = Shader.PropertyToID("_Surface");
    private static readonly int BlendProperty = Shader.PropertyToID("_Blend");
    private static readonly int AlphaClipProperty = Shader.PropertyToID("_AlphaClip");
    private static readonly int SourceBlendProperty = Shader.PropertyToID("_SrcBlend");
    private static readonly int DestinationBlendProperty = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteProperty = Shader.PropertyToID("_ZWrite");

    private static Shader runtimeMeshShader;
    private static Shader runtimeSpriteShader;
    private static Material runtimeSpriteMaterial;

    public static void ApplyMeshColor(Renderer renderer, Color color)
    {
        ApplyMeshColor(renderer, color, false);
    }

    public static void ApplyMeshColor(Renderer renderer, Color color, bool transparent)
    {
        Material material = EnsureMeshMaterial(renderer, transparent);
        if (material == null)
        {
            return;
        }

        ApplyMaterialColor(material, color);
    }

    public static Material EnsureMeshMaterial(Renderer renderer, bool transparent = false)
    {
        if (renderer == null)
        {
            return null;
        }

        Material material = renderer.material;
        Shader shader = FindRuntimeMeshShader();
        if (shader != null && (material == null || !IsUrpMeshShader(material.shader)))
        {
            material = new Material(shader)
            {
                name = "TIDE Runtime URP Mesh Material"
            };
            renderer.material = material;
        }

        if (material != null && transparent)
        {
            ConfigureTransparentMaterial(material);
        }

        return material;
    }

    public static void ApplySpriteMaterial(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material material = renderer.sharedMaterial;
        if (material != null && IsUrpSpriteShader(material.shader))
        {
            return;
        }

        Material fallbackMaterial = GetRuntimeSpriteMaterial();
        if (fallbackMaterial != null)
        {
            renderer.sharedMaterial = fallbackMaterial;
        }
    }

    public static void ApplySpriteColor(SpriteRenderer renderer, Color color)
    {
        if (renderer == null)
        {
            return;
        }

        ApplySpriteMaterial(renderer);
        renderer.color = color;
    }

    private static Shader FindRuntimeMeshShader()
    {
        if (runtimeMeshShader != null)
        {
            return runtimeMeshShader;
        }

        runtimeMeshShader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        return runtimeMeshShader;
    }

    private static Shader FindRuntimeSpriteShader()
    {
        if (runtimeSpriteShader != null)
        {
            return runtimeSpriteShader;
        }

        runtimeSpriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
            ?? Shader.Find("Sprites/Default");
        return runtimeSpriteShader;
    }

    private static Material GetRuntimeSpriteMaterial()
    {
        if (runtimeSpriteMaterial != null)
        {
            return runtimeSpriteMaterial;
        }

        Shader shader = FindRuntimeSpriteShader();
        if (shader == null)
        {
            return null;
        }

        runtimeSpriteMaterial = new Material(shader)
        {
            name = "TIDE Runtime URP Sprite Material"
        };
        return runtimeSpriteMaterial;
    }

    private static bool IsUrpMeshShader(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        string shaderName = shader.name;
        return shaderName.StartsWith("Universal Render Pipeline/", System.StringComparison.Ordinal)
            && (shaderName.IndexOf("/Lit", System.StringComparison.Ordinal) >= 0
                || shaderName.IndexOf("/Unlit", System.StringComparison.Ordinal) >= 0);
    }

    private static bool IsUrpSpriteShader(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        string shaderName = shader.name;
        return shaderName == "Universal Render Pipeline/2D/Sprite-Unlit-Default"
            || shaderName == "Universal Render Pipeline/2D/Sprite-Lit-Default";
    }

    private static void ApplyMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(BaseColorProperty))
        {
            material.SetColor(BaseColorProperty, color);
        }

        if (material.HasProperty(LegacyColorProperty))
        {
            material.SetColor(LegacyColorProperty, color);
        }
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(SurfaceProperty))
        {
            material.SetFloat(SurfaceProperty, 1f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        if (material.HasProperty(BlendProperty))
        {
            material.SetFloat(BlendProperty, 0f);
        }

        if (material.HasProperty(AlphaClipProperty))
        {
            material.SetFloat(AlphaClipProperty, 0f);
        }

        if (material.HasProperty(SourceBlendProperty))
        {
            material.SetInt(SourceBlendProperty, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty(DestinationBlendProperty))
        {
            material.SetInt(DestinationBlendProperty, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty(ZWriteProperty))
        {
            material.SetInt(ZWriteProperty, 0);
        }

        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
