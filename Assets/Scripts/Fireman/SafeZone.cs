using UnityEngine;

public class SafeZone : MonoBehaviour
{
    [Header("安全区类型")]
    public bool isFinalZone = false; // 是否为最终安全区

    // 可选：用于可视化
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isFinalZone ? Color.blue : Color.green;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
                Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
            else if (col is CircleCollider2D circle)
                Gizmos.DrawWireSphere(circle.bounds.center, circle.radius);
        }
    }
}
