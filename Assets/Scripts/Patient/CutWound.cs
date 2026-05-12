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
        Inside,
        Part
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
    [SerializeField] private Sprite stabilizedLacerationSprite;
    [SerializeField] private string stabilizedLacerationSpriteResource = "Sprites/CutVerticalSheet(2)";
    [SerializeField] private Vector2 stabilizedLacerationSqueeze = new Vector2(0.82f, 1.06f);

    [Header("State")]
    [SerializeField] private WoundType woundType = WoundType.Cut;
    [SerializeField] private WoundLocation woundLocation = WoundLocation.Outside;
    [SerializeField] private bool isOpen = true;
    [SerializeField] private bool applyBleedOnStart = true;
    [SerializeField] private bool isStabilized;
    [SerializeField] private string spawnAreaId;

    public Patient Patient => patient;
    public Collider2D CutHitbox => cutHitbox;
    public Collider2D SpellBoundsHitbox => spellBoundsHitbox;
    public bool IsOpen => isOpen;
    public bool IsStabilized => isStabilized;
    public WoundType Type => woundType;
    public WoundLocation Location => woundLocation;
    public string SpawnAreaId => spawnAreaId;

    public void SetPatient(Patient owner)
    {
        patient = owner;
    }

    private Sprite originalVisualSprite;
    private Vector3 originalVisualLocalPosition;
    private Vector3 originalVisualLocalScale;
    private bool hasCachedVisualDefaults;

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

        CacheVisualDefaults();

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

    public void ApplyMissionLayout(WoundType type, WoundLocation location, bool active)
    {
        ApplyMissionLayout(type, location, active, spawnAreaId);
    }

    public void ApplyMissionLayout(WoundType type, WoundLocation location, bool active, string areaId)
    {
        if (patient == null)
        {
            patient = GetComponentInParent<Patient>();
        }

        if (woundSpriteRenderer == null)
        {
            woundSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        woundType = type;
        woundLocation = location;
        spawnAreaId = areaId;
        isOpen = active;
        isStabilized = false;

        gameObject.SetActive(true);
        CacheVisualDefaults();
        RefreshVisualState();
        SetLayoutVisibility(active);
        NotifyPatient();
    }

    public void SetLayoutActive(bool active)
    {
        isOpen = active;
        gameObject.SetActive(true);
        SetLayoutVisibility(active);
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
                targetSprite = isStabilized ? GetStabilizedLacerationSprite() : lacerationSprite;
                break;
        }

        if (targetSprite != null)
        {
            Vector2 scaleMultiplier = isStabilized && woundType == WoundType.Laceration
                ? stabilizedLacerationSqueeze
                : Vector2.one;
            ApplyVisualSprite(targetSprite, scaleMultiplier);
        }
    }

    Sprite GetStabilizedLacerationSprite()
    {
        if (stabilizedLacerationSprite != null)
        {
            return stabilizedLacerationSprite;
        }

        Sprite[] sprites = Resources.LoadAll<Sprite>(stabilizedLacerationSpriteResource);
        if (sprites != null && sprites.Length > 0)
        {
            stabilizedLacerationSprite = sprites[0];
            return stabilizedLacerationSprite;
        }

        stabilizedLacerationSprite = Resources.Load<Sprite>(stabilizedLacerationSpriteResource);
        if (stabilizedLacerationSprite != null)
        {
            return stabilizedLacerationSprite;
        }

        return cutSprite != null ? cutSprite : lacerationSprite;
    }

    void CacheVisualDefaults()
    {
        if (hasCachedVisualDefaults)
        {
            return;
        }

        if (woundSpriteRenderer == null)
        {
            woundSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (woundSpriteRenderer == null)
        {
            return;
        }

        originalVisualSprite = woundSpriteRenderer.sprite;
        originalVisualLocalPosition = woundSpriteRenderer.transform.localPosition;
        originalVisualLocalScale = woundSpriteRenderer.transform.localScale;
        hasCachedVisualDefaults = true;
    }

    void SetLayoutVisibility(bool visible)
    {
        if (woundSpriteRenderer != null)
        {
            woundSpriteRenderer.enabled = visible;
        }

        if (cutHitbox != null)
        {
            cutHitbox.enabled = visible;
        }

        if (spellBoundsHitbox != null)
        {
            spellBoundsHitbox.enabled = visible;
        }
    }

    void ApplyVisualSprite(Sprite targetSprite, Vector2 scaleMultiplier)
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

        adjustedScale.x *= scaleMultiplier.x;
        adjustedScale.y *= scaleMultiplier.y;

        Vector3 referenceCenter = referenceSprite.bounds.center;
        Vector3 targetCenter = targetSprite.bounds.center;
        Vector3 centerOffset = referenceCenter - targetCenter;

        visualTransform.localScale = adjustedScale;
        visualTransform.localPosition = originalVisualLocalPosition + centerOffset;
    }
}
