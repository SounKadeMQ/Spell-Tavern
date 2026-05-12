using System.Collections.Generic;
using UnityEngine;

public class PatientWounds : MonoBehaviour
{
    [SerializeField] private List<CutWound> cutWounds = new List<CutWound>();
    [SerializeField] private Vector2 chestLayoutMin = new Vector2(-3f, -16f);
    [SerializeField] private Vector2 chestLayoutMax = new Vector2(9f, -4f);
    [SerializeField] private string defaultSpawnAreaId = "Chest";
    [SerializeField] private float spawnAreaEdgePadding = 0.15f;
    [SerializeField] private float worldSpaceWoundScaleMultiplier = 10f;

    public IReadOnlyList<CutWound> CutWounds => cutWounds;

    void Awake()
    {
        RebuildWoundList();
    }

    public bool TryGetWoundAtSpellPoint(Vector2 worldPoint, out CutWound wound)
    {
        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound candidate = cutWounds[i];
            if (candidate != null && candidate.ContainsSpellPoint(worldPoint))
            {
                wound = candidate;
                return true;
            }
        }

        wound = null;
        return false;
    }

    public bool TryGetWoundTouchedByLine(LineRenderer lineRenderer, out CutWound wound)
    {
        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound candidate = cutWounds[i];
            if (candidate != null && candidate.ContainsAnySpellPoint(lineRenderer))
            {
                wound = candidate;
                return true;
            }
        }

        wound = null;
        return false;
    }

    public void Register(CutWound wound)
    {
        if (wound == null || cutWounds.Contains(wound))
        {
            return;
        }

        cutWounds.Add(wound);
    }

    public float GetTotalBleedRate()
    {
        float totalBleedRate = 0f;

        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null)
            {
                totalBleedRate += wound.GetBleedRate();
            }
        }

        return totalBleedRate;
    }

    public bool TryGetFirstOpenWound(out CutWound wound)
    {
        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound candidate = cutWounds[i];
            if (candidate != null && candidate.IsOpen)
            {
                wound = candidate;
                return true;
            }
        }

        wound = null;
        return false;
    }

    public int GetOpenWoundCount(CutWound.WoundLocation location)
    {
        int count = 0;

        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null && wound.IsOpen && wound.Location == location)
            {
                count++;
            }
        }

        return count;
    }

    public int GetOpenWoundCount(CutWound.WoundLocation location, string spawnAreaId)
    {
        if (string.IsNullOrWhiteSpace(spawnAreaId))
        {
            return GetOpenWoundCount(location);
        }

        int count = 0;

        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null &&
                wound.IsOpen &&
                wound.Location == location &&
                string.Equals(wound.SpawnAreaId, spawnAreaId, System.StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    public void SetWoundsActiveBySpawnArea(string spawnAreaId, bool active)
    {
        if (string.IsNullOrWhiteSpace(spawnAreaId))
        {
            return;
        }

        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null &&
                string.Equals(wound.SpawnAreaId, spawnAreaId, System.StringComparison.OrdinalIgnoreCase))
            {
                wound.SetLayoutActive(active);
            }
        }
    }

    public int GetOpenWoundCount()
    {
        int count = 0;

        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null && wound.IsOpen)
            {
                count++;
            }
        }

        return count;
    }

    public void ApplyMissionLayout(MissionData mission)
    {
        if (mission == null)
        {
            return;
        }

        ApplyLayout(mission.woundLayout, "Mission");
    }

    public void ApplyPatientLayout(PatientData patientData)
    {
        if (patientData == null)
        {
            return;
        }

        ApplyLayout(patientData.woundLayout, "Patient");
    }

    void ApplyLayout(WoundLayoutEntry[] woundLayout, string sourceLabel)
    {
        if (woundLayout == null || woundLayout.Length == 0)
        {
            return;
        }

        ClearExistingWoundsForLayout();

        for (int i = 0; i < woundLayout.Length; i++)
        {
            WoundLayoutEntry entry = woundLayout[i];
            if (entry == null)
            {
                continue;
            }

            int woundIndex = ResolveWoundIndex(entry);
            CutWound wound = woundIndex >= 0 ? cutWounds[woundIndex] : CreateWound(entry);
            if (wound == null)
            {
                continue;
            }

            Transform woundParent = ResolveWoundParent(entry);
            Transform woundTransform = GetLayoutTransform(wound, woundParent);
            if (woundTransform == null)
            {
                continue;
            }

            if (woundParent != null && woundTransform.parent != woundParent)
            {
                woundTransform.SetParent(woundParent, false);
            }

            woundTransform.rotation = Quaternion.Euler(0f, 0f, entry.rotationDegrees);
            woundTransform.localScale = GetLayoutWoundScale(entry, woundParent);
            woundTransform.localPosition = new Vector3(entry.localPosition.x, entry.localPosition.y, 0f);
            wound.ApplyMissionLayout(entry.woundType, entry.woundLocation, entry.active, GetSpawnAreaId(entry));
        }

        Patient patient = GetComponent<Patient>();
        if (patient != null)
        {
            patient.NotifyBleedSourcesChanged();
        }

        Debug.Log(sourceLabel + " wound layout applied: " + woundLayout.Length + " entries.");
    }

    Transform GetLayoutTransform(CutWound wound, Transform woundParent)
    {
        if (wound == null)
        {
            return null;
        }

        Transform layoutTransform = wound.transform;
        while (layoutTransform.parent != null &&
               layoutTransform.parent != woundParent &&
               layoutTransform.parent != transform)
        {
            layoutTransform = layoutTransform.parent;
        }

        return layoutTransform;
    }

    int ResolveWoundIndex(WoundLayoutEntry entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.woundName))
        {
            int closestIndex = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < cutWounds.Count; i++)
            {
                CutWound wound = cutWounds[i];
                if (wound != null && wound.name == entry.woundName)
                {
                    Vector2 woundPosition = wound.transform.position;
                    float distance = (woundPosition - entry.localPosition).sqrMagnitude;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestIndex = i;
                    }
                }
            }

            if (closestIndex >= 0)
            {
                return closestIndex;
            }
        }

        if (entry.woundIndex >= 0 && entry.woundIndex < cutWounds.Count)
        {
            return entry.woundIndex;
        }

        return -1;
    }

    Vector2 ClampToChestLayout(Vector2 worldPosition)
    {
        return new Vector2(
            Mathf.Clamp(worldPosition.x, chestLayoutMin.x, chestLayoutMax.x),
            Mathf.Clamp(worldPosition.y, chestLayoutMin.y, chestLayoutMax.y));
    }

    Vector2 ResolveLayoutPosition(WoundLayoutEntry entry, CutWound wound)
    {
        string areaId = GetSpawnAreaId(entry);
        WoundSpawnArea spawnArea = FindSpawnArea(entry);
        if (spawnArea != null && spawnArea.Collider is BoxCollider2D boxCollider)
        {
            return ClampToBoxSpawnArea(entry.localPosition, wound, boxCollider, spawnArea.edgePadding);
        }

        Debug.LogWarning("Wound layout could not find spawn area '" + areaId + "'. Falling back to chest bounds.");
        return ClampToChestLayout(entry.localPosition);
    }

    string GetSpawnAreaId(WoundLayoutEntry entry)
    {
        return entry != null && !string.IsNullOrWhiteSpace(entry.spawnAreaId) ? entry.spawnAreaId : defaultSpawnAreaId;
    }

    Vector3 GetLayoutWoundScale(WoundLayoutEntry entry, Transform parent)
    {
        float multiplier = Mathf.Max(1f, worldSpaceWoundScaleMultiplier);
        Vector3 desiredWorldScale = new Vector3(entry.localScale.x * multiplier, entry.localScale.y * multiplier, 1f);
        if (parent == null)
        {
            return desiredWorldScale;
        }

        Vector3 parentWorldScale = parent.lossyScale;
        return new Vector3(
            DivideScale(desiredWorldScale.x, parentWorldScale.x),
            DivideScale(desiredWorldScale.y, parentWorldScale.y),
            1f);
    }

    float GetLayoutWoundZ(WoundLayoutEntry entry, Transform parent)
    {
        if (parent != null)
        {
            return parent.position.z;
        }

        Transform layerRoot = entry != null ? ResolveLayerRoot(entry.woundLocation) : null;
        if (layerRoot != null)
        {
            return layerRoot.position.z;
        }

        return transform.position.z;
    }

    float DivideScale(float value, float divisor)
    {
        return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
    }

    WoundSpawnArea FindSpawnArea(WoundLayoutEntry entry)
    {
        string areaId = GetSpawnAreaId(entry);
        WoundSpawnArea[] areas = GetComponentsInChildren<WoundSpawnArea>(true);

        for (int i = 0; i < areas.Length; i++)
        {
            WoundSpawnArea area = areas[i];
            if (area != null && string.Equals(area.areaId, areaId, System.StringComparison.OrdinalIgnoreCase))
            {
                return area;
            }
        }

        Transform layerRoot = ResolveLayerRoot(entry.woundLocation);
        WoundSpawnArea layerArea = FindSpawnAreaInLayer(layerRoot, areaId);
        if (layerArea != null)
        {
            return layerArea;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        BoxCollider2D singleSpawnCandidate = null;
        int spawnCandidateCount = 0;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D candidate = colliders[i];
            if (candidate == null || candidate.GetComponentInParent<CutWound>() != null)
            {
                continue;
            }

            if (candidate is BoxCollider2D boxCandidate)
            {
                spawnCandidateCount++;
                singleSpawnCandidate = boxCandidate;
            }

            if (IsSpawnAreaCollider(candidate, areaId))
            {
                return GetOrCreateSpawnArea(candidate, areaId);
            }
        }

        if (spawnCandidateCount == 1 && singleSpawnCandidate != null)
        {
            return GetOrCreateSpawnArea(singleSpawnCandidate, areaId);
        }

        return null;
    }

    WoundSpawnArea FindSpawnAreaInLayer(Transform layerRoot, string areaId)
    {
        if (layerRoot == null)
        {
            return null;
        }

        Collider2D[] colliders = layerRoot.GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D candidate = colliders[i];
            if (candidate != null)
            {
                return GetOrCreateSpawnArea(candidate, areaId);
            }
        }

        colliders = layerRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D candidate = colliders[i];
            if (candidate != null && candidate.GetComponentInParent<CutWound>() == null)
            {
                return GetOrCreateSpawnArea(candidate, areaId);
            }
        }

        return null;
    }

    bool IsSpawnAreaCollider(Collider2D candidate, string areaId)
    {
        string objectName = candidate.gameObject.name;
        return objectName.IndexOf(areaId, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("WoundSpawn", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("SpawnArea", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    WoundSpawnArea GetOrCreateSpawnArea(Collider2D collider, string areaId)
    {
        WoundSpawnArea area = collider.GetComponent<WoundSpawnArea>();
        if (area == null)
        {
            area = collider.gameObject.AddComponent<WoundSpawnArea>();
        }

        area.areaId = string.IsNullOrWhiteSpace(area.areaId) ? areaId : area.areaId;
        area.areaCollider = collider;
        if (area.edgePadding <= 0f)
        {
            area.edgePadding = spawnAreaEdgePadding;
        }

        return area;
    }

    Vector2 ClampToBoxSpawnArea(Vector2 desiredWorldPosition, CutWound wound, BoxCollider2D boxCollider, float areaPadding)
    {
        Vector2 woundHalfSize = GetWoundHalfSizeInAreaSpace(wound, boxCollider.transform);
        float padding = Mathf.Max(0f, areaPadding);

        Vector2 min = boxCollider.offset - (boxCollider.size * 0.5f) + woundHalfSize + Vector2.one * padding;
        Vector2 max = boxCollider.offset + (boxCollider.size * 0.5f) - woundHalfSize - Vector2.one * padding;

        if (min.x > max.x)
        {
            float center = boxCollider.offset.x;
            min.x = center;
            max.x = center;
        }

        if (min.y > max.y)
        {
            float center = boxCollider.offset.y;
            min.y = center;
            max.y = center;
        }

        Vector2 desiredAreaLocal = ResolveDesiredAreaLocalPosition(desiredWorldPosition, boxCollider, min, max);
        Vector3 clampedAreaLocal = desiredAreaLocal;
        clampedAreaLocal.x = Mathf.Clamp(clampedAreaLocal.x, min.x, max.x);
        clampedAreaLocal.y = Mathf.Clamp(clampedAreaLocal.y, min.y, max.y);

        Vector3 clampedWorld = boxCollider.transform.TransformPoint(clampedAreaLocal);
        return new Vector2(clampedWorld.x, clampedWorld.y);
    }

    Vector2 ResolveDesiredAreaLocalPosition(Vector2 layoutPosition, BoxCollider2D boxCollider, Vector2 min, Vector2 max)
    {
        if (IsInsideAreaBounds(layoutPosition, boxCollider))
        {
            return layoutPosition;
        }

        if (IsInsideChestLayout(layoutPosition))
        {
            float x = Mathf.InverseLerp(chestLayoutMin.x, chestLayoutMax.x, layoutPosition.x);
            float y = Mathf.InverseLerp(chestLayoutMin.y, chestLayoutMax.y, layoutPosition.y);
            return new Vector2(Mathf.Lerp(min.x, max.x, x), Mathf.Lerp(min.y, max.y, y));
        }

        if (layoutPosition.x >= -1f && layoutPosition.x <= 1f && layoutPosition.y >= -1f && layoutPosition.y <= 1f)
        {
            float x = Mathf.InverseLerp(-1f, 1f, layoutPosition.x);
            float y = Mathf.InverseLerp(-1f, 1f, layoutPosition.y);
            return new Vector2(Mathf.Lerp(min.x, max.x, x), Mathf.Lerp(min.y, max.y, y));
        }

        Vector3 desiredWorld = new Vector3(layoutPosition.x, layoutPosition.y, boxCollider.transform.position.z);
        return boxCollider.transform.InverseTransformPoint(desiredWorld);
    }

    bool IsInsideAreaBounds(Vector2 position, BoxCollider2D boxCollider)
    {
        if (boxCollider == null)
        {
            return false;
        }

        Vector2 min = boxCollider.offset - (boxCollider.size * 0.5f);
        Vector2 max = boxCollider.offset + (boxCollider.size * 0.5f);
        return position.x >= min.x &&
               position.x <= max.x &&
               position.y >= min.y &&
               position.y <= max.y;
    }

    bool IsInsideChestLayout(Vector2 position)
    {
        return position.x >= chestLayoutMin.x &&
               position.x <= chestLayoutMax.x &&
               position.y >= chestLayoutMin.y &&
               position.y <= chestLayoutMax.y;
    }

    Vector2 GetWoundHalfSizeInAreaSpace(CutWound wound, Transform areaTransform)
    {
        if (wound == null || areaTransform == null)
        {
            return Vector2.zero;
        }

        Bounds? worldBounds = GetWoundWorldBounds(wound);
        if (!worldBounds.HasValue)
        {
            return Vector2.zero;
        }

        Bounds bounds = worldBounds.Value;
        Vector3[] corners =
        {
            new Vector3(bounds.min.x, bounds.min.y, bounds.center.z),
            new Vector3(bounds.min.x, bounds.max.y, bounds.center.z),
            new Vector3(bounds.max.x, bounds.min.y, bounds.center.z),
            new Vector3(bounds.max.x, bounds.max.y, bounds.center.z)
        };

        Vector2 min = areaTransform.InverseTransformPoint(corners[0]);
        Vector2 max = min;

        for (int i = 1; i < corners.Length; i++)
        {
            Vector2 point = areaTransform.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return (max - min) * 0.5f;
    }

    Bounds? GetWoundWorldBounds(CutWound wound)
    {
        Collider2D hitbox = wound.CutHitbox != null ? wound.CutHitbox : wound.SpellBoundsHitbox;
        if (hitbox != null)
        {
            return hitbox.bounds;
        }

        SpriteRenderer[] renderers = wound.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds? bounds = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            bounds = bounds.HasValue ? Encapsulate(bounds.Value, renderer.bounds) : renderer.bounds;
        }

        return bounds;
    }

    Bounds Encapsulate(Bounds bounds, Bounds other)
    {
        bounds.Encapsulate(other.min);
        bounds.Encapsulate(other.max);
        return bounds;
    }

    void ClearExistingWoundsForLayout()
    {
        for (int i = 0; i < cutWounds.Count; i++)
        {
            CutWound wound = cutWounds[i];
            if (wound != null)
            {
                Transform woundRoot = GetSpawnedWoundRoot(wound);
                Destroy(woundRoot != null ? woundRoot.gameObject : wound.gameObject);
            }
        }

        cutWounds.Clear();
    }

    Transform GetSpawnedWoundRoot(CutWound wound)
    {
        if (wound == null)
        {
            return null;
        }

        Transform current = wound.transform;
        while (current.parent != null &&
               current.parent != transform &&
               current.parent.GetComponent<PatientWounds>() == null &&
               current.parent.name != "Wounds" &&
               !string.Equals(current.parent.name, wound.SpawnAreaId, System.StringComparison.OrdinalIgnoreCase))
        {
            current = current.parent;
        }

        return current;
    }

    CutWound CreateWound(WoundLayoutEntry entry)
    {
        string prefabPath = entry.woundType == CutWound.WoundType.Laceration ? "Fabs/NLaceration" : "Fabs/NCutWound";
        GameObject woundPrefab = Resources.Load<GameObject>(prefabPath);
        if (woundPrefab == null)
        {
            Debug.LogWarning("Mission wound layout could not find Resources/" + prefabPath + ".");
            return null;
        }

        Transform woundParent = ResolveWoundParent(entry);
        GameObject woundObject = woundParent != null ? Instantiate(woundPrefab, woundParent) : Instantiate(woundPrefab);
        NormalizeSpawnedLayoutWound(woundObject.transform);
        CutWound wound = woundObject.GetComponent<CutWound>();
        if (wound == null)
        {
            wound = woundObject.GetComponentInChildren<CutWound>(true);
        }

        if (wound == null)
        {
            Debug.LogWarning("Mission wound layout prefab has no CutWound component: Resources/" + prefabPath + ".");
            Destroy(woundObject);
            return null;
        }

        if (!string.IsNullOrWhiteSpace(entry.woundName))
        {
            woundObject.name = entry.woundName;
            wound.name = entry.woundName;
        }

        Patient patient = GetComponent<Patient>();
        if (patient != null)
        {
            wound.SetPatient(patient);
        }

        cutWounds.Add(wound);
        return wound;
    }

    void NormalizeSpawnedLayoutWound(Transform woundRoot)
    {
        if (woundRoot == null)
        {
            return;
        }

        BoxCollider2D[] boxColliders = woundRoot.GetComponentsInChildren<BoxCollider2D>(true);
        for (int i = 0; i < boxColliders.Length; i++)
        {
            NormalizeBoxColliderScale(boxColliders[i]);
        }
    }

    void NormalizeBoxColliderScale(BoxCollider2D boxCollider)
    {
        if (boxCollider == null)
        {
            return;
        }

        Transform colliderTransform = boxCollider.transform;
        Vector3 scale = colliderTransform.localScale;
        if (Mathf.Approximately(scale.x, 1f) &&
            Mathf.Approximately(scale.y, 1f) &&
            Mathf.Approximately(scale.z, 1f))
        {
            return;
        }

        boxCollider.offset = new Vector2(boxCollider.offset.x * scale.x, boxCollider.offset.y * scale.y);
        boxCollider.size = new Vector2(boxCollider.size.x * Mathf.Abs(scale.x), boxCollider.size.y * Mathf.Abs(scale.y));
        colliderTransform.localScale = Vector3.one;
    }

    Transform ResolveWoundParent(WoundLayoutEntry entry)
    {
        if (entry == null)
        {
            return transform;
        }

        Transform layerRoot = ResolveLayerRoot(entry.woundLocation);
        Transform openWoundLayer = layerRoot != null ? FindChildByName(layerRoot, "OpenWoundLayer") : null;
        Transform woundsRoot = openWoundLayer != null ? FindChildByName(openWoundLayer, "Wounds") : null;
        if (woundsRoot == null)
        {
            woundsRoot = layerRoot != null ? FindChildByName(layerRoot, "Wounds") : null;
        }

        Transform baseParent = woundsRoot != null ? woundsRoot : (layerRoot != null ? layerRoot : transform);
        return GetOrCreateSpawnAreaParent(baseParent, GetSpawnAreaId(entry));
    }

    Transform GetOrCreateSpawnAreaParent(Transform baseParent, string spawnAreaId)
    {
        if (baseParent == null || string.IsNullOrWhiteSpace(spawnAreaId))
        {
            return baseParent;
        }

        Transform existing = FindDirectChildByName(baseParent, spawnAreaId);
        if (existing != null)
        {
            return existing;
        }

        GameObject areaObject = new GameObject(spawnAreaId);
        Transform areaTransform = areaObject.transform;
        areaTransform.SetParent(baseParent, false);
        areaTransform.localPosition = Vector3.zero;
        areaTransform.localRotation = Quaternion.identity;
        areaTransform.localScale = Vector3.one;
        return areaTransform;
    }

    Transform ResolveLayerRoot(CutWound.WoundLocation location)
    {
        string rootName = null;

        switch (location)
        {
            case CutWound.WoundLocation.Outside:
                rootName = "BodyLayer";
                break;
            case CutWound.WoundLocation.Inside:
                rootName = "OrganLayer";
                break;
            case CutWound.WoundLocation.Part:
                rootName = "PartLayer";
                break;
        }

        return string.IsNullOrEmpty(rootName) ? null : FindChildByName(transform, rootName);
    }

    Transform FindChildByName(Transform searchRoot, string childName)
    {
        if (searchRoot == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < searchRoot.childCount; i++)
        {
            Transform child = searchRoot.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform match = FindChildByName(child, childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    Transform FindDirectChildByName(Transform searchRoot, string childName)
    {
        if (searchRoot == null || string.IsNullOrEmpty(childName))
        {
            return null;
        }

        for (int i = 0; i < searchRoot.childCount; i++)
        {
            Transform child = searchRoot.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    public void RebuildWoundList()
    {
        cutWounds.Clear();
        cutWounds.AddRange(GetComponentsInChildren<CutWound>(true));
        cutWounds.RemoveAll(wound => wound == null);
    }
}
