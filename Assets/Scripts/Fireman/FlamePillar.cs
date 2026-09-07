using System.Collections;
using UnityEngine;

public class FlamePillar : MonoBehaviour
{
    public int damage = 3;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;
    [Header("阶段火柱")]
    public GameObject stage1;  // 阶段1（最小）
    public GameObject stage2;  // 阶段2（中等）
    public GameObject stage3;  // 阶段3（最高）
    private Animator animator;
    public string stage1Anim = "FireStorm4-1";
    public string stage2Anim = "FireStorm4-2";
    public string stage3Anim = "FireStorm4-3";
    [Header("参数设置")]
    public float growTime = 0.5f;   // 从一个阶段长到下个阶段的时间
    public float sustainTime = 2f;  // 在3阶段持续的时间

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(PillarRoutine());
    }

    private IEnumerator PillarRoutine()
    {
        // 出生时显示1阶段
        SetStage(1);

        // 过渡到2阶段
        yield return new WaitForSeconds(growTime);
        SetStage(2);

        // 过渡到3阶段
        yield return new WaitForSeconds(growTime);
        SetStage(3);

        // 维持3阶段
        yield return new WaitForSeconds(sustainTime);

        // 退化回2阶段
        SetStage(2);
        yield return new WaitForSeconds(growTime);

        // 退化回1阶段
        SetStage(1);
        yield return new WaitForSeconds(growTime);

        // 最终销毁
        Destroy(gameObject);
    }

    private void SetStage(int stage)
    {
        // 先关闭所有阶段
        if (stage1 != null) stage1.SetActive(false);
        if (stage2 != null) stage2.SetActive(false);
        if (stage3 != null) stage3.SetActive(false);

        // 根据阶段激活对应 GameObject 并播放动画
        switch (stage)
        {
            case 1:
                if (stage1 != null) stage1.SetActive(true);
                if (animator != null) animator.Play(stage1Anim);
                break;
            case 2:
                if (stage2 != null) stage2.SetActive(true);
                if (animator != null) animator.Play(stage2Anim);
                break;
            case 3:
                if (stage3 != null) stage3.SetActive(true);
                if (animator != null) animator.Play(stage3Anim);
                break;
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
                if (currentChar == "Bombman") finalDamage = 5;
                if (currentChar == "Iceman" || currentChar == "Fireman") finalDamage = 1;

                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage;

                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Bombman") finalDamage = 5;
                if (currentChar == "Iceman" || currentChar == "Fireman") finalDamage = 1;

                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }
}
