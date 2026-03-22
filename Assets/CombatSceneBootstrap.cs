using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatSceneBootstrap : MonoBehaviour
{
    [Header("Environment")]
    [SerializeField] private Color battlefieldColor = new Color(0.23f, 0.26f, 0.31f);
    [SerializeField] private Color cameraBackground = new Color(0.1f, 0.12f, 0.16f);
    [SerializeField] private Color playerMarkerColor = new Color(0.21f, 0.73f, 0.84f);
    [SerializeField] private Color enemyMarkerColor = new Color(0.89f, 0.38f, 0.25f);
    [SerializeField] private Color reserveMarkerColor = new Color(0.9f, 0.9f, 0.2f);
    [SerializeField] private Color allyUnitColor = new Color(0.21f, 0.73f, 0.84f);
    [SerializeField] private Color enemyUnitColor = new Color(0.89f, 0.38f, 0.25f);
    [SerializeField] private float lightIntensity = 1.15f;

    [Header("Layout")]
    [SerializeField] private float playerSideX = -4f;
    [SerializeField] private float enemySideX = 4f;
    [SerializeField] private float slotSpacing = 2f;
    [SerializeField] private Vector3 battlefieldScale = new Vector3(1.6f, 1f, 1.2f);

    [Header("Camera")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(0f, 8f, -10f);
    [SerializeField] private Vector3 cameraLookTarget = new Vector3(0f, 0.5f, 0f);
    [SerializeField] private float cameraFieldOfView = 45f;

    [Header("Markers")]
    [SerializeField] private Vector3 markerScale = new Vector3(0.75f, 0.08f, 0.75f);
    [SerializeField] private float markerYOffset = 0.08f;

    [Header("Unit Prefabs")]
    [SerializeField] private GameObject playerUnitPrefab;
    [SerializeField] private GameObject enemyUnitPrefab;

    [Header("Party")]
    [Tooltip("Fallback party data used when no PartyManager is present (e.g. standalone combat testing).")]
    [SerializeField] private PartyData partyData;

    private void Awake()
    {
        EnsureGameManager();

        if (GameStateManager.Instance != null && GameStateManager.Instance.PendingEnemyComposition != null)
        {
            enemyComposition = GameStateManager.Instance.PendingEnemyComposition;
            GameStateManager.Instance.PendingEnemyComposition = null;
        }

        EnsureDirectionalLight();
        EnsureBattlefield();
        EnsureCombatCamera();
        EnsureSpawnPoints();
        EnsureBattleManager();
        SpawnCombatUnits();
        EnsureBattleHud();
        EnsureBattleEscapeMenu();
    }

    private void EnsureGameManager()
    {
        if (GameStateManager.Instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("GameManager");
        managerObject.AddComponent<GameStateManager>();
    }

    private void EnsureDirectionalLight()
    {
        Light directionalLight = null;
        bool createdLight = false;
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                directionalLight = lights[i];
                break;
            }
        }

        if (directionalLight == null)
        {
            Transform lightTransform = GetOrCreateChild(transform, "Directional Light");
            directionalLight = lightTransform.GetComponent<Light>();
            if (directionalLight == null)
            {
                directionalLight = lightTransform.gameObject.AddComponent<Light>();
            }

            createdLight = true;
        }

        directionalLight.type = LightType.Directional;
        directionalLight.enabled = true;
        directionalLight.intensity = lightIntensity;
        if (createdLight)
        {
            directionalLight.transform.SetParent(transform, true);
        }

        directionalLight.transform.position = new Vector3(0f, 8f, 0f);
        directionalLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
    }

    private void EnsureBattlefield()
    {
        Transform environmentRoot = GetOrCreateChild(transform, "Environment");
        Transform groundTransform = environmentRoot.Find("CombatGround");

        if (groundTransform == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "CombatGround";
            groundTransform = ground.transform;
            groundTransform.SetParent(environmentRoot, false);
        }

        groundTransform.localPosition = Vector3.zero;
        groundTransform.localRotation = Quaternion.identity;
        groundTransform.localScale = battlefieldScale;

        Renderer groundRenderer = groundTransform.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.material.color = battlefieldColor;
        }
    }

    private void EnsureCombatCamera()
    {
        Camera combatCamera = Camera.main;
        if (combatCamera == null)
        {
            combatCamera = FindFirstObjectByType<Camera>();
        }

        if (combatCamera == null)
        {
            Transform cameraTransform = GetOrCreateChild(transform, "Combat Camera");
            cameraTransform.gameObject.tag = "MainCamera";

            combatCamera = cameraTransform.GetComponent<Camera>();
            if (combatCamera == null)
            {
                combatCamera = cameraTransform.gameObject.AddComponent<Camera>();
            }

            if (cameraTransform.GetComponent<AudioListener>() == null)
            {
                cameraTransform.gameObject.AddComponent<AudioListener>();
            }
        }
        else if (!combatCamera.CompareTag("MainCamera"))
        {
            combatCamera.tag = "MainCamera";
        }

        if (combatCamera.GetComponent<AudioListener>() == null)
        {
            combatCamera.gameObject.AddComponent<AudioListener>();
        }

        ConfigureCamera(combatCamera);
    }

    private void ConfigureCamera(Camera combatCamera)
    {
        combatCamera.enabled = true;
        combatCamera.transform.position = cameraPosition;
        combatCamera.transform.rotation = Quaternion.LookRotation(cameraLookTarget - cameraPosition);
        combatCamera.orthographic = false;
        combatCamera.fieldOfView = cameraFieldOfView;
        combatCamera.nearClipPlane = 0.1f;
        combatCamera.farClipPlane = 100f;
        combatCamera.clearFlags = CameraClearFlags.SolidColor;
        combatCamera.backgroundColor = cameraBackground;
    }

    private Transform[] playerSpawnPoints;
    private Transform[] enemySpawnPoints;
    private Transform[] reserveSpawnPoints;
    private EnemyComposition enemyComposition;
    public List<CombatUnit> spawnedReserveUnits = new List<CombatUnit>();

    private void EnsureSpawnPoints()
    {
        Transform playerRoot = GetOrCreateChild(transform, "PlayerSpawnPoints");
        Transform enemyRoot = GetOrCreateChild(transform, "EnemySpawnPoints");
        Transform reserveRoot = GetOrCreateChild(transform, "ReserveSpawnPoints");

        playerSpawnPoints = new Transform[3];
        enemySpawnPoints = new Transform[3];
        reserveSpawnPoints = new Transform[2];

        for (int slotIndex = 0; slotIndex < 3; slotIndex++)
        {
            float zOffset = (slotIndex - 1) * slotSpacing;
            playerSpawnPoints[slotIndex] = EnsureSlot(playerRoot, $"PlayerSlot_{slotIndex + 1}", new Vector3(playerSideX, 0f, zOffset), playerMarkerColor);
            enemySpawnPoints[slotIndex] = EnsureSlot(enemyRoot, $"EnemySlot_{slotIndex + 1}", new Vector3(enemySideX, 0f, zOffset), enemyMarkerColor);
        }

        for (int i = 0; i < 2; i++)
        {
            float zOffset = (i - 0.5f) * slotSpacing;
            reserveSpawnPoints[i] = EnsureSlot(reserveRoot, $"ReserveSlot_{i + 1}", new Vector3(playerSideX - 2f, 0f, zOffset), reserveMarkerColor);
        }
    }

    private void EnsureBattleManager()
    {
        if (GetComponent<BattleManager>() == null)
        {
            gameObject.AddComponent<BattleManager>();
        }
    }

    public void SetEnemyComposition(EnemyComposition composition)
    {
        enemyComposition = composition;
    }

    private Transform EnsureSlot(Transform parent, string slotName, Vector3 localPosition, Color markerColor)
    {
        Transform slotTransform = parent.Find(slotName);
        if (slotTransform == null)
        {
            GameObject slotObject = new GameObject(slotName);
            slotTransform = slotObject.transform;
            slotTransform.SetParent(parent, false);
        }

        slotTransform.localPosition = localPosition;
        slotTransform.localRotation = Quaternion.identity;
        slotTransform.localScale = Vector3.one;

        EnsureSlotMarker(slotTransform, markerColor);
        
        return slotTransform;
    }

    private void EnsureSlotMarker(Transform slotTransform, Color markerColor)
    {
        Transform markerTransform = slotTransform.Find("Marker");
        if (markerTransform == null)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Marker";
            markerTransform = marker.transform;
            markerTransform.SetParent(slotTransform, false);
        }

        markerTransform.localPosition = new Vector3(0f, markerYOffset, 0f);
        markerTransform.localRotation = Quaternion.identity;
        markerTransform.localScale = markerScale;

        Renderer markerRenderer = markerTransform.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.material.color = markerColor;
        }
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform childTransform = parent.Find(childName);
        if (childTransform != null)
        {
            return childTransform;
        }

        GameObject childObject = new GameObject(childName);
        childTransform = childObject.transform;
        childTransform.SetParent(parent, false);
        return childTransform;
    }

    private void SpawnCombatUnits()
    {
        BattleManager battleManager = GetComponent<BattleManager>();
        HeroData[] activeHeroes = GetActiveHeroes();

        if (playerSpawnPoints != null)
        {
            for (int i = 0; i < playerSpawnPoints.Length; i++)
            {
                if (playerSpawnPoints[i] != null)
                {
                    GameObject unitObject = SpawnOrCreateUnit(playerUnitPrefab, playerSpawnPoints[i], $"PlayerUnit_{i + 1}", allyUnitColor);
                    CombatUnit unit = GetOrAddCombatUnit(unitObject);
                    unit.Type = CombatUnit.UnitType.Ally;
                    SetUnitColor(unitObject, allyUnitColor);

                    if (i < activeHeroes.Length && activeHeroes[i] != null)
                    {
                        ApplyHeroToUnit(unit, activeHeroes[i]);
                    }
                    else
                    {
                        unit.UnitName = $"Ally_{i + 1}";
                        Debug.Log($"[CombatSceneBootstrap] No hero data for active slot {i}. Using fallback stats.");
                    }

                    if (battleManager != null)
                    {
                        battleManager.RegisterUnit(unit);
                    }
                }
            }
        }

        // Spawn reserve units (inactive)
        HeroData[] reserveHeroes = GetReserveHeroes();
        List<CombatUnit> reserveUnits = new List<CombatUnit>();
        if (reserveSpawnPoints != null)
        {
            for (int i = 0; i < reserveSpawnPoints.Length; i++)
            {
                if (reserveSpawnPoints[i] != null)
                {
                    GameObject unitObject = SpawnOrCreateUnit(playerUnitPrefab, reserveSpawnPoints[i], $"ReserveUnit_{i + 1}", reserveMarkerColor);
                    CombatUnit unit = GetOrAddCombatUnit(unitObject);
                    unit.Type = CombatUnit.UnitType.Ally;
                    SetUnitColor(unitObject, reserveMarkerColor);
                    unitObject.SetActive(false); // Reserve units start inactive

                    if (i < reserveHeroes.Length && reserveHeroes[i] != null)
                    {
                        ApplyHeroToUnit(unit, reserveHeroes[i]);
                    }
                    else
                    {
                        unit.UnitName = $"Reserve_{i + 1}";
                        Debug.Log($"[CombatSceneBootstrap] No hero data for reserve slot {i}. Using fallback stats.");
                    }

                    reserveUnits.Add(unit);
                }
            }
        }
        if (battleManager != null)
        {
            battleManager.SetAllyReserveUnits(reserveUnits);
            Debug.Log($"[CombatSceneBootstrap] Set {reserveUnits.Count} reserve units.");
        }

        if (enemySpawnPoints != null)
        {
            string[] defaultEnemyNames = { "Imp", "Orc", "Troll" };
            int enemySpawnCount = enemySpawnPoints.Length;
            if (enemyComposition != null && enemyComposition.Count > 0)
            {
                enemySpawnCount = Mathf.Min(enemyComposition.Count, enemySpawnPoints.Length);
            }

            for (int i = 0; i < enemySpawnCount; i++)
            {
                if (enemySpawnPoints[i] != null)
                {
                    GameObject unitObject = SpawnOrCreateUnit(enemyUnitPrefab, enemySpawnPoints[i], $"EnemyUnit_{i + 1}", enemyUnitColor);
                    CombatUnit unit = GetOrAddCombatUnit(unitObject);
                    unit.Type = CombatUnit.UnitType.Enemy;

                    EnemyData enemyData = null;
                    if (enemyComposition != null && enemyComposition.HasEnemyDataSlots)
                    {
                        enemyData = enemyComposition.GetEnemyData(i);
                    }

                    if (enemyData != null)
                    {
                        ApplyEnemyDataToUnit(unit, enemyData);
                    }
                    else if (enemyComposition != null && !enemyComposition.HasEnemyDataSlots && enemyComposition.IsValidIndex(i))
                    {
                        unit.UnitName = enemyComposition.names[i];
                        unit.ElementType = enemyComposition.elements[i];
                        unit.Attack += enemyComposition.attackModifiers[i];
                        unit.Defense += enemyComposition.defenseModifiers[i];
                        unit.MaxHP = Mathf.Max(1, unit.MaxHP + enemyComposition.maxHpModifiers[i]);
                        unit.HP = unit.MaxHP;
                    }
                    else
                    {
                        if (enemyComposition != null)
                        {
                            Debug.LogWarning($"[CombatSceneBootstrap] Missing enemy data for configured slot {i}. Using fallback enemy stats.");
                        }

                        unit.UnitName = i < defaultEnemyNames.Length ? defaultEnemyNames[i] : $"Enemy_{i + 1}";
                        switch (i)
                        {
                            case 0:
                                unit.ElementType = CombatUnit.Element.Fire;
                                break;
                            case 1:
                                unit.ElementType = CombatUnit.Element.Water;
                                break;
                            case 2:
                                unit.ElementType = CombatUnit.Element.Earth;
                                break;
                        }

                        unit.Defense += i;
                        unit.MaxHP += i * 10;
                        unit.HP = unit.MaxHP;
                    }

                    SetUnitColor(unitObject, enemyUnitColor);

                    if (battleManager != null)
                    {
                        battleManager.RegisterUnit(unit);
                    }
                }
            }
        }
    }

    private static CombatUnit GetOrAddCombatUnit(GameObject unitObject)
    {
        CombatUnit unit = unitObject.GetComponent<CombatUnit>();
        if (unit == null)
        {
            unit = unitObject.AddComponent<CombatUnit>();
        }

        return unit;
    }

    private GameObject SpawnOrCreateUnit(GameObject unitPrefab, Transform spawnPoint, string runtimeName, Color fallbackColor)
    {
        GameObject unitObject = null;

        if (unitPrefab != null)
        {
            unitObject = Instantiate(unitPrefab, spawnPoint.position, Quaternion.identity);
            unitObject.transform.SetParent(spawnPoint, false);
            unitObject.transform.localPosition = Vector3.zero;
            unitObject.transform.localRotation = Quaternion.identity;
        }
        else
        {
            unitObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unitObject.transform.SetParent(spawnPoint, false);
            unitObject.transform.localPosition = Vector3.zero;
            unitObject.transform.localRotation = Quaternion.identity;
            unitObject.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
            SetUnitColor(unitObject, fallbackColor);
        }

        unitObject.name = runtimeName;
        return unitObject;
    }

    private void SetUnitColor(GameObject unitObject, Color color)
    {
        Renderer renderer = unitObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.material.color = color;
    }

    private HeroData[] GetActiveHeroes()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.PartyData != null)
        {
            return PartyManager.Instance.GetActiveParty();
        }

        if (partyData == null)
        {
            partyData = Resources.Load<PartyData>("PartyData/DefaultParty");
        }

        if (partyData != null)
        {
            return partyData.activeSlots ?? System.Array.Empty<HeroData>();
        }

        return System.Array.Empty<HeroData>();
    }

    private HeroData[] GetReserveHeroes()
    {
        if (PartyManager.Instance != null && PartyManager.Instance.PartyData != null)
        {
            return PartyManager.Instance.GetReserveParty();
        }

        if (partyData == null)
        {
            partyData = Resources.Load<PartyData>("PartyData/DefaultParty");
        }

        if (partyData != null)
        {
            return partyData.reserveSlots ?? System.Array.Empty<HeroData>();
        }

        return System.Array.Empty<HeroData>();
    }

    private void ApplyHeroToUnit(CombatUnit unit, HeroData hero)
    {
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.ApplyHeroToUnit(unit, hero);
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

    private void ApplyEnemyDataToUnit(CombatUnit unit, EnemyData enemyData)
    {
        if (unit == null || enemyData == null)
        {
            return;
        }

        unit.UnitName = enemyData.displayName;
        unit.ElementType = enemyData.element;
        unit.MaxHP = enemyData.baseMaxHP;
        unit.HP = enemyData.baseMaxHP;
        unit.MaxMP = enemyData.baseMaxMP;
        unit.MP = enemyData.baseMaxMP;
        unit.Attack = enemyData.baseAttack;
        unit.Defense = enemyData.baseDefense;
        unit.Speed = enemyData.baseSpeed;

        if (enemyData.skills != null)
        {
            unit.SetSkills(enemyData.skills);
        }

        unit.XpReward = enemyData.xpReward;

        Debug.Log($"[CombatSceneBootstrap] Applied enemy data '{enemyData.displayName}' ({unit.ElementType}, {unit.XpReward} XP) to unit.");
    }

    private void EnsureBattleHud()
    {
        if (FindFirstObjectByType<BattleHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("BattleHud");
        hudObject.AddComponent<BattleHud>();
    }

    private void EnsureBattleEscapeMenu()
    {
        if (FindFirstObjectByType<BattleEscapeMenu>() != null)
        {
            return;
        }

        GameObject menuObject = new GameObject("BattleEscapeMenu");
        menuObject.AddComponent<BattleEscapeMenu>();
    }
}
