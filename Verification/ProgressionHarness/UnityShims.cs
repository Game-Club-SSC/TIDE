// Minimal host adapters only. Gameplay decisions are compiled from Assets sources.
using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        internal static readonly List<Object> Objects = new();
        public Object() { Objects.Add(this); }
        public static void Destroy(Object obj) { if (obj != null) { Invoke(obj, "OnDisable"); Invoke(obj, "OnDestroy"); Objects.Remove(obj); } }
        public static void DestroyImmediate(Object obj) => Destroy(obj);
        public static T FindFirstObjectByType<T>() where T : Object => Objects.OfType<T>().FirstOrDefault();
        public static void DontDestroyOnLoad(Object obj) { }
        public int GetInstanceID() => GetHashCode();
        internal static void Invoke(object obj, string name) => obj.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(obj, null);
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform => gameObject?.transform;
        public T GetComponent<T>() where T : Component => gameObject?.Components.OfType<T>().FirstOrDefault();
    }
    public class MonoBehaviour : Component { }
    public class ScriptableObject : Object { }
    public class GameObject : Object
    {
        public readonly List<Component> Components = new();
        public readonly Transform transform = new();
        public GameObject(string name = "") { }
        public T AddComponent<T>() where T : Component, new()
        {
            T c = new() { gameObject = this }; Components.Add(c); Object.Invoke(c, "Awake"); Object.Invoke(c, "OnEnable"); return c;
        }
    }
    public class Transform { public Vector3 position; }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero => new(0,0,0);
    }
    public struct Color { public Color(float r,float g,float b,float a=1) { } }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b); public static int Min(int a,int b)=>Math.Min(a,b);
        public static float Clamp01(float a)=>Math.Clamp(a,0,1);
    }
    public static class Application { public static bool isPlaying = true; }
    public static class Debug { public static void Log(object x) { } public static void LogWarning(object x) { } public static void LogError(object x) { } }
    public static class Resources
    {
        public static IslandConfig[] Configs;
        public static T[] LoadAll<T>(string path) where T : Object => Configs.OfType<T>().ToArray();
    }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class SerializeField : Attribute { }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float a,float b) { } }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public static class SceneManager { public static event Action<Scene> sceneUnloaded; }
}
public class IslandAudioProfile : UnityEngine.ScriptableObject { }
public class IslandVisualProfile : UnityEngine.ScriptableObject { }
public class PuzzleData : UnityEngine.ScriptableObject { }
public class EnemyData : UnityEngine.ScriptableObject { }
public class EncounterConfig { public EnemyData[] enemies = Array.Empty<EnemyData>(); }
public class CombatUnit { public enum Element { Fire, Water, Earth, Air, Space, None } }
public class IsometricPlayer : UnityEngine.MonoBehaviour { }
public class GameStateManager
{
    public static GameStateManager Instance;
    public IslandFlowController FlowController;
    public string PendingCombatIslandId, PendingCombatEncounterId, PendingPuzzleIslandId, PendingPuzzleEncounterId, PendingPuzzleBoxId;
    public float PendingCombatRestorationValue, PendingPuzzleRestorationValue;
    public EnemyComposition PendingEnemyComposition;
    public PuzzleData PendingPuzzleData; public int[,] PendingPuzzleLayout;
    public bool Busy, PuzzleSolved; public int CombatEntries, PuzzleEntries, SaveCount;
    public bool CanEnterCombatScene() => !Busy;
    public bool CanEnterPuzzle() => !Busy;
    public void SaveWorldState() { SaveCount++; }
    public void SetBossDefeatTrackingContext(string id,bool track) { }
    public void MarkPuzzleUnsolvedForEntry() { PuzzleSolved=false; }
    public void EnterCombatScene() { CombatEntries++; Busy=true; }
    public void EnterPuzzleScene(UnityEngine.Vector3 pos,string id) { PuzzleEntries++; PendingPuzzleBoxId=id; Busy=true; }
}
public class PowerBudgetTracker
{
    public static PowerBudgetTracker Instance; public float Remaining = 100; public int Consumptions;
    public void RefundBudget(string id,float amount) { Remaining+=amount; }
    public bool TryConsumeBudget(string id,float amount) { if (Remaining < amount) return false; Remaining-=amount; Consumptions++; return true; }
}
public class DialogueSystem
{
    public static DialogueSystem Instance; public bool IsDialogueActive;
    public event Action<string> OnDialogueTreeCompleted;
    public void Complete(string id) { IsDialogueActive=false; OnDialogueTreeCompleted?.Invoke(id); }
}
public static class NarrativeBeatDirector { public static bool PlayPreBossDialogue(string id) => false; }
