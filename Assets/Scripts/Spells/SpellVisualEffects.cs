using System.Collections;
using UnityEngine;

public class SpellVisualEffects : MonoBehaviour
{
    private static SpellVisualEffects instance;
    private static Sprite pixelSprite;
    private static Sprite flameSprite;

    [SerializeField] private float effectZ = -8f;

    public static void PlayEarthSqueeze(CutWound wound)
    {
        SpriteRenderer renderer = GetWoundRenderer(wound);
        if (renderer == null)
        {
            return;
        }

        EnsureInstance();
        instance.StartCoroutine(instance.AnimateEarthSqueeze(renderer.transform));
    }

    public static void PlayFireDots(Vector3 center, float radius)
    {
        EnsureInstance();
        instance.EmitFlameBurst(center, Mathf.Max(0.12f, radius));
    }

    public static void PlayFireDrawFlame(Vector3 center)
    {
        EnsureInstance();
        instance.EmitDrawFlame(center);
    }

    public static void PlayWaterSplash(Vector3 center)
    {
        EnsureInstance();
        int count = Application.isMobilePlatform ? 14 : 36;
        instance.EmitDots(center, 0.5f, new Color(0.22f, 0.82f, 1f, 0.88f), count, 0.62f, 2.2f);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        SpellVisualEffects existing = FindAnyObjectByType<SpellVisualEffects>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject effectObject = new GameObject("SpellVisualEffects");
        instance = effectObject.AddComponent<SpellVisualEffects>();
    }

    private static SpriteRenderer GetWoundRenderer(CutWound wound)
    {
        return wound != null ? wound.GetComponentInChildren<SpriteRenderer>(true) : null;
    }

    private IEnumerator AnimateEarthSqueeze(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 baseScale = target.localScale;
        Vector3 compressedScale = new Vector3(baseScale.x * 0.72f, baseScale.y * 1.16f, baseScale.z);
        Vector3 reboundScale = new Vector3(baseScale.x * 1.05f, baseScale.y * 0.96f, baseScale.z);

        yield return ScaleOverTime(target, baseScale, compressedScale, 0.12f);
        yield return ScaleOverTime(target, compressedScale, reboundScale, 0.09f);
        yield return ScaleOverTime(target, reboundScale, baseScale, 0.12f);
    }

    private IEnumerator ScaleOverTime(Transform target, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / safeDuration));
            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = to;
        }
    }

    private void EmitDots(Vector3 center, float radius, Color color, int count, float lifetime, float speed)
    {
        center.z = effectZ;

        for (int i = 0; i < count; i++)
        {
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            Vector3 start = center + (Vector3)(Random.insideUnitCircle * radius * 0.28f);
            Vector3 velocity = new Vector3(direction.x, direction.y, 0f) * Random.Range(speed * 0.45f, speed);
            float size = Random.Range(0.035f, 0.08f);
            StartCoroutine(AnimateDot(start, velocity, color, size, Random.Range(lifetime * 0.75f, lifetime * 1.2f)));
        }
    }

    private void EmitDrawFlame(Vector3 center)
    {
        center.z = effectZ;
        Vector3 drift = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(0.25f, 0.5f), 0f);
        Color color = new Color(1f, Random.Range(0.28f, 0.52f), 0.04f, 0.24f);
        float size = Application.isMobilePlatform ? Random.Range(0.12f, 0.2f) : Random.Range(0.16f, 0.27f);
        float lifetime = Application.isMobilePlatform ? Random.Range(0.12f, 0.18f) : Random.Range(0.18f, 0.28f);
        StartCoroutine(AnimateFlame(center, drift, color, size, lifetime));
    }

    private void EmitFlameBurst(Vector3 center, float radius)
    {
        center.z = effectZ;
        int count = Application.isMobilePlatform ? 8 : 18;
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = Random.insideUnitCircle;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                direction = Vector2.up;
            }

            direction.Normalize();
            direction.y = Mathf.Abs(direction.y) + 0.22f;
            direction.Normalize();

            Vector3 start = center + (Vector3)(Random.insideUnitCircle * radius * 0.18f);
            Vector3 drift = new Vector3(direction.x, direction.y, 0f) * Random.Range(radius * 1.6f, radius * 3.1f);
            Color color = new Color(1f, Random.Range(0.22f, 0.58f), 0.02f, Random.Range(0.28f, 0.42f));
            StartCoroutine(AnimateFlame(start, drift, color, Random.Range(0.28f, 0.46f), Random.Range(0.3f, 0.48f)));
        }

        int emberCount = Application.isMobilePlatform ? 4 : 12;
        EmitDots(center, radius, new Color(1f, 0.58f, 0.08f, 0.35f), emberCount, 0.32f, 1.2f);
    }

    private IEnumerator AnimateFlame(Vector3 start, Vector3 drift, Color color, float size, float lifetime)
    {
        GameObject flameObject = new GameObject("SpellFireFlame");
        flameObject.transform.position = start;
        flameObject.transform.localScale = Vector3.one * size;

        SpriteRenderer renderer = flameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetFlameSprite();
        renderer.color = color;
        renderer.sortingOrder = 78;

        float elapsed = 0f;
        float rotationSpeed = Random.Range(-90f, 90f);
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
            flameObject.transform.position = Vector3.Lerp(start, start + drift, t);
            flameObject.transform.localScale = Vector3.one * Mathf.Lerp(size, size * 1.65f, t);
            flameObject.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            Color faded = color;
            faded.a = color.a * (1f - Mathf.SmoothStep(0.15f, 1f, t));
            renderer.color = faded;
            yield return null;
        }

        Destroy(flameObject);
    }

    private IEnumerator AnimateDot(Vector3 start, Vector3 velocity, Color color, float size, float lifetime)
    {
        GameObject dotObject = new GameObject("SpellEffectDot");
        dotObject.transform.position = start;
        dotObject.transform.localScale = Vector3.one * size;

        SpriteRenderer renderer = dotObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPixelSprite();
        renderer.color = color;
        renderer.sortingOrder = 80;

        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
            dotObject.transform.position += velocity * Time.deltaTime;
            dotObject.transform.localScale = Vector3.one * Mathf.Lerp(size, size * 0.35f, t);

            Color faded = color;
            faded.a = color.a * (1f - Mathf.SmoothStep(0f, 1f, t));
            renderer.color = faded;
            yield return null;
        }

        Destroy(dotObject);
    }

    private static Sprite GetPixelSprite()
    {
        if (pixelSprite != null)
        {
            return pixelSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        pixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 16f);
        return pixelSprite;
    }

    private static Sprite GetFlameSprite()
    {
        if (flameSprite != null)
        {
            return flameSprite;
        }

        int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)(size - 1) - 0.5f) * 2f;
                float v = y / (float)(size - 1);
                float width = Mathf.Lerp(0.75f, 0.08f, v);
                float body = Mathf.Clamp01(1f - Mathf.Abs(u) / Mathf.Max(0.01f, width));
                float taper = Mathf.Sin(v * Mathf.PI);
                float alpha = body * taper;
                alpha *= Mathf.SmoothStep(0f, 0.22f, v) * (1f - Mathf.SmoothStep(0.82f, 1f, v));

                Color color = Color.Lerp(new Color(1f, 0.12f, 0f, alpha), new Color(1f, 0.8f, 0.08f, alpha), v);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        flameSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.12f), 32f);
        return flameSprite;
    }
}
