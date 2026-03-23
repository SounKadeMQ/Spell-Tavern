using UnityEngine;

public class CutWound : MonoBehaviour
{
    [Header("Ownership")]
    [SerializeField] private Patient patient;

    [Header("Hitboxes")]
    [SerializeField] private Collider2D cutHitbox;
    [SerializeField] private Collider2D spellBoundsHitbox;

    [Header("State")]
    [SerializeField] private bool isOpen = true;
    [SerializeField] private float bleedAmount = 5f;
    [SerializeField] private bool applyBleedOnStart = true;

    public Patient Patient => patient;
    public Collider2D CutHitbox => cutHitbox;
    public Collider2D SpellBoundsHitbox => spellBoundsHitbox;
    public bool IsOpen => isOpen;
    public float BleedAmount => bleedAmount;

    void Start()
    {
        if (!applyBleedOnStart || !isOpen || patient == null)
        {
            return;
        }

        patient.applyDamage(bleedAmount);
    }

    public bool ContainsSpellPoint(Vector2 worldPoint)
    {
        return isOpen &&
               spellBoundsHitbox != null &&
               spellBoundsHitbox.OverlapPoint(worldPoint);
    }

    public bool ContainsCutPoint(Vector2 worldPoint)
    {
        return isOpen &&
               cutHitbox != null &&
               cutHitbox.OverlapPoint(worldPoint);
    }

    public bool ContainsAnySpellPoint(LineRenderer lineRenderer)
    {
        if (!isOpen || spellBoundsHitbox == null || lineRenderer == null)
        {
            return false;
        }

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 point = lineRenderer.GetPosition(i);
            if (spellBoundsHitbox.OverlapPoint(point))
            {
                return true;
            }
        }

        return false;
    }

    public void Open()
    {
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }
}
