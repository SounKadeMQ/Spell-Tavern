using UnityEngine;

public class SurgeryLatticeBackground : MonoBehaviour
{
    private static Sprite latticeSprite;

    [SerializeField] private Vector2 worldSize = new Vector2(48f, 32f);
    [SerializeField] private float backgroundZ = 8f;
    [SerializeField] private int sortingOrder = -100;
    [SerializeField] private Color baseColor = new Color(0.025f, 0.085f, 0.12f, 1f);
    [SerializeField] private Color latticeColor = new Color(0.08f, 0.95f, 1f, 0.38f);
    [SerializeField] private Color glowColor = new Color(0.05f, 0.45f, 0.55f, 0.28f);

    void Awake()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = GetLatticeSprite();
        renderer.sortingOrder = sortingOrder;
        renderer.color = Color.white;
        transform.position = new Vector3(0f, 0f, backgroundZ);
        transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
    }

    Sprite GetLatticeSprite()
    {
        if (latticeSprite != null)
        {
            return latticeSprite;
        }

        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                Color color = baseColor;

                float grid = Mathf.Min(GridLine(u, 8f), GridLine(v, 8f));
                float diagonalA = DiagonalLine(u + v, 0.25f);
                float diagonalB = DiagonalLine(u - v, 0.25f);
                float lattice = Mathf.Max(grid, Mathf.Max(diagonalA, diagonalB) * 0.72f);
                color = Color.Lerp(color, glowColor, Mathf.Clamp01(lattice * 0.65f));
                color = Color.Lerp(color, latticeColor, Mathf.Clamp01(lattice));

                float vignette = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.4f);
                color = Color.Lerp(color, new Color(0.006f, 0.018f, 0.027f, 1f), vignette * 0.45f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        latticeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return latticeSprite;
    }

    float GridLine(float coordinate, float frequency)
    {
        float centered = Mathf.Abs(Mathf.Repeat(coordinate * frequency, 1f) - 0.5f);
        return 1f - Mathf.SmoothStep(0.455f, 0.5f, centered);
    }

    float DiagonalLine(float coordinate, float spacing)
    {
        float centered = Mathf.Abs(Mathf.Repeat(coordinate / spacing, 1f) - 0.5f);
        return 1f - Mathf.SmoothStep(0.47f, 0.5f, centered);
    }
}
