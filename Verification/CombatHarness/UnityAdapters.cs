// Deliberately minimal engine adapters. These do not reproduce Unity rendering,
// lifecycle timing, coroutine scheduling, scene loading, or asset serialization.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
namespace UnityEngine
{
 [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute {}
 [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute {}
 [AttributeUsage(AttributeTargets.Field)] public class Header : Attribute { public Header(string s) {} }
 [AttributeUsage(AttributeTargets.Field)] public class Tooltip : Attribute { public Tooltip(string s) {} }
 [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a,float b) {} }
 [AttributeUsage(AttributeTargets.Field)] public class Min : Attribute { public Min(float a) {} }
 [AttributeUsage(AttributeTargets.Field)] public class TextArea : Attribute { public TextArea() {} public TextArea(int a,int b) {} }
 [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenu : Attribute { public string fileName; public string menuName; }
 [AttributeUsage(AttributeTargets.Method)] public class ContextMenu : Attribute { public ContextMenu(string s) {} }
 public class Object
 {
  public static void DontDestroyOnLoad(Object value) {}
  public string name;
  public static void Destroy(Object obj) { if(obj is GameObject go) go.Clear(); }
  public static void DestroyImmediate(Object obj) => Destroy(obj);
  public static T FindFirstObjectByType<T>() where T : Object => null;
 }
 public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject,new() => new T(); }
 public class Component : Object
 {
  public GameObject gameObject; public Transform transform => gameObject.transform;
  public T GetComponent<T>() where T : Component => gameObject.GetComponent<T>();
  public T GetComponentInChildren<T>(bool includeInactive = false) where T : Component => GetComponent<T>();
 }
 public class MonoBehaviour : Component
 {
  public bool isActiveAndEnabled = true;
  public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
  public void StopCoroutine(Coroutine routine) {}
 }
 public class Coroutine {}
 public class GameObject : Object
 {
  private readonly Dictionary<Type,Component> components = new Dictionary<Type,Component>();
  public Transform transform;
  public GameObject(string name="GameObject") { this.name=name; transform=new Transform {gameObject=this}; }
  public T AddComponent<T>() where T : Component,new()
  {
   T component=new T {gameObject=this}; components[typeof(T)]=component;
   var awake=typeof(T).GetMethod("Awake",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
   awake?.Invoke(component,null);
   typeof(T).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.Invoke(component,null); return component;
  }
  public T GetComponent<T>() where T : Component => components.TryGetValue(typeof(T),out Component c) ? (T)c : null;
  public void SetActive(bool active) {}
  public void Clear() { foreach(var c in components.Values) c.GetType().GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null); foreach(var c in components.Values) c.GetType().GetMethod("OnDestroy",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null); components.Clear(); }
 }
 public class Transform : Component
 {
  public Vector3 localPosition; public Vector3 localScale=Vector3.one;
  public Transform parent;
  public void SetParent(Transform value,bool worldPositionStays=true) { parent=value; }
  public Transform Find(string name) => null;
 }
 public struct Vector2 { public float x,y; public Vector2(float x,float y) {this.x=x;this.y=y;} }
 public struct Vector3
 {
  public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
  public static Vector3 one => new Vector3(1,1,1);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>new Vector3(a.x+(b.x-a.x)*t,a.y+(b.y-a.y)*t,a.z+(b.z-a.z)*t);
 }
 public struct Color
 {
  public float r,g,b,a; public Color(float r,float g,float b,float a=1) {this.r=r;this.g=g;this.b=b;this.a=a;}
  public static Color white=>new Color(1,1,1); public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);
 }
 public class Sprite : Object {}
 public class SpriteRenderer : Component { public bool enabled=true; public Sprite sprite; public Color color; public int sortingOrder; public Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows; }
 public static class Mathf
 {
  public const float PI=(float)Math.PI;
  public static int Max(int a,int b)=>Math.Max(a,b); public static float Max(float a,float b)=>Math.Max(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b); public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Clamp(int v,int a,int b)=>Math.Clamp(v,a,b); public static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b);
  public static float Clamp01(float v)=>Clamp(v,0,1); public static float Abs(float v)=>Math.Abs(v);
  public static int RoundToInt(float v)=>(int)Math.Round(v,MidpointRounding.ToEven); public static int CeilToInt(float v)=>(int)Math.Ceiling(v);
  public static float Pow(float a,float b)=>(float)Math.Pow(a,b); public static float Sin(float v)=>(float)Math.Sin(v);
  public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
 }
 public static class Random
 {
  private static System.Random rng=new System.Random(1234);
  public static float value => (float)rng.NextDouble(); public static void InitState(int seed) { rng=new System.Random(seed); }
  public static float Range(float min,float max)=>min+(max-min)*value; public static int Range(int min,int max)=>rng.Next(min,max);
 }
 public static class Debug { public static void Log(object value,Object context=null) {} public static void LogWarning(object value,Object context=null) {} public static void LogError(object value,Object context=null) {} }
 public static class Application { public static bool isPlaying=false; }
 public static class Time { public static float deltaTime=1f; public static float unscaledDeltaTime=1f; }
 public enum KeyCode { N,V,X }
 public static class Resources
 {
  // Only scalar fields needed by the combat tests are read from the actual
  // shipped YAML. Object-reference resolution remains a Unity check.
  public static T[] LoadAll<T>(string path) where T : Object,new()
  {
   var result=new List<T>(); string dir=System.IO.Path.Combine(Program.RepositoryRoot,"Assets/Resources",path);
   if(!System.IO.Directory.Exists(dir)) return result.ToArray();
   foreach(string file in System.IO.Directory.GetFiles(dir,"*.asset",System.IO.SearchOption.AllDirectories))
   {
    T item=new T();
    foreach(string line in System.IO.File.ReadAllLines(file))
    {
     string text=line.Trim(); int colon=text.IndexOf(':'); if(colon<1) continue;
     string key=text.Substring(0,colon); string value=text.Substring(colon+1).Trim();
     FieldInfo field=typeof(T).GetField(key); if(field==null) continue;
     if(field.FieldType==typeof(string)) field.SetValue(item,value);
     else if(field.FieldType==typeof(int) && int.TryParse(value,out int i)) field.SetValue(item,i);
     else if(field.FieldType==typeof(bool) && int.TryParse(value,out int b)) field.SetValue(item,b!=0);
     else if(field.FieldType.IsEnum && int.TryParse(value,out int e)) field.SetValue(item,Enum.ToObject(field.FieldType,e));
     else if(field.FieldType==typeof(float) && float.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float f)) field.SetValue(item,f);
    }
    result.Add(item);
   }
   return result.ToArray();
  }
  public static T Load<T>(string path) where T : Object,new()=>null;
 }
}
namespace UnityEngine.Rendering { public enum ShadowCastingMode { Off } }
namespace UnityEngine.UI {}
internal class QTESession : UnityEngine.MonoBehaviour
{
 public event Action<bool> OnQTEResolved; public bool IsQTEActive;
 public bool ShowQTE(float duration) { IsQTEActive=true;return true; }
 public void CancelQTE() {IsQTEActive=false;}
 public void Resolve(bool result) { IsQTEActive=false;OnQTEResolved?.Invoke(result); }
}
internal class BattleHud : UnityEngine.MonoBehaviour { public void ShowTeamDynamicDescription(string s) {} }
internal class AudioManager
{
 public static AudioManager Instance => null;
 public void HandleCombatVictory() {} public void HandleCombatDefeat() {} public void HandleAttackMiss() {} public void HandleAttackCrit() {} public void HandleAttackHit() {} public void HandleHeal() {} public void HandleTideBreakActivation() {} public void HandleQTESuccess() {} public void HandleQTEFail() {}
}
internal class GameStateManager
{
 public static GameStateManager Instance; public int CombatEndedCalls;
 public void OnCombatEnded(bool won,bool fled) { CombatEndedCalls++; }
}
internal class HeroProgressionManager
{
 public static HeroProgressionManager Instance => null;
 public bool TrySpendCurrency(int amount)=>false;
}
internal class DialogueSystem { public static DialogueSystem Instance=>null; public int GetBondLevel(string a,string b)=>50; }
internal static class RelationshipCombatEffects
{
 public static float GetTeamDamageMultiplier(IReadOnlyList<CombatUnit> units)=>1;
 public static float GetTeamDefenseMultiplier(IReadOnlyList<CombatUnit> units)=>1;
 public static float GetTeamHealingMultiplier(IReadOnlyList<CombatUnit> units)=>1;
 public static float GetTideBreakMultiplier(IReadOnlyList<CombatUnit> units)=>1;
 public static float GetClashBonus(IReadOnlyList<CombatUnit> units)=>0;
 public static float GetTeamUpChance(IReadOnlyList<CombatUnit> units)=>0;
 public static string GetTeamDynamicDescription(IReadOnlyList<CombatUnit> units)=>"";
}
internal static class FuturisticSpriteLibrary { public static UnityEngine.Sprite GetHitEffectSprite()=>null; }
internal static class TideRuntimeVisualUtility { public static void ApplySpriteColor(UnityEngine.SpriteRenderer renderer,UnityEngine.Color color) {renderer.color=color;} }
