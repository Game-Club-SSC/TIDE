using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class BattleHudPolishService
{
    private static Dictionary<Image, Coroutine> activeImageCoroutines = new Dictionary<Image, Coroutine>();
    private static Dictionary<SpriteRenderer, Coroutine> activeSpriteCoroutines = new Dictionary<SpriteRenderer, Coroutine>();
    private static Dictionary<Transform, Coroutine> activeTransformCoroutines = new Dictionary<Transform, Coroutine>();

    public static Color GetCritFlashColor()
    {
        return new Color(1f, 0.93f, 0.42f, 1f);
    }

    public static float GetCritFlashDuration()
    {
        return 0.4f;
    }

    public static Color GetStatusEffectIconColor(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.BuffAttack: return new Color(1f, 0.45f, 0.2f, 1f);
            case StatusEffectType.BuffDefense: return new Color(0.4f, 0.7f, 1f, 1f);
            case StatusEffectType.DebuffAttack: return new Color(0.5f, 0.5f, 0.5f, 1f);
            case StatusEffectType.DebuffDefense: return new Color(0.4f, 0.3f, 0.3f, 1f);
            case StatusEffectType.Poison: return new Color(0.4f, 0.85f, 0.2f, 1f);
            case StatusEffectType.Slow: return new Color(0.5f, 0.6f, 0.95f, 1f);
            case StatusEffectType.Drowsy: return new Color(0.7f, 0.55f, 0.95f, 1f);
            case StatusEffectType.Stun: return new Color(1f, 0.85f, 0.2f, 1f);
            case StatusEffectType.Burn: return new Color(1f, 0.45f, 0f, 1f);
            case StatusEffectType.Regeneration: return new Color(0.2f, 0.8f, 0.3f, 1f);
            case StatusEffectType.Shield: return new Color(0.2f, 0.75f, 0.9f, 1f);
            case StatusEffectType.Berserk: return new Color(0.85f, 0.15f, 0.15f, 1f);
            case StatusEffectType.Taunt: return new Color(1f, 0.55f, 0.1f, 1f);
            case StatusEffectType.AccuracyDown: return new Color(0.9f, 0.45f, 0.15f, 1f);
            case StatusEffectType.AccuracyBuff: return new Color(0.4f, 0.85f, 0.4f, 1f);
            case StatusEffectType.None: return Color.clear;
            default:
                Debug.LogWarning($"[BattleHudPolishService] Unknown StatusEffectType '{type}'. Returning white.");
                return Color.white;
        }
    }

    public static string GetStatusEffectLabel(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.BuffAttack: return "ATK Up";
            case StatusEffectType.BuffDefense: return "DEF Up";
            case StatusEffectType.DebuffAttack: return "ATK Down";
            case StatusEffectType.DebuffDefense: return "DEF Down";
            case StatusEffectType.Poison: return "Poison";
            case StatusEffectType.Slow: return "Slow";
            case StatusEffectType.Drowsy: return "Drowsy";
            case StatusEffectType.Stun: return "Stun";
            case StatusEffectType.Burn: return "Burn";
            case StatusEffectType.Regeneration: return "Regen";
            case StatusEffectType.Shield: return "Shield";
            case StatusEffectType.Berserk: return "Berserk";
            case StatusEffectType.Taunt: return "Taunt";
            case StatusEffectType.AccuracyDown: return "ACC Down";
            case StatusEffectType.AccuracyBuff: return "ACC Up";
            case StatusEffectType.None: return string.Empty;
            default: return type.ToString();
        }
    }

    public static Color GetMomentumBarColor(float momentumValue)
    {
        if (momentumValue >= 1f) return new Color(1f, 0.85f, 0.3f, 1f);
        if (momentumValue >= 0.5f) return new Color(0.95f, 0.65f, 0.4f, 1f);
        return new Color(0.4f, 0.7f, 0.95f, 1f);
    }

    public static Coroutine PlayCritFlash(MonoBehaviour host, Image target)
    {
        if (host == null || target == null) return null;
        if (activeImageCoroutines.TryGetValue(target, out Coroutine existing))
        {
            host.StopCoroutine(existing);
        }
        Coroutine coroutine = host.StartCoroutine(FlashRoutine(target, GetCritFlashColor(), GetCritFlashDuration()));
        activeImageCoroutines[target] = coroutine;
        return coroutine;
    }

    public static Coroutine PlayDamageShake(MonoBehaviour host, Transform target, float duration = 0.15f, float magnitude = 0.08f)
    {
        if (host == null || target == null) return null;
        if (activeTransformCoroutines.TryGetValue(target, out Coroutine existing))
        {
            host.StopCoroutine(existing);
        }
        Coroutine coroutine = host.StartCoroutine(ShakeRoutine(target, duration, magnitude));
        activeTransformCoroutines[target] = coroutine;
        return coroutine;
    }

    public static Coroutine PlayStatusPulse(MonoBehaviour host, Image target, StatusEffectType type, int pulseCount = 2, float pulseScale = 1.25f)
    {
        if (host == null || target == null) return null;
        if (activeImageCoroutines.TryGetValue(target, out Coroutine existing))
        {
            host.StopCoroutine(existing);
        }
        Coroutine coroutine = host.StartCoroutine(PulseRoutine(target, GetStatusEffectIconColor(type), pulseCount, pulseScale));
        activeImageCoroutines[target] = coroutine;
        return coroutine;
    }

    public static Coroutine PlayHitFlash(MonoBehaviour host, SpriteRenderer target, float duration = 0.12f)
    {
        if (host == null || target == null) return null;
        if (activeSpriteCoroutines.TryGetValue(target, out Coroutine existing))
        {
            host.StopCoroutine(existing);
        }
        Coroutine coroutine = host.StartCoroutine(SpriteFlashRoutine(target, duration));
        activeSpriteCoroutines[target] = coroutine;
        return coroutine;
    }

    private static IEnumerator FlashRoutine(Image target, Color flashColor, float duration)
    {
        if (target == null) yield break;
        Color original = target.color;
        target.color = flashColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.color = Color.Lerp(flashColor, original, t);
            yield return null;
        }
        if (target != null)
        {
            target.color = original;
            activeImageCoroutines.Remove(target);
        }
    }

    private static IEnumerator ShakeRoutine(Transform target, float duration, float magnitude)
    {
        if (target == null) yield break;
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damper = 1f - Mathf.Clamp01(elapsed / duration);
            float offsetX = Random.Range(-1f, 1f) * magnitude * damper;
            float offsetY = Random.Range(-1f, 1f) * magnitude * damper;
            target.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }
        if (target != null)
        {
            target.localPosition = originalPos;
            activeTransformCoroutines.Remove(target);
        }
    }

    private static IEnumerator PulseRoutine(Image target, Color pulseColor, int pulseCount, float pulseScale)
    {
        if (target == null) yield break;
        Transform t = target.rectTransform;
        Vector3 originalScale = t.localScale;
        Color originalColor = target.color;
        float pulseDuration = 0.15f;
        for (int i = 0; i < pulseCount; i++)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / pulseDuration);
                float scale = Mathf.Lerp(1f, pulseScale, p);
                t.localScale = originalScale * scale;
                target.color = Color.Lerp(originalColor, pulseColor, p);
                yield return null;
            }
            while (elapsed > 0f)
            {
                elapsed -= Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / pulseDuration);
                float scale = Mathf.Lerp(1f, pulseScale, p);
                t.localScale = originalScale * scale;
                target.color = Color.Lerp(originalColor, pulseColor, p);
                yield return null;
            }
        }
        if (target != null)
        {
            t.localScale = originalScale;
            target.color = originalColor;
        }
    }

    private static IEnumerator SpriteFlashRoutine(SpriteRenderer target, float duration)
    {
        if (target == null) yield break;
        Color original = target.color;
        Color flash = Color.white;
        target.color = flash;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            target.color = Color.Lerp(flash, original, t);
            yield return null;
        }
        if (target != null) target.color = original;
    }
}
