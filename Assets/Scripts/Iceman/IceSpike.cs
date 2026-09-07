using System.Collections;
using UnityEngine;

public class IceSpike : MonoBehaviour
{
    [Header("基础设置")]
    public float lifeTime = 4f;              // 冰刺存在时间

    [Header("伤害设置")]
    public int damage = 3;
    public float damageCooldown = 0.2f;      // 防止多次触发
    private float lastDamageTime;

    [Header("碎片特效")]
    public GameObject leftFragmentPrefab;    // 左碎片
    public GameObject rightFragmentPrefab;   // 右碎片
    public float fragmentLifeTime = 3f;      // 碎片存在时间
    public float fragmentForce = 3f;         // 抛出力度

    private bool isDestroyed = false;        // 防止重复销毁

    void Start()
    { 
        // 特殊角色加伤
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Fireman")
        {
            damage += 1;
        }
        // 4秒后自动销毁
        Invoke(nameof(DestroySelf), lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed) return;

        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }

            // 命中后直接销毁
            DestroySelf();
        }
    }

    void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        SpawnFragments();

        Destroy(gameObject);
    }

    void SpawnFragments()
    {
        // 左碎片
        if (leftFragmentPrefab != null)
        {
            GameObject left = Instantiate(leftFragmentPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = left.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(-fragmentForce, fragmentForce);
            }
            Destroy(left, fragmentLifeTime);
        }

        // 右碎片
        if (rightFragmentPrefab != null)
        {
            GameObject right = Instantiate(rightFragmentPrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = right.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(fragmentForce, fragmentForce);
            }
            Destroy(right, fragmentLifeTime);
        }
    }
}