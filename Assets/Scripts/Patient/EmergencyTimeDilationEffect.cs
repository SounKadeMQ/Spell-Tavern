using UnityEngine;
using UnityEngine.UI;

public class EmergencyTimeDilationEffect : MonoBehaviour
{
    static EmergencyTimeDilationEffect instance;

    [SerializeField] private float slowedTimeScale = 0.5f;
    [SerializeField] private Color purpleFadeColor = new Color(0.09f, 0f, 0.18f, 0.18f);
    [SerializeField] private Color edgePulseColor = new Color(0.42f, 0f, 0.78f, 0.28f);
    [SerializeField] private Color ecgColor = new Color(1f, 0.06f, 0.04f, 0.95f);
    [SerializeField] private Color ecgGlowColor = new Color(1f, 0.02f, 0.1f, 0.32f);
    [SerializeField] private Color bloodPixelColor = new Color(0.92f, 0f, 0f, 0.88f);
    [SerializeField] private float ecgPulseSpeed = 0.22f;
    [SerializeField] private Vector2 ecgSize = new Vector2(620f, 150f);
    [SerializeField] private float zoomOutMultiplier = 1.55f;
    [SerializeField] private float zoomOutDuration = 0.7f;
    [SerializeField] private float zoomInDuration = 0.85f;
    [SerializeField] private float ecgDrawDuration = 0.9f;
    [SerializeField] private float ecgFadeDuration = 0.65f;
    [SerializeField] private float pulseRampDuration = 1.25f;
    [SerializeField] private float impactShakeDuration = 0.55f;
    [SerializeField] private float impactShakeMagnitude = 0.08f;
    [SerializeField] private int bloodPixelCount = 46;

    Image fadeImage;
    Image edgeImage;
    LineRenderer ecgLine;
    LineRenderer ecgGlowLine;
    Canvas overlayCanvas;
    bool active;
    float pulseRamp;
    float baseOrthographicSize;
    bool hasBaseOrthographicSize;
    Coroutine activationRoutine;
    Coroutine impactRoutine;
    Vector3 impactBasePosition;
    bool hasImpactBasePosition;

    public static void Activate()
    {
        EnsureInstance();
        instance.SetEffectActive(true);
    }

    public static void Deactivate()
    {
        if (instance != null)
        {
            instance.SetEffectActive(false);
        }
    }

    public static void PlayImpact()
    {
        EnsureInstance();
        instance.PlayImpactInternal();
    }

