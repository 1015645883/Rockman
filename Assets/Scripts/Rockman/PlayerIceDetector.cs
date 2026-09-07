using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerIceDetector : MonoBehaviour
{
    public Tilemap groundTilemap;
    public TileBase[] iceTiles;  // 支持多个冰块 tile

    public Transform groundCheck;
    public Vector2 checkSize = new Vector2(0.3f, 1.8f);
    public LayerMask groundLayer;

    public bool IsOnIce { get; private set; }

    void Update()
    {
        Collider2D hit = Physics2D.OverlapBox(groundCheck.position, checkSize, 0f, groundLayer);
        if (hit != null && hit.CompareTag("IceFloor"))
        {
            IsOnIce = true;
        }
        else
        {
            IsOnIce = false;
        }
    }



    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(groundCheck.position, checkSize);
        }
    }
}
