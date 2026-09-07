using System.Collections;
using UnityEngine;

public class ChangKey : MonoBehaviour
{
    [Header("组件引用")]
    public Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    [Header("属性")]
    public int health = 3;
    public float upwardDistance = 12f;   // 初始向上跳的距离
    public float moveDuration = 0.3f;    // 上升时间
    public float minFallSpeedX = 0.5f;
    public float maxFallSpeedX = 1.5f;
    public float minFallSpeedY = -0.5f;
    public float maxFallSpeedY = -0.2f;
    public float lifeTime = 10f;
    private Vector2 fallVelocity;        // 当前下落速度
    private float fallTimer = 0f;        // 计时器
    public float fallRefreshInterval = 1f; // 每秒刷新一次速度
    private bool isDead = false;
    [Header("攻击")]
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    public AudioClip Damage;
    private AudioSource audioSource;
    public event System.Action OnDie;
    public bool isFrozen = false;

    // 上升/下落状态
    private enum MovementState { Idle, JumpingUp, Falling }
    private MovementState moveState = MovementState.Idle;

    private Vector2 jumpStartPos;
    private Vector2 jumpTargetPos;
    private float jumpTimer = 0f;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 初始化上升
        jumpStartPos = transform.position;
        jumpTargetPos = jumpStartPos + Vector2.up * upwardDistance;
        jumpTimer = 0f;
        moveState = MovementState.JumpingUp;

        spawnTime = Time.time;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 生命周期判断
        if (Time.time - spawnTime >= lifeTime)
        {
            Die();
            return;
        }

        if (isFrozen)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 控制运动状态
        switch (moveState)
        {
            case MovementState.JumpingUp:
                jumpTimer += Time.fixedDeltaTime;
                float t = jumpTimer / moveDuration;
                if (t >= 1f)
                {
                    transform.position = jumpTargetPos;
                    moveState = MovementState.Falling;
                }
                else
                {
                    transform.position = Vector2.Lerp(jumpStartPos, jumpTargetPos, t);
                }
                break;

            case MovementState.Falling:
                fallTimer += Time.fixedDeltaTime;
                if (fallTimer >= fallRefreshInterval || fallVelocity == Vector2.zero)
                {
                    float speedX = Random.Range(minFallSpeedX, maxFallSpeedX) * (Random.value > 0.5f ? 1f : -1f);
                    float speedY = Random.Range(minFallSpeedY, maxFallSpeedY);
                    fallVelocity = new Vector2(speedX, speedY);
                    fallTimer = 0f;
                }
                rb.velocity = fallVelocity;
                break;

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 玩家伤害
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            var playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        // 子弹伤害
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
            bullet.DestroyBullet();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(3);
        }

        // 滚刀伤害
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

        // 超级手臂
        if (collision.CompareTag("SuperArm")) TakeDamage(3);

        // 冰箭
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

        if (collision.CompareTag("HyperBomb")) TakeDamage(3);
        if (collision.CompareTag("FireStorm")) TakeDamage(2);
        if (collision.CompareTag("ThunderBeam"))
        {
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();
            if (thunder != null) TakeDamage(3);
            if (smallThunder != null && Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
            {
                TakeDamage(2);
                smallThunder.lastDamageTime = Time.time;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 持续伤害玩家
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            var playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        // 持续滚刀伤害
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

    private void TakeDamage(int amount)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Damage;
            audioSource.playOnAwake = false;
        }

        audioSource.Play();
        health -= amount;

        if (health <= 0) Die();

        if (spriteRenderer != null)
            StartCoroutine(HitFlash());
    }

    private IEnumerator HitFlash()
    {
        spriteRenderer.enabled = false;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.enabled = true;
    }

    private void Die()
    {
        isFrozen = false;
        isDead = true;
        rb.velocity = Vector2.zero;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        OnDie?.Invoke();

        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null) drop.TryDrop();

        Destroy(gameObject, 0.3f);
    }

}
