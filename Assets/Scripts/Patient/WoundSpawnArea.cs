using UnityEngine;

public class WoundSpawnArea : MonoBehaviour
{
    public string areaId = "Chest";
    public Collider2D areaCollider;
    public float edgePadding = 0.1f;

    public Collider2D Collider
    {
        get
        {
            if (areaCollider == null)
            {
                areaCollider = GetComponent<Collider2D>();
            }

            return areaCollider;
        }
    }
}
