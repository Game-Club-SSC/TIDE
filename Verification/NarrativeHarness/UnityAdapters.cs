using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

// Engine and neighboring-service adapters only. Narrative behavior is linked
// directly from Assets. This harness does not emulate Unity rendering/import.
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TextAreaAttribute : Attribute { public TextAreaAttribute(int a, int b) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    public class Object
    {
        internal static readonly List<Component> Components = new List<Component>();
        public static void Destroy(object value) { }
        public static void Destroy(object value, float delay) { }
        public static void DestroyImmediate(object value) { }
        public static void DontDestroyOnLoad(object value) { }
        public static T FindFirstObjectByType<T>() where T : class => Components.Find(c => c is T) as T;
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : class => Components.FindAll(c => c is T).ConvertAll(c => c as T).ToArray();
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : Component, new() => gameObject.GetComponent<T>();
        public T GetComponentInParent<T>() where T : Component, new() => GetComponent<T>();
    }
    public class MonoBehaviour : Component
    {
        protected Coroutine StartCoroutine(IEnumerator routine) { routine.MoveNext(); return new Coroutine(); }
        protected void StopCoroutine(Coroutine routine) { }
    }
    public class Coroutine { }
    public class GameObject : Object
    {
        public string name;
        public bool activeInHierarchy = true;
        public readonly RectTransform transform;
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public GameObject(string name) { this.name = name; transform = new RectTransform { gameObject = this }; components[typeof(RectTransform)] = transform; }
        public T AddComponent<T>() where T : Component, new()
        {
            if (typeof(T) == typeof(RectTransform)) return (T)(Component)transform;
            T value = new T { gameObject = this }; components[typeof(T)] = value; Components.Add(value);
            typeof(T).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(value, null);
            typeof(T).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(value, null);
            return value;
        }
        public T GetComponent<T>() where T : Component, new() => components.TryGetValue(typeof(T), out Component value) ? (T)value : AddComponent<T>();
        public void SetActive(bool value) { activeInHierarchy = value; }
    }
    public class Transform : Component
    {
        public Vector3 position;
        private readonly List<Transform> children = new List<Transform>();
        public int childCount => children.Count;
        public Transform GetChild(int i) => children[i];
        public void SetParent(Transform parent, bool worldPositionStays = true) { parent?.children.Add(this); }
        public void SetAsFirstSibling() { }
    }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, offsetMin, offsetMax, pivot, anchoredPosition, sizeDelta; }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1); }
    public struct Vector3 { public float x, y, z; public float magnitude => (float)Math.Sqrt(x*x+y*y+z*z); public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3 { x=a.x-b.x, y=a.y-b.y, z=a.z-b.z }; }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; } public static Color white => new Color(1,1,1,1); }
    public static class Mathf
    {
        public const float Epsilon = float.Epsilon;
        public static int Clamp(int n,int a,int b) => Math.Clamp(n,a,b);
        public static float Clamp(float n,float a,float b) => Math.Clamp(n,a,b);
        public static float Clamp01(float n) => Math.Clamp(n,0,1);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static float Min(float a,float b) => Math.Min(a,b);
    }
    public enum FindObjectsSortMode { None }
    public enum KeyCode { Return, KeypadEnter, Space }
    public enum RenderMode { ScreenSpaceOverlay }
    public enum FontStyle { Bold, Italic }
    public enum TextAnchor { MiddleLeft, LowerRight }
    public enum HorizontalWrapMode { Wrap }
    public enum VerticalWrapMode { Overflow }
    public class Font { }
    public class Canvas : Component { public RenderMode renderMode; public int sortingOrder; }
    public class CanvasGroup : Component { public float alpha; public bool blocksRaycasts; }
    public static class Input { public static bool GetKeyDown(KeyCode code) => false; }
    public static class Time { public static float deltaTime = 0.1f; public static float time; }
    public class Collider : Component { public bool CompareTag(string tag) => tag=="Player"; }
    public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float time) { } }
    public static class Resources
    {
        public static T GetBuiltinResource<T>(string name) where T : new() => new T();
        public static T[] LoadAll<T>(string path)
        {
            if(typeof(T)!=typeof(IslandConfig))return Array.Empty<T>();
            var configs=new List<IslandConfig>();
            foreach(string id in new[]{"island_lust","island_greed","island_desire","island_anger","island_envy","island_ego"})
                configs.Add(new IslandConfig{islandId=id});
            return (T[])(object)configs.ToArray();
        }
    }
    public static class Debug { public static readonly List<string> Errors = new List<string>(); public static void Log(object v) { } public static void LogWarning(object v) { } public static void LogError(object v) { Errors.Add(v.ToString()); } }
    public static class PlayerPrefs
    {
        private static readonly Dictionary<string,string> values = new Dictionary<string,string>();
        public static bool HasKey(string key) => values.ContainsKey(key);
        public static string GetString(string key,string fallback) => values.TryGetValue(key,out string value) ? value : fallback;
        public static void SetString(string key,string value) { values[key]=value; }
        public static void Save() { }
        public static void Reset() { values.Clear(); }
    }
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object value) => JsonSerializer.Serialize(value,value.GetType(),options);
        public static T FromJson<T>(string value)
        {
            try { return JsonSerializer.Deserialize<T>(value,options); }
            catch(JsonException e) { throw new ArgumentException("Invalid serialized member type",e); }
        }
    }
}
namespace UnityEngine.UI
{
    using UnityEngine;
    public class Graphic : Component { public Color color; public bool raycastTarget; }
    public class Image : Graphic { public enum Type { Filled } public enum FillMethod { Radial360 } public Type type; public FillMethod fillMethod; public bool fillClockwise; public float fillAmount; }
    public class Text : Graphic { public Font font; public int fontSize; public FontStyle fontStyle; public TextAnchor alignment; public string text; public HorizontalWrapMode horizontalOverflow; public VerticalWrapMode verticalOverflow; public float lineSpacing; }
    public class CanvasScaler : Component { public enum ScaleMode { ScaleWithScreenSize } public ScaleMode uiScaleMode; public Vector2 referenceResolution; }
    public class GraphicRaycaster : Component { }
    public struct ColorBlock { public Color highlightedColor, pressedColor, selectedColor; }
    public class Selectable : Component { public enum Transition { None } public Transition transition; }
    public class Button : Selectable { public Graphic targetGraphic; public ColorBlock colors; public readonly ClickEvent onClick = new ClickEvent(); }
    public class ClickEvent { public void AddListener(Action handler) { } }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public enum LoadSceneMode { Single }
    public static class SceneManager { public static event Action<Scene,LoadSceneMode> sceneLoaded; public static void LoadForTest() { sceneLoaded?.Invoke(new Scene { name="test" },LoadSceneMode.Single); } }
}
public class IsometricPlayer : UnityEngine.MonoBehaviour { public bool canMove = true; }
public class CombatUnit { public string HeroId, UnitName; public bool IsAlive=true; }
public class GearSetData { public string setId; }
public class GearInstance { }
public static class GearDropService { public enum GearRarity { Common } }
public class HeroProgressionManager
{
    public static HeroProgressionManager Instance;
    public GearSetData[] AvailableGearSets;
    public int Xp, Currency;
    public Action OnCurrencyAdded;
    public bool TryGrantXp(string id,int amount,out bool levelUp) { Xp+=amount;levelUp=false;return true; }
    public void AddCurrency(int amount) { Currency+=amount;OnCurrencyAdded?.Invoke();GameStateManager.Instance?.SaveWorldState(); }
}
public class PlayerGearInventory { public static PlayerGearInventory Instance; public GearInstance AddGear(string id,GearDropService.GearRarity rarity) => new GearInstance(); }
public class TideBreakProgressionManager { public static TideBreakProgressionManager Instance; public bool HasTideBreak(string id,string name) => false; public bool UnlockTideBreak(string id,string name) => true; }
public class IslandRestorationTracker { public static IslandRestorationTracker Instance; public float GetRestorationPercent(string id) => 100; public bool IsIslandRestored(string id) => true; }
public static class ExpandedAncientTexts { public static bool TryGetText(string id,out object value) { value=null; return false; } }
public class AncientTextRevealDirector { public static AncientTextRevealDirector Instance; public bool IsFragmentDiscovered(string id) => false; }
public static class PersonaUIStyle
{
    public static UnityEngine.Color DialoguePanel, BrightBlue, PortraitTint, DimText;
    public static void AddDiagonalEdge(UnityEngine.RectTransform t,float f) { }
    public static void CreateAccentSlash(UnityEngine.Transform t,UnityEngine.Color c,float f) { }
}
public class CeremonyIntroDirector : UnityEngine.MonoBehaviour { }
public class AncientTextLogUI : UnityEngine.MonoBehaviour { public void ShowEntry(string id,string title,string text,bool discovered) { } }
public class OverworldEnemy : UnityEngine.MonoBehaviour { }
public class IslandProgressionManager { public static IslandProgressionManager Instance; public string ActiveIslandId="island_lust"; }
public class EncounterDefinition { public bool isBossEncounter; public string encounterId; public float restorationValue; }
public class IslandConfig { public string islandId; public EncounterDefinition[] encounters = new[] { new EncounterDefinition { encounterId="optional", restorationValue=0.25f } }; }
public class GameStateManager
{
    public enum StoryAct { ActI=1, ActII=2, ActIII=3 }
    public enum EndingBranch { None, Good, Bad }
    public enum GameState { Exploration }
    public enum MinimumRestorationBadEndingRuleMode { OptionalContentOnly, BossDefeatedAtThreshold, ProceededAtThreshold }
    public static GameStateManager Instance;
    public StoryAct CurrentStoryAct=StoryAct.ActI, HighestStoryActReached=StoryAct.ActI;
    public EndingBranch ResolvedEndingBranch;
    public bool IsEndingTriggered, IsTransitioning, CeremonyIntroCompleted=true;
    public GameState currentState;
    public MinimumRestorationBadEndingRuleMode MinimumRestorationBadEndingRule;
    public string[] ThresholdBossIds=Array.Empty<string>(), ThresholdProceedIds=Array.Empty<string>();
    public int FinalLosses;
    private readonly HashSet<string> beats = new HashSet<string>();
    public bool IsNarrativeBeatCompleted(string id) => beats.Contains(id);
    public bool MarkNarrativeBeatCompleted(string id) => beats.Add(id);
    public void RegisterAncientText(string id,string title,string body) { }
    public void DiscoverAncientText(string id) { }
    public Action OnSave;
    public void SaveWorldState() { OnSave?.Invoke(); }
    public void ForceStoryActForDebug(StoryAct act) { CurrentStoryAct=act;HighestStoryActReached=act; }
    public void ForceEndingBranchForDebug(EndingBranch branch) { ResolvedEndingBranch=branch; }
    public int GetFinalBossDefeatCount(string id) => FinalLosses;
    public int GetConfiguredFinalBossDefeatThreshold(string id) => 4;
    public string GetFinalProgressionIslandIdForDebug() => "island_ego";
    public string[] GetThresholdOnlyBossVictoryIslandIds() => ThresholdBossIds;
    public string[] GetThresholdOnlyProceedIslandIds() => ThresholdProceedIds;
}
