using UnityEngine;

public class PlayerDamageEffect : MonoBehaviour
{
    public GameObject damageEffect1Prefab; // 拖拽你的 DamageEffect1 预制体
    public Transform headTransform;        // 设置为角色头部的位置

    // 相对于头部的三个特效位置
    private Vector3[] damageOffsets = new Vector3[]
    {
        new Vector3(-0.6f, 0.5f, 0),
        new Vector3(0f, 0.7f, 0),
        new Vector3(0.6f, 0.5f, 0)
    };

    public void PlayDamageEffects()
    {
        foreach (Vector3 offset in damageOffsets)
        {
            Vector3 spawnPos = headTransform.position + offset;
            Instantiate(damageEffect1Prefab, spawnPos, Quaternion.identity);
        }
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}
