using UnityEngine;
using System.Collections;

public class GutsmanController : MonoBehaviour, IBossController
{
    private Rigidbody2D rb;
    private Animator animator;
    public Transform player;
    private SpriteRenderer spriteRenderer;
    public SpriteRenderer overlayRenderer;     // 叠加 sprite renderer (DamageEffect2)
    public float flashInterval = 0.1f;      // 每帧闪烁间隔时间
    private PlayerDamageEffect damageEffect;

    private enum GutsmanState { Idle, Jumping, WaitingForDig, Digging }
    private GutsmanState currentState = GutsmanState.Idle;

    private int jumpCount = 0;
    private float waitTimer = 0f;

    private BossHealthSystem healthSystem;
    public int health = 28;
    public float jumpForce = 18f;
    public float forwardJumpSpeed = 5f;
    public float backwardJumpSpeed = 4f;
    public float jumpCooldown = 3f;
    public int damage = 4;
    private float damageCooldown = 1.4f;
    private float lastDamageTime;
    private float jumpTimer = 0f;
    private bool isJumping = false;
    private bool isStraightJump = false;
    public bool isHurt = false;
    public bool IsHurt => isHurt;
    public bool isDead = false;
    private float invincibilityTime = 1.5f;
    private float invincibilityTimer = 0f;
    public bool isInDialogue = true;
    private bool isDiggingInvincible = false;

    private Vector2 targetPosition;
    public LayerMask groundLayer;
    public Transform groundCheck;
    private Collider2D capsuleCollider;
    public GameObject stonePrefab;
    private Stone currentStone;  // 记录当前实例化的石头引用
    [SerializeField]private Transform stoneSpawnPoint;  // 石头生成点（空物体挂点）
    public float groundCheckRadius = 0.1f;

    [SerializeField] private GameObject dustPrefab;
    [SerializeField] private float digDownDuration = 1.0f;
    [SerializeField] private float jumpOutForce = 12f;
    [SerializeField] private float digDownSpeed = 10f;
    [SerializeField] private float digMoveSpeed = 5f;
    private bool isGrounded;
    private bool wasGrounded = true; // 新增字段记录上一帧的落地状态
    private AudioSource audioSource;
    public AudioClip attackSound;
    public AudioClip damageSound;
    public AudioClip earthquackSound;
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
        capsuleCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        overlayRenderer.enabled = false;  // 初始隐藏
        damageEffect = GetComponent<PlayerDamageEffect>();

