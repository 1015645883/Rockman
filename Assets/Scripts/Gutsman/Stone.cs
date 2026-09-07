using UnityEngine;

public class Stone : MonoBehaviour
{
    [Header("Gutsman投掷")]
    public GutsmanController gutsmanController;
    public float throwForce = 10f;              // 抛出的力度
    public float throwAngle = 30f;              // 抛出的角度
    private bool canThrow = true;
    private bool isFollowing = false;
    private Transform followTarget;
    [Header("碎裂设置")]
    public GameObject[] debrisPrefabs;          // 小碎片预制体
    public int minDebris = 3;
    public int maxDebris = 6;
    public float debrisForce = 3f;              // 碎片飞出的基础力度
    public float debrisLifetime = 2f;

    [Header("伤害设置")]
    public int damage = 3;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    private bool hasShattered = false;
    private bool isThrown = false;
    private Vector2 throwDirection = Vector2.zero;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        if (gutsmanController == null)
            gutsmanController = FindObjectOfType<GutsmanController>();
    }

    private void Update()
    {
        if (isFollowing && followTarget != null)
        {
            transform.position = followTarget.position;
        }
    }

    public void EnableThrow()
    {
        canThrow = true;

        Detach();

        if (rb != null)
        {
            rb.simulated = true;
            
        }
    }

    public void DisableThrow()
    {
        canThrow = false;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasShattered) return;
        Debug.Log("碰到了: " + other.gameObject.name + " Tag: " + other.tag);
        // Gutsman触发投掷（只触发一次）
        // 如果碰撞对象是Gutsman，且canThrow为true才投掷
        if (other.CompareTag("Gutsman"))
        {
            if (!canThrow) return;  // 还不能投掷，直接返回

            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player != null)
            {
                throwDirection = (player.position - transform.position).normalized;
                ThrowStone(throwDirection);
            }
            return;
        }

        // 撞击 Player 并造成伤害
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage; // 默认伤害

                // 判断玩家当前角色
                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Elecman")
                {
                    finalDamage = 4; // 属性克制，伤害提高
                }
                if (currentChar == "Cutman")
                {
                    finalDamage = 5; // 属性克制，伤害提高
                }
                playerHealth.TakeDamage(finalDamage);

                lastDamageTime = Time.time;
            }
            Shatter();
        }

        // 撞击 Player 或 Ground 都会碎裂
        if (other.CompareTag("Player") || other.CompareTag("Ground") || other.CompareTag("HyperBomb"))
        {
            if (!canThrow) return;
            Shatter();
        }
    }

    public void AttachTo(Transform target)
    {
        followTarget = target;
        isFollowing = true;
        // 禁用物理和碰撞，防止掉落
        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;
    }

    public void Detach()
    {
        isFollowing = false;
        followTarget = null;

        // 解除绑定，恢复碰撞等，由投掷逻辑控制
        if (col != null) col.enabled = true;
    }

    private void ThrowStone(Vector2 direction)
    {
        isThrown = true;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector2.zero;

            // 抛掷的主要力（水平和竖直方向）
            Vector2 throwForceVector = direction.normalized * throwForce;

            // 额外的轻微向上力，比如在竖直方向上增加一个固定的力值
            Vector2 upwardForce = Vector2.up * (throwForce * 0.1f); // 0.1f 代表10%的额外向上力，可以调整

            // 合成最终力，给刚体一个冲量
            rb.AddForce(throwForceVector + upwardForce, ForceMode2D.Impulse);
        }

        // ✅ 播放投掷动画
        if (gutsmanController != null)
        {
            gutsmanController.PlayThrowAnimation();
            gutsmanController.PlayRandomVoice(gutsmanController.attackVoices);
        }
    }


    private void Shatter()
    {
        hasShattered = true;

        // 停止物理和碰撞
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        // 隐藏主Sprite
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr) sr.enabled = false;

        // 生成碎片
        int count = Random.Range(minDebris, maxDebris + 1);
        for (int i = 0; i < count; i++)
        {
            if (debrisPrefabs.Length == 0) break;
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            GameObject frag = Instantiate(prefab, transform.position, Quaternion.identity);

            Rigidbody2D fragRb = frag.GetComponent<Rigidbody2D>();
            if (fragRb)
            {
                Vector2 baseDir = isThrown ? throwDirection : Vector2.up;
                float randomAngle = Random.Range(-60f, 60f);
                Vector2 dir = Quaternion.Euler(0, 0, randomAngle) * baseDir.normalized;
                fragRb.AddForce(dir * debrisForce, ForceMode2D.Impulse);
            }

            Destroy(frag, debrisLifetime);
        }

        Destroy(gameObject);
    }
}
