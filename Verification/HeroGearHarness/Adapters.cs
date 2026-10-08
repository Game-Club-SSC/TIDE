using System;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void DestroyImmediate(Object obj) { if(obj is GameObject go) go.Dispose(); else Call(obj,"OnDisable"); }
        protected static void Destroy(Object obj) => DestroyImmediate(obj);
        protected static void DontDestroyOnLoad(Object obj) {}
        internal static void Call(object obj,string method) => obj?.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance)?.Invoke(obj,null);
    }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject.transform; }
    public class MonoBehaviour : Component {}
    public class Transform { public void SetParent(Transform p,bool keep) {} }
    public class GameObject : Object
    {
        public readonly Transform transform=new Transform(); private readonly List<Component> components=new List<Component>();
        public GameObject(string name) { this.name=name; }
        public T AddComponent<T>() where T:Component,new() { var obj=new T {gameObject=this}; components.Add(obj); Call(obj,"Awake"); Call(obj,"OnEnable"); return obj; }
        public void Dispose() { foreach(var obj in components) { Call(obj,"OnDisable"); Call(obj,"OnDestroy"); } }
    }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T:ScriptableObject,new() { var obj=new T(); Call(obj,"OnEnable"); return obj; } }
    public class Sprite : Object {} public class RuntimeAnimatorController : Object {}
    public struct Color { public Color(float r,float g,float b,float a=1) {} public static Color white=>new Color(); }
    public static class Application { public static bool isPlaying=false; }
    public static class Debug { public static void Log(object obj) {} public static void LogWarning(object obj) {} }
    public static class Random { private static readonly System.Random rng=new System.Random(732); public static float value => (float)rng.NextDouble(); public static int Range(int a,int b) => rng.Next(a,b); public static float Range(float a,float b) => a+(b-a)*value; }
    public static class Mathf
    {
        public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Min(int a,int b)=>Math.Min(a,b); public static float Min(float a,float b)=>Math.Min(a,b);
        public static int Clamp(int n,int a,int b)=>Math.Clamp(n,a,b); public static float Clamp(float n,float a,float b)=>Math.Clamp(n,a,b);
        public static float Clamp01(float n)=>Math.Clamp(n,0,1); public static int RoundToInt(float n)=>(int)Math.Round(n); public static float Round(float n)=>(float)Math.Round(n);
        public static float Abs(float n)=>Math.Abs(n); public static float Pow(float n,float p)=>(float)Math.Pow(n,p);
    }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField:Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute:Attribute { public HeaderAttribute(string n) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TooltipAttribute:Attribute { public TooltipAttribute(string n) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class TextAreaAttribute:Attribute { public TextAreaAttribute() {} public TextAreaAttribute(int a,int b) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute:Attribute { public MinAttribute(float a) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute:Attribute { public RangeAttribute(float a,float b) {} }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenuAttribute:Attribute { public string fileName,menuName; }
    [AttributeUsage(AttributeTargets.Method)] public sealed class ContextMenuAttribute:Attribute { public ContextMenuAttribute(string n) {} }
}
namespace NUnit.Framework
{
    public static class Assert
    {
        public static int Checks;
        public static void IsTrue(bool ok,string message="") { Checks++; if(!ok) throw new Exception(message); }
        public static void IsFalse(bool ok,string message="")=>IsTrue(!ok,message);
        public static void IsNull(object value,string message="")=>IsTrue(value==null,message);
        public static void IsNotNull(object value,string message="")=>IsTrue(value!=null,message);
        public static void AreEqual(object a,object b,string message="")=>IsTrue(Equals(a,b),$"{message} expected={a} actual={b}");
        public static void AreEqual(float a,float b,float tolerance,string message="")=>IsTrue(Math.Abs(a-b)<=tolerance,message);
        public static void AreNotEqual(object a,object b,string message="")=>IsTrue(!Equals(a,b),message);
        public static void AreSame(object a,object b,string message="")=>IsTrue(ReferenceEquals(a,b),message);
    }
}
// Other services are explicit adapters, not the subject of this harness.
public class CombatUnit : UnityEngine.MonoBehaviour
{
    public enum Element {None,Fire,Water,Earth,Air,Space} public enum UnitType {Ally,Enemy}
    public int MaxHP,HP,MaxMP,MP,Attack,Defense,Speed,XpReward; public float CritRate,CritDamage; public string UnitName; public UnitType Type;
}
public class BattleManager : UnityEngine.MonoBehaviour { public readonly List<CombatUnit> EnemyUnits=new List<CombatUnit>(); public void RegisterUnit(CombatUnit unit) { if(unit.Type==CombatUnit.UnitType.Enemy) EnemyUnits.Add(unit); } }
public class BalanceConfig { public struct StatGrowth { public int hp,mp,attack,defense,speed; } public StatGrowth GetCumulativeStatGrowth(int level)=>default; }
public static class GearSetFactory { public static GearSetData[] CreateStarterGearSets()=>Array.Empty<GearSetData>(); }
public static class HeroDatabase { public static readonly Dictionary<string,HeroData> Heroes=new Dictionary<string,HeroData>(); public static HeroData FindHeroById(string id)=>Heroes.TryGetValue(id,out var hero)?hero:null; }
public class PartyManager { public static PartyManager Instance; public PartyData PartyData; public CombatUnit.Element ResolveElement(HeroData hero)=>hero.element; public HeroData[] GetActiveParty()=>PartyData.activeSlots; public HeroData[] GetReserveParty()=>PartyData.reserveSlots; }
public class DifficultyModeService { public static DifficultyModeService Instance; public float XpMultiplier = 1f; public int CurrencyMultiplier = 1; public float GetXpMultiplier()=>XpMultiplier; public int GetCurrencyMultiplier()=>CurrencyMultiplier; }
public class GameStateManager { public static GameStateManager Instance; public int SaveCalls; public void SaveWorldState() { SaveCalls++; } }
public static class IslandThemeRegistry { public static string ResolveIslandId(string id)=>id; }
public class NewGamePlusService { public static NewGamePlusService Instance; public float XpMultiplier=1; public float GetXpMultiplier()=>XpMultiplier; }
