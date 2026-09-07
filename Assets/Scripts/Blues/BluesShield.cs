using UnityEngine;

public class BluesShield : MonoBehaviour
{
    [Header("格挡音效")]
    public AudioClip blockSfx;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnemyWeapon"))
        {
            // 播放格挡音效
            if (blockSfx != null && audioSource != null)
            {
                audioSource.PlayOneShot(blockSfx);
            }

            // 获取 Rigidbody2D
            Rigidbody2D rb = collision.attachedRigidbody;
            if (rb != null)
            {
                // 反转速度（X 轴反转，Y 轴保留，像“打回去”）
                rb.velocity = new Vector2(-rb.velocity.x * 1.2f, -rb.velocity.y * 1.2f);

                // 把它的标签改成 Bullet（相当于变成玩家的攻击）
                collision.tag = "Bullet";
            }
            // 🔹 反转精灵
            SpriteRenderer sr = collision.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = rb.velocity.x > 0; // X 正方向朝右，负方向朝左
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        // 获取 Collider2D 来显示范围
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(circle.offset, circle.radius);
            }
            else if (col is CapsuleCollider2D capsule)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(capsule.offset, capsule.size); // 简单画方框替代
            }
        }
    }
}
