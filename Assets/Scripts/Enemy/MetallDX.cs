using UnityEngine;
using System.Collections;

public class MetallDX : MonoBehaviour
{
    [Header("基础属性")]
    public int health = 1;
    public int damage = 2;
    public float damageCooldown = 1.5f;
    private float lastDamageTime;

    [Header("攻击相关")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float timeBetweenShots = 0.3f;
    public float bulletSpeed = 10f;
    public float[] attackAngles = { 60f, 30f, -30f, -60f };
    public float attackInterval = 2f;
    public float detectionRadius = 6f;
    public LayerMask playerLayer;

    [Header("浮动设置")]
    public float floatAmplitude = 0.5f;   // 上下浮动幅度
    public float floatFrequency = 1.5f;   // 浮动速度
    private Vector3 startPosition;

    [Header("引用")]
    public Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    public AudioClip damageSound;
    private bool isAttacking;
    private bool playerInRange;
    public bool isFrozen = false;

    // ✅ 动态绑定玩家
    private Transform Player
    {
        get
        {
            SpecialLevelManager slm = FindObjectOfType<SpecialLevelManager>();
            if (slm != null && slm.currentPlayer != null)
                return slm.currentPlayer.transform;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            return playerObj != null ? playerObj.transform : null;
        }
    }

    void Awake()
    {
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isFrozen) return;

        // ✅ 上下漂浮
        transform.position = startPosition + new Vector3(0f, Mathf.Sin(Time.time * floatFrequency) * floatAmplitude, 0f);

        if (Player == null) return;
        CheckPlayerInRange();

        if (playerInRange && !isAttacking)
            StartCoroutine(AttackCycle());
    }

    void CheckPlayerInRange()
    {
        if (Player == null)
        {
            playerInRange = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, Player.position);
        playerInRange = distance <= detectionRadius;
    }

    IEnumerator AttackCycle()
    {
        isAttacking = true;

        while (playerInRange)
        {
            yield return StartCoroutine(AttackSequence());
            yield return new WaitForSeconds(attackInterval);
        }

        isAttacking = false;
    }

    IEnumerator AttackSequence()
    {
        foreach (float angle in attackAngles)
        {
            FireAtAngle(angle);
            yield return new WaitForSeconds(timeBetweenShots);
        }
    }

    void FireAtAngle(float angle)
    {
        if (!bulletPrefab || !firePoint) return;

        float worldAngle = transform.eulerAngles.z + angle;
        Quaternion rotation = Quaternion.Euler(0, 0, worldAngle);
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotation);

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = rotation * Vector2.right * bulletSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // ✅ 玩家接触伤害
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        // ✅ 子弹类攻击
        if (collision.CompareTag("Bullet"))
        {
            TakeDamage(1);
        }
        else if (collision.CompareTag("ChargeBullet"))
        {
            TakeDamage(4);
        }
        else if (collision.CompareTag("RollingCutter") ||
                 collision.CompareTag("FireStorm") ||
                 collision.CompareTag("ThunderBeam") ||
                 collision.CompareTag("HyperBomb") ||
                 collision.CompareTag("SuperArm"))
        {
            TakeDamage(1);
        }
        else if (collision.CompareTag("IceArrow"))
        {
            // ✅ 冰箭逻辑（冰冻或伤害）
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!isFrozen)
                {
                    iceArrow.Freeze(this);   // 调用 Freeze 冰冻
                    iceArrow.DestroyBullet();
                }
                else
                {
                    TakeDamage(1);           // 已经冻结，造成伤害
                    iceArrow.DestroyBullet();
                }
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
    }

    void TakeDamage(int amount)
    {
        if (audioSource && damageSound)
            audioSource.PlayOneShot(damageSound);

        health -= amount;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        if (animator != null)
            animator.SetTrigger("Die");

        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
            drop.TryDrop();

        Destroy(gameObject, 0.18f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
