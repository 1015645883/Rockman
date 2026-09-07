using System.Collections;
using UnityEngine;

public class BunbyHeli : MonoBehaviour
{
    [Header("移动参数")]
    public float patrolSpeed = 3f;
    public float diveSpeed = 10f;
    public float diveDistance = 7f;
    public float detectionRange = 5f;
    public Transform startPos;
    public float patrolRange = 2f;

    [Header("玩家与伤害")]
    public Transform player;
    public int damage = 1;
    public float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("生命")]
    public int health = 1;

    private bool isDiving = false;
    private bool isReturning = false;
    private Vector3 diveTarget;
    private Vector3 ascendTarget;
    private int patrolDirection = 1; // 1 = 右, -1 = 左
    public bool isFrozen = false;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    public AudioClip Damage;

    private Vector3 patrolCenter; // 当前巡逻中心点

    void Start()
    {
        StartCoroutine(FindPlayerWithDelay());

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        if (startPos != null)
            transform.position = startPos.position;

        animator = GetComponent<Animator>();
        if (animator != null)
            animator.Play("Flying");

        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        patrolDirection = 1;
        patrolCenter = transform.position; // 初始化巡逻中心为出生点
    }

    private IEnumerator FindPlayerWithDelay()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                yield break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;
        if (isFrozen)
        {
            return;
        }
        if (isDiving)
        {
            DiveMove();
        }
        else if (isReturning)
        {
            ReturnMove();
        }
        else
        {
            PatrolMove();

            if (Mathf.Abs(player.position.x - transform.position.x) < detectionRange)
            {
                StartDive();
            }
        }
    }

    void PatrolMove()
    {
        Vector3 newPos = transform.position + Vector3.right * patrolDirection * patrolSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);

        // 超出当前巡逻中心范围就反向
        if (transform.position.x > patrolCenter.x + patrolRange)
            patrolDirection = -1;
        else if (transform.position.x < patrolCenter.x - patrolRange)
            patrolDirection = 1;

        // 翻转贴图
        if (spriteRenderer != null)
            spriteRenderer.flipX = patrolDirection > 0;
    }

    void StartDive()
    {
        isDiving = true;
        diveTarget = new Vector3(player.position.x, transform.position.y - diveDistance, transform.position.z);
        ascendTarget = new Vector3(transform.position.x, startPos.position.y, transform.position.z);
    }

    void DiveMove()
    {
        Vector3 dir = (diveTarget - transform.position).normalized;
        rb.MovePosition(transform.position + dir * diveSpeed * Time.fixedDeltaTime);

        patrolDirection = dir.x >= 0 ? 1 : -1;
        if (spriteRenderer != null)
            spriteRenderer.flipX = patrolDirection < 0;

        if (Vector3.Distance(transform.position, diveTarget) < 0.1f)
        {
            isDiving = false;
            isReturning = true;
        }
    }

    void ReturnMove()
    {
        Vector3 dir = (ascendTarget - transform.position).normalized;
        rb.MovePosition(transform.position + dir * diveSpeed * Time.fixedDeltaTime);

        patrolDirection = dir.x >= 0 ? 1 : -1;
        if (spriteRenderer != null)
            spriteRenderer.flipX = patrolDirection < 0;

        if (Vector3.Distance(transform.position, ascendTarget) < 0.1f)
        {
            isReturning = false;

            // 完成俯冲后，以当前位置为新的巡逻中心
            patrolCenter = transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collider2D collision)
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
            if (bullet != null)
            {
                TakeDamage(1);
                if (bullet != null)
                {
                    bullet.DestroyBullet();
                }
            }
        }

        if (collision.CompareTag("ChargeBullet") || collision.CompareTag("FireStorm") || collision.CompareTag("HyperBomb") || collision.CompareTag("ThunderBeam"))
        {
            TakeDamage(1);
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
        if (collision.CompareTag("RollingCutter"))
        {
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null && Time.time > cutter.lastDamageTime + cutter.damageCooldown)
            {
                TakeDamage(1);
                cutter.lastDamageTime = Time.time;
            }
        }

    }

    void TakeDamage(int amount)
    {
        if (audioSource != null && Damage != null)
            audioSource.PlayOneShot(Damage);

        health -= amount;
        if (health <= 0)
            Die();
    }

    void Die()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;
        if (animator != null) animator.SetTrigger("Die");
        EnemyAutoDrop drop = GetComponent<EnemyAutoDrop>();
        if (drop != null)
        {
            drop.TryDrop();
        }
        Destroy(gameObject, 0.18f);
    }
}
