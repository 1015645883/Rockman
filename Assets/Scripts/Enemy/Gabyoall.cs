using UnityEngine;
using System.Collections;

public class Gabyoall : MonoBehaviour
{
    public int health = 1;
    public float normalSpeed = 1.5f;
    public float angrySpeedMultiplier = 2f;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    public float movementRange = 5f; // 小怪的左右活动范围
    private float originX; // 初始出生点

    public Animator animator;
    public string wakeTrigger = "Wake";
    public string sleepTrigger = "Sleep";
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.left;
    private bool isSleeping = false;
    private bool isAngry = false;
    private float sleepTimer = 0f;
    private float currentSpeed;
    private AudioSource audioSource;
    public AudioClip Damage;
    public bool isFrozen = false;
    private Transform Player
    {
        get
        {
            // 特殊关卡优先获取当前受控角色
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform;

            // 普通关卡查找标签为 Player 的对象
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null;
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = normalSpeed;
        originX = transform.position.x;
    }

    void Update()
    {
        if (isFrozen)
        {
            return;
        }
        if (isSleeping)
        {
            sleepTimer -= Time.deltaTime;
            if (sleepTimer <= 0f) WakeUp();
            return;
        }

        // 怒气状态逻辑
        if (Player != null && Mathf.Abs(Player.position.y - transform.position.y) < 1f)
        {
            if (!isAngry) EnterAngryState();
        }
        else
        {
            if (isAngry) EnterNormalState();
        }



        // 范围检测，决定是否掉头
        if (ShouldTurnByRange())
        {
            Flip();
        }

        // 移动
        rb.velocity = new Vector2(direction.x * currentSpeed, rb.velocity.y);
    }

    private bool ShouldTurnByRange()
    {
        float posX = transform.position.x;
        if (direction.x < 0 && posX <= originX - movementRange)
        {
            return true;
        }
        if (direction.x > 0 && posX >= originX + movementRange)
        {
            return true;
        }
        return false;
    }

    private void Flip()
    {
        direction = -direction;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void EnterNormalState()
    {
        isAngry = false;
        currentSpeed = normalSpeed;
        animator.SetTrigger(wakeTrigger);
    }

    private void EnterAngryState()
    {
        isAngry = true;
        currentSpeed = normalSpeed * angrySpeedMultiplier;
    }

    private void EnterSleepState()
    {
        isSleeping = true;
        rb.velocity = Vector2.zero;
        sleepTimer = 5f;
        animator.SetTrigger("Sleep");
    }

    private void WakeUp()
    {
        isSleeping = false;
        EnterNormalState();
    }

    void OnTriggerEnter2D(Collider2D collision)
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
            bullet.Deflect();
            EnterSleepState();
        }

        if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(1);
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
            TakeDamage(1);
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen) // 如果敌人未被冻结
                {
                    iceArrow.Freeze(this); // 调用冰冻方法
                    iceArrow.DestroyBullet();
                }
                else // 已被冻结，再次接触造成大量伤害
                {
                    TakeDamage(1); // 可以根据实际需求调整伤害数值
                    iceArrow.DestroyBullet();
                }
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
        }
    }

    void OnTriggerStay2D(Collider2D collision)
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
            TakeDamage(1);
        }
        if (collision.CompareTag("HyperBomb"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("FireStorm"))
        {
            TakeDamage(1);
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
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
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.18f);
    }
}
