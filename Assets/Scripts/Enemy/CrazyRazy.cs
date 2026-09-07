using System.Collections;
using UnityEngine;

public class CrazyRazy : MonoBehaviour
{
    [Header("基础属性")]
    public int health = 8;
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("行动属性")]
    public float detectRadius = 5f;   // 玩家检测范围
    public float moveSpeed = 2f;      // 移动速度
    private Transform player;
    private bool isChasing = false;

    [Header("分裂配置")]
    public GameObject upperBodyPrefab;
    public Transform spawnPoint;

    [Header("音效 & 动画")]
    public AudioClip Damage;
    private AudioSource audioSource;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public bool isFrozen = false;

    [Header("特效")]
    public GameObject explosionPrefab;


    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        // 延迟绑定玩家
        StartCoroutine(FindPlayerWithDelay());
    }
    private IEnumerator FindPlayerWithDelay()
    {
        // 每隔 0.1 秒查找一次直到找到玩家
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("CrazyRazy: 成功找到玩家对象。");
                yield break; // 找到后结束协程
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    private void Update()
    {
        if (isFrozen)
        {
            return;
        }
        if (!isChasing)
        {
            // Idle 状态下检测玩家
            Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRadius, LayerMask.GetMask("Player"));
            if (hit != null)
            {
                isChasing = true;
                if (animator != null) animator.SetBool("isChasing", true);
            }
        }
        else
        {
            // 追踪状态：向玩家移动
            transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);

            // 翻转朝向（改用 flipX，而不是 scale）
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = player.position.x > transform.position.x;
            }
        }

    }

    private void OnDrawGizmosSelected()
    {
        // 在 Scene 视图中画出检测范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

    // ====== 碰撞伤害 & 受击逻辑（保持你的原逻辑） ======
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet.DestroyBullet();
        }
        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(4);
            bullet.DestroyBullet();
        }
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm")) TakeDamage(4);
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
                else
                {
                    TakeDamage(2);
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb")) TakeDamage(4);
        if (collision.CompareTag("FireStorm")) TakeDamage(2);
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            if (thunder != null)
            {
                TakeDamage(4);
                thunder.DestroyOnEnemyHit();
                return;
            }
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    TakeDamage(2);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(2);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(2);
                shockWave.lastDamageTime = Time.time;
            }
        }
    }

    void TakeDamage(int amount)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Damage;
            audioSource.playOnAwake = false;
        }

        audioSource.Play();
        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
        health -= amount;
        if (health <= 0)
        {
            SplitIntoUpperBody();
        }
    }
    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        // 先隐藏
        spriteRenderer.enabled = false;

        // 等待0.1秒
        yield return new WaitForSeconds(0.06f);

        // 显示回来
        spriteRenderer.enabled = true;

        yield return null;
    }

    void SplitIntoUpperBody()
    {
        // 在当前位置生成爆炸特效
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 生成上半身
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Instantiate(upperBodyPrefab, spawnPos, Quaternion.identity);

        Destroy(gameObject);
    }

}
