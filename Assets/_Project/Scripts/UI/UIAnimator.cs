using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight coroutine-based tween utility.
/// No external dependencies — attach to any GameObject or use a persistent host.
/// All methods return IEnumerator for use with StartCoroutine.
/// </summary>
public static class UIAnimator
{
    // ==========================================
    // EASING FUNCTIONS
    // ==========================================

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public static float EaseOutElastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float c4 = (2f * Mathf.PI) / 3f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
    }

    public static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public static float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    public static float EaseOutQuart(float t)
    {
        return 1f - Mathf.Pow(1f - t, 4f);
    }

    // ==========================================
    // SCALE
    // ==========================================

    /// <summary>
    /// Scales a transform from one value to another over duration.
    /// </summary>
    public static IEnumerator ScaleTo(Transform target, Vector3 from, Vector3 to, float duration,
        Func<float, float> ease = null)
    {
        if (target == null) yield break;
        ease ??= EaseOutCubic;

        target.localScale = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));
            if (target != null)
                target.localScale = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (target != null)
            target.localScale = to;
    }

    /// <summary>
    /// Quick "punch" scale — scales up then back to original. Great for confirm feedback.
    /// </summary>
    public static IEnumerator PunchScale(Transform target, float punchAmount = 0.2f, float duration = 0.3f)
    {
        if (target == null) yield break;

        Vector3 original = target.localScale;
        Vector3 punched = original * (1f + punchAmount);
        float half = duration * 0.3f; // quick punch up, slower settle

        // Punch up
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / half));
            if (target != null)
                target.localScale = Vector3.LerpUnclamped(original, punched, t);
            yield return null;
        }

        // Settle back with overshoot
        elapsed = 0f;
        float settleTime = duration - half;
        while (elapsed < settleTime)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / settleTime));
            if (target != null)
                target.localScale = Vector3.LerpUnclamped(punched, original, t);
            yield return null;
        }

        if (target != null)
            target.localScale = original;
    }

    // ==========================================
    // FADE (CanvasGroup)
    // ==========================================

    /// <summary>
    /// Fades a CanvasGroup alpha from one value to another.
    /// </summary>
    public static IEnumerator FadeTo(CanvasGroup group, float from, float to, float duration,
        Func<float, float> ease = null)
    {
        if (group == null) yield break;
        ease ??= EaseOutCubic;

        group.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));
            if (group != null)
                group.alpha = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (group != null)
            group.alpha = to;
    }

    // ==========================================
    // SLIDE (RectTransform)
    // ==========================================

    /// <summary>
    /// Slides a RectTransform from one anchored position to another.
    /// </summary>
    public static IEnumerator SlideTo(RectTransform target, Vector2 from, Vector2 to, float duration,
        Func<float, float> ease = null)
    {
        if (target == null) yield break;
        ease ??= EaseOutCubic;

        target.anchoredPosition = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));
            if (target != null)
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (target != null)
            target.anchoredPosition = to;
    }

    // ==========================================
    // COLOR
    // ==========================================

    /// <summary>
    /// Lerps a UnityEngine.UI.Image color.
    /// </summary>
    public static IEnumerator ColorTo(UnityEngine.UI.Image image, Color from, Color to, float duration,
        Func<float, float> ease = null)
    {
        if (image == null) yield break;
        ease ??= EaseOutCubic;

        image.color = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));
            if (image != null)
                image.color = Color.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (image != null)
            image.color = to;
    }

    /// <summary>
    /// Lerps a TMPro text alpha.
    /// </summary>
    public static IEnumerator TextFade(TMPro.TMP_Text text, float from, float to, float duration,
        Func<float, float> ease = null)
    {
        if (text == null) yield break;
        ease ??= EaseOutCubic;

        text.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));
            if (text != null)
                text.alpha = Mathf.LerpUnclamped(from, to, t);
            yield return null;
        }

        if (text != null)
            text.alpha = to;
    }

    // ==========================================
    // COMPOSITES
    // ==========================================

    /// <summary>
    /// Slide + fade in from a direction. Common for panel entrances.
    /// </summary>
    public static IEnumerator SlideAndFadeIn(RectTransform rect, CanvasGroup group,
        Vector2 offset, float duration, Func<float, float> ease = null)
    {
        if (rect == null) yield break;
        ease ??= EaseOutCubic;

        Vector2 target = rect.anchoredPosition;
        Vector2 start = target + offset;
        rect.anchoredPosition = start;

        if (group != null) group.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = ease(Mathf.Clamp01(elapsed / duration));

            if (rect != null)
                rect.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            if (group != null)
                group.alpha = Mathf.LerpUnclamped(0f, 1f, t);

            yield return null;
        }

        if (rect != null) rect.anchoredPosition = target;
        if (group != null) group.alpha = 1f;
    }

    /// <summary>
    /// Pop in from zero scale with optional fade.
    /// </summary>
    public static IEnumerator PopIn(Transform target, CanvasGroup group = null, float duration = 0.4f)
    {
        if (target == null) yield break;

        target.localScale = Vector3.zero;
        if (group != null) group.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));

            if (target != null)
                target.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, t);
            if (group != null)
                group.alpha = Mathf.Clamp01(t);

            yield return null;
        }

        if (target != null) target.localScale = Vector3.one;
        if (group != null) group.alpha = 1f;
    }

    // ==========================================
    // DELAY UTILITY
    // ==========================================

    /// <summary>
    /// Runs an action after a delay. Useful for staggered sequences.
    /// </summary>
    public static IEnumerator DelayedAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}
