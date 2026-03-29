using UnityEngine;

public class BloodPoolVisual : MonoBehaviour
{
    [SerializeField] private CutWound wound;
    [SerializeField] private Transform poolVisual;
    [SerializeField] private SpriteRenderer poolRenderer;
    [SerializeField] private Vector3 maxScale = new Vector3(1.5f, 1.5f, 1f);
    [SerializeField] private float growSpeed = 0.2f;
    [SerializeField] private float alphaSpeed = 0.5f;

    private Vector3 startScale;

    void Start()
    {
        if (poolVisual != null)
        {
            startScale = poolVisual.localScale;
        }
    }

    void Update()
    {
        if (wound == null || poolVisual == null || poolRenderer == null)
        {
            return;
        }

        if (wound.IsOpen)
        {
            poolVisual.localScale = Vector3.MoveTowards(
                poolVisual.localScale,
                maxScale,
                growSpeed * Time.deltaTime);

            Color c = poolRenderer.color;
            c.a = Mathf.MoveTowards(c.a, 0.8f, alphaSpeed * Time.deltaTime);
            poolRenderer.color = c;
        }
    }
}
