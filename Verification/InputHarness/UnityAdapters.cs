using System;
using System.Collections.Generic;

// Only input and lifecycle API adapters. Tests execute the shipped manager.
namespace UnityEngine
{
    public class GameObject { public bool activeInHierarchy=true; }
    public class Object { public static T FindFirstObjectByType<T>() where T:class => null; }
    public class MonoBehaviour { protected void Destroy(object value) {} protected void DontDestroyOnLoad(object value) {} public object gameObject => this; }
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute {}
    [AttributeUsage(AttributeTargets.Class)] public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute {}
    [AttributeUsage(AttributeTargets.Field)] public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class RangeAttribute : Attribute { public RangeAttribute(float a, float b) {} }
    [AttributeUsage(AttributeTargets.Field)] public sealed class MinAttribute : Attribute { public MinAttribute(float a) {} }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public enum KeyCode { JoystickButton0 = 330 }
    public static class Time { public static float unscaledTime; }
    public static class Debug { public static void Log(object value) {} public static void LogWarning(object value) {} }
    public static class Mathf
    {
        public static float Abs(float n) => Math.Abs(n);
        public static float Sign(float n) => Math.Sign(n);
        public static float Max(float a, float b) => Math.Max(a,b);
        public static int Max(int a, int b) => Math.Max(a,b);
        public static float Clamp(float n,float a,float b) => Math.Clamp(n,a,b);
        public static float Clamp01(float n) => Math.Clamp(n,0,1);
    }
    public static class Input
    {
        public static string[] Names = Array.Empty<string>();
        public static int ConnectionReads;
        public static readonly Dictionary<string,float> Axes = new Dictionary<string,float> {
            {"Gamepad Horizontal",0}, {"Gamepad Vertical",0}, {"DPad Horizontal",0}, {"DPad Vertical",0}
        };
        public static readonly HashSet<int> Held = new HashSet<int>();
        public static readonly HashSet<int> Pressed = new HashSet<int>();
        public static string[] GetJoystickNames() { ConnectionReads++; return Names; }
        public static float GetAxisRaw(string name) => Axes[name];
        public static bool GetKey(KeyCode key) => Held.Contains((int)key - (int)KeyCode.JoystickButton0);
        public static bool GetKeyDown(KeyCode key) => Pressed.Contains((int)key - (int)KeyCode.JoystickButton0);
    }
}
namespace UnityEngine.EventSystems
{
    public class EventSystem
    {
        public static EventSystem current;
        public UnityEngine.GameObject currentSelectedGameObject;
        public void SetSelectedGameObject(UnityEngine.GameObject obj) { currentSelectedGameObject=obj; }
    }
}
namespace UnityEngine.UI
{
    public struct Navigation
    {
        public enum Mode {Explicit}
        public Mode mode;
        public Button selectOnUp,selectOnDown,selectOnLeft,selectOnRight;
    }
    public class Button
    {
        public bool interactable=true;
        public readonly UnityEngine.GameObject gameObject=new UnityEngine.GameObject();
        public Navigation navigation;
        public void Select() {}
    }
}
namespace UnityEngine.InputSystem.Controls
{
    public class ButtonControl { public bool isPressed, wasPressedThisFrame; }
    public class Vector2Control { public UnityEngine.Vector2 Value; public UnityEngine.Vector2 ReadValue() => Value; }
}
namespace UnityEngine.InputSystem
{
    using UnityEngine.InputSystem.Controls;
    public class Gamepad
    {
        public static Gamepad current;
        public readonly Vector2Control leftStick = new Vector2Control(), dpad = new Vector2Control();
        public readonly ButtonControl buttonSouth=new ButtonControl(), buttonEast=new ButtonControl(),
            buttonWest=new ButtonControl(), buttonNorth=new ButtonControl(), leftShoulder=new ButtonControl(),
            rightShoulder=new ButtonControl(), leftStickButton=new ButtonControl(), startButton=new ButtonControl();
    }
}
