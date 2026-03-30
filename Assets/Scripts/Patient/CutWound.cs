using UnityEngine;

public class CutWound : MonoBehaviour
{
    public enum WoundType
    {
        Cut,
        Laceration
    }

    public enum WoundLocation
    {
        Outside,
        Inside
    }

    public static event System.Action<CutWound> WoundCauterised;

    [Header("Ownership")]
    [SerializeField] private Patient patient;

    [Header("Hitboxes")]
    [SerializeField] private Collider2D cutHitbox;
    [SerializeField] private Collider2D spellBoundsHitbox;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer woundSpriteRenderer;
    [SerializeField] private Sprite cutSprite;
    [SerializeField] private Sprite lacerationSprite;

    [Header("State")]
    [SerializeField] private WoundType woundType = WoundType.Cut;
    [SerializeField] private WoundLocation woundLocation = WoundLocation.Outside;
    [SerializeField] private bool isOpen = true;
    [SerializeField] private bool applyBleedOnStart = true;
    [SerializeField] private bool isStabilized;

    public Patient Patient => patient;
    public Collider2D CutHitbox => cutHitbox;
    public Collider2D SpellBoundsHitbox => spellBoundsHitbox;
    public bool IsOpen => isOpen;
    public bool IsStabilized => isStabilized;
    public WoundType Type => woundType;
    public WoundLocation Location => woundLocation;

    private Sprite originalVisualSprite;
    private Vector3 originalVisualLocalPosition;
    private Vector3 originalVisualLocalScale;

    void Start()
    {
        if (patient == null)
        {
            patient = GetComponentInParent<Patient>();
        }

        if (woundSpriteRenderer == null)
        {
            woundSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (woundSpriteRenderer != null)
        {
            originalVisualSprite = woundSpriteRenderer.sprite;
            originalVisualLocalPosition = woundSpriteRenderer.transform.localPosition;
            originalVisualLocalScale = woundSpriteRenderer.transform.localScale;
        }

        PatientWounds patientWounds = GetComponentInParent<PatientWounds>();
        if (patientWounds != null)
        {
            patientWounds.Register(this);
        }

        if (!applyBleedOnStart || patient == null)
        {
            RefreshVisualState();
            return;
        }

        RefreshVisualState();
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

    public Vector3 GetSpellAnchorPosition(Vector3 referencePosition)
    {
        Collider2D anchorCollider = spellBoundsHitbox != null ? spellBoundsHitbox : cutHitbox;
        if (anchorCollider == null)
        {
            return transform.position;
        }

        Bounds bounds = anchorCollider.bounds;
        Vector3 center = bounds.center;
        Vector3 offset = referencePosition - center;

        if (offset.sqrMagnitude <= Mathf.Epsilon)
        {
            return center + new Vector3(bounds.extents.x, 0f, 0f);
        }

        float scaleX = Mathf.Abs(offset.x) > Mathf.Epsilon ? bounds.extents.x / Mathf.Abs(offset.x) : float.MaxValue;
        float scaleY = Mathf.Abs(offset.y) > Mathf.Epsilon ? bounds.extents.y / Mathf.Abs(offset.y) : float.MaxValue;
        float scale = Mathf.Min(scaleX, scaleY);

        Vector3 edgePoint = center + (offset * scale);
        edgePoint.z = transform.position.z;
        return edgePoint;
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
        WoundCauterised?.Invoke(this);
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
                    RefreshVisualState();
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

    void RefreshVisualState()
    {
        if (woundSpriteRenderer == null)
        {
            return;
        }

        Sprite targetSprite = null;

        switch (woundType)
        {
            case WoundType.Cut:
                targetSprite = cutSprite;
                break;
            case WoundType.Laceration:
                targetSprite = isStabilized && cutSprite != null ? cutSprite : lacerationSprite;
                break;
        }

        if (targetSprite != null)
        {
            ApplyVisualSprite(targetSprite);
        }
    }

    void ApplyVisualSprite(Sprite targetSprite)
    {
        if (woundSpriteRenderer == null || targetSprite == null)
        {
            return;
        }

        Sprite referenceSprite = originalVisualSprite != null ? originalVisualSprite : woundSpriteRenderer.sprite;
        Transform visualTransform = woundSpriteRenderer.transform;

        woundSpriteRenderer.sprite = targetSprite;

        visualTransform.localScale = originalVisualLocalScale;
        visualTransform.localPosition = originalVisualLocalPosition;

        if (referenceSprite == null)
        {
            return;
        }

        Vector2 referenceSize = referenceSprite.bounds.size;
        Vector2 targetSize = targetSprite.bounds.size;
        Vector3 adjustedScale = originalVisualLocalScale;

        if (targetSize.x > Mathf.Epsilon)
        {
            adjustedScale.x *= referenceSize.x / targetSize.x;
        }

        if (targetSize.y > Mathf.Epsilon)
        {
            adjustedScale.y *= referenceSize.y / targetSize.y;
        }

        Vector3 referenceCenter = referenceSprite.bounds.center;
        Vector3 targetCenter = targetSprite.bounds.center;
        Vector3 centerOffset = referenceCenter - targetCenter;

        visualTransform.localScale = adjustedScale;
        visualTransform.localPosition = originalVisualLocalPosition + centerOffset;
    }
}
