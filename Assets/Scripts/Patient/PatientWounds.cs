using System.Collections.Generic;
using UnityEngine;

public class PatientWounds : MonoBehaviour
{
    [SerializeField] private List<CutWound> cutWounds = new List<CutWound>();

    public IReadOnlyList<CutWound> CutWounds => cutWounds;

    void Awake()
    {
        cutWounds.RemoveAll(wound => wound == null);
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
}
