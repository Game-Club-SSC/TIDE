using System;
using System.Collections.Generic;
using System.Text.Json;

// Test doubles model PlayerPrefs' working values and flushed values separately.
// This compiles the shipped persistence classes, not the Unity engine itself.
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    public class MonoBehaviour
    {
        public object gameObject = new object();
        protected static void Destroy(object value) { }
        protected static void DontDestroyOnLoad(object value) { }
    }
    public sealed class PlayerPrefsException : Exception
    {
        public PlayerPrefsException(string message) : base(message) { }
    }
    public static class Debug
    {
        public static void Log(string value) { }
        public static void LogWarning(string value) { }
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
    }
    public static class JsonUtility
    {
        public static string ToJson(object value) => JsonSerializer.Serialize(value, value.GetType(),
            new JsonSerializerOptions { IncludeFields = true });
    }
    public static class PlayerPrefs
    {
        private static Dictionary<string, string> values = new Dictionary<string, string>();
        private static Dictionary<string, string> flushed = new Dictionary<string, string>();
        public static Func<string, string, string> BeforeSet;
        public static Action BeforeSave;
        public static int SaveCount;
        public static bool HasKey(string key) => values.ContainsKey(key);
        public static string GetString(string key, string fallback = "") => values.TryGetValue(key, out string value) ? value : fallback;
        public static void SetString(string key, string value) { values[key] = BeforeSet == null ? value : BeforeSet(key, value); }
        public static void DeleteKey(string key) { values.Remove(key); }
        public static void Save()
        {
            BeforeSave?.Invoke();
            SaveCount++;
            flushed = new Dictionary<string, string>(values);
        }
        public static void Restart() { values = new Dictionary<string, string>(flushed); }
        public static void Reset()
        {
            values.Clear(); flushed.Clear(); BeforeSet = null; BeforeSave = null; SaveCount = 0;
        }
    }
}
public sealed class HeroData { public string heroId; }
public sealed class PartyManager
{
    public static PartyManager Instance;
    public HeroData[] ActiveParty;
    public HeroData[] GetActiveParty() => ActiveParty;
}
