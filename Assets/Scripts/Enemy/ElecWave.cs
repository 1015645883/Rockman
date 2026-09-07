using UnityEngine;

public class ElecWave : MonoBehaviour
{
    public int damage = 2;               // 基础伤害
    public float damageCooldown = 1f;
    private float lastDamageTime;
    public AudioClip elecSound;          // 可选：播放音效
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (elecSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(elecSound);
        }

        // 特殊处理：如果角色是 Iceman，伤害+1
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman")
        {
            damage += 1;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
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
