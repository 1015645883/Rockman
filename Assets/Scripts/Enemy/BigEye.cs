using System.Collections;
using UnityEngine;

public class BigEye : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] public int health = 20;
    [SerializeField] public int damage = 6;
    [SerializeField] private float damageCooldown = 1.4f;
    private float lastDamageTime;
    public float selfDamageCooldown = 0.5f;
    private float lastSelfDamageTime;
    public GameObject explosionPrefab;
    [Header("跳跃参数")]
    [SerializeField] private float smallJumpForce = 5f;
    [SerializeField] private float bigJumpForce = 10f;
    [SerializeField] private float jumpCooldown = 2f;
    private float lastJumpTime;

    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform; // 特殊关卡当前受控角色

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null; // 普通关卡
        }
    }

    [SerializeField] private float activationDistance = 10f;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    // 状态变量
    private bool isJumping = false;
    private bool isGrounded = true;
    private bool isActive = false;
    public bool isFrozen = false;
    // 组件引用
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deadSound;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }


    private void Update()
    {
        if (Player == null) return; // 用 Player 替换原来的 player
        if (isFrozen) return;

        // 检测地面
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0, groundLayer);

        // 实时检测玩家是否在激活范围内
        float distanceToPlayer = Vector2.Distance(transform.position, Player.position);
        isActive = distanceToPlayer < activationDistance;

        if (isActive && isGrounded && Time.time > lastJumpTime + jumpCooldown)
        {
            JumpTowardsPlayer();
        }

        if (isGrounded && isJumping && Mathf.Abs(rb.velocity.y) < 0.01f)
        {
            isJumping = false;
            if (animator != null)
            {
                animator.SetBool("IsJumping", false);
            }
        }
    }


    private void JumpTowardsPlayer()
    {
        if (Player == null) return;

        // 转向玩家
        bool playerOnRight = Player.position.x > transform.position.x;
        spriteRenderer.flipX = playerOnRight;

        // 随机选择小跳或大跳
        float jumpForce = Random.value < 0.5f ? smallJumpForce : bigJumpForce;

        // 计算水平位移力
        float horizontalDistance = Player.position.x - transform.position.x;
        float horizontalForce = Mathf.Clamp(horizontalDistance, -3f, 3f); // 限制水平速度，避免过快

        // 重置速度并施加跳跃力
        rb.velocity = new Vector2(0f, 0f); // 重置速度，避免叠加
        rb.AddForce(new Vector2(horizontalForce, jumpForce), ForceMode2D.Impulse);

        if (audioSource != null && jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }

        lastJumpTime = Time.time;
        isJumping = true;
        if (animator != null)
        {
            animator.SetBool("IsJumping", true);
        }
    }


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
            TakeDamage(1);
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(4);
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.DestroyBullet();
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("SuperArm"))
        {
            TakeDamage(5);
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen) // 如果敌人未被冻结
                {
                    iceArrow.Freeze(this); // 调用冰冻方法
                    TakeDamage(1);
                    iceArrow.DestroyBullet();
                }
                else // 已被冻结，再次接触造成大量伤害
                {
                    TakeDamage(3); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(5);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(4);
            FireStorm fire = collision.GetComponent<FireStorm>();
            if (fire != null) fire.DestroyBullet();
        }
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
                     TakeDamage(4);
                     smallThunder.lastDamageTime = Time.time;
                   }
                    return;
             }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 玩家与敌人持续重叠时才伤害玩家
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;  // 更新最后一次伤害时间
            }
        }

        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null && Time.time > shockWave.lastDamageTime + shockWave.damageCooldown)
            {
                TakeDamage(1);
                shockWave.lastDamageTime = Time.time;
            }
        }
    }

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        if (!isActive) isActive = true;

        if (health <= 0)
        {
            Die();
        }
        // ✅ 播放受击闪烁效果
        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
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
    private void Die()
    {
        isActive = false;
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        // 禁用碰撞器和物理效果
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = deadSound;
        audioSource.playOnAwake = false;
        audioSource.Play();
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.02f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
}
