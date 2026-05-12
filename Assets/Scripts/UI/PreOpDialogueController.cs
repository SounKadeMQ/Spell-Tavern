using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;
}

public class PreOpDialogueController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private string nextSceneName = "PatientScene";
    [SerializeField] private DialogueLine[] lines;
    [SerializeField] private float characterRevealInterval = 0.5f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceTestClip;
    [SerializeField] private Color backgroundBaseColor = new Color(0.07f, 0.13f, 0.16f, 1f);
    [SerializeField] private Color backgroundMistColor = new Color(0.22f, 0.35f, 0.38f, 1f);

    private int currentLineIndex;
    private int lastAdvanceFrame = -1;
    private Coroutine typewriterRoutine;
    private bool isTyping;
    private static Sprite generatedPreOpBackground;

    void Start()
    {
        EnsurePreOpBackground();

        MissionData mission = MissionFlowState.CurrentMission;
        if (mission != null)
        {
            if (mission.preOpLines != null && mission.preOpLines.Length > 0)
            {
                lines = mission.preOpLines;
            }

            if (!string.IsNullOrEmpty(mission.sceneName) &&
                mission.kind == MissionData.MissionKind.Surgery)
            {
                nextSceneName = "PatientScene";
            }
        }

        ShowCurrentLine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || WasAdvanceTap())
        {
            NextLine();
        }
    }

    public void NextLine()
    {
        if (lastAdvanceFrame == Time.frameCount)
        {
            return;
        }

        lastAdvanceFrame = Time.frameCount;

        if (lines == null || lines.Length == 0)
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (isTyping)
        {
            FinishCurrentLineImmediately();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= lines.Length)
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        ShowCurrentLine();
    }

    bool WasAdvanceTap()
    {
        if (Input.GetMouseButtonUp(0))
        {
            return true;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                return true;
            }
        }

        return false;
    }

    void ShowCurrentLine()
    {
        if (speakerText == null || dialogueText == null)
        {
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            speakerText.text = string.Empty;
            dialogueText.text = string.Empty;
            return;
        }

        speakerText.text = lines[currentLineIndex].speaker;
        StartTypewriter(lines[currentLineIndex].text);
    }

    void StartTypewriter(string lineText)
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
        }

        typewriterRoutine = StartCoroutine(TypeLine(lineText));
    }

    IEnumerator TypeLine(string lineText)
    {
        isTyping = true;
        dialogueText.text = string.Empty;

        for (int i = 0; i < lineText.Length; i++)
        {
            dialogueText.text += lineText[i];

            if (audioSource != null &&
                voiceTestClip != null &&
                !char.IsWhiteSpace(lineText[i]))
            {
                audioSource.PlayOneShot(voiceTestClip);
            }

            yield return new WaitForSeconds(characterRevealInterval);
        }

        isTyping = false;
        typewriterRoutine = null;
    }

    void FinishCurrentLineImmediately()
    {
        if (typewriterRoutine != null)
        {
            StopCoroutine(typewriterRoutine);
            typewriterRoutine = null;
        }

        isTyping = false;
        dialogueText.text = lines[currentLineIndex].text;
    }

    void EnsurePreOpBackground()
    {
        Canvas canvas = dialogueText != null ? dialogueText.GetComponentInParent<Canvas>() : FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        RectTransform backgroundRect = FindDirectChild(canvasRect, "PreOpBackground");
        Image backgroundImage;
        if (backgroundRect == null)
        {
            GameObject backgroundObject = new GameObject("PreOpBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.SetParent(canvasRect, false);
            backgroundImage = backgroundObject.GetComponent<Image>();
        }
        else
        {
            backgroundImage = backgroundRect.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = backgroundRect.gameObject.AddComponent<Image>();
            }
        }

        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundImage.sprite = GetPreOpBackgroundSprite();
        backgroundImage.color = Color.white;
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;
        backgroundRect.SetAsFirstSibling();
    }

    RectTransform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == childName)
            {
                return child as RectTransform;
            }
        }

        return null;
    }

    Sprite GetPreOpBackgroundSprite()
    {
        if (generatedPreOpBackground != null)
        {
            return generatedPreOpBackground;
        }

        Texture2D texture = new Texture2D(640, 360, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float u = x / (float)(texture.width - 1);
                float v = y / (float)(texture.height - 1);
                Color color = Color.Lerp(backgroundBaseColor, backgroundMistColor, Mathf.Clamp01((1f - u) * 0.35f + v * 0.2f));

                float vignette = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.47f, 0.52f)) * 1.25f);
                color = Color.Lerp(color, new Color(0.015f, 0.025f, 0.032f, 1f), vignette * 0.55f);

                if (u > 0.44f && u < 0.96f)
                {
                    float shelfLine = Mathf.Abs(Mathf.Repeat(v * 9.2f, 1f) - 0.5f);
                    float verticalLine = Mathf.Abs(Mathf.Repeat((u - 0.44f) * 11f, 1f) - 0.5f);
                    if (shelfLine > 0.46f || verticalLine > 0.47f)
                    {
                        color = Color.Lerp(color, new Color(0.06f, 0.13f, 0.15f, 1f), 0.8f);
                    }
                    else if (RandomValue(x, y) > 0.74f)
                    {
                        color = Color.Lerp(color, new Color(0.28f, 0.25f, 0.24f, 1f), 0.28f);
                    }
                }

                if (u < 0.18f && v < 0.82f)
                {
                    color = Color.Lerp(color, new Color(0.018f, 0.034f, 0.045f, 1f), 0.72f);
                }

                if (u > 0.18f && u < 0.34f && v < 0.55f)
                {
                    color = Color.Lerp(color, new Color(0.13f, 0.17f, 0.17f, 1f), 0.45f);
                }

                float lightBeam = Mathf.Clamp01(1f - Mathf.Abs((u * 1.8f + v) - 0.58f) * 9f);
                if (u < 0.38f && v > 0.42f)
                {
                    color = Color.Lerp(color, new Color(0.66f, 0.74f, 0.66f, 1f), lightBeam * 0.22f);
                }

                if (u > 0.04f && u < 0.25f && v > 0.86f && v < 0.89f)
                {
                    color = Color.Lerp(color, new Color(0.78f, 0.84f, 0.72f, 1f), 0.82f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        generatedPreOpBackground = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return generatedPreOpBackground;
    }

    float RandomValue(int x, int y)
    {
        int n = (x * 73856093) ^ (y * 19349663);
        n = (n << 13) ^ n;
        return 1f - (((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f);
    }
}
