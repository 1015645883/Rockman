using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFireStorm2 : MonoBehaviour
{
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    [Header("飞行参数")]
    public float upSpeed = 6f;
    public float downSpeed = 8f;
    public float upDistance = 10f;
    public float downDistance = 20f;
    public int splitCount = 3;

    [Header("自定义分裂范围 (X 轴)")]
    public float leftX = -5f;   // 分裂范围最小 X
    public float rightX = 5f;   // 分裂范围最大 X

    private Rigidbody2D rb;
    private Vector3 startPos;
    private bool hasSplit = false;

    private bool isClone = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        if (isClone) return; // ✅ 克隆体不执行上升逻辑

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        startPos = transform.position;
        rb.velocity = Vector2.up * upSpeed;
    }

    void Update()
    {
        if (!isClone && !hasSplit && Vector3.Distance(startPos, transform.position) >= upDistance)
        {
            DoSplit();
        }
    }

    void DoSplit()
    {
        hasSplit = true;

        for (int i = 0; i < splitCount; i++)
        {
            float randomX = Random.Range(leftX, rightX); // ✅ 只在自定义范围内
            Vector3 spawnPos = new Vector3(randomX, transform.position.y, 0);

            GameObject clone = Instantiate(gameObject, spawnPos, Quaternion.identity);

            EnemyFireStorm2 fire = clone.GetComponent<EnemyFireStorm2>();
            if (fire != null)
            {
                fire.isClone = true;
                fire.BeginFalling();
            }
        }

        Destroy(gameObject);
    }

    public void BeginFalling()
    {
        hasSplit = true;
        startPos = transform.position;

        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.velocity = Vector2.down * downSpeed;

        // ✅ 翻转火焰朝下
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipY = true;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.y = -Mathf.Abs(scale.y);
            transform.localScale = scale;
        }

        StartCoroutine(DestroyAfterDistance());
    }

    private IEnumerator DestroyAfterDistance()
    {
        while (true)
        {
            if (Vector3.Distance(startPos, transform.position) >= downDistance)
            {
                Destroy(gameObject);
                yield break;
            }
            yield return null;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage;

                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Bombman")
                    finalDamage = 3;
                if (currentChar == "Iceman" || currentChar == "Fireman")
                    finalDamage = 1;

                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }
}
