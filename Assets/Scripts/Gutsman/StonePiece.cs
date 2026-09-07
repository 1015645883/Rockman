using UnityEngine;

public class StonePiece : MonoBehaviour
{

    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    private void Start()
    {
        // 5秒后自动销毁
        Destroy(gameObject, 5f);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastDamageTime = Time.time;
            }
        }

    }
}
