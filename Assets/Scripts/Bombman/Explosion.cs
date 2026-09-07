using UnityEngine;

public class Explosion : MonoBehaviour
{
    [Header("伤害设置")]
    public int damage = 3;                 // 伤害值
    public float damageCooldown = 1.5f;     // 冷却时间（秒）

    [Header("生命周期设置")]
    public float lifeTime = 0.5f;             // 存在时间

    private float lastDamageTime = -Mathf.Infinity;
    private Collider2D col;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true; // 确保为触发器
        }

        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Gutsman" || selectedChar == "Iceman")
        {
            damage = 4;
        }
        if (selectedChar == "Fireman" || selectedChar == "Bombman")
        {
            damage = 2;
        }
        // 自动销毁
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        DealDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        DealDamage(collision);
    }

    private void DealDamage(Collider2D collision)
    {
        if (collision.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = collision.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;

            }
        }
    }
}