        var collider = GetComponent<Collider2D>();
        if (collider != null && collider.sharedMaterial == null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("HighFriction");
            mat.friction = 1f;  // 物理摩擦力大
            mat.bounciness = 0f;
            collider.sharedMaterial = mat;
        }
    }

    public void CheckAndStopCoroutines()
    {
        if (isDead)
        {
            StopAllCoroutines();
            Debug.Log($"{gameObject.name} 的所有协程已停止（因为 isDead = true）");
        }
    }

    // 当对象被启用时调用，Boss激活时执行
    private void OnEnable()
    {
        StartCoroutine(FindPlayerCoroutine());
    }

    private IEnumerator FindPlayerCoroutine()
    {
        while (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("成功找到Player对象");
                yield break;
            }
            Debug.Log("未找到Player，等待下一帧重试");
            yield return null;
        }
    }

    public IEnumerator ChooseAction()
    {
        yield return new WaitForSeconds(0.5f);
        currentState = GutsmanState.Idle;
        yield break;
    }

    void Update()
    {
        if (isDead) return;
        if (isInDialogue) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        animator.SetBool("IsJumping", !isGrounded);
        if (isGrounded && !wasGrounded)
        {
            if (isJumping)
            {
                isJumping = false;
                jumpTimer = jumpCooldown;
                rb.velocity = Vector2.zero;
                animator.SetBool("IsJumping", false);

                if (CameraShake.Instance != null)
                    StartCoroutine(CameraShake.Instance.Shake(0.2f, 0.3f));
                if (earthquackSound != null && audioSource != null)
                    audioSource.PlayOneShot(earthquackSound);

                GameObject player = GameObject.FindWithTag("Player");
                player?.GetComponent<PlayerMovement>()?.Shock();

                StartCoroutine(SpawnStoneAfterDelay(1.5f));

                jumpCount++;

                if (jumpCount >= 3)
                {
                    currentState = GutsmanState.WaitingForDig;
                    waitTimer = 4f;
                }
                else
                {
                    // 跳跃次数未达到3，回到Idle等待下一跳
                    currentState = GutsmanState.Idle;
                }
            }
        }


        switch (currentState)
        {
            case GutsmanState.Idle:
                if (!isJumping && jumpTimer <= 0f && isGrounded)
                {
                    int choice = Random.Range(0, 3);
                    StartCoroutine(JumpRoutine(choice));
                    currentState = GutsmanState.Jumping;
                }
                else
                {
                    jumpTimer -= Time.deltaTime;
                }
                break;

            case GutsmanState.Jumping:
                // 等待落地后在上面处理
                break;

            case GutsmanState.WaitingForDig:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    StartCoroutine(DigRoutine());
                    currentState = GutsmanState.Digging;
                }
                break;

            case GutsmanState.Digging:
                // 由协程控制结束
                break;
        }

        FacePlayer();
        wasGrounded = isGrounded;
    }



    IEnumerator JumpRoutine(int choice)
    {
        if (isDead) yield break;
        isJumping = true;

        // 标记是否是直跳
        isStraightJump = (choice == 0);

        // 播放准备起跳动画（比如蹲下）
        animator.SetTrigger("PrepareJump");

        yield return new WaitForSeconds(0.25f);
        PlayRandomVoice(jumpVoices);

        animator.SetBool("IsJumping", true);

        float xSpeed = 0f;
        bool facingRight = transform.localScale.x > 0;

        switch (choice)
        {
            case 0: xSpeed = 0f; break;
            case 1: xSpeed = facingRight ? forwardJumpSpeed : -forwardJumpSpeed; break;
            case 2: xSpeed = facingRight ? -backwardJumpSpeed : backwardJumpSpeed; break;
        }

        rb.velocity = new Vector2(xSpeed, jumpForce);
    }



    private Vector2[] spawnPoints = new Vector2[]
{
    new Vector2(215, -54),
    new Vector2(217, -54),
    new Vector2(219, -54),
    new Vector2(221, -54),
    new Vector2(223, -54),
    new Vector2(225, -54),
    new Vector2(227, -54),
    new Vector2(229, -54),
    new Vector2(231, -54),
    new Vector2(233, -54),
};

    private IEnumerator SpawnStoneAfterDelay(float delay)
    {
        if (isDead) yield break;
        yield return new WaitForSeconds(delay);

        Vector2 spawnPosition;

        if (isStraightJump)
        {
            // 直跳时石头在 Gutsman X轴、Y = -54
            spawnPosition = new Vector2(transform.position.x, -54f);
            Debug.Log("直跳");
        }
        else
        {
            // 非直跳时从随机位置选一个
            int index = Random.Range(0, spawnPoints.Length);
            spawnPosition = spawnPoints[index];
        }
        isStraightJump = false;
        if (stonePrefab != null)
        {
            Instantiate(stonePrefab, spawnPosition, Quaternion.identity);
        }
    }


    public void PlayThrowAnimation()
    {
        if (animator != null)
        {
            animator.Play("Attack"); // 替换为你的投掷动画名称
        }
    }

    private IEnumerator DigRoutine()
    {
        if (isDead) yield break;
        rb.velocity = Vector2.zero;

        // ❌ 1. 禁用 Collider，防止物理影响
        if (capsuleCollider != null)
            capsuleCollider.enabled = false;
        RigidbodyConstraints2D originalConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        // 2. 播放下潜动画
        animator.SetBool("IsDigging", true);
        animator.SetTrigger("DigDown");
        // ✅ 在下潜动画期间生成 Dust 效果
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(SpawnDustWhileDigging(digDownDuration));
        yield return new WaitForSeconds(digDownDuration);
        isHurt = true;
        isDiggingInvincible = true;
        invincibilityTimer = 0f; // 避免和闪烁计时冲突
        // 3. 下潜至 Y = -68f
        float undergroundY = -68f;
        while (transform.position.y > undergroundY)
        {
            transform.position += Vector3.down * digDownSpeed * Time.deltaTime;
            yield return null;
        }

        // 4. 锁定 Y 值，防止继续往下掉
        animator.SetBool("IsDigging", false);
        transform.position = new Vector3(transform.position.x, undergroundY, transform.position.z);

        // 禁止当前石头投掷
        if (currentStone != null)
        {
            currentStone.DisableThrow();
        }

        // 5. 潜地水平移动并生成 Dust 效果
        Vector2 lastDustPos = Vector2.zero;
        float dustInterval = 1f;

        // 6. 播放跳出准备动画
        animator.SetTrigger("PrepareJump");
        animator.SetBool("IsJumping", true);
        // 新增：播放 HoldStoneJumping 动画，假设动画参数是 "HoldStoneJumping"
        animator.SetBool("HoldStoneJumping", true);

        while (Mathf.Abs(transform.position.x - player.position.x) > 0.1f)
        {
            float direction = player.position.x > transform.position.x ? 1f : -1f;
            float newX = transform.position.x + direction * digMoveSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, undergroundY, transform.position.z); // 锁定地下 Y 值

            // 在地底生成 Dust（略高于 Gutsman 的 Y 轴）
            if (Vector2.Distance(transform.position, lastDustPos) >= dustInterval)
            {
                Vector3 dustPos = new Vector3(transform.position.x, undergroundY + 3f, transform.position.z);
                Instantiate(dustPrefab, dustPos, Quaternion.identity);
                lastDustPos = transform.position;
            }

            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        // 7. 跳出前生成石头并禁止投掷
        SpawnStone();


        // ✅ 9. 上浮到地面
        float targetY = -63f;
        float digUpSpeed = 20f;
        while (transform.position.y < targetY)
        {
            transform.position += Vector3.up * digUpSpeed * Time.deltaTime;

            // 当到达 -65 位置时解除无敌
            if (isHurt && transform.position.y >= -65f)
            {
                isHurt = false;
                isDiggingInvincible = false;
            }

            yield return null;
        }

        // ✅ 10. 强制锁定位置
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

        // ✅ 11. 等一帧，确保坐标更新
        yield return null;

        // ✅ 12. 恢复 Rigidbody 物理
        rb.constraints = originalConstraints;

        // ✅ 13. 恢复 Collider
        if (capsuleCollider != null)
            capsuleCollider.enabled = true;

        //执行跳跃
        JumpOut();
        isGrounded = false;

        // 10. 等待落地
        yield return new WaitUntil(() => isGrounded);
        // 落地后关闭 HoldStoneJumping 动画
        animator.SetBool("HoldStoneJumping", false);
        animator.SetBool("IsJumping", false);
        // 11. 落地后允许石头投掷
        if (currentStone != null)
        {
            currentStone.EnableThrow();
        }

        // 12. 重置状态
        isJumping = false;
        jumpCount = 0;
        currentState = GutsmanState.Idle;
    }



    private void SpawnStone()
    {
        if (stonePrefab == null || stoneSpawnPoint == null) return;

        GameObject stoneObj = Instantiate(stonePrefab, stoneSpawnPoint.position, Quaternion.identity);
        currentStone = stoneObj.GetComponent<Stone>();

        if (currentStone != null)
        {
            // 初始不允许投掷
            currentStone.DisableThrow();

            // 让石头跟随挂点
            currentStone.AttachTo(stoneSpawnPoint);
        }
    }


    private void JumpOut()
    {
        if (rb == null || player == null) return;

        // 设置仅向上跳跃
        rb.velocity = new Vector2(0f, jumpOutForce);
        isGrounded = false;

        // 可播放跳出音效
        PlayRandomVoice(attackVoices);
    }

    private IEnumerator SpawnDustWhileDigging(float duration)
    {
        float timer = 0f;
        float spawnInterval = 0.1f; // 每0.1秒生成一次
        while (timer < duration)
        {
            float offsetX = Random.Range(-0.5f, 0.5f); // X轴随机偏移
            Vector3 spawnPos = new Vector3(transform.position.x + offsetX, -65f, transform.position.z);
            Instantiate(dustPrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(spawnInterval);
            timer += spawnInterval;
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
                bossHealth.TakeDamage(1); // shockWave 的伤害
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
                bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(2);
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
                    bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4);
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
                bossHealth.TakeDamage(2);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(1); // 小Beam伤害可单独设置
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
            if (bullet == null) return; // 如果没有Bullet组件则直接返回

            // 再获取BossHealthSystem组件
            BossHealthSystem bossHealth = GetComponent<BossHealthSystem>(); // 注意这里改为从自身获取
            if (bossHealth == null) return; // 如果没有BossHealthSystem则返回

            if (!isHurt)
            {
                // 如果 Boss 没有处于无敌状态，造成伤害并销毁子弹
                bossHealth.TakeDamage(1);
                TakeDamage(bullet.transform.position);
                bullet.DestroyBullet();
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
                bossHealth.TakeDamage(1); // shockWave 的伤害
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
                bossHealth.TakeDamage(3);
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
                bossHealth.TakeDamage(2);
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
                    bossHealth.TakeDamage(2);
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
                bossHealth.TakeDamage(4);
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
                bossHealth.TakeDamage(2);
                TakeDamage(thunder.transform.position);
                thunder.DestroyOnEnemyHit();
                return;
            }

            // 小Beam：有伤害冷却，不销毁
            if (smallThunder != null)
            {
                if (Time.time >= smallThunder.lastDamageTime + smallThunder.damageCooldown)
                {
                    bossHealth.TakeDamage(1); // 小Beam伤害可单独设置
                    TakeDamage(smallThunder.transform.position);
                    smallThunder.lastDamageTime = Time.time;
                }
                return;
            }
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
        if (!isDiggingInvincible)  // 只有非钻洞情况下才取消无敌
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

    public void SetDialogueState(bool state)
    {
        isInDialogue = state;
        if (state)
        {
            isInDialogue = true;

        }
        else
        {
            isInDialogue = false;
            // 对话结束后恢复行为
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
