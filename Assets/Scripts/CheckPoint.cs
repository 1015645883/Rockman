using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public static Vector3 lastCheckpointPosition;

    private bool hasBeenActivated = false;

    [Header("旗子动画")]
    public GameObject flagObject;           // Flag 子对象（默认隐藏）
    public Animator flagAnimator;           // Flag 上的 Animator

    [Header("音效")]
    public AudioClip activateSound;         // 激活时播放的音效
    private AudioSource audioSource;        // 用于播放音效

    void Start()
    {
        flagObject.SetActive(false);

        // 获取或添加 AudioSource 组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false; // 禁止自动播放
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !hasBeenActivated)
        {
            lastCheckpointPosition = transform.position;
            hasBeenActivated = true;

            // 激活旗子对象
            if (flagObject != null && !flagObject.activeSelf)
            {
                flagObject.SetActive(true);
            }

            // 播放旗子升起动画
            if (flagAnimator != null)
            {
                flagAnimator.SetTrigger("FlagUp");
            }

            // 播放激活音效
            if (activateSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(activateSound);
            }

            Debug.Log("存档点已激活：" + transform.position);
        }
    }
}
