using TMPro;
using UnityEngine;

public class StrokeStartIndicator : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private float riseSpeed = 0.25f;

    private TextMeshPro textMesh;
    private Color baseColor;
    private float elapsed;

    public static void Create(Vector3 worldPosition, Color color)
    {
        GameObject indicatorObject = new GameObject("StrokeStartIndicator");
        indicatorObject.transform.position = worldPosition + (Vector3.up * 0.2f);

        StrokeStartIndicator indicator = indicatorObject.AddComponent<StrokeStartIndicator>();
        indicator.Initialize(color);
    }

    void Initialize(Color color)
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.text = "+";
        textMesh.fontSize = 2.2f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.sortingOrder = 45;

        baseColor = color;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        float fadeProgress = Mathf.InverseLerp(0f, lifetime, elapsed);
        Color fadedColor = baseColor;
        fadedColor.a = Mathf.Lerp(baseColor.a, 0f, fadeProgress);
        textMesh.color = fadedColor;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
