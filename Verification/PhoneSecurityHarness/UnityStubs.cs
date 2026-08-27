using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class DisallowMultipleComponentAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class HeaderAttribute : Attribute
    {
        internal HeaderAttribute(string value)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class SerializeFieldAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class TooltipAttribute : Attribute
    {
        internal TooltipAttribute(string value)
        {
        }
    }

    public class MonoBehaviour
    {
        protected object gameObject { get; } = new object();

        protected static void Destroy(object value)
        {
        }

        protected static void DontDestroyOnLoad(object value)
        {
        }
    }

    internal static class Debug
    {
        internal static void Log(object message)
        {
        }
    }

    internal static class PlayerPrefs
    {
        private static readonly Dictionary<string, string> Values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        internal static string GetString(string key, string defaultValue)
        {
            return Values.TryGetValue(key, out string value) ? value : defaultValue;
        }

        internal static void SetString(string key, string value)
        {
            Values[key] = value;
        }

        internal static void Save()
        {
        }
    }
}

internal sealed class PhoneInputBridge
{
    internal static PhoneInputBridge Instance => null;

    internal void SetPaired(bool paired)
    {
    }
}

internal static class GameStateSerializer
{
    internal static string BuildFullStateJson()
    {
        return "{}";
    }
}