    static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        EmergencyTimeDilationEffect existing = FindAnyObjectByType<EmergencyTimeDilationEffect>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject effectObject = new GameObject("EmergencyTimeDilationEffect");
        instance = effectObject.AddComponent<EmergencyTimeDilationEffect>();
    }

    void Awake()
    {
        instance = this;
    }

    void OnDisable()
    {
        if (active)
        {
            SetEffectActive(false);
        }
    }

    void Update()
    {
        if (!active || edgeImage == null)
        {
            return;
        }

        pulseRamp = Mathf.MoveTowards(pulseRamp, 1f, Time.unscaledDeltaTime / Mathf.Max(0.01f, pulseRampDuration));
        float pulse = (Mathf.Sin(Time.unscaledTime * ecgPulseSpeed * Mathf.PI * 2f) * 0.5f) + 0.5f;
        Color edgeColor = edgePulseColor;
        edgeColor.a = Mathf.Lerp(0.08f, edgePulseColor.a, pulse) * pulseRamp;
        edgeImage.color = edgeColor;
    }

    void SetEffectActive(bool enabled)
    {
        EnsureOverlay();
        active = enabled;
        GameplayPause.SetGameplayTimeScale(enabled ? slowedTimeScale : 1f);

        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        if (fadeImage != null)
        {
            fadeImage.enabled = enabled;
        }

        if (edgeImage != null)
        {
            edgeImage.enabled = enabled;
        }

        if (ecgLine != null)
        {
            ecgLine.enabled = false;
        }

        if (ecgGlowLine != null)
        {
            ecgGlowLine.enabled = false;
        }

        if (enabled)
        {
            pulseRamp = 0f;
            activationRoutine = StartCoroutine(PlayActivationSequence());
        }
        else
        {
            RestoreCameraZoom();
        }
    }

    void EnsureOverlay()
    {
        if (fadeImage != null && edgeImage != null && ecgLine != null && ecgGlowLine != null)
        {
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        overlayCanvas = canvas;
        fadeImage = CreateOverlayImage(canvas.transform, "EmergencyPurpleFade", null, purpleFadeColor);
        edgeImage = CreateOverlayImage(canvas.transform, "EmergencyEdgeAfterimage", CreateEdgeSprite(256), edgePulseColor);
        ecgGlowLine = CreateEcgLine(canvas.transform, "EmergencyECGGlow", ecgGlowColor, 16f);
        ecgLine = CreateEcgLine(canvas.transform, "EmergencyECG", ecgColor, 4f);
        SetOverlayOrder();
    }

    Image CreateOverlayImage(Transform parent, string name, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    System.Collections.IEnumerator PlayActivationSequence()
    {
        yield return ZoomOutThenIn();

        if (ecgLine != null && ecgGlowLine != null)
        {
            ecgLine.enabled = true;
            ecgGlowLine.enabled = true;
            yield return DrawEcgLine();
            yield return FadeEcgLine();
            ecgLine.enabled = false;
            ecgGlowLine.enabled = false;
        }

        activationRoutine = null;
    }

    System.Collections.IEnumerator ZoomOutThenIn()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
        {
            yield break;
        }

        if (!hasBaseOrthographicSize)
        {
            baseOrthographicSize = camera.orthographicSize;
            hasBaseOrthographicSize = true;
        }

        float startSize = camera.orthographicSize;
        float zoomedOutSize = baseOrthographicSize * zoomOutMultiplier;
        float elapsed = 0f;
        while (elapsed < zoomOutDuration && active)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, zoomOutDuration)));
            camera.orthographicSize = Mathf.Lerp(startSize, zoomedOutSize, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < zoomInDuration && active)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, zoomInDuration)));
            camera.orthographicSize = Mathf.Lerp(zoomedOutSize, baseOrthographicSize, t);
            yield return null;
        }

        if (active)
        {
            camera.orthographicSize = baseOrthographicSize;
        }
    }

    void PlayImpactInternal()
    {
        EnsureOverlay();
        SpawnBloodPixels();

        if (impactRoutine != null)
        {
            StopCoroutine(impactRoutine);
            RestoreImpactCameraPosition();
        }

        impactRoutine = StartCoroutine(ShakeCamera());
    }

    System.Collections.IEnumerator ShakeCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            impactRoutine = null;
            yield break;
        }

        Transform cameraTransform = camera.transform;
        impactBasePosition = cameraTransform.position;
        hasImpactBasePosition = true;
        float elapsed = 0f;

        while (elapsed < impactShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, impactShakeDuration));
            Vector2 jitter = Random.insideUnitCircle * impactShakeMagnitude * fade;
            cameraTransform.position = impactBasePosition + new Vector3(jitter.x, jitter.y, 0f);
            yield return null;
        }

        RestoreImpactCameraPosition();
        impactRoutine = null;
    }

    void RestoreImpactCameraPosition()
    {
        if (!hasImpactBasePosition)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = impactBasePosition;
        }

        hasImpactBasePosition = false;
    }

    void SpawnBloodPixels()
    {
        if (overlayCanvas == null)
        {
            overlayCanvas = FindAnyObjectByType<Canvas>();
        }

        if (overlayCanvas == null)
        {
            return;
        }

        if (overlayCanvas.transform as RectTransform == null)
        {
            return;
        }

        for (int i = 0; i < bloodPixelCount; i++)
        {
            GameObject dotObject = new GameObject("EmergencyBloodPixel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            dotRect.SetParent(overlayCanvas.transform, false);
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = Vector2.one * Random.Range(2f, 5f);

            Vector2 burst = Random.insideUnitCircle;
            burst.x *= 190f;
            burst.y *= 86f;
            dotRect.anchoredPosition = burst;

            Image dotImage = dotObject.GetComponent<Image>();
            dotImage.color = bloodPixelColor;
            dotImage.raycastTarget = false;
            dotRect.SetSiblingIndex(GetEffectSiblingIndex(edgeImage != null ? edgeImage.transform : null));
            StartCoroutine(FadeBloodPixel(dotRect, dotImage, Random.insideUnitCircle * Random.Range(28f, 84f)));
        }
    }

    System.Collections.IEnumerator FadeBloodPixel(RectTransform dotRect, Image dotImage, Vector2 drift)
    {
        float duration = Random.Range(0.55f, 0.95f);
        float elapsed = 0f;
        Vector2 start = dotRect.anchoredPosition;
        Color startColor = dotImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            dotRect.anchoredPosition = Vector2.Lerp(start, start + drift, t);
            Color color = startColor;
            color.a = startColor.a * (1f - Mathf.SmoothStep(0f, 1f, t));
            dotImage.color = color;
            yield return null;
        }

        if (dotRect != null)
        {
            Destroy(dotRect.gameObject);
        }
    }

    System.Collections.IEnumerator DrawEcgLine()
    {
        Vector3[] points = GetEcgPoints();
        float elapsed = 0f;
        while (elapsed < ecgDrawDuration && active)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, ecgDrawDuration));
            ApplyPartialEcg(points, t);
            SetLineAlpha(ecgLine, ecgColor, ecgColor.a);
            SetLineAlpha(ecgGlowLine, ecgGlowColor, ecgGlowColor.a);
            yield return null;
        }

        ApplyPartialEcg(points, 1f);
    }

    System.Collections.IEnumerator FadeEcgLine()
    {
        float elapsed = 0f;
        while (elapsed < ecgFadeDuration && active)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, ecgFadeDuration));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, t);
            SetLineAlpha(ecgLine, ecgColor, ecgColor.a * fade);
            SetLineAlpha(ecgGlowLine, ecgGlowColor, ecgGlowColor.a * fade);
            yield return null;
        }
    }

    void RestoreCameraZoom()
    {
        Camera camera = Camera.main;
        if (camera != null && camera.orthographic && hasBaseOrthographicSize)
        {
            camera.orthographicSize = baseOrthographicSize;
        }

        hasBaseOrthographicSize = false;
    }

    LineRenderer CreateEcgLine(Transform parent, string name, Color color, float width)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(LineRenderer));
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = ecgSize;

        LineRenderer line = lineObject.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = false;
        line.positionCount = 13;
        line.widthMultiplier = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 5000;
        SetLineAlpha(line, color, color.a);
        line.SetPositions(GetEcgPoints());
        line.enabled = false;
        return line;
    }

    void SetOverlayOrder()
    {
        if (fadeImage != null)
        {
            fadeImage.transform.SetAsFirstSibling();
        }

        if (edgeImage != null)
        {
            edgeImage.transform.SetSiblingIndex(GetEffectSiblingIndex(fadeImage != null ? fadeImage.transform : null));
        }

        if (ecgGlowLine != null)
        {
            ecgGlowLine.transform.SetSiblingIndex(GetEffectSiblingIndex(edgeImage != null ? edgeImage.transform : null));
        }

        if (ecgLine != null)
        {
            ecgLine.transform.SetSiblingIndex(GetEffectSiblingIndex(ecgGlowLine != null ? ecgGlowLine.transform : null));
        }
    }

    int GetEffectSiblingIndex(Transform previousEffect)
    {
        if (previousEffect == null)
        {
            return 0;
        }

        return Mathf.Min(previousEffect.GetSiblingIndex() + 1, previousEffect.parent.childCount - 1);
    }

    Sprite CreateEdgeSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = Vector2.Distance(Vector2.zero, center);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0.56f, 0.98f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    Vector3[] GetEcgPoints()
    {
        return new[]
        {
            new Vector3(-310f, 0f, 0f),
            new Vector3(-210f, 0f, 0f),
            new Vector3(-178f, -18f, 0f),
            new Vector3(-145f, 58f, 0f),
            new Vector3(-115f, -92f, 0f),
            new Vector3(-74f, 0f, 0f),
            new Vector3(16f, 0f, 0f),
            new Vector3(56f, -26f, 0f),
            new Vector3(104f, 126f, 0f),
            new Vector3(145f, -58f, 0f),
            new Vector3(214f, 0f, 0f),
            new Vector3(270f, 0f, 0f),
            new Vector3(310f, 0f, 0f)
        };
    }

    void SetLineAlpha(LineRenderer line, Color baseColor, float alpha)
    {
        Color color = baseColor;
        color.a = alpha;
        line.startColor = color;
        line.endColor = color;
    }

    void ApplyPartialEcg(Vector3[] points, float progress)
    {
        if (points == null || points.Length < 2 || ecgLine == null || ecgGlowLine == null)
        {
            return;
        }

        int segmentCount = points.Length - 1;
        float scaledProgress = Mathf.Clamp01(progress) * segmentCount;
        int fullSegments = Mathf.FloorToInt(scaledProgress);
        float segmentT = scaledProgress - fullSegments;
        int visiblePoints = Mathf.Clamp(fullSegments + 2, 2, points.Length);

        Vector3[] visible = new Vector3[visiblePoints];
        for (int i = 0; i < visiblePoints; i++)
        {
            visible[i] = points[i];
        }

        if (fullSegments < segmentCount)
        {
            visible[visiblePoints - 1] = Vector3.Lerp(points[fullSegments], points[fullSegments + 1], segmentT);
        }

        ecgLine.positionCount = visiblePoints;
        ecgGlowLine.positionCount = visiblePoints;
        ecgLine.SetPositions(visible);
        ecgGlowLine.SetPositions(visible);
    }
}
