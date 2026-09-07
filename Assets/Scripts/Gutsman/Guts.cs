using UnityEngine;
using System.Collections;

public class GutsmanMelee : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    private int attackStep = 0;
    private bool canQueueNext = false;
    private bool queuedNext = false;
    private float comboResetTime = 0.6f;
    private float lastAttackTime = 0f;

    public bool queuedMoveInput = false;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource punchSource;
    [SerializeField] private AudioClip[] punchVoices;
    [SerializeField] private AudioClip punch1SFX;
    [SerializeField] private AudioClip punch2SFX;
    [Range(0f, 1f)][SerializeField] private float voicePlayChance = 0.5f;
    public AudioClip GAttackSound;
    [Header("Movement")]
    [SerializeField] private float punchMoveDistance = 0.4f;
    [SerializeField] private float punchMoveTime = 0.1f;
    private bool pendingMoveAfterPunch = false;
    private float pendingMoveInput = 0f; // -1,0,1

    [Header("Air Smash")]
    [SerializeField] private string airSmashAnim = "AirSmash";
    [SerializeField] private float airSmashDownForce = 10f;
    public bool isAirSmashing = false;
    public Transform GFirePoint;     
    public GameObject GStonePrefab;  
    private bool stoneAttackOnCooldown = false;
    private float stoneAttackCooldown = 4f;

    [Header("Air Smash Effects")]
    [SerializeField] private AudioClip airSmashSFX;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private GameObject dustPrefab; // ✅ 踩踏落地尘土
    [SerializeField] private float dustOffsetX = 3f; // ✅ 左右随机偏移
    [Header("Hitboxes")]
    [SerializeField] private GameObject hitboxPrefab1; // 第一拳用
    [SerializeField] private GameObject hitboxPrefab2; // 第二拳用
    [SerializeField] private GameObject footHitboxPrefab; // 踩踏用
    [SerializeField] private Transform firePoint;  // 出拳点
    [SerializeField] private Transform footPoint;  // 脚部点

    private GameObject currentHitbox;
    private GameObject stompHitbox; // 脚部攻击判定的实例

    private PlayerMovement playerMovement;
    public bool dialogueActive = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement == null)
            Debug.LogError("GutsmanMelee: PlayerMovement component not found!");
    }
    void Start()
    {
        // 延迟寻找Camera和CameraShake组件
        StartCoroutine(DelayedFindCamera());
    }

    private IEnumerator DelayedFindCamera()
    {
        // 等待一帧，确保所有组件都已初始化
        yield return null;

        Camera cam = null;

        // ① 优先在子物体中查找名为 "Camera" 的相机
        Camera[] childCameras = GetComponentsInChildren<Camera>(true);
        foreach (var c in childCameras)
        {
            if (c.name == "Camera")
            {
                cam = c;
                break;
            }
        }

        // ② 如果子物体中没有找到，就在场景中全局查找名为 "Camera" 的对象
        if (cam == null)
        {
            GameObject camObj = GameObject.Find("Camera");
            if (camObj != null)
            {
                cam = camObj.GetComponent<Camera>();
                Debug.Log("GutsmanMelee: Found scene camera named 'Camera'.");
            }
            else
            {
                Debug.LogWarning("GutsmanMelee: No camera named 'Camera' found in scene!");
            }
        }

        // ③ 如果找到相机，尝试获取 CameraShake 组件
        if (cam != null)
        {
            cameraShake = cam.GetComponent<CameraShake>();
            if (cameraShake == null)
            {
                Debug.LogWarning("GutsmanMelee: CameraShake component not found on 'Camera'!");
            }
        }
    }


    void Update()
    {
        if (dialogueActive) return;
        if (!playerMovement.canMove) return;
        float moveInput = Input.GetAxisRaw("Horizontal");

        // 空中踩踏（增加保护）
        if (!playerMovement.isGrounded && Input.GetKeyDown(KeyCode.J))
        {
            if (!isAirSmashing)  // ✅ 防止重复触发
            {
                StartCoroutine(AirSmash());
            }
            return;
        }

        // 地面拳击逻辑
        if (playerMovement.isGrounded && Input.GetKeyDown(KeyCode.J))
        {
            if (IsPunching())
            {
                queuedMoveInput = Mathf.Abs(moveInput) > 0.1f;
                TryMeleeAttack();
            }
            else
            {
                playerMovement.currentSpeedX = 0f;
                TryMeleeAttack();
            }
        }
    }



    public bool IsPunching()
    {
        return attackStep > 0;
    }

    public void TryMeleeAttack()
    {
        if (playerMovement.isGrounded == false || playerMovement.isClimbing == true)
        {
            return;
        }
        if (Time.time - lastAttackTime > comboResetTime)
            ResetCombo();

        if (attackStep == 0)
        {
            isAirSmashing = false;
            attackStep = 1;
            animator.SetBool("IsPunching", true);
            animator.SetInteger("AttackStep", attackStep);
            lastAttackTime = Time.time;
        }
        else if ((attackStep == 1 || attackStep == 2) && canQueueNext)
        {
            queuedNext = true;
        }
    }

    public void EnableNextCombo() { canQueueNext = true; }

    public void CheckComboContinue()
    {
        if (queuedNext)
        {
            attackStep = attackStep == 1 ? 2 : 1;
            animator.SetInteger("AttackStep", attackStep);
            queuedNext = false;
            canQueueNext = false;
            lastAttackTime = Time.time;
        }
        else
        {
            ResetCombo();
        }
    }

    // 🔹 新增动画事件方法：完全收拳动画结束时调用
    public void OnPunchAnimationEnd()
    {
        ResetCombo();

        // 动画完全结束后才应用移动
        if (pendingMoveAfterPunch && playerMovement != null)
        {
            playerMovement.currentSpeedX = pendingMoveInput * playerMovement.moveSpeed;
            pendingMoveAfterPunch = false;
            pendingMoveInput = 0f;
        }
    }


    public void ResetCombo()
    {
        attackStep = 0;
        queuedNext = false;
        canQueueNext = false;
        animator.SetBool("IsPunching", false);
        animator.SetInteger("AttackStep", 0);
    }

    public void PlayPunchVoice()
    {
        if (punchVoices.Length > 0 && voiceSource != null && Random.value <= voicePlayChance)
        {
            voiceSource.PlayOneShot(punchVoices[Random.Range(0, punchVoices.Length)]);
        }
    }

    public void PlayPunch1SFX()
    {
        if (punchSource && punch1SFX != null) punchSource.PlayOneShot(punch1SFX);
    }

    public void PlayPunch2SFX()
    {
        if (punchSource && punch2SFX != null) punchSource.PlayOneShot(punch2SFX);
    }

    // 🔹 动画事件：出拳瞬间生成Hitbox
    public void ActivateHitbox()
    {
        if (firePoint == null) return;

        GameObject prefabToUse = null;

        // 根据当前拳数选择Hitbox
        if (attackStep == 1 && hitboxPrefab1 != null)
        {
            prefabToUse = hitboxPrefab1;
        }
        else if (attackStep == 2 && hitboxPrefab2 != null)
        {
            prefabToUse = hitboxPrefab2;
        }

        if (prefabToUse != null)
        {
            // 实例化 Hitbox，不作为子物体
            currentHitbox = Instantiate(prefabToUse, firePoint.position, firePoint.rotation);
        }
    }



    // 🔹 动画事件：出拳结束销毁Hitbox
    public void DeactivateHitbox()
    {
        if (currentHitbox != null)
        {
            Destroy(currentHitbox);
            currentHitbox = null;
        }
    }

    public void PunchForwardStep()
    {
        if (rb == null || spriteRenderer == null) return;

        float dir = spriteRenderer.flipX ? 1f : -1f;
        Vector2 targetPos = rb.position + new Vector2(punchMoveDistance * dir, 0);

        StartCoroutine(SmoothMove(rb.position, targetPos, punchMoveTime));
    }

    public void CancelPunch()
    {
        // 重置连击状态
        attackStep = 0;
        queuedNext = false;
        canQueueNext = false;
        animator.SetBool("IsPunching", false);
        animator.SetInteger("AttackStep", 0);

        // 清除待移动标记
        pendingMoveAfterPunch = false;
        pendingMoveInput = 0f;
        queuedMoveInput = false;
    }

    private IEnumerator SmoothMove(Vector2 start, Vector2 end, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            rb.MovePosition(Vector2.Lerp(start, end, timer / duration));
            yield return null;
        }
        rb.MovePosition(end);
    }


    private IEnumerator AirSmash()
    {
        if (isAirSmashing) yield break; // ✅ 二次保险
        isAirSmashing = true;           // ✅ 锁定状态

        if (dialogueActive) { isAirSmashing = false; yield break; }
        if (playerMovement.isClimbing) { isAirSmashing = false; yield break; }

        animator.Play(airSmashAnim);
        rb.velocity = new Vector2(rb.velocity.x, -airSmashDownForce);

        // ✅ 生成脚部Hitbox
        if (footHitboxPrefab != null && footPoint != null && stompHitbox == null)
        {
            stompHitbox = Instantiate(footHitboxPrefab, footPoint.position, footPoint.rotation);
        }

        // 等待落地
        while (!playerMovement.isGrounded)
        {
            if (stompHitbox != null)
                stompHitbox.transform.position = footPoint.position;
            yield return null;
        }

        // 落地震动 + 特效逻辑（保持不变）
        if (cameraShake != null)
            StartCoroutine(cameraShake.Shake(0.1f, 0.14f));

        if (sfxSource != null && airSmashSFX != null)
            sfxSource.PlayOneShot(airSmashSFX);

        if (dustPrefab != null && footPoint != null)
        {
            int dustCount = Random.Range(4, 7);
            for (int i = 0; i < dustCount; i++)
            {
                float offsetX = Random.Range(-dustOffsetX, dustOffsetX);
                float offsetY = Random.Range(-0.1f, 0.1f);
                Vector3 spawnPos = footPoint.position + new Vector3(offsetX, offsetY, 0f);
                GameObject dust = Instantiate(dustPrefab, spawnPos, Quaternion.identity);
                if (Random.value > 0.5f)
                {
                    var sr = dust.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.flipX = !sr.flipX;
                }
            }
        }
        // ✅ 落地产生石头攻击（带冷却）
        if (!stoneAttackOnCooldown && GFirePoint != null && GStonePrefab != null)
        {
            stoneAttackOnCooldown = true; // 开启冷却
            StartCoroutine(StoneAttackCooldownTimer()); // 启动计时器

            float direction = spriteRenderer.flipX ? 1f : -1f;
            float[] offsets = new float[] { -6f, -2f, 2f, 6f };

            foreach (float offset in offsets)
            {
                Vector3 spawnPos = GFirePoint.position + new Vector3(offset * direction, 0f, 0f);
                GameObject stone = Instantiate(GStonePrefab, spawnPos, Quaternion.identity);

                if (stone.TryGetComponent<Rigidbody2D>(out var rb2D))
                    rb2D.velocity = Vector2.zero;
            }

            if (GAttackSound != null && punchSource != null)
                punchSource.PlayOneShot(GAttackSound);
        }


        // 落地后延迟销毁Hitbox
        float remainingTime = 0.2f;
        while (remainingTime > 0f)
        {
            if (stompHitbox != null)
                stompHitbox.transform.position = footPoint.position;
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        if (stompHitbox != null)
        {
            Destroy(stompHitbox);
            stompHitbox = null;
        }

        isAirSmashing = false;  // ✅ 释放锁
    }


    private IEnumerator StunRigidbody(Rigidbody2D rb, float duration)
    {
        if (rb == null) yield break;

        Vector2 oldVel = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        yield return new WaitForSeconds(duration);

        rb.isKinematic = false;
        rb.velocity = oldVel;
    }

    private IEnumerator StoneAttackCooldownTimer()
    {
        yield return new WaitForSeconds(stoneAttackCooldown);
        stoneAttackOnCooldown = false;
    }

}
