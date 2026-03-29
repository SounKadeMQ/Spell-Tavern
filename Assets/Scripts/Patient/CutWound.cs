using UnityEngine;

public class CutWound : MonoBehaviour
{
    public enum WoundType
    {
        Cut,
        Laceration
    }

    [Header("Ownership")]
    [SerializeField] private Patient patient;

    [Header("Hitboxes")]
    [SerializeField] private Collider2D cutHitbox;
    [SerializeField] private Collider2D spellBoundsHitbox;

    [Header("State")]
    [SerializeField] private WoundType woundType = WoundType.Cut;
    [SerializeField] private bool isOpen = true;
    [SerializeField] private bool applyBleedOnStart = true;
    [SerializeField] private bool isStabilized;

    public Patient Patient => patient;
    public Collider2D CutHitbox => cutHitbox;
    public Collider2D SpellBoundsHitbox => spellBoundsHitbox;
    public bool IsOpen => isOpen;
    public bool IsStabilized => isStabilized;
    public WoundType Type => woundType;

    void Start()
    {
        if (patient == null)
        {
            patient = GetComponentInParent<Patient>();
        }

        if (!applyBleedOnStart || patient == null)
        {
            return;
        }

        patient.NotifyBleedSourcesChanged();
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
        isStabilized = false;
        NotifyPatient();
    }

    public void Close()
    {
        isOpen = false;
        isStabilized = false;
        NotifyPatient();
    }

    void CauteriseAndRemove()
    {
        Close();
        Destroy(gameObject);
    }

    public float GetBleedRate()
    {
        if (!isOpen)
        {
            return 0f;
        }

        switch (woundType)
        {
            case WoundType.Cut:
                return 0.5f;
            case WoundType.Laceration:
                return isStabilized ? 0f : 1.5f;
            default:
                return 0f;
        }
    }

    public bool TryApplySpell(SpellController.SpellType spellType, out string outcome)
    {
        outcome = "Nothing happened.";

        if (!isOpen)
        {
            outcome = "Wound is already closed.";
            return false;
        }

        switch (woundType)
        {
            case WoundType.Cut:
                if (spellType == SpellController.SpellType.Fire)
                {
                    CauteriseAndRemove();
                    outcome = "Cut cauterised.";
                    return true;
                }
                break;

            case WoundType.Laceration:
                if (spellType == SpellController.SpellType.Earth && !isStabilized)
                {
                    isStabilized = true;
                    NotifyPatient();
                    outcome = "Laceration stabilized.";
                    return true;
                }

                if (spellType == SpellController.SpellType.Fire && isStabilized)
                {
                    CauteriseAndRemove();
                    outcome = "Laceration cauterised.";
                    return true;
                }

                if (spellType == SpellController.SpellType.Fire)
                {
                    outcome = "Laceration must be stabilized with earth first.";
                    return false;
                }
                break;
        }

        outcome = spellType + " does not treat this wound.";
        return false;
    }

    void NotifyPatient()
    {
        if (patient != null)
        {
            patient.NotifyBleedSourcesChanged();
        }
    }
}
