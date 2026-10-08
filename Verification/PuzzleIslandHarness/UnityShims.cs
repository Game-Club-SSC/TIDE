// Only the Unity-facing boundary is stubbed. PuzzleData, TideTile and TideManager
// are linked directly from Assets. No rendering, timing or scene runtime is simulated.
using System;
using System.Collections;
using System.Collections.Generic;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=true)] public sealed class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class Header : Attribute { public Header(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class Tooltip : Attribute { public Tooltip(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a,float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class Min : Attribute { public Min(float a) { } }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public sealed class CreateAssetMenu : Attribute { public string fileName; public string menuName; }
    public class Object { public string name; public static void Destroy(Object o) { } public static T FindFirstObjectByType<T>() where T:class => null; }
    public class Component : Object { public GameObject gameObject; public Transform transform => gameObject?.transform; public T GetComponent<T>() where T:class => gameObject?.GetComponent<T>(); public T GetComponentInParent<T>() where T:class => gameObject?.GetComponent<T>(); }
    public class MonoBehaviour : Component { public bool enabled=true; public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine(); public void StopCoroutine(Coroutine c) { } }
    public class ScriptableObject : Object { }
    public class Coroutine { }
    public class Transform : Component { public Vector3 position; public Vector3 localPosition; public Vector3 localScale=Vector3.one; public Quaternion rotation; public Quaternion localRotation; public Transform parent; public int childCount=>0; public void SetParent(Transform t,bool b) { parent=t; } public Transform Find(string n)=>null; public Transform GetChild(int i)=>null; }
    public class RectTransform : Transform { public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition, offsetMin, offsetMax; }
    public class GameObject : Object
    {
        private readonly Dictionary<Type, Component> components=new Dictionary<Type, Component>(); public Transform transform; public int layer;
        public GameObject(string name="",params Type[] types) { this.name=name; transform=Array.Exists(types,t=>t==typeof(RectTransform))?new RectTransform():new Transform(); transform.gameObject=this; components[transform.GetType()]=transform; foreach(var t in types) if(t!=typeof(RectTransform)) AddComponent(t); }
        private Component AddComponent(Type t) { var c=(Component)Activator.CreateInstance(t); c.gameObject=this; components[t]=c; return c; }
        public T AddComponent<T>() where T:Component,new()=> (T)AddComponent(typeof(T)); public T GetComponent<T>() where T:class { components.TryGetValue(typeof(T),out var c); return c as T; } public T GetComponentInParent<T>() where T:class=>GetComponent<T>(); public void SetActive(bool a) { }
        public static GameObject CreatePrimitive(PrimitiveType p) { var o=new GameObject(); o.AddComponent<Renderer>(); o.AddComponent<Collider>(); return o; }
    }
    public enum PrimitiveType { Cube,Capsule,Sphere }
    public struct Vector2Int : IEquatable<Vector2Int> { public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;} public bool Equals(Vector2Int other)=>x==other.x&&y==other.y; public override bool Equals(object o)=>o is Vector2Int other&&Equals(other); public override int GetHashCode()=>HashCode.Combine(x,y); public static bool operator ==(Vector2Int a,Vector2Int b)=>a.Equals(b);public static bool operator !=(Vector2Int a,Vector2Int b)=>!a.Equals(b); }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero=>new Vector2(); public static Vector2 one=>new Vector2(1,1); public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b); public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a; }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 zero=>new Vector3(); public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a; }
    public struct Quaternion { public static Quaternion Euler(float x,float y,float z)=>new Quaternion(); }
    public struct Color { public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color black=>new Color();public static Color white=>new Color(1,1,1);public static Color Lerp(Color a,Color b,float t)=>a; public static Color operator *(Color a,float t)=>a; }
    public static class Mathf { public const float Infinity=float.PositiveInfinity;public static int Clamp(int v,int min,int max)=>Math.Clamp(v,min,max);public static float Clamp01(float v)=>Math.Clamp(v,0,1);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b); public static float PingPong(float t,float l)=>0;public static float Lerp(float a,float b,float t)=>a+(b-a)*t; public static float InverseLerp(float a,float b,float v)=>Clamp01((v-a)/(b-a)); public static float SmoothStep(float a,float b,float t)=>Lerp(a,b,t);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v); }
    public class Material:Object { public Color color; } public class Renderer:Component {public Material material=new Material(); public bool enabled=true;} public class Collider:Component { public bool enabled=true; }
    public class Canvas:Component { public RenderMode renderMode; public int sortingOrder; }public enum RenderMode{ScreenSpaceOverlay} public class CanvasGroup:Component{public float alpha;public bool blocksRaycasts,interactable;}
    public class Camera:Component {public static Camera main; public Ray ScreenPointToRay(Vector3 v)=>new Ray();}public struct Ray{}public struct RaycastHit{public Collider collider; }public struct LayerMask {public static implicit operator LayerMask(int v)=>new LayerMask();} public static class Physics{public static bool Raycast(Ray r,out RaycastHit h,float d,LayerMask l){h=new RaycastHit();return false;}}
    public class Font:Object{} public enum FontStyle{Bold}public enum TextAnchor{MiddleCenter}public static class Resources {public static readonly Dictionary<string,Object> Items=new Dictionary<string,Object>();public static T Load<T>(string p)where T:class=>Items.TryGetValue(p,out var o)?o as T:null;public static T[] LoadAll<T>(string p) where T:class {var l=new List<T>(); foreach(var item in Items) if(item.Key.StartsWith(p+"/")&&item.Value is T v)l.Add(v);return l.ToArray();}public static T GetBuiltinResource<T>(string n)where T:new()=>new T();}
    public static class Debug{public static void Log(object o){}public static void LogWarning(object o,Object context=null){}public static void LogError(object o){}}
    public static class Time{public static float unscaledDeltaTime=.02f;}public class WaitForSeconds{public WaitForSeconds(float s){}}public enum KeyCode{Escape,Alpha0,Alpha1,Keypad0,Keypad1}public static class Input{public static Vector3 mousePosition;public static bool GetKeyDown(KeyCode k)=>false; public static bool GetMouseButtonDown(int b)=>false;}
}
namespace UnityEngine.UI
{
    using UnityEngine;
    public class Selectable:Component {public enum Transition{ColorTint}public Transition transition;}
    public class Button:Selectable {public bool interactable;public ColorBlock colors;public readonly ClickEvent onClick=new ClickEvent();public class ClickEvent{public void RemoveAllListeners(){}public void AddListener(Action a){}}}
    public struct ColorBlock{public Color normalColor,highlightedColor,pressedColor,selectedColor;public float fadeDuration;}
    public class Image:Component{public Color color;}public class Text:Component{public Font font;public TextAnchor alignment;public int fontSize;public FontStyle fontStyle;public Color color;public bool raycastTarget;public string text;public HorizontalWrapMode horizontalOverflow;public VerticalWrapMode verticalOverflow;}
    public enum HorizontalWrapMode{Wrap}public enum VerticalWrapMode{Overflow}public class GraphicRaycaster:Component{}public class CanvasScaler:Component{public enum ScaleMode{ScaleWithScreenSize}public ScaleMode uiScaleMode;public Vector2 referenceResolution;public float matchWidthOrHeight;}
}
namespace UnityEngine.EventSystems { public class EventSystem:UnityEngine.Component {public static EventSystem current;public void SetSelectedGameObject(UnityEngine.GameObject o){}}public class StandaloneInputModule:UnityEngine.Component{} }
namespace UnityEngine.SceneManagement{public struct Scene{public string name;}public static class SceneManager{public static Scene GetActiveScene()=>new Scene{name="PuzzleScene"};}}
namespace TMPro{public class TextMeshPro:UnityEngine.Component{public object font;public TextAlignmentOptions alignment;public float fontSize;public TextWrappingModes textWrappingMode;public string text;public UnityEngine.Color color;}public enum TextAlignmentOptions{Center}public enum TextWrappingModes{NoWrap}public static class TMP_Settings{public static object defaultFontAsset;}}
public class GameStateManager
{
    public static GameStateManager Instance;public const string PuzzleSceneName="PuzzleScene";public PuzzleData PendingPuzzleData;public string PendingPuzzleIslandId,PendingPuzzleEncounterId,PendingPuzzleBoxId;public UnityEngine.Vector2Int PendingPuzzleSealedTile;public int[,] PendingPuzzleLayout;public object PendingEnemyComposition;public bool HasActiveFlowController;
    public readonly Dictionary<string,int[,]> Grids=new Dictionary<string,int[,]>();public int Completions;public void EnterPuzzle(){}public bool CanEnterCombatFromPuzzle()=>true;public void EnterCombatSceneFromPuzzle(string a,string b,float c){}public void SavePuzzleRuntimeState(string id,int[,] grid,bool solved){if(!string.IsNullOrEmpty(id))Grids[id]=(int[,])grid.Clone();}public bool TryGetPuzzleRuntimeGrid(string id,out int[,] grid,out bool solved){solved=false;grid=null;return !string.IsNullOrEmpty(id)&&Grids.TryGetValue(id,out grid);}public void CompletePuzzleInExploration(string a,string b,string c,float d){Completions++;}public void MarkPuzzleSolved(){Completions++;}public void ReturnToMainScene(){}
}
public class IslandRestorationTracker{public static IslandRestorationTracker Instance;public readonly HashSet<string> Cleared=new HashSet<string>();public bool HasClearedEncounter(string island,string encounter)=>Cleared.Contains(island+"::"+encounter);}
public static class IslandThemeRegistry{public static string ResolveIslandId(string id)=>id;public static string GetActiveIslandId()=>"island_lust";}
public class IslandConfig:UnityEngine.ScriptableObject{public string islandId,viceName;public UnityEngine.Color vicePrimaryColor,viceSecondaryColor,ambientColor,skyColor,corruptedTint,purifiedTint;}
public class EncounterConfig:UnityEngine.ScriptableObject{public string encounterId;}
public static class EnemyComposition{public static object FromEncounterConfig(EncounterConfig c)=>new object();}
public class AudioManager{public static AudioManager Instance;public void HandleTileTake(){}public void HandleTilePlace(){}}
public static class TideRuntimeVisualUtility{public static void EnsureMeshMaterial(UnityEngine.Renderer r){}public static void ApplyMeshColor(UnityEngine.Renderer r,UnityEngine.Color c){}}
