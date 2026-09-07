using System.Collections;
using UnityEngine;

public class BigHyperBomb : MonoBehaviour
{
    [Header("物理参数")]
    public float initialBounceForce = 8f;    // 初始反弹力
    public float bounceDecay = 0.7f;         // 每次反弹衰减倍率
    public float lifeTime = 4f;              // 生命周期

    [Header("检测参数")]
    public LayerMask groundLayer;            // 地面/墙壁层，用于过滤触发
    public CircleCollider2D groundCheck;     // GroundCheck 的触发器 (isTrigger=true)
    [HideInInspector] public bool canBounce = true; // 是否允许反弹

    [Header("爆炸参数")]
    public GameObject explosionPrefab;       // 爆炸风预制体
    public float explosionDelay = 0.5f;      // 近点和远点生成间隔

    [Header("音效参数")]
    public AudioClip bounceSFX;              // 反弹音效
    public float bounceVolume = 1f;          // 音量（可调）

    private Rigidbody2D rb;
    private float currentBounceForce;
    private bool isExploding = false;
    private AudioSource audioSource;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentBounceForce = initialBounceForce;

        // 确保有 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D 音效

        StartCoroutine(LifeTimer());
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isExploding) return;
        if (collision.CompareTag("Bullet") || collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.Deflect();
            return;
        }

        if (collision.CompareTag("SuperArm"))
        {
            StartCoroutine(Explode());
            return;
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null) iceArrow.DestroyBullet();
            return;
        }

        if (collision.CompareTag("HyperBomb"))
        {
            StartCoroutine(Explode());
            return;
        }

        if (collision.CompareTag("FireStorm"))
        {
            StartCoroutine(Explode());
            return;
        }

        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            if (thunder != null)
            {
                thunder.DestroyOnEnemyHit();
            }
            return;
        }

        if (!canBounce) return; // 禁用反弹

        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            Bounce(collision);
            return;
        }
    }

    private void Bounce(Collider2D hit)
    {
        Vector2 velocity = rb.velocity;

        // 根据当前速度衰减，不完全丢失
        velocity *= bounceDecay;

        // 确保竖向至少有一定反弹力
        Vector2 normal = ((Vector2)transform.position - hit.ClosestPoint(transform.position)).normalized;
        if (Mathf.Abs(normal.y) > 0.5f) // 碰地面
        {
            velocity.y = Mathf.Abs(velocity.y) + currentBounceForce * 0.3f; // 增加一点反弹力
        }
        if (Mathf.Abs(normal.x) > 0.5f) // 碰墙壁
        {
            velocity.x = -velocity.x; // 水平反向
        }

        rb.velocity = velocity;

        // 继续衰减反弹力
        currentBounceForce *= bounceDecay;

        // 播放反弹音效
        if (bounceSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(bounceSFX, bounceVolume);
        }
    }


    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        if (!isExploding)
        {
            StartCoroutine(Explode());
        }
    }

    private IEnumerator Explode()
    {
        isExploding = true;

        Vector2 explosionCenter = transform.position;

        // 隐藏自身 sprite 和 collider
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (groundCheck != null) groundCheck.enabled = false;

        Vector2[] nearOffsets = {
            new Vector2(-2,0), new Vector2(2,0),
            new Vector2(0,2), new Vector2(0,-2)
        };
        Vector2[] farOffsets = {
            new Vector2(-4,0), new Vector2(4,0),
            new Vector2(0,4), new Vector2(0,-4)
        };

        // 生成近爆炸
        foreach (Vector2 offset in nearOffsets)
        {
            Instantiate(explosionPrefab, explosionCenter + offset, Quaternion.identity);
        }

        yield return new WaitForSeconds(explosionDelay);

        // 生成远爆炸
        foreach (Vector2 offset in farOffsets)
        {
            Instantiate(explosionPrefab, explosionCenter + offset, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.transform.position, groundCheck.radius);
        }
    }
}
