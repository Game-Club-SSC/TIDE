using System;
using System.Collections.Generic;

public static class PhoneControllerAuthService
{
    private static readonly Dictionary<string, DateTime> ActiveTokens = new Dictionary<string, DateTime>();
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    public static string GenerateToken()
    {
        string token = Guid.NewGuid().ToString("N");
        ActiveTokens[token] = DateTime.UtcNow + TokenLifetime;
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
            return false;
        }

        return true;
    }

    public static bool RegisterToken(string token, TimeSpan lifetime)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        ActiveTokens[token] = DateTime.UtcNow + lifetime;
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
        return count;
    }

    public static void RevokeAllTokens()
    {
        ActiveTokens.Clear();
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
