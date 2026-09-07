using System.Collections;
using UnityEngine;

public class RollingCutter_Big : MonoBehaviour
{
    [Header("伤害")]
    public int damage = 4;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;
    public float moveSpeed = 12f;
    public float followSpeed = 6f;
    public float returnSpeed = 6f;

    private Transform owner;
    private Vector2 startPos;

    private bool isReturning = false;
    private bool isDying = false; //  防止重复执行

    private CutmanController cutman;
    private BossHealthSystem bossHealth; 

    // ⭐ 初始化入口（模式1）
    public void Initialize_Mode1(Transform ownerTransform, Vector2 wallPoint)
    {
        owner = ownerTransform;
        cutman = owner.GetComponent<CutmanController>();

        // ⭐ 绑定Boss死亡事件
        BindBossDeath(ownerTransform);

        startPos = owner.position;

        StartCoroutine(TriangleRoutine(wallPoint));
    }

    // ⭐ 初始化入口（模式2）
    public void Initialize_Mode2(Transform ownerTransform)
    {
        owner = ownerTransform;
        cutman = owner.GetComponent<CutmanController>();

        // ⭐ 绑定Boss死亡事件
        BindBossDeath(ownerTransform);

        startPos = owner.position;

        StartCoroutine(FollowRoutine());
    }

    // ⭐ 统一绑定
    void BindBossDeath(Transform ownerTransform)
    {
        bossHealth = ownerTransform.GetComponent<BossHealthSystem>();

        if (bossHealth != null)
        {
            bossHealth.OnBossDeathStart += OnBossDeathStart;
        }
    }

    // ==============================
    // ⭐ 模式1：三角轨迹
    // ==============================
    IEnumerator TriangleRoutine(Vector2 wallPoint)
    {
        Vector2 topPoint = new Vector2(wallPoint.x, wallPoint.y + 8f);

        yield return MoveTo(wallPoint);
        yield return MoveTo(topPoint);
        yield return ReturnToOwner();
    }

    // ==============================
    // ⭐ 模式2：悬浮 + 跟随
    // ==============================
    IEnumerator FollowRoutine()
    {
        // ⭐ 第一步：先飞到头顶（只执行一次）
        Vector2 topPoint = new Vector2(owner.position.x, owner.position.y + 8f);
        yield return MoveTo(topPoint);

        // ⭐ 第二步：开始追踪Boss本体

        while (!isReturning && !isDying)
        {
            //  直接追踪Boss当前位置
            Vector2 followTarget = owner.position;

            transform.position = Vector2.MoveTowards(
                transform.position,
                followTarget,
                followSpeed * Time.deltaTime
            );
            if (Vector2.Distance(transform.position, owner.position) < 0.6f)
            {
                StartCoroutine(ReturnToOwner());
                yield break;
            }
            yield return null;
        }

    }

    // ==============================
    // ⭐ 通用移动
    // ==============================
    IEnumerator MoveTo(Vector2 target)
    {
        while (Vector2.Distance(transform.position, target) > 0.1f)
        {
            if (isDying) yield break; // ⭐ 死亡立即中断

            transform.position = Vector2.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    // ==============================
    // ⭐ 回到Boss
    // ==============================
    IEnumerator ReturnToOwner()
    {
        isReturning = true;

        while (Vector2.Distance(transform.position, owner.position) > 0.5f)
        {
            if (isDying) yield break; // ⭐ 死亡立即中断

            transform.position = Vector2.MoveTowards(
                transform.position,
                owner.position,
                returnSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (cutman != null)
        {
            cutman.ReturnCutter();
        }

        Destroy(gameObject);
    }

    // ==============================
    // ⭐ 外部回收（模式2）
    // ==============================
    public void Recall()
    {
        if (!isReturning && !isDying)
        {
            StartCoroutine(ReturnToOwner());
        }
    }

    // ==============================
    // ⭐ Boss死亡（核心）
    // ==============================
    private void OnBossDeathStart()
    {
        if (isDying) return;

        isDying = true;

        StopAllCoroutines(); // ⭐ 立即停止所有轨迹

        // ❌ 不调用 ReturnCutter（避免逻辑污染）
        Destroy(gameObject); // ⭐ 直接消失
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage;

                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");

                if (currentChar == "Bombman")
                    finalDamage = 5;

                if (currentChar == "Elecman")
                    finalDamage = 6;

                playerHealth.TakeDamage(finalDamage);

                lastDamageTime = Time.time;
            }
        }
        if (collision.CompareTag("Bullet") || collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.Deflect();
            return;
        }
    }

    // ==============================
    // ⭐ 防止内存/事件泄漏
    // ==============================
    private void OnDestroy()
    {
        if (bossHealth != null)
        {
            bossHealth.OnBossDeathStart -= OnBossDeathStart;
        }
    }
}