using System;
using System.Collections.Generic;
using UnityEngine;

public static class PhoneControllerAuthService
{
    private static readonly Dictionary<string, DateTime> ActiveTokens = new Dictionary<string, DateTime>();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RefreshWindow = TimeSpan.FromMinutes(10);
    private const string PersistedTokensKey = "TIDE_PHONE_CONTROLLER_TOKENS";

    public static string GenerateToken()
    {
        string token = Guid.NewGuid().ToString("N");
        ActiveTokens[token] = DateTime.UtcNow + TokenLifetime;
        PersistTokens();
        return token;
    }

    public static bool ValidateToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (!ActiveTokens.TryGetValue(token, out DateTime expiresAt))
        {
            return false;
        }

        if (DateTime.UtcNow > expiresAt)
        {
            ActiveTokens.Remove(token);
            PersistTokens();
            return false;
        }

        return true;
    }

    public static bool RefreshToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (!ActiveTokens.TryGetValue(token, out DateTime expiresAt))
        {
            return false;
        }

        if (DateTime.UtcNow > expiresAt)
        {
            ActiveTokens.Remove(token);
            PersistTokens();
            return false;
        }

        TimeSpan remaining = expiresAt - DateTime.UtcNow;
        if (remaining > RefreshWindow)
        {
            return true;
        }

        ActiveTokens[token] = DateTime.UtcNow + TokenLifetime;
        PersistTokens();
        return true;
    }

    public static bool RegisterToken(string token, TimeSpan lifetime)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        ActiveTokens[token] = DateTime.UtcNow + lifetime;
        PersistTokens();
        return true;
    }

    public static int GetActiveTokenCount()
    {
        int count = 0;
        DateTime now = DateTime.UtcNow;
        List<string> expired = new List<string>();
        foreach (KeyValuePair<string, DateTime> kvp in ActiveTokens)
        {
            if (now > kvp.Value) expired.Add(kvp.Key);
            else count++;
        }
        for (int i = 0; i < expired.Count; i++)
        {
            ActiveTokens.Remove(expired[i]);
        }
        if (expired.Count > 0)
        {
            PersistTokens();
        }
        return count;
    }

    public static void RevokeAllTokens()
    {
        ActiveTokens.Clear();
        PersistTokens();
    }

    public static void LoadPersistedTokens()
    {
        string raw = PlayerPrefs.GetString(PersistedTokensKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            return;
        }

        string[] entries = raw.Split(';');
        DateTime now = DateTime.UtcNow;
        for (int i = 0; i < entries.Length; i++)
        {
            if (string.IsNullOrEmpty(entries[i]))
            {
                continue;
            }

            string[] parts = entries[i].Split(',');
            if (parts.Length != 2)
            {
                continue;
            }

            string token = parts[0];
            if (!long.TryParse(parts[1], out long ticks))
            {
                continue;
            }

            DateTime expiry = new DateTime(ticks, DateTimeKind.Utc);
            if (expiry > now)
            {
                ActiveTokens[token] = expiry;
            }
        }
    }

    private static void PersistTokens()
    {
        DateTime now = DateTime.UtcNow;
        List<string> entries = new List<string>();
        foreach (KeyValuePair<string, DateTime> kvp in ActiveTokens)
        {
            if (kvp.Value > now)
            {
                entries.Add(kvp.Key + "," + kvp.Value.Ticks);
            }
        }

        PlayerPrefs.SetString(PersistedTokensKey, string.Join(";", entries));
        PlayerPrefs.Save();
    }

    public static bool LogicOk()
    {
        RevokeAllTokens();
        string token = GenerateToken();
        bool ok = ValidateToken(token) && GetActiveTokenCount() == 1;
        RevokeAllTokens();
        return ok;
    }
}
