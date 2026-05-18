using TMPro;
using UnityEngine;

public class SpellCastPopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private float fadeStartTime = 0.35f;
    [SerializeField] private float referenceOrthographicSize = 5f;
    [SerializeField] private float baseFontSize = 4f;
    [SerializeField] private float letterSpacing = 0.42f;
    [SerializeField] private float punctuationSpacingMultiplier = 0.45f;
    [SerializeField] private float jumpHeight = 0.24f;
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private float letterDelay = 0.07f;

    private TextMeshPro[] letterMeshes;
    private Vector3[] letterBasePositions;
    private Color baseColor;
    private float elapsed;
    private float cameraScale = 1f;

    public static void Create(Vector3 worldPosition, string message, Color color)
    {
        GameObject popupObject = new GameObject("SpellCastPopup");
        popupObject.transform.position = worldPosition;

        SpellCastPopup popup = popupObject.AddComponent<SpellCastPopup>();
        popup.Initialize(message, color);
    }

    void Initialize(string message, Color color)
    {
        cameraScale = GetCameraScale();
        baseColor = color;
        CreateLetters(message, color);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * cameraScale * Time.deltaTime);

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (elapsed >= fadeStartTime)
        {
            float fadeProgress = Mathf.InverseLerp(fadeStartTime, lifetime, elapsed);
            Color fadedColor = baseColor;
            fadedColor.a = Mathf.Lerp(baseColor.a, 0f, fadeProgress);
            ApplyLetterColor(fadedColor);
        }

        AnimateLetters();

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    float GetCameraScale()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic || camera.orthographicSize <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp(camera.orthographicSize / referenceOrthographicSize, 0.42f, 1.25f);
    }

    void CreateLetters(string message, Color color)
    {
        message ??= string.Empty;
        letterMeshes = new TextMeshPro[message.Length];
        letterBasePositions = new Vector3[message.Length];

        float fontSize = baseFontSize * cameraScale;
        float[] xPositions = GetLetterPositions(message, fontSize);

        for (int i = 0; i < message.Length; i++)
        {
            GameObject letterObject = new GameObject("Letter_" + i);
            letterObject.transform.SetParent(transform, false);

            TextMeshPro letterMesh = letterObject.AddComponent<TextMeshPro>();
            letterMesh.text = message[i].ToString();
            letterMesh.fontSize = fontSize;
            letterMesh.alignment = TextAlignmentOptions.Center;
            letterMesh.color = color;
            letterMesh.sortingOrder = 50;

            Vector3 localPosition = new Vector3(xPositions[i], 0f, 0f);
            if (char.IsWhiteSpace(message[i]))
            {
                letterMesh.text = string.Empty;
            }

            letterObject.transform.localPosition = localPosition;
            letterMeshes[i] = letterMesh;
            letterBasePositions[i] = localPosition;
        }
    }

    float[] GetLetterPositions(string message, float fontSize)
    {
        float[] positions = new float[message.Length];
        if (message.Length == 0)
        {
            return positions;
        }

        float baseSpacing = fontSize * letterSpacing * 0.32f;
        float totalWidth = 0f;

        for (int i = 1; i < message.Length; i++)
        {
            totalWidth += GetSpacingForCharacter(message[i - 1], baseSpacing);
        }

        float x = -totalWidth * 0.5f;
        positions[0] = x;

        for (int i = 1; i < message.Length; i++)
        {
            x += GetSpacingForCharacter(message[i - 1], baseSpacing);
            positions[i] = x;
        }

        return positions;
    }

    float GetSpacingForCharacter(char character, float baseSpacing)
    {
        return char.IsPunctuation(character)
            ? baseSpacing * punctuationSpacingMultiplier
            : baseSpacing;
    }

    void AnimateLetters()
    {
        if (letterMeshes == null)
        {
            return;
        }

        for (int i = 0; i < letterMeshes.Length; i++)
        {
            TextMeshPro letterMesh = letterMeshes[i];
            if (letterMesh == null)
            {
                continue;
            }

            float letterTime = Mathf.Max(0f, elapsed - (i * letterDelay));
            float jump = Mathf.Sin(letterTime * jumpSpeed) * Mathf.Exp(-letterTime * 2.2f);
            jump = Mathf.Max(0f, jump) * jumpHeight * cameraScale;
            letterMesh.transform.localPosition = letterBasePositions[i] + (Vector3.up * jump);
        }
    }

    void ApplyLetterColor(Color color)
    {
        if (letterMeshes == null)
        {
            return;
        }

        for (int i = 0; i < letterMeshes.Length; i++)
        {
            if (letterMeshes[i] != null)
            {
                letterMeshes[i].color = color;
            }
        }
    }
}
