using UnityEngine;
using System;

public class IceBlockWatcher : MonoBehaviour
{
    [Header("冰冻音效")]
    public AudioClip freezeSound;   // 指定冰冻音效
    private AudioSource audioSource;

    public string enemyName;                  // 要监视的敌人名字
    public Action onEnemyDeadCallback;        // 当敌人死亡时调用的回调
    private bool triggered = false;

    void Start()
    {
        // 播放音效
        if (freezeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = freezeSound;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (triggered) return;

        if (string.IsNullOrEmpty(enemyName)) return;

        // 检查敌人是否存在
        GameObject enemyObj = GameObject.Find(enemyName);
        if (enemyObj == null)
        {
            triggered = true;
            onEnemyDeadCallback?.Invoke();  // 通知外部敌人死亡
            Destroy(gameObject);            // 可以直接销毁冰块
        }
    }
}
