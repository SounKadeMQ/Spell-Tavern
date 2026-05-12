using TMPro;
using UnityEngine;

public class SpellCastPopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseSpeed = 1.2f;
    [SerializeField] private float fadeStartTime = 0.35f;
    [SerializeField] private float referenceOrthographicSize = 5f;
    [SerializeField] private float baseFontSize = 4f;

    private TextMeshPro textMesh;
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
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.text = message;
        textMesh.fontSize = baseFontSize * cameraScale;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.sortingOrder = 50;

        baseColor = color;
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
            textMesh.color = fadedColor;
        }

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
}
