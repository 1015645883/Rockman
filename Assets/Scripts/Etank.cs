using UnityEngine;
using System.Collections;

public class EtankPickup : MonoBehaviour
{
    [Header("音效/特效")]
    public AudioClip pickupSound;
    public GameObject pickupEffect; // 比如闪光特效
    private AudioSource audioSource;

    private void Start()
    {
        StartCoroutine(InitAudioSource());
    }

    IEnumerator InitAudioSource()
    {
        // ✅ 等到 Camera.main 出现
        while (Camera.main == null)
            yield return null;

        audioSource = Camera.main.GetComponent<AudioSource>();

        // ✅ 没有就自动加
        if (audioSource == null)
        {
            audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            GamePauseManager pauseManager = FindObjectOfType<GamePauseManager>();
            if (pauseManager != null)
            {
                pauseManager.AddEtank(1);
            }

            if (pickupSound != null && audioSource != null)
                audioSource.PlayOneShot(pickupSound);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

}
