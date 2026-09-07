using UnityEngine;

public class HealItem : MonoBehaviour
{
    [SerializeField] private int healAmount = 6;
    [SerializeField] private AudioClip healSound;
    private Animator anim;           // 动画组件

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HealthSystem health = collision.GetComponent<HealthSystem>();

        if (health != null)
        {
            health.Heal(healAmount);
            PlayHealSound();

            Destroy(gameObject); // 销毁道具本体
        }
    }

    private void PlayHealSound()
    {
        if (healSound != null)
        {
            GameObject soundObj = new GameObject("HealSound");
            AudioSource tempAudio = soundObj.AddComponent<AudioSource>();
            tempAudio.clip = healSound;
            tempAudio.Play();

            Destroy(soundObj, healSound.length);
        }
    }
}
