using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IcemanController : MonoBehaviour, IBossController
{
    private Rigidbody2D rb;
    private Animator animator;

    public Transform player;
    public GameObject iceArrowPrefab;
    public GameObject icePillarPrefab;  // Inspector 赋值
    public float pillarSpawnHeight = 10f; // 生成冰柱时的Y坐标（天空高度）
    public float pillarDropDuration = 1.5f; // 冰柱降落动画时长（如果需要控制）
    public Transform firePoint;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;     // 叠加 sprite renderer (DamageEffect2)
    public float flashInterval = 0.1f;      // 每帧闪烁间隔时间
    private PlayerDamageEffect damageEffect;

    private BossHealthSystem healthSystem;
    public int health = 28;
    public float moveSpeed = 3f;
    public float attackInterval = 0.4f;
    public float attackGroupCooldown = 2f;
    public float moveDistance = 3f;

    public int damage = 2;
    private float lastDamageTime = 0f;
    private float damageCooldown = 1.5f;
    private float invincibilityTime = 1.5f;
    private float invincibilityTimer = 0f;
    private bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    public bool isInDialogue = true;


    private AudioSource audioSource;
    public AudioClip iceArrowSound;
    public AudioClip damageSound;
    public AudioClip[] jumpVoices;
    public AudioClip[] attackVoices;
    public AudioClip[] hurtVoices;
    public AudioClip[] deadVoices;
    public AudioClip[] DeadVoices => deadVoices;

    [Range(0, 1)] public float voiceProbability = 0.7f;
    [Range(0, 1)] public float hurtVoiceProbability = 0.2f;

    void Start()
    {
        healthSystem = GetComponent<BossHealthSystem>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;  // 初始隐藏
        damageEffect = GetComponent<PlayerDamageEffect>();

        // 启动协程延迟获取玩家
        StartCoroutine(FindPlayer());
    }

    public void CheckAndStopCoroutines()
    {
        if (isDead)
        {
            StopAllCoroutines();
            Debug.Log($"{gameObject.name} 的所有协程已停止（因为 isDead = true）");
        }
    }

    private IEnumerator FindPlayer()
    {
        while (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Debug.Log("找到玩家: " + player.name);
                break;
            }
            yield return null; // 每帧检查一次
        }
    }


    void Update()
    {
        if (!isDead && !isInDialogue)
        {
            FacePlayer();
        }
        if (isDead)
        {
            StopAllCoroutines();
        }
    }

    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        yield break;
    }

    private IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(0.5f);
        if (isInDialogue || isDead) yield break;
        while (!isDead)
        {
            yield return StartCoroutine(AttackGroup());
            yield return new WaitForSeconds(attackGroupCooldown);
            yield return StartCoroutine(MoveOnce());
        }
    }

    private IEnumerator AttackGroup()
    {
        if (isInDialogue || isDead) yield break;

        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        animator.SetBool("IsWalking", false);
        animator.SetBool("IsJumping", false);

        Vector3 basePos = transform.position;

        float lowY = basePos.y;
        float midY = basePos.y + 1.5f;
        float highY = basePos.y + 3f;

        float[] pattern = new float[]
        {
        lowY, midY, highY,
        midY, lowY, midY,
        highY, midY, lowY
        };

        // =========================
        // ✅ 第1发（特殊处理）
        // =========================
        animator.Play("Attack2", 0, 0f); // 强制播放 Attack2
        PlayRandomVoice(attackVoices);

        yield return new WaitForSeconds(0.5f);

        // =========================
        // ✅ 后8发
        // =========================
        for (int i = 1; i < pattern.Length; i++)
        {
            float targetY = pattern[i];

            // 👉 先移动（一定发生8次）
            yield return StartCoroutine(MoveToY(targetY));

            yield return null; // ⭐ 给Animator一帧缓冲

            // 👉 强制播放 Attack（不用Trigger，避免吞）
            animator.Play("Attack", 0, 0f);

            yield return new WaitForSeconds(0.5f);
        }

        // =========================
        // 收尾
        // =========================
        yield return StartCoroutine(MoveToY(lowY));

        rb.bodyType = RigidbodyType2D.Dynamic;

        animator.Play("Idle");
        yield return new WaitForSeconds(1f);

        animator.Play("Pose");
        yield return new WaitForSeconds(1.5f);

        PlayRandomVoice(attackVoices);
        SpawnIcePillarsBetweenSelfAndPlayer();

        yield return new WaitForSeconds(pillarDropDuration + 0.5f);

        animator.Play("Idle");
    }

    private IEnumerator MoveToY(float targetY)
    {
        float speed = 8f; // 移动速度（可调）

        while (Mathf.Abs(transform.position.y - targetY) > 0.05f)
        {
            float newY = Mathf.MoveTowards(transform.position.y, targetY, speed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }

        // 最终对齐
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    private void FireIceArrow()
    {
        if (!isDead)
        {
            if (iceArrowPrefab != null && firePoint != null)
            {
                animator.SetBool("IsAttacking", true);

                GameObject arrow = Instantiate(iceArrowPrefab, firePoint.position, Quaternion.identity);

                // ✅ 根据 Boss 面朝方向设置发射方向
                Vector2 direction = transform.localScale.x > 0 ? Vector2.left : Vector2.right;

                IceArrow iceArrowScript = arrow.GetComponent<IceArrow>();
                if (iceArrowScript != null)
                {
                    iceArrowScript.SetDirection(direction);
                }

                if (iceArrowSound) audioSource.PlayOneShot(iceArrowSound);
            }
        }
    }



    private IEnumerator MoveOnce()
    {
        if (isInDialogue || isDead) yield break;

        animator.SetBool("IsWalking", true);
        animator.SetBool("IsJumping", false);
        animator.SetBool("IsAttacking", false);

        Vector2 direction = (player.position.x > transform.position.x) ? Vector2.right : Vector2.left;
        float moveDuration = moveDistance / moveSpeed;  // 移动距离除以速度 = 时间
        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            rb.velocity = new Vector2(direction.x * moveSpeed, rb.velocity.y);
            yield return null;
        }

        rb.velocity = new Vector2(0, rb.velocity.y);
        animator.SetBool("IsWalking", false);
    }


    public void SpawnIcePillarsBetweenSelfAndPlayer()
    {
        if (icePillarPrefab == null) return;

        //  固定生成区间
        float minX = 300.8f;
        float maxX = 314.2f;

        float selfX = transform.position.x;

        HashSet<float> chosenPositions = new HashSet<float>();
        int pillarsCount = 3;
        int attempts = 0;
        int maxAttempts = 30; // 稍微提高一点，避免筛选失败

        while (chosenPositions.Count < pillarsCount && attempts < maxAttempts)
        {
            attempts++;

            float randomX = Random.Range(minX, maxX);

            //  1. 避开自身 ±1范围
            if (Mathf.Abs(randomX - selfX) < 1f)
            {
                continue;
            }

            //  2. 避免柱子之间太近
            bool tooClose = false;
            foreach (var pos in chosenPositions)
            {
                if (Mathf.Abs(pos - randomX) < 1f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                chosenPositions.Add(randomX);
            }
        }

        foreach (var x in chosenPositions)
        {
            Vector3 spawnPos = new Vector3(x, transform.position.y + pillarSpawnHeight, 0);
            GameObject pillar = Instantiate(icePillarPrefab, spawnPos, Quaternion.identity);
            StartCoroutine(DropPillar(pillar));
        }
    }

    // 简单的冰柱下落协程（Y轴线性下降）
    private IEnumerator DropPillar(GameObject pillar)
    {
        Vector3 startPos = pillar.transform.position;
        Vector3 endPos = new Vector3(startPos.x, transform.position.y, startPos.z);

        float elapsed = 0f;
        while (elapsed < pillarDropDuration)
        {
            // 🔒 防御性检查：pillar 被销毁时直接退出协程
            if (pillar == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / pillarDropDuration;

            pillar.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        pillar.transform.position = endPos;
        IcePillar pillarScript = pillar.GetComponent<IcePillar>();
        if (pillarScript != null)
        {
            pillarScript.SpawnIceSpikes();
        }
    }

    void FacePlayer()
    {
        if (player != null)
        {
            bool shouldFaceRight = player.position.x > transform.position.x;
            bool isFacingRight = transform.localScale.x < 0;

            if (shouldFaceRight != isFacingRight)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
    }

    public void PlayRandomVoice(AudioClip[] voices)
    {
        if (voices == null || voices.Length == 0 || Random.value > voiceProbability)
            return;

        AudioClip clip = voices[Random.Range(0, voices.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void SetDialogueState(bool state)
    {
        isInDialogue = state;

        if (!isInDialogue)
        {
            // 仅当对话结束时启动行为循环
            StartCoroutine(BehaviorLoop());
        }
    }


    void TakeDamage(Vector2 enemyPosition)
    {
        if (isHurt) return;

        isHurt = true;
        invincibilityTimer = invincibilityTime;
        if (healthSystem != null && healthSystem.CurrentHealth > 0)
        {
            StartCoroutine(InvincibilityFlash(invincibilityTime));
        }
        if (damageEffect != null)
        {
            damageEffect.PlayDamageEffects();
        }
        // 播放受伤音效和语音
        if (damageSound != null)
            audioSource.PlayOneShot(damageSound);
        if (hurtVoices.Length > 0 && Random.value <= hurtVoiceProbability)
        {
            PlayRandomVoice(hurtVoices);
        }


        StartCoroutine(ResetInvincibility());
    }

    IEnumerator ResetInvincibility()
    {
        yield return new WaitForSeconds(invincibilityTime);
        isHurt = false;
    }

    IEnumerator InvincibilityFlash(float duration)
    {
        float timer = 0f;
        int state = 0;

        while (timer < duration)
        {
            switch (state % 3)
            {
                case 0: // 正常显示
                    spriteRenderer.enabled = true;
                    overlayRenderer.enabled = false;
                    break;
                case 1: // 显示受伤叠加
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = true;
                    break;
                case 2: // 隐藏角色
                    spriteRenderer.enabled = false;
                    overlayRenderer.enabled = false;
                    break;
            }

            state++;
            timer += flashInterval;
            yield return new WaitForSeconds(flashInterval);
        }

        // 恢复正常
        spriteRenderer.enabled = true;
        overlayRenderer.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return; // 确保碰撞体存在

        // 处理玩家碰撞
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                TakeDamage(bullet.transform.position);
                if (bullet != null)
                {
                    bullet.DestroyBullet();
                }
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("SuperArm"))
        {
            // 尝试获取四种可能的脚本
            SuperArm1 arm1 = collision.GetComponent<SuperArm1>();
            SuperArm2 arm2 = collision.GetComponent<SuperArm2>();
            SuperArmFoot foot = collision.GetComponent<SuperArmFoot>();
            GStone gstone = collision.GetComponent<GStone>();

            // 获取BossHealthSystem
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            Vector3 hitPos = collision.transform.position;

            if (arm1 != null || foot != null)
            {
                // SuperArm1 和 SuperArmFoot 处理同样伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(2);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，SuperArm1/SuperArmFoot 攻击无效");
                }
            }
            else if (arm2 != null)
            {
                // SuperArm2 忽略无敌，造成伤害，适合连击
                bossHealth.TakeDamage(2, ignoreInvincibility: true);
                TakeDamage(hitPos);
            }
            else if (gstone != null)
            {
                // GStone 造成较低伤害，受无敌影响
                if (!isHurt)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(hitPos);
                }
                else
                {
                    Debug.Log("Boss 处于无敌状态，GStone 攻击无效");
                }
            }
        }

        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(1);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(1);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(3);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(4);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(3); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
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
        // 处理子弹碰撞
        if (collision.CompareTag("Bullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            IceArrow ice = collision.GetComponent<IceArrow>();
            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                if (bullet != null)
                {
                    TakeDamage(bullet.transform.position);
                    bullet.DestroyBullet();
                }
                if (ice != null)
                {
                    TakeDamage(ice.transform.position);
                    ice.DestroyBullet();
                }
            }
            else
            {

            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("ChargeBullet"))
        {
            // 先获取子弹组件
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(2);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
            }
            else
            {

            }
        }
        // 处理子弹碰撞
        if (collision.CompareTag("RollingCutter"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            if (isHurt)
            {
                return;
            }

            // 尝试获取 RollingCutter 组件
            RollingCutter cutter = collision.GetComponent<RollingCutter>();
            if (cutter != null)
            {
                bossHealth.TakeDamage(1); // cutter 的伤害
                TakeDamage(cutter.transform.position);
                return;
            }

            // 尝试获取 CShockWave 组件
            CShockWave shockWave = collision.GetComponent<CShockWave>();
            if (shockWave != null)
            {
                bossHealth.TakeDamage(2); // shockWave 的伤害
                TakeDamage(shockWave.transform.position);
                return;
            }

            Debug.LogWarning("RollingCutter 标签命中但没有匹配的脚本组件");
        }
        if (collision.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = collision.GetComponent<RIceArrow>();
            if (iceArrow == null) return;

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害
                bossHealth.TakeDamage(1);
                TakeDamage(iceArrow.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("FireStorm"))
        {
            // 获取 Boss 生命系统
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 FireStorm
            FireStorm fire = collision.GetComponent<FireStorm>();

            // 护盾型 FireStorm
            FireStormShield shield = collision.GetComponent<FireStormShield>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，FireStorm 穿过");
                return;
            }

            // 飞行型 FireStorm：造成伤害并销毁
            if (fire != null && shield == null)
            {
                bossHealth.TakeDamage(1);
                TakeDamage(fire.transform.position);
                fire.DestroyBullet();
                return;
            }

            // 护盾型 FireStorm：有攻击冷却，不销毁
            if (shield != null)
            {
                // 检查冷却
                if (Time.time >= shield.lastDamageTime + shield.damageCooldown)
                {
                    bossHealth.TakeDamage(1);
                    TakeDamage(shield.transform.position);
                    shield.lastDamageTime = Time.time;
                }
                return;
            }
        }
        if (collision.CompareTag("HyperBomb"))
        {
            // 先获取子弹组件
            HyperBombExplosion bomb = collision.GetComponent<HyperBombExplosion>();
            if (bomb == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(3);
                TakeDamage(bomb.transform.position);
            }
            else
            {
                // 如果 Boss 处于无敌状态，子弹穿过 Boss
                Debug.Log("Boss 处于无敌状态，子弹穿过");
                // 可以在这里添加子弹穿过效果
            }
        }
        if (collision.CompareTag("ThunderBeam"))
        {
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>();
            if (bossHealth == null) return;

            // 飞行型 ThunderBeam
            ThunderBeam thunder = collision.GetComponent<ThunderBeam>();

            // 小Beam
            SmallThunderBeam smallThunder = collision.GetComponent<SmallThunderBeam>();

            // Boss 无敌判定
            if (isHurt)
            {
                Debug.Log("Boss 处于无敌状态，ThunderBeam 穿过");
                return;
            }

            // 飞行型 ThunderBeam：造成伤害并销毁
            if (thunder != null && smallThunder == null)
            {
                bossHealth.TakeDamage(4);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(3); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
        }
    }
}
