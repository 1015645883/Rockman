using UnityEngine;

/// <summary>
/// 一个手动摆放的相机区域。BoxCollider2D 表示相机可见画面的合法世界范围。
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class StageCameraZone : MonoBehaviour
{
    [Tooltip("区域重叠时数值越高越优先。")]
    [SerializeField] private int priority;

    private BoxCollider2D zoneBounds;

    public int Priority => priority;
    public Bounds WorldBounds => zoneBounds != null ? zoneBounds.bounds : new Bounds(transform.position, Vector3.zero);

    private void Awake()
    {
        zoneBounds = GetComponent<BoxCollider2D>();
    }

    private void OnValidate()
    {
        zoneBounds = GetComponent<BoxCollider2D>();
        if (zoneBounds != null)
            zoneBounds.isTrigger = true;
    }

    public bool Contains(Vector3 worldPosition)
    {
        return zoneBounds != null && zoneBounds.OverlapPoint(worldPosition);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
            return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }
}
