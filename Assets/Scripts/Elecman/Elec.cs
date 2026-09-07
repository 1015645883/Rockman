using System.Collections;
using UnityEngine;

public class ElecmanSpecial : MonoBehaviour
{
    [Header("引用")]
    private Rigidbody2D rb;
    public GameObject electricEffectPrefab; // 电力特效预制体
    private GameObject currentElectricEffect;
    public Transform effectSpawnPoint;      // 特效生成位置（比如角色中心）
    public EEnergyManager eEnergyBar;       // E能量管理器

    [Header("强化参数")]
    public float buffMultiplier = 1.3f;     // 强化倍率
    public float effectInterval = 0.8f;     // 电力特效间隔
    public float energyInterval = 1f;       // 消耗能量间隔
    public int energyCost = 2;              // 每秒消耗能量
    private float originalGravityScale= 2.7f;      // 原始重力
    [Header("残影参数")]
    public GameObject afterImagePrefab;   // 残影预制体，带 SpriteRenderer + 渐隐脚本
    public float afterImageInterval = 0.05f; // 残影生成间隔
    private float afterImageTimer = 0f;

    [Header("充电参数")]
    public float chargeDuration = 0.5f;     // 充电动画时长
    private float lastSKeyTime = 0f;        // 上次按 S 的时间
    private float doubleClickThreshold = 0.5f; // 双击判定间隔
    private bool isCharging = false;

    [Header("角色属性")]
    public PlayerMovement playerMovement;   // 角色的移动脚本引用
    [Header("音效")]
    public AudioClip chargeSfx;      // 充能音效
    private AudioSource audioSource; // 播放器
    private float originalMoveSpeed;
    private float originalJumpForce;
    private float originalClimbSpeed;
    private Coroutine buffRoutine;

    private bool lastCanMoveState = true; // 上一帧的 canMove 状态

    private void Start()
    {
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody2D>();
        originalMoveSpeed = playerMovement.moveSpeed;
        originalJumpForce = playerMovement.jumpForce;
        originalClimbSpeed = playerMovement.climbSpeed;

        // 🔹 自动寻找自己身上的 E 能量管理器
        if (eEnergyBar == null)
        {
            eEnergyBar = GetComponent<EEnergyManager>();
            if (eEnergyBar == null)
            {
                Debug.LogWarning($"{gameObject.name} 身上没有挂 EEnergyManager 组件");
            }
        }


        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        // 🔹 检测 canMove 状态变化
        if (lastCanMoveState && !playerMovement.canMove)
        {
            // 从 true -> false，立即打断 Buff
            StopBuff();
        }
        lastCanMoveState = playerMovement.canMove;

        // 🔹 只有能移动时才允许按键输入
        if (playerMovement.canMove)
        {
            HandleBuffInput();
            HandleChargeInput();
        }
    }

    // ---------------- 处理强化输入 ----------------
    private void HandleBuffInput()
    {
        // 如果角色死亡则禁止 Buff
        if (playerMovement != null && playerMovement.isDead)
        {
            StopBuff(); // 确保死亡时强制停止
            return;
        }

        if (Input.GetKey(KeyCode.O))
        {
            if (buffRoutine == null)
            {
                buffRoutine = StartCoroutine(BuffRoutine());
            }
        }
        else
        {
            StopBuff(); // 松开 O 键时直接停止协程并销毁当前特效
        }
    }

