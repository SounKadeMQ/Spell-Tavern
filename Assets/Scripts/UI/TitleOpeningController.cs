using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleOpeningController : MonoBehaviour
{
    class BloodDrop
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public float startY;
        public float endY;
        public float delay;
        public float duration;
        public bool hasPlayedSound;
    }

    class EcgSegment
    {
        public RectTransform rectTransform;
        public Image image;
        public float length;
        public Vector2 start;
        public Vector2 end;
        public bool triggerHeartbeatOnComplete;
    }

    [SerializeField] private string missionSelectSceneName = "ChapterSelect";
    [SerializeField] private Color backgroundColor = new Color(0.01f, 0.012f, 0.015f, 1f);
    [SerializeField] private Color bloodColor = new Color(0.55f, 0.02f, 0.015f, 0.82f);
    [SerializeField] private Color ecgColor = new Color(0.68f, 0.02f, 0.08f, 1f);
    [SerializeField] private float bloodDropDuration = 2.4f;
    [SerializeField] private float ecgDrawDuration = 1.25f;
    [SerializeField] private float ecgTitleDimAlpha = 0.28f;
    [SerializeField] private string titleTextValue = "SPELLTAVERN";
    [SerializeField] private float titleCharacterRevealInterval = 0.075f;
    [SerializeField] private float titleCharacterPopScale = 1.08f;
    [SerializeField] private float titleCharacterPopDuration = 0.08f;
    [SerializeField] private float menuFadeDuration = 0.55f;
    [SerializeField] private float bloodDropVolume = 0.55f;
    [SerializeField] private float heartbeatVolume = 0.35f;
    [SerializeField] private float titleTypewriterVolume = 0.38f;
    [SerializeField] private AudioClip titleTypewriterClip;
    [SerializeField] private float titlePulseScale = 1.045f;
    [SerializeField] private float titlePulseDuration = 1.35f;
    [SerializeField] private float ecgGlowLoopDuration = 2.4f;
    [SerializeField] private float ecgGlowLoopDelay = 0.65f;
    [SerializeField] private float ecgLineThickness = 7f;
    [SerializeField] private float ecgGlowSize = 28f;
    [SerializeField] private Color heartbeatVignetteColor = new Color(0.55f, 0.02f, 0.015f, 1f);
    [SerializeField] private float heartbeatVignetteMinAlpha = 0.06f;
    [SerializeField] private float heartbeatVignetteMaxAlpha = 0.22f;
    [SerializeField] private float heartbeatVignetteFallbackDuration = 1.35f;
    [SerializeField] private float heartbeatVignettePulseSharpness = 2.6f;
    [SerializeField] private string debugUnlockCode = "debug";

    private BloodDrop[] bloodDrops;
    private AudioSource bloodDropSource;
    private AudioSource heartbeatSource;
    private AudioSource titleTypewriterSource;
    private AudioClip bloodDropClip;
    private AudioClip heartbeatClip;
    private CanvasGroup ecgGroup;
    private CanvasGroup titleGroup;
    private CanvasGroup menuGroup;
    private EcgSegment[] ecgSegments;
    private Image ecgGlow;
    private Image heartbeatVignette;
    private RectTransform titleRect;
    private TextMeshProUGUI titleText;
    private RectTransform openingRoot;
    private GameObject debugMenuRoot;
    private MissionData[] debugSurgeryMissions;
    private string debugInputBuffer = string.Empty;
    private bool isDebugMenuOpen;

    void Start()
    {
        BuildOpening();
        StartCoroutine(PlayOpening());
    }

    void BuildOpening()
    {
        EnsureEventSystem();

        GameObject canvasObject = new GameObject("TitleOpeningCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = canvasObject.AddComponent<Image>();
        background.color = backgroundColor;

        bloodDropSource = canvasObject.AddComponent<AudioSource>();
        bloodDropSource.playOnAwake = false;
        bloodDropClip = Resources.Load<AudioClip>("Audio/bloodDrop");

        titleTypewriterSource = canvasObject.AddComponent<AudioSource>();
        titleTypewriterSource.playOnAwake = false;
        titleTypewriterSource.volume = titleTypewriterVolume;
        if (titleTypewriterClip == null)
        {
            titleTypewriterClip = Resources.Load<AudioClip>("Audio/voi_test");
        }

        heartbeatSource = canvasObject.AddComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;
        heartbeatSource.loop = true;
        heartbeatSource.volume = heartbeatVolume;
        heartbeatClip = Resources.Load<AudioClip>("Audio/HeartBeat");
        if (heartbeatClip != null)
        {
            heartbeatSource.clip = heartbeatClip;
            heartbeatSource.Play();
        }

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        openingRoot = root;
        CreateHeartbeatVignette(root);
        CreateBloodDrops(root);
        CreateEcgLine(root);
        CreateTitle(root);
        CreateMenu(root);

        StartCoroutine(PulseHeartbeatVignette());
    }

    void Update()
    {
        if (isDebugMenuOpen)
        {
            HandleDebugMenuInput();
            return;
        }

        ListenForDebugCode();
    }

    void CreateHeartbeatVignette(RectTransform root)
    {
        GameObject vignetteObject = new GameObject("HeartbeatVignette", typeof(RectTransform), typeof(Image));
        vignetteObject.transform.SetParent(root, false);

        RectTransform rectTransform = vignetteObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        heartbeatVignette = vignetteObject.GetComponent<Image>();
        heartbeatVignette.sprite = CreateVignetteSprite(256);
        heartbeatVignette.color = WithAlpha(heartbeatVignetteColor, heartbeatVignetteMinAlpha);
        heartbeatVignette.raycastTarget = false;
    }

    void CreateBloodDrops(RectTransform root)
    {
        Sprite dropSprite = CreateCircleSprite(96);
        int dropCount = 18;
        float[] sizes = { 22f, 28f, 34f, 36f, 20f, 22f, 24f, 38f, 44f, 52f, 24f, 20f, 28f, 32f, 34f, 42f, 26f, 30f };
        float[] delays = { 0f, 0.18f, 0.42f, 0.62f, 0.9f, 0.55f, 0.32f, 0.72f, 0.76f, 0.36f, 1.08f, 1.02f, 0.06f, 1.26f, 0.92f, 0.28f, 1.16f, 0.48f };
        float[] durations = { 1.7f, 1.8f, 1.62f, 1.55f, 2.2f, 2.1f, 1.95f, 1.74f, 1.72f, 1.7f, 1.92f, 2.25f, 2.1f, 1.82f, 1.86f, 1.62f, 2.05f, 1.9f };
        bloodDrops = new BloodDrop[dropCount];

        for (int i = 0; i < dropCount; i++)
        {
            float xPosition = Mathf.Lerp(0.04f, 0.96f, (float)i / (dropCount - 1));
            GameObject dropObject = new GameObject("BloodDrop_" + i, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            dropObject.transform.SetParent(root, false);

            RectTransform rectTransform = dropObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(xPosition, 1.12f);
            rectTransform.anchorMax = new Vector2(xPosition, 1.12f);
            rectTransform.sizeDelta = new Vector2(sizes[i], sizes[i] * 1.28f);
            rectTransform.anchoredPosition = Vector2.zero;

            Image image = dropObject.GetComponent<Image>();
            image.sprite = dropSprite;
            image.color = bloodColor;
            image.raycastTarget = false;

            CanvasGroup group = dropObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;

            bloodDrops[i] = new BloodDrop
            {
                rectTransform = rectTransform,
                canvasGroup = group,
                startY = 1.1f + (i % 2 * 0.08f),
                endY = -0.16f - (i % 3 * 0.04f),
                delay = delays[i],
                duration = durations[i]
            };
        }
    }

    void CreateEcgLine(RectTransform root)
    {
        RectTransform ecgRoot = CreatePanel("EcgRoot", root, new Vector2(0.02f, 0.38f), new Vector2(0.98f, 0.68f), Vector2.zero, Vector2.zero);
        ecgGroup = ecgRoot.gameObject.AddComponent<CanvasGroup>();
        ecgGroup.alpha = 1f;

        Vector2[] points =
        {
            new Vector2(0f, 0.5f),
            new Vector2(0.2f, 0.5f),
            new Vector2(0.3f, 0.96f),
            new Vector2(0.35f, 0.08f),
            new Vector2(0.405f, 0.5f),
            new Vector2(0.72f, 0.5f),
            new Vector2(0.74f, 0.5f),
            new Vector2(0.765f, 0.2f),
            new Vector2(0.805f, 0.82f),
            new Vector2(0.835f, 0.5f),
            new Vector2(1f, 0.5f)
        };
        bool[] heartbeatPointTriggers =
        {
            false,
            false,
            true,
            true,
            false,
            false,
            false,
            true,
            true,
            false,
            false
        };

        ecgSegments = new EcgSegment[points.Length - 1];
        for (int i = 0; i < points.Length - 1; i++)
        {
            ecgSegments[i] = CreateLineSegment(ecgRoot, points[i], points[i + 1], ecgLineThickness, ecgColor);
            ecgSegments[i].triggerHeartbeatOnComplete = heartbeatPointTriggers[i + 1];
            ecgSegments[i].image.gameObject.SetActive(false);
        }

        ecgGlow = CreateGlow(ecgRoot);
        ecgGlow.gameObject.SetActive(false);
    }

    void CreateTitle(RectTransform root)
    {
        RectTransform titleRoot = CreatePanel("TitleRoot", root, new Vector2(0.18f, 0.48f), new Vector2(0.82f, 0.72f), Vector2.zero, Vector2.zero);
        titleGroup = titleRoot.gameObject.AddComponent<CanvasGroup>();
        titleGroup.alpha = 0f;
        titleRect = titleRoot;

        titleText = CreateText("TitleText", titleRoot, titleTextValue, 76f, TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = Vector2.zero;
        titleText.rectTransform.anchorMax = Vector2.one;
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = Vector2.zero;
        titleText.color = new Color(0.92f, 0.86f, 0.76f, 1f);
        titleText.fontStyle = FontStyles.UpperCase;
        titleText.characterSpacing = 0f;
        titleText.maxVisibleCharacters = 0;
    }

    void CreateMenu(RectTransform root)
    {
        RectTransform menuRoot = CreatePanel("MenuRoot", root, new Vector2(0.34f, 0.12f), new Vector2(0.66f, 0.3f), Vector2.zero, Vector2.zero);
        menuGroup = menuRoot.gameObject.AddComponent<CanvasGroup>();
        menuGroup.alpha = 0f;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;

        Button playButton = CreateButton("PlayButton", menuRoot, "Play", new Vector2(0f, 0.52f), new Vector2(1f, 1f));
        playButton.onClick.AddListener(Play);

        Button exitButton = CreateButton("ExitButton", menuRoot, "Exit", new Vector2(0f, 0f), new Vector2(1f, 0.42f));
        exitButton.onClick.AddListener(Exit);
    }

    void ListenForDebugCode()
    {
        if (string.IsNullOrEmpty(debugUnlockCode))
        {
            return;
        }

        string typed = Input.inputString;
        if (string.IsNullOrEmpty(typed))
        {
            return;
        }

        for (int i = 0; i < typed.Length; i++)
        {
            char c = char.ToLowerInvariant(typed[i]);
            if (!char.IsLetterOrDigit(c))
            {
                continue;
            }

            debugInputBuffer += c;
            if (debugInputBuffer.Length > debugUnlockCode.Length)
            {
                debugInputBuffer = debugInputBuffer.Substring(debugInputBuffer.Length - debugUnlockCode.Length);
            }

            if (debugInputBuffer == debugUnlockCode.ToLowerInvariant())
            {
                OpenDebugMenu();
                debugInputBuffer = string.Empty;
                return;
            }
        }
    }

    void HandleDebugMenuInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDebugMenu();
            return;
        }

        if (debugSurgeryMissions == null)
        {
            return;
        }

        for (int i = 0; i < debugSurgeryMissions.Length && i < 9; i++)
        {
            KeyCode key = (KeyCode)((int)KeyCode.Alpha1 + i);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad1 + i);
            if (Input.GetKeyDown(key) || Input.GetKeyDown(keypadKey))
            {
                LoadDebugSurgery(debugSurgeryMissions[i]);
                return;
            }
        }
    }

    void OpenDebugMenu()
    {
        if (openingRoot == null || isDebugMenuOpen)
        {
            return;
        }

        debugSurgeryMissions = LoadDebugSurgeryMissions();
        debugMenuRoot = new GameObject("DebugSurgeryMenu", typeof(RectTransform), typeof(Image));
        debugMenuRoot.transform.SetParent(openingRoot, false);

        RectTransform root = debugMenuRoot.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.18f, 0.14f);
        root.anchorMax = new Vector2(0.82f, 0.86f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        Image panelImage = debugMenuRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0.035f, 0.015f, 0.96f);

        TextMeshProUGUI header = CreateText("Header", root, "DEBUG SURGERY SELECT", 42f, TextAlignmentOptions.TopLeft);
        header.rectTransform.anchorMin = new Vector2(0f, 0.86f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.offsetMin = new Vector2(36f, 0f);
        header.rectTransform.offsetMax = new Vector2(-36f, -24f);
        header.color = new Color(0.36f, 1f, 0.42f, 1f);
        header.fontStyle = FontStyles.UpperCase;

        TextMeshProUGUI hint = CreateText("Hint", root, "PRESS 1-9 TO LOAD - ESC TO CLOSE", 24f, TextAlignmentOptions.TopLeft);
        hint.rectTransform.anchorMin = new Vector2(0f, 0.78f);
        hint.rectTransform.anchorMax = new Vector2(1f, 0.86f);
        hint.rectTransform.offsetMin = new Vector2(36f, 0f);
        hint.rectTransform.offsetMax = new Vector2(-36f, 0f);
        hint.color = new Color(0.2f, 0.82f, 0.28f, 1f);

        BuildDebugMissionRows(root);
        isDebugMenuOpen = true;
    }

    MissionData[] LoadDebugSurgeryMissions()
    {
        MissionData[] missions = Resources.LoadAll<MissionData>("MissionData");
        System.Array.Sort(missions, CompareMissions);

        int count = 0;
        for (int i = 0; i < missions.Length; i++)
        {
            if (missions[i] != null && missions[i].kind == MissionData.MissionKind.Surgery)
            {
                count++;
            }
        }

        MissionData[] surgeries = new MissionData[count];
        int index = 0;
        for (int i = 0; i < missions.Length; i++)
        {
            if (missions[i] != null && missions[i].kind == MissionData.MissionKind.Surgery)
            {
                surgeries[index] = missions[i];
                index++;
            }
        }

        return surgeries;
    }

    void BuildDebugMissionRows(RectTransform root)
    {
        if (debugSurgeryMissions == null || debugSurgeryMissions.Length == 0)
        {
            TextMeshProUGUI empty = CreateText("Empty", root, "NO SURGERIES FOUND", 30f, TextAlignmentOptions.TopLeft);
            empty.rectTransform.anchorMin = new Vector2(0f, 0.12f);
            empty.rectTransform.anchorMax = new Vector2(1f, 0.76f);
            empty.rectTransform.offsetMin = new Vector2(36f, 0f);
            empty.rectTransform.offsetMax = new Vector2(-36f, 0f);
            empty.color = new Color(0.36f, 1f, 0.42f, 1f);
            return;
        }

        int rowCount = Mathf.Min(debugSurgeryMissions.Length, 9);
        for (int i = 0; i < rowCount; i++)
        {
            MissionData mission = debugSurgeryMissions[i];
            float yMax = 0.74f - (i * 0.1f);
            float yMin = yMax - 0.075f;
            Button rowButton = CreateButton("DebugMission_" + i, root, FormatDebugMissionLabel(i, mission), new Vector2(0.04f, yMin), new Vector2(0.96f, yMax));
            Image image = rowButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0.11f, 0.035f, 0.88f);
            }

            TextMeshProUGUI label = rowButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 26f;
                label.alignment = TextAlignmentOptions.Left;
                label.color = new Color(0.36f, 1f, 0.42f, 1f);
                label.rectTransform.offsetMin = new Vector2(18f, 0f);
                label.rectTransform.offsetMax = new Vector2(-18f, 0f);
            }

            MissionData capturedMission = mission;
            rowButton.onClick.AddListener(() => LoadDebugSurgery(capturedMission));
        }
    }

    string FormatDebugMissionLabel(int index, MissionData mission)
    {
        string code = mission != null ? mission.episodeCode : "--";
        string title = mission != null ? mission.displayName : "EMPTY";
        return (index + 1) + "  >  " + code + "  " + title;
    }

    void CloseDebugMenu()
    {
        if (debugMenuRoot != null)
        {
            Destroy(debugMenuRoot);
            debugMenuRoot = null;
        }

        isDebugMenuOpen = false;
    }

    void LoadDebugSurgery(MissionData mission)
    {
        if (mission == null)
        {
            return;
        }

        MissionFlowState.SetCurrentMission(mission);
        SceneManager.LoadScene(mission.sceneName);
    }

    int CompareMissions(MissionData left, MissionData right)
    {
        if (left == null && right == null)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int orderComparison = left.episodeOrder.CompareTo(right.episodeOrder);
        if (orderComparison != 0)
        {
            return orderComparison;
        }

        return string.Compare(left.missionId, right.missionId, System.StringComparison.Ordinal);
    }

    IEnumerator PlayOpening()
    {
        float elapsed = 0f;
        float totalBloodDropDuration = GetTotalBloodDropDuration();
        while (elapsed < totalBloodDropDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            UpdateBloodDrops(elapsed);

            yield return null;
        }

        UpdateBloodDrops(totalBloodDropDuration);

        yield return new WaitForSecondsRealtime(0.25f);

        yield return DrawEcgLine();

        yield return FadeCanvasGroup(ecgGroup, 1f, ecgTitleDimAlpha, 0.35f);
        yield return RevealTitleTypewriter();
        yield return new WaitForSecondsRealtime(0.25f);
        yield return FadeCanvasGroup(menuGroup, 0f, 1f, menuFadeDuration);

        menuGroup.interactable = true;
        menuGroup.blocksRaycasts = true;

        StartCoroutine(PulseTitle());
        StartCoroutine(LoopEcgGlow());
    }

    float GetTotalBloodDropDuration()
    {
        if (bloodDrops == null || bloodDrops.Length == 0)
        {
            return bloodDropDuration;
        }

        float totalDuration = bloodDropDuration;
        for (int i = 0; i < bloodDrops.Length; i++)
        {
            BloodDrop drop = bloodDrops[i];
            if (drop != null)
            {
                totalDuration = Mathf.Max(totalDuration, drop.delay + drop.duration);
            }
        }

        return totalDuration;
    }

    IEnumerator DrawEcgLine()
    {
        if (ecgSegments == null || ecgSegments.Length == 0)
        {
            yield break;
        }

        float totalLength = 0f;
        for (int i = 0; i < ecgSegments.Length; i++)
        {
            totalLength += ecgSegments[i].length;
        }

        if (ecgGlow != null)
        {
            ecgGlow.gameObject.SetActive(true);
        }

        for (int i = 0; i < ecgSegments.Length; i++)
        {
            EcgSegment segment = ecgSegments[i];
            if (segment == null || segment.rectTransform == null)
            {
                continue;
            }

            segment.image.gameObject.SetActive(true);
            float segmentDuration = ecgDrawDuration * (segment.length / Mathf.Max(1f, totalLength));
            float elapsed = 0f;

            while (elapsed < segmentDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, segmentDuration));
                float eased = Mathf.SmoothStep(0f, 1f, t);
                segment.rectTransform.sizeDelta = new Vector2(segment.length * eased, segment.rectTransform.sizeDelta.y);

                if (ecgGlow != null)
                {
                    MoveGlow(Vector2.Lerp(segment.start, segment.end, eased));
                }

                yield return null;
            }

            segment.rectTransform.sizeDelta = new Vector2(segment.length, segment.rectTransform.sizeDelta.y);

            if (segment.triggerHeartbeatOnComplete)
            {
                PlayHeartbeatHit();
            }
        }

        if (ecgGlow != null)
        {
            yield return FadeGraphic(ecgGlow, ecgGlow.color.a, 0f, 0.22f);
            ecgGlow.gameObject.SetActive(false);
        }
    }

    IEnumerator RevealTitleTypewriter()
    {
        if (titleGroup == null || titleText == null || titleRect == null)
        {
            yield break;
        }

        titleText.text = titleTextValue;
        titleText.ForceMeshUpdate();
        titleText.maxVisibleCharacters = 0;
        titleGroup.alpha = 1f;

        int characterCount = titleText.textInfo.characterCount;
        for (int i = 0; i < characterCount; i++)
        {
            titleText.maxVisibleCharacters = i + 1;
            PlayTitleTypewriterTick();

            float elapsed = 0f;
            while (elapsed < titleCharacterPopDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, titleCharacterPopDuration));
                float scale = Mathf.Lerp(titleCharacterPopScale, 1f, Mathf.SmoothStep(0f, 1f, t));
                titleRect.localScale = Vector3.one * scale;
                yield return null;
            }

            titleRect.localScale = Vector3.one;
            yield return new WaitForSecondsRealtime(titleCharacterRevealInterval);
        }

        titleText.maxVisibleCharacters = int.MaxValue;
        titleRect.localScale = Vector3.one;
    }

    IEnumerator PulseTitle()
    {
        if (titleRect == null || titleText == null)
        {
            yield break;
        }

        Vector3 baseScale = Vector3.one;
        Color baseColor = titleText.color;

        while (true)
        {
            float elapsed = 0f;
            while (elapsed < titlePulseDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, titlePulseDuration));
                float wave = Mathf.Sin(t * Mathf.PI);
                float scale = Mathf.Lerp(1f, titlePulseScale, wave);
                titleRect.localScale = baseScale * scale;

                Color color = baseColor;
                color.a = Mathf.Lerp(0.86f, 1f, wave);
                titleText.color = color;

                yield return null;
            }

            titleRect.localScale = baseScale;
            titleText.color = baseColor;
        }
    }

    IEnumerator PulseHeartbeatVignette()
    {
        if (heartbeatVignette == null)
        {
            yield break;
        }

        while (true)
        {
            float normalizedTime = GetHeartbeatNormalizedTime();
            float pulse = Mathf.Pow((Mathf.Cos(normalizedTime * Mathf.PI * 2f) * 0.5f) + 0.5f, heartbeatVignettePulseSharpness);
            heartbeatVignette.color = WithAlpha(
                heartbeatVignetteColor,
                Mathf.Lerp(heartbeatVignetteMinAlpha, heartbeatVignetteMaxAlpha, pulse));

            yield return null;
        }
    }

    float GetHeartbeatNormalizedTime()
    {
        if (heartbeatSource != null &&
            heartbeatClip != null &&
            heartbeatClip.length > 0.01f &&
            heartbeatSource.isPlaying)
        {
            return Mathf.Repeat(heartbeatSource.time / heartbeatClip.length, 1f);
        }

        float fallbackDuration = Mathf.Max(0.1f, heartbeatVignetteFallbackDuration);
        return Mathf.Repeat(Time.unscaledTime / fallbackDuration, 1f);
    }

    IEnumerator LoopEcgGlow()
    {
        if (ecgGlow == null || ecgSegments == null || ecgSegments.Length == 0)
        {
            yield break;
        }

        while (true)
        {
            yield return new WaitForSecondsRealtime(ecgGlowLoopDelay);

            Color glowColor = ecgGlow.color;
            glowColor.a = 0.9f;
            ecgGlow.color = glowColor;
            ecgGlow.gameObject.SetActive(true);

            float totalLength = 0f;
            for (int i = 0; i < ecgSegments.Length; i++)
            {
                if (ecgSegments[i] != null)
                {
                    totalLength += ecgSegments[i].length;
                }
            }

            for (int i = 0; i < ecgSegments.Length; i++)
            {
                EcgSegment segment = ecgSegments[i];
                if (segment == null)
                {
                    continue;
                }

                float segmentDuration = ecgGlowLoopDuration * (segment.length / Mathf.Max(1f, totalLength));
                float elapsed = 0f;
                while (elapsed < segmentDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, segmentDuration));
                    MoveGlow(Vector2.Lerp(segment.start, segment.end, Mathf.SmoothStep(0f, 1f, t)));
                    yield return null;
                }
            }

            yield return FadeGraphic(ecgGlow, ecgGlow.color.a, 0f, 0.18f);
            ecgGlow.gameObject.SetActive(false);
        }
    }

    void UpdateBloodDrops(float elapsed)
    {
        if (bloodDrops == null)
        {
            return;
        }

        for (int i = 0; i < bloodDrops.Length; i++)
        {
            BloodDrop drop = bloodDrops[i];
            if (drop == null || drop.rectTransform == null || drop.canvasGroup == null)
            {
                continue;
            }

            if (elapsed < drop.delay)
            {
                drop.canvasGroup.alpha = 0f;
                drop.rectTransform.gameObject.SetActive(true);
                continue;
            }

            if (!drop.hasPlayedSound && elapsed >= drop.delay)
            {
                PlayBloodDropSound(i);
                drop.hasPlayedSound = true;
            }

            float t = Mathf.Clamp01((elapsed - drop.delay) / Mathf.Max(0.01f, drop.duration));
            if (t >= 1f)
            {
                Vector2 hiddenAnchor = drop.rectTransform.anchorMin;
                hiddenAnchor.y = drop.endY;
                drop.rectTransform.anchorMin = hiddenAnchor;
                drop.rectTransform.anchorMax = hiddenAnchor;
                drop.canvasGroup.alpha = 0f;
                drop.rectTransform.gameObject.SetActive(false);
                continue;
            }

            float eased = t * t * (3f - (2f * t));
            Vector2 anchor = drop.rectTransform.anchorMin;
            anchor.y = Mathf.Lerp(drop.startY, drop.endY, eased);
            drop.rectTransform.anchorMin = anchor;
            drop.rectTransform.anchorMax = anchor;

            float fadeIn = Mathf.Clamp01(t / 0.12f);
            float fadeOut = 1f - Mathf.Clamp01((t - 0.82f) / 0.18f);
            drop.canvasGroup.alpha = Mathf.Min(fadeIn, fadeOut);
        }
    }

    void PlayBloodDropSound(int dropIndex)
    {
        if (bloodDropSource == null || bloodDropClip == null)
        {
            return;
        }

        float pitch = 0.92f + ((dropIndex % 5) * 0.04f);
        bloodDropSource.pitch = pitch;
        bloodDropSource.PlayOneShot(bloodDropClip, bloodDropVolume);
    }

    void PlayHeartbeatHit()
    {
        if (heartbeatSource == null || heartbeatClip == null)
        {
            return;
        }

        heartbeatSource.PlayOneShot(heartbeatClip, heartbeatVolume);
    }

    void PlayTitleTypewriterTick()
    {
        if (titleTypewriterSource == null || titleTypewriterClip == null)
        {
            return;
        }

        titleTypewriterSource.pitch = Random.Range(0.96f, 1.04f);
        titleTypewriterSource.PlayOneShot(titleTypewriterClip, titleTypewriterVolume);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            group.alpha = Mathf.SmoothStep(fromAlpha, toAlpha, t);
            yield return null;
        }

        group.alpha = toAlpha;
    }

    void Play()
    {
        TitleScreenUI titleScreen = FindAnyObjectByType<TitleScreenUI>();
        if (titleScreen != null)
        {
            titleScreen.StartGame();
            return;
        }

        SceneManager.LoadScene(missionSelectSceneName);
    }

    void Exit()
    {
        TitleScreenUI titleScreen = FindAnyObjectByType<TitleScreenUI>();
        if (titleScreen != null)
        {
            titleScreen.QuitGame();
            return;
        }

        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    EcgSegment CreateLineSegment(RectTransform parent, Vector2 start, Vector2 end, float thickness, Color color)
    {
        GameObject lineObject = new GameObject("EcgSegment", typeof(RectTransform), typeof(Image));
        lineObject.transform.SetParent(parent, false);

        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = start;
        rectTransform.anchorMax = start;

        Vector2 rootSize = new Vector2(2560f, 1440f);
        Vector2 parentSize = new Vector2(
            (parent.anchorMax.x - parent.anchorMin.x) * rootSize.x,
            (parent.anchorMax.y - parent.anchorMin.y) * rootSize.y);
        Vector2 startPixels = start * parentSize;
        Vector2 endPixels = end * parentSize;
        Vector2 delta = endPixels - startPixels;

        rectTransform.sizeDelta = new Vector2(delta.magnitude, thickness);
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        Image image = lineObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        float fullLength = delta.magnitude;
        rectTransform.sizeDelta = new Vector2(0f, thickness);

        return new EcgSegment
        {
            rectTransform = rectTransform,
            image = image,
            length = fullLength,
            start = start,
            end = end
        };
    }

    Image CreateGlow(RectTransform parent)
    {
        GameObject glowObject = new GameObject("EcgGlow", typeof(RectTransform), typeof(Image));
        glowObject.transform.SetParent(parent, false);
        RectTransform rectTransform = glowObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(ecgGlowSize, ecgGlowSize);

        Image image = glowObject.GetComponent<Image>();
        image.sprite = CreateCircleSprite(64);
        image.color = new Color(1f, 1f, 1f, 0.9f);
        image.raycastTarget = false;
        return image;
    }

    void MoveGlow(Vector2 normalizedPoint)
    {
        if (ecgGlow == null)
        {
            return;
        }

        RectTransform rectTransform = ecgGlow.rectTransform;
        rectTransform.anchorMin = normalizedPoint;
        rectTransform.anchorMax = normalizedPoint;
        rectTransform.anchoredPosition = Vector2.zero;
    }

    IEnumerator FadeGraphic(Graphic graphic, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);
        Color color = graphic.color;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            color.a = Mathf.SmoothStep(fromAlpha, toAlpha, t);
            graphic.color = color;
            yield return null;
        }

        color.a = toAlpha;
        graphic.color = color;
    }

    RectTransform CreatePanel(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        return rectTransform;
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        return label;
    }

    Button CreateButton(string objectName, Transform parent, string labelText, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform buttonRect = CreatePanel(objectName, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color(0.48f, 0.08f, 0.045f, 0.96f);

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI label = CreateText("Label", buttonRect, labelText, 28f, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.color = new Color(0.92f, 0.86f, 0.76f, 1f);
        return button;
    }

    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - distance) / 4f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    Sprite CreateVignetteSprite(int size)
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

    Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
