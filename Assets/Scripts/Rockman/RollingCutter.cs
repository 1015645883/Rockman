using UnityEngine;

public class RollingCutter : MonoBehaviour
{
    [Header("通用参数")]
    public float returnSpeed = 8f;
    public float flightDuration = 0.5f;
    public float arcHeight = 1.5f;
    public float flightDistance = 6f;

    [Header("圆周模式参数")]
    public float circleRadius = 2f;
    public float circleDuration = 0.6f;
    private int circleDirection = 1; // +1 = 逆时针, -1 = 顺时针

    [Header("滚地模式参数")]
    public float groundSpeed = 6f;
    public float groundLifeTime = 5f;
    public LayerMask groundLayer;       // 用于检测墙壁
    public Transform groundCheck;       // 地面/墙壁检测点
    public Vector2 groundCheckSize = new Vector2(1f, 0.5f); // 矩形大小
    public float groundProtectTime = 0.5f; // 前0.2秒保护
    private float groundProtectTimer = 0f; // 计时
    public float bounceProtectTime = 0.2f; // 两次反弹间隔保护
    private float lastBounceTime = -10f; // 上一次反弹时间，初始化一个很小的负数
    private int groundBounces = 0;      // 反弹次数
    private int maxGroundBounces = 3;

    [Header("伤害参数")]
    public float damageCooldown = 0.7f;
    public float lastDamageTime;

    private Vector2 startPoint;
    private Vector2 endPoint;
    private Transform player;
    private float timer = 0f;
    private bool isReturning = false;

    // 圆周模式
    private bool isCircularMode = false;
    private Vector2 circleCenter;
    private float circleAngle = 0f;

    // 滚地模式
    public bool isGroundMode = false;
    private float groundDirection = 1f;
    private float groundTimer = 0f;
    public float groundCheckRadius = 0.7f;
    private PlayerShooting playerShooting;
    private AudioSource audioSource;
    public AudioClip cutterLoopSound;
    private bool isDying = false;
    public Animator animator;  // Animator 组件

    private bool hasCalledDestroyed = false; //  防止多次调用回调

    void Start()
    {
        animator = GetComponent<Animator>(); // 获取 Animator 组件
    }
    /// <summary>
    /// 初始化武器
    /// </summary>
    public void Initialize(Vector2 direction, Transform playerRef, bool circularMode = false, bool groundMode = false)
    {
        player = playerRef;
        isCircularMode = circularMode;
        isGroundMode = groundMode;

        if (isGroundMode)
        {
            // 🔹 滚地模式
            groundDirection = direction.x >= 0 ? 1f : -1f; // ✅ 根据传入方向设置
            transform.position += Vector3.right * groundDirection * groundSpeed * Time.deltaTime;
        }
        else if (!isCircularMode)
        {
            // 普通半圆飞行
            startPoint = transform.position;
            endPoint = startPoint + direction.normalized * flightDistance;
        }
        else
        {
            // 圆周模式
            circleCenter = new Vector2(player.position.x, player.position.y + circleRadius);
            circleAngle = 270f;

            Transform firePoint = playerRef.GetComponent<PlayerShooting>()?.firePoint;
            if (firePoint != null && firePoint.localPosition.x < 0)
                circleDirection = -1;
            else
                circleDirection = 1;
        }

        playerShooting = player.GetComponent<PlayerShooting>();

        // 播放音效
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = cutterLoopSound;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
        audioSource.loop = true;
        audioSource.Play();
    }

    void Update()
    {
        if (isGroundMode)
        {
            if (isDying) return; // 🔹 先阻止后续更新
            // 🔹 计时保护
            groundProtectTimer += Time.deltaTime;
            // 检测墙壁
            Collider2D hit = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
            if (hit != null && Time.time - lastBounceTime >= bounceProtectTime)
            {
                // ✅ 翻转方向总是生效
                groundDirection *= -1f;
                lastBounceTime = Time.time;
                // ✅ 只有保护时间结束才增加反弹次数
                if (groundProtectTimer >= groundProtectTime)
                {
                    groundBounces++;
                    if (groundBounces >= maxGroundBounces)
                    {
                        Die();
                        return;
                    }
                }
            }

            // 计时寿命
            groundTimer += Time.deltaTime;
            if (groundTimer >= groundLifeTime)
            {
                Die();
                return;
            }
            if (isDying) return;
            // 🔹 滚地模式
            transform.position += Vector3.right * groundDirection * groundSpeed * Time.deltaTime;
            return;
        }

        if (!isReturning)
        {
            if (!isCircularMode)
            {
                // 普通半圆
                timer += Time.deltaTime;
                float t = timer / flightDuration;
                Vector2 horizontal = Vector2.Lerp(startPoint, endPoint, t);
                float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = new Vector2(horizontal.x, horizontal.y + arc);

                if (t >= 1f) isReturning = true;
            }
            else
            {
                // 圆周
                circleAngle += circleDirection * (360f / circleDuration) * Time.deltaTime;
                float rad = circleAngle * Mathf.Deg2Rad;
                transform.position = new Vector2(
                    circleCenter.x + Mathf.Cos(rad) * circleRadius,
                    circleCenter.y + Mathf.Sin(rad) * circleRadius
                );

                if ((circleDirection == 1 && circleAngle >= 630f) ||
                    (circleDirection == -1 && circleAngle <= -90f))
                    isReturning = true;
            }
        }
        else
        {
            // 返回玩家
            Vector2 dir = (player.position - transform.position).normalized;
            transform.position += (Vector3)(dir * returnSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, player.position) < 0.5f)
            {
                Destroy(gameObject);
                playerShooting?.OnBulletDestroyed();
            }
        }
    }

    private void Die()
    {
        if (isDying) return;
        isDying = true;

        animator?.SetTrigger("Die");
        Destroy(gameObject, 0.18f);

        if (!hasCalledDestroyed)
        {
            hasCalledDestroyed = true;
            playerShooting?.OnBulletDestroyed();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (isGroundMode && groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }

}