    // ---------------- 处理充能输入 ----------------
    private void HandleChargeInput()
    {
        if (isCharging) return; // 正在充能中忽略输入

        // 玩家在地面、Idle 状态
        bool isIdle = playerMovement.isGrounded &&
                      !playerMovement.isWalking &&
                      !playerMovement.isDashing &&
                      !playerMovement.isJumping &&
                      !playerMovement.isShooting &&
                      !playerMovement.isShocking &&
                      !playerMovement.isDead &&
                      !playerMovement.isClimbing;

        if (!isIdle) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            float timeSinceLast = Time.time - lastSKeyTime;
            if (timeSinceLast <= doubleClickThreshold)
            {
                // 双击成功 → 开始充能
                StartCoroutine(ChargeEnergyRoutine());
            }
            lastSKeyTime = Time.time;
        }
    }

    // ---------------- 平滑充能协程 ----------------
    private IEnumerator ChargeEnergyRoutine()
    {
        isCharging = true;

        // 禁止移动
        if (playerMovement != null)
            playerMovement.canMove = false;

        // 播放 Charge 动画
        if (playerMovement.animator != null)
            playerMovement.animator.SetTrigger("Charge");

        // 播放充能音效
        if (chargeSfx != null && audioSource != null)
            audioSource.PlayOneShot(chargeSfx);

        float elapsed = 0f;
        int startEnergy = eEnergyBar != null ? eEnergyBar.CurrentEnergy : 0;
        int targetEnergy = 28;

        while (elapsed < chargeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / chargeDuration;
            int newEnergy = Mathf.RoundToInt(Mathf.Lerp(startEnergy, targetEnergy, t));
            if (eEnergyBar != null)
                eEnergyBar.SetEnergyDirectly(newEnergy);

            yield return null;
        }

        // 确保最终填满
        if (eEnergyBar != null)
            eEnergyBar.SetEnergyDirectly(targetEnergy);

        // 切换回 Idle
        if (playerMovement.animator != null)
            playerMovement.animator.Play("Idle");

        // 恢复移动
        if (playerMovement != null)
            playerMovement.canMove = true;

        isCharging = false;
    }


    private IEnumerator BuffRoutine()
    {
        playerMovement.moveSpeed = originalMoveSpeed * buffMultiplier;
        playerMovement.jumpForce = originalJumpForce * buffMultiplier;
        playerMovement.climbSpeed = originalClimbSpeed * buffMultiplier;

        float effectTimer = effectInterval;
        float energyTimer = 0f;

        while (true)
        {
            if (eEnergyBar == null || eEnergyBar.CurrentEnergy < energyCost)
            {
                ResetStats();
                buffRoutine = null;
                yield break;
            }

            // 生成电力特效
            effectTimer += Time.deltaTime;
            if (effectTimer >= effectInterval)
            {
                effectTimer = 0f;
                if (electricEffectPrefab != null && effectSpawnPoint != null)
                {
                    if (currentElectricEffect != null)
                    {
                        var effectScript = currentElectricEffect.GetComponent<EffectAutoDestroy>();
                        if (effectScript != null)
                            effectScript.DestroySelf();
                    }

                    currentElectricEffect = Instantiate(electricEffectPrefab, effectSpawnPoint.position, Quaternion.identity);
                    currentElectricEffect.transform.SetParent(transform);
                    currentElectricEffect.transform.localScale = Vector3.one * 0.5f;
                }
            }

            // ✅ 生成残影
            afterImageTimer += Time.deltaTime;
            if (afterImagePrefab != null && afterImageTimer >= afterImageInterval)
            {
                afterImageTimer = 0f;
                GameObject ghost = Instantiate(afterImagePrefab, transform.position, transform.rotation);
                SpriteRenderer sr = ghost.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = playerMovement.mainRenderer.sprite; // 复制当前精灵
                    sr.flipX = playerMovement.mainRenderer.flipX;
                }
            }
            HandleFallGravity();
            // 消耗能量
            energyTimer += Time.deltaTime;
            if (energyTimer >= energyInterval)
            {
                energyTimer = 0f;
                eEnergyBar.TryUseEnergy(energyCost);
            }

            yield return null;
        }

    }

    // 松开 O 键时调用
    private void StopBuff()
    {
        if (buffRoutine != null)
        {
            StopCoroutine(buffRoutine);
            buffRoutine = null;
            ResetStats();

            // 立即销毁当前特效
            if (currentElectricEffect != null)
            {
                var effectScript = currentElectricEffect.GetComponent<EffectAutoDestroy>();
                if (effectScript != null)
                    effectScript.DestroySelf();

                currentElectricEffect = null;
            }
        }
    }

    private void HandleFallGravity()
    {
        if (playerMovement.isClimbing == true)
            return;
        if (rb == null) return;

        if (rb.velocity.y < 0f) // 下落时生效
            rb.gravityScale = originalGravityScale * 0.1f;
        else
            rb.gravityScale = originalGravityScale;
    }

    private void ResetStats()
    {
        // 恢复速度属性
        playerMovement.moveSpeed = originalMoveSpeed;
        playerMovement.jumpForce = originalJumpForce;

        // 仅在非攀爬时恢复重力
        if (!playerMovement.isClimbing && rb != null)
        {
            rb.gravityScale = originalGravityScale;
        }
    }

}
