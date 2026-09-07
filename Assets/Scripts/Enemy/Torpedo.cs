using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class TorpedoEnemy : MonoBehaviour
{
    [Header("悬浮设置")]
    public float floatAmplitude = 0.1f;  // 悬浮幅度
    public float floatSpeed = 2f;        // 悬浮速度

    [Header("探测设置")]
    public float detectRadius = 3f;      // 玩家探测半径

    [Header("追踪设置")]
    public float moveSpeed = 2f;         // 追踪速度
    public float maxChaseTime = 3f;      // 追踪最长时间

    [Header("爆炸设置")]
    public GameObject explosionPrefab;   // 爆炸预制体

    [Header("玩家引用")]
    public Transform player;             // 玩家对象，可在编辑器拖入或运行时查找

    private Rigidbody2D rb;
    public Animator animator;
    private Vector2 startPos;
    private bool isActive = false;
    public bool isFrozen = false;
    private float chaseTimer = 0f;
    private bool hasTriggeredExplodeAnim = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        startPos = transform.position;
        animator = GetComponent<Animator>();
        // 延迟绑定玩家
        StartCoroutine(FindPlayerWithDelay());
    }
    private IEnumerator FindPlayerWithDelay()
    {
        // 每隔 0.1 秒查找一次直到找到玩家
        while (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("Torpedo: 成功找到玩家对象。");
                yield break; // 找到后结束协程
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    private void Update()
    {
        if (isFrozen) return;
        if (!isActive)
        {
            // 悬浮效果
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            rb.MovePosition(startPos + new Vector2(0f, yOffset));

            // 检测玩家是否进入范围
            if (player != null && Vector2.Distance(transform.position, player.position) <= detectRadius)
            {
                ActivateTorpedo();
            }
        }
        else
        {
            if (player != null)
            {
                // 缓慢追踪玩家
                Vector2 direction = ((Vector2)player.position - rb.position).normalized;
                rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

                // 检测追踪到玩家
                if (Vector2.Distance(rb.position, player.position) < 1.5f)
                {
                    Explode();
                }
            }

            chaseTimer += Time.deltaTime;
            // 在爆炸前 0.5 秒播放动画
            if (!hasTriggeredExplodeAnim && chaseTimer >= maxChaseTime - 0.5f)
            {
                animator.SetTrigger("Explode");
                hasTriggeredExplodeAnim = true;
            }

            if (chaseTimer >= maxChaseTime)
            {
                Explode();
            }
        }
    }

    private void ActivateTorpedo()
    {
        if (isActive) return;
        isActive = true;
        animator.SetTrigger("Wake");
        chaseTimer = 0f;
    }

    private void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 各种武器打到鱼雷
        if (collision.CompareTag("Bullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.Deflect();
            ActivateTorpedo();
        }
        else if (collision.CompareTag("ChargeBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            if (bullet != null) bullet.Deflect();
            ActivateTorpedo();
        }
        else if (collision.CompareTag("RollingCutter"))
        {
            ActivateTorpedo();
        }
        else if (collision.CompareTag("SuperArm"))
        {
            Explode();
        }
        else if (collision.CompareTag("IceArrow"))
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
                    iceArrow.DestroyBullet();
                }
            }
        }
        else if (collision.CompareTag("HyperBomb"))
        {
            Explode();
        }
        else if (collision.CompareTag("FireStorm"))
        {
            Explode();
        }
        else if (collision.CompareTag("ThunderBeam"))
        {
            Explode();
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }


}
