using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VitalsUI : MonoBehaviour
{
    public int test = 5;
    public Patient patient;

    [Header("UI")] 
    public Slider healthBar;
    public Slider bloodBar;
    public TextMeshProUGUI vitalsText;

    [Header("Blood Bottle")]
    public bool useBloodBottle = true;
    public Image bloodBottleImage;
    [SerializeField] private string bloodBottleSheetResource = "Sprites/BloodBottle_ANIMATED-SpriteSheeRESIZEt";
    [SerializeField] private string fallbackBloodBottleSheetResource = "Sprites/BloodBottleFULLSHEET";
    [SerializeField] private int horizontalBottleFrameCount = 20;
    [SerializeField] private int fallbackBottleSheetColumns = 5;
    [SerializeField] private int fallbackBottleSheetRows = 4;
    [SerializeField] private int bottlePixelScale = 2;
    [SerializeField] private Vector2 bottleOffsetFromVitalsPanel = new Vector2(0f, -18f);
    [SerializeField] private Vector2 mobileBottleOffset = new Vector2(48f, -32f);
    [SerializeField] private bool hideBloodSlidersWhenUsingBottle = true;
    [SerializeField] private bool hideOldVitalsTextWhenUsingBottle = true;
    [SerializeField] private bool hideOldVitalsPanelWhenUsingBottle = true;
    [SerializeField] private float bleedPulseScale = 0.035f;
    [SerializeField] private float bleedPulseSpeedMultiplier = 0.85f;
    [SerializeField] private float heavyBleedRate = 3f;

    [Header("Danger Vignette")]
    [SerializeField] private bool useDangerVignette = true;
    [SerializeField, Range(0f, 1f)] private float dangerBloodThresholdPercent = 0.55f;
    [SerializeField] private Color dangerVignetteColor = new Color(0.55f, 0.02f, 0.015f, 1f);
    [SerializeField] private float dangerVignetteMinAlpha = 0f;
    [SerializeField] private float dangerVignetteMaxAlpha = 0.68f;
    [SerializeField] private float dangerVignettePulseSpeed = 2.1f;
    [SerializeField] private float dangerVignettePulseSharpness = 1.7f;
    [SerializeField] private Color lowHealthTintColor = new Color(0.75f, 0.03f, 0.02f, 1f);
    [SerializeField] private float lowHealthTintMaxAlpha = 0.48f;

    private Sprite[] bloodBottleFrames;
    private int currentBottleFrame = -1;
    private RectTransform bloodBottleRect;
    private Image dangerVignetteImage;
    private Image lowHealthTintImage;

    void Start()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        EnsureDangerVignette();

        if (!useBloodBottle) return;

        bloodBottleFrames = LoadBottleFrames();
        EnsureBloodBottleImage();
        DisableOldBloodUI();

        if (hideBloodSlidersWhenUsingBottle)
        {
            if (bloodBar != null) bloodBar.gameObject.SetActive(false);
            if (healthBar != null) healthBar.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (patient == null)
        {
            patient = FindAnyObjectByType<Patient>();
        }

        if (useBloodBottle)
        {
            EnsureBloodBottleReady();
        }

        if (patient == null) return;

        if (bloodBar != null)
        {
            bloodBar.value = patient.bloodLevel;
        }

        if (healthBar != null)
        {
            healthBar.value = patient.bloodLevel;
        }

        UpdateBloodBottle();
        UpdateDangerVignette();
        DisableOldBloodUI();

        if (vitalsText != null && vitalsText.gameObject.activeInHierarchy)
        {
            float bleed = patient.getBleedRate();
            vitalsText.text =
                (useBloodBottle ? string.Empty : $"Blood: {patient.bloodLevel:F0}\n") +
                "Bleeding: " + (patient.bleed ? "YES":"NO") + "\n" +
                "Bleed/sec: " + bleed.ToString("F2");
        }
    }

    private void EnsureBloodBottleImage()
    {
        if (bloodBottleImage != null && bloodBottleImage.gameObject.activeInHierarchy) return;

        RectTransform parentRect = GetCanvasRect();
        if (parentRect == null)
        {
            RectTransform panelRect = FindVitalsPanelRect();
            parentRect = panelRect != null ? panelRect : vitalsText != null ? vitalsText.rectTransform.parent as RectTransform : null;
        }

        if (parentRect == null) return;

        bloodBottleImage = null;
        bloodBottleRect = null;

        GameObject bottleObject = new GameObject("BloodBottleVitals", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform bottleRect = bottleObject.GetComponent<RectTransform>();
        bloodBottleRect = bottleRect;
        bottleRect.SetParent(parentRect, false);
        bottleRect.anchorMin = new Vector2(0f, 1f);
        bottleRect.anchorMax = new Vector2(0f, 1f);
        bottleRect.pivot = new Vector2(0f, 1f);

        bloodBottleImage = bottleObject.GetComponent<Image>();
        bloodBottleImage.preserveAspect = true;
        bloodBottleImage.raycastTarget = false;
        ApplyBottleRectSize();
    }

    private void EnsureBloodBottleReady()
    {
        if (bloodBottleFrames == null || bloodBottleFrames.Length == 0)
        {
            bloodBottleFrames = LoadBottleFrames();
            currentBottleFrame = -1;
        }

        EnsureBloodBottleImage();

        if (bloodBottleImage == null)
        {
            return;
        }

        RectTransform canvasRect = GetCanvasRect();
        if (canvasRect != null && bloodBottleImage.rectTransform.parent != canvasRect)
        {
            bloodBottleImage.rectTransform.SetParent(canvasRect, false);
        }

        if (!bloodBottleImage.gameObject.activeSelf)
        {
            bloodBottleImage.gameObject.SetActive(true);
        }

        bloodBottleImage.enabled = true;
        bloodBottleImage.preserveAspect = true;
        bloodBottleImage.raycastTarget = false;

        if (bloodBottleImage.sprite == null)
        {
            currentBottleFrame = -1;
            UpdateBloodBottle();
        }
    }

    private void EnsureDangerVignette()
    {
        if (!useDangerVignette || (dangerVignetteImage != null && lowHealthTintImage != null)) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        if (canvas == null) return;

        if (lowHealthTintImage == null)
        {
            GameObject tintObject = new GameObject("PatientLowHealthTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform tintRect = tintObject.GetComponent<RectTransform>();
            tintRect.SetParent(canvas.transform, false);
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            tintRect.SetAsFirstSibling();

            lowHealthTintImage = tintObject.GetComponent<Image>();
            lowHealthTintImage.color = WithAlpha(lowHealthTintColor, 0f);
            lowHealthTintImage.raycastTarget = false;
        }

        if (dangerVignetteImage == null)
        {
            GameObject vignetteObject = new GameObject("PatientDangerVignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform vignetteRect = vignetteObject.GetComponent<RectTransform>();
            vignetteRect.SetParent(canvas.transform, false);
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.offsetMin = Vector2.zero;
            vignetteRect.offsetMax = Vector2.zero;
            vignetteRect.SetAsLastSibling();

            dangerVignetteImage = vignetteObject.GetComponent<Image>();
            dangerVignetteImage.sprite = CreateVignetteSprite(256);
            dangerVignetteImage.color = WithAlpha(dangerVignetteColor, 0f);
            dangerVignetteImage.raycastTarget = false;
        }
    }

    private void LateUpdate()
    {
        if (useBloodBottle)
        {
            ApplyBottleRectSize();
            ApplyBleedPulse();
        }
    }

    private void ApplyBottleRectSize()
    {
        if (bloodBottleRect == null && bloodBottleImage != null)
        {
            bloodBottleRect = bloodBottleImage.rectTransform;
        }

        if (bloodBottleRect == null) return;

        bloodBottleRect.localScale = Vector3.one;
        ApplyBottleScreenPlacement();
        if (bloodBottleImage == null || bloodBottleImage.sprite == null) return;

        Rect spriteRect = bloodBottleImage.sprite.rect;
        int scale = Mathf.Max(1, bottlePixelScale);
        bloodBottleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, spriteRect.width * scale);
        bloodBottleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spriteRect.height * scale);
    }

    private void DisableOldBloodUI()
    {
        if (vitalsText != null)
        {
            vitalsText.text = string.Empty;
            vitalsText.enabled = false;
            vitalsText.gameObject.SetActive(false);
        }

        GameObject oldVitalsPanel = GameObject.Find("VitalsUI");
        if (oldVitalsPanel != null && oldVitalsPanel != gameObject)
        {
            Image oldPanelImage = oldVitalsPanel.GetComponent<Image>();
            if (oldPanelImage != null)
            {
                oldPanelImage.enabled = false;
            }
        }
    }

    private RectTransform FindVitalsPanelRect()
    {
        GameObject panel = GameObject.Find("VitalsUI");
        if (panel == null || panel == gameObject) return null;
        return panel.GetComponent<RectTransform>();
    }

    private RectTransform GetCanvasRect()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindAnyObjectByType<Canvas>();
        }

        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private void ApplyBottleScreenPlacement()
    {
        if (bloodBottleRect == null)
        {
            return;
        }

        if (Application.isMobilePlatform || Screen.width > Screen.height)
        {
            RectTransform canvasRect = GetCanvasRect();
            if (canvasRect != null && bloodBottleRect.parent != canvasRect)
            {
                bloodBottleRect.SetParent(canvasRect, false);
            }

            bloodBottleRect.anchorMin = new Vector2(0f, 1f);
            bloodBottleRect.anchorMax = new Vector2(0f, 1f);
            bloodBottleRect.pivot = new Vector2(0f, 1f);
            bloodBottleRect.anchoredPosition = mobileBottleOffset;
            bloodBottleRect.SetAsLastSibling();
            return;
        }

        bloodBottleRect.anchoredPosition = bottleOffsetFromVitalsPanel;
    }

    private void UpdateBloodBottle()
    {
        if (!useBloodBottle || bloodBottleImage == null || bloodBottleFrames == null || bloodBottleFrames.Length == 0)
        {
            return;
        }

        float bloodPercent = patient != null
            ? Mathf.Clamp01(patient.bloodLevel / Mathf.Max(1f, patient.MaxBlood))
            : 1f;
        int frame = Mathf.RoundToInt((1f - bloodPercent) * (bloodBottleFrames.Length - 1));
        frame = Mathf.Clamp(frame, 0, bloodBottleFrames.Length - 1);
        if (frame == currentBottleFrame) return;

        currentBottleFrame = frame;
        bloodBottleImage.sprite = bloodBottleFrames[frame];
        ApplyBottleRectSize();
    }

    private void UpdateDangerVignette()
    {
        if (!useDangerVignette)
        {
            return;
        }

        if (dangerVignetteImage == null || lowHealthTintImage == null)
        {
            EnsureDangerVignette();
        }

        if (dangerVignetteImage == null || lowHealthTintImage == null)
        {
            return;
        }

        float bloodPercent = patient != null
            ? Mathf.Clamp01(patient.bloodLevel / Mathf.Max(1f, patient.MaxBlood))
            : 1f;
        float dangerThreshold = Mathf.Clamp01(dangerBloodThresholdPercent);
        float tintDanger = 1f - Mathf.Clamp01(bloodPercent / Mathf.Max(0.01f, dangerThreshold));
        lowHealthTintImage.color = WithAlpha(lowHealthTintColor, tintDanger * lowHealthTintMaxAlpha);

        bool shouldShow = patient != null && bloodPercent <= dangerThreshold;

        float targetAlpha = 0f;
        if (shouldShow)
        {
            float danger = tintDanger;
            float pulse = Mathf.Pow((Mathf.Sin(Time.unscaledTime * dangerVignettePulseSpeed * Mathf.PI * 2f) * 0.5f) + 0.5f, dangerVignettePulseSharpness);
            float pulseAlpha = Mathf.Lerp(0.78f, 1f, pulse);
            targetAlpha = Mathf.Lerp(dangerVignetteMinAlpha, dangerVignetteMaxAlpha, danger) * pulseAlpha;
        }

        dangerVignetteImage.color = WithAlpha(dangerVignetteColor, targetAlpha);
    }

    private void ApplyBleedPulse()
    {
        if (bloodBottleRect == null || patient == null)
        {
            return;
        }

        float bleedRate = Mathf.Max(0f, patient.getBleedRate());
        if (bleedRate <= 0f)
        {
            bloodBottleRect.localScale = Vector3.one;
            return;
        }

        float bleedIntensity = Mathf.Clamp01(bleedRate / Mathf.Max(0.01f, heavyBleedRate));
        float pulseSpeed = Mathf.Lerp(1.2f, 3.8f, bleedIntensity) * bleedPulseSpeedMultiplier;
        float pulse = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) * 0.5f) + 0.5f;
        float scale = 1f + (pulse * bleedPulseScale * bleedIntensity);
        bloodBottleRect.localScale = new Vector3(scale, scale, 1f);
    }

    private Sprite[] LoadBottleFrames()
    {
        Texture2D texture = Resources.Load<Texture2D>(bloodBottleSheetResource);
        if (texture != null)
        {
            return SliceHorizontalBottleSheet(texture, horizontalBottleFrameCount);
        }

        texture = Resources.Load<Texture2D>(fallbackBloodBottleSheetResource);
        if (texture != null)
        {
            return SliceGridBottleSheet(texture, fallbackBottleSheetColumns, fallbackBottleSheetRows);
        }

        Debug.LogWarning("Blood bottle sprite sheet could not be loaded from Resources/Sprites.");
        return new Sprite[0];
    }

    private Sprite[] SliceHorizontalBottleSheet(Texture2D texture, int frameCount)
    {
        frameCount = Mathf.Max(1, frameCount);
        int frameWidth = texture.width / frameCount;
        Sprite[] frames = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            float croppedWidth = frameWidth * 0.47f;
            float croppedX = i * frameWidth + ((frameWidth - croppedWidth) * 0.5f);
            Rect rect = new Rect(croppedX, 4f, croppedWidth, texture.height - 24f);
            frames[i] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 1f);
        }

        return frames;
    }

    private Sprite[] SliceGridBottleSheet(Texture2D texture, int columns, int rows)
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);

        int frameWidth = texture.width / columns;
        int frameHeight = texture.height / rows;
        Sprite[] frames = new Sprite[columns * rows];
        int frame = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                float y = texture.height - ((row + 1) * frameHeight);
                Rect rect = new Rect(column * frameWidth, y, frameWidth, frameHeight);
                frames[frame] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 1f);
                frame++;
            }
        }

        return frames;
    }

    private Sprite CreateVignetteSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxDistance = Vector2.Distance(Vector2.zero, center);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 1f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}


