using UnityEngine;

public class FireWave : MonoBehaviour
{
    public int damage = 2;
    public float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("组件引用")]
    private Animator animator;
    private Collider2D coll;
    private AudioSource audioSource;

    public AudioClip fireSound;

    void Awake()
    {
        animator = GetComponent<Animator>();
        coll = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        // 特殊处理：如果角色是 Iceman，伤害+1
        string selectedChar = PlayerPrefs.GetString("SelectedCharacter", "");
        if (selectedChar == "Iceman" || selectedChar == "Fireman")
        {
            damage = 1;
        }
        else if (selectedChar == "Bombman")
        {
            damage = 3;
        }
        // 一开始关闭碰撞器
        if (coll != null) coll.enabled = false;
    }

    public void ActivateWave()
    {
        if (coll != null) coll.enabled = true;
        if (animator != null) animator.Play("Wake");

        if (fireSound != null && audioSource != null)
            audioSource.PlayOneShot(fireSound);
    }

    public void SleepWave()
    {
        if (coll != null) coll.enabled = false;
        if (animator != null) animator.Play("Sleep");
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
