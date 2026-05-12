using TMPro;
using UnityEngine;

public class SurgeryForegroundLayer : MonoBehaviour
{
    [SerializeField] private float foregroundZ = -8f;
    [SerializeField] private int lineSortingOrder = 80;
    [SerializeField] private int woundSortingOrder = 60;
    [SerializeField] private int textSortingOrder = 90;
    [SerializeField] private int renderQueue = 4000;

    void LateUpdate()
    {
        LiftLineRenderers();
        LiftWounds();
        LiftWorldText();
    }

    void LiftLineRenderers()
    {
        LineRenderer[] lineRenderers = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
        for (int i = 0; i < lineRenderers.Length; i++)
        {
            LineRenderer lineRenderer = lineRenderers[i];
            if (lineRenderer == null)
            {
                continue;
            }

            lineRenderer.sortingOrder = lineSortingOrder;
            if (lineRenderer.sharedMaterial != null)
            {
                lineRenderer.sharedMaterial.renderQueue = renderQueue;
            }

            for (int pointIndex = 0; pointIndex < lineRenderer.positionCount; pointIndex++)
            {
                Vector3 point = lineRenderer.GetPosition(pointIndex);
                point.z = foregroundZ;
                lineRenderer.SetPosition(pointIndex, point);
            }
        }
    }

    void LiftWounds()
    {
        CutWound[] wounds = FindObjectsByType<CutWound>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < wounds.Length; i++)
        {
            CutWound wound = wounds[i];
            if (wound == null)
            {
                continue;
            }

            Vector3 position = wound.transform.position;
            position.z = foregroundZ;
            wound.transform.position = position;

            SpriteRenderer[] spriteRenderers = wound.GetComponentsInChildren<SpriteRenderer>(true);
            for (int spriteIndex = 0; spriteIndex < spriteRenderers.Length; spriteIndex++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[spriteIndex];
                if (spriteRenderer == null)
                {
                    continue;
                }

                spriteRenderer.sortingOrder = woundSortingOrder;
                if (spriteRenderer.sharedMaterial != null)
                {
                    spriteRenderer.sharedMaterial.renderQueue = renderQueue;
                }
            }
        }
    }

    void LiftWorldText()
    {
        TextMeshPro[] textMeshes = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
        for (int i = 0; i < textMeshes.Length; i++)
        {
            TextMeshPro textMesh = textMeshes[i];
            if (textMesh == null)
            {
                continue;
            }

            textMesh.sortingOrder = textSortingOrder;
            Vector3 position = textMesh.transform.position;
            position.z = foregroundZ;
            textMesh.transform.position = position;
        }
    }
}
