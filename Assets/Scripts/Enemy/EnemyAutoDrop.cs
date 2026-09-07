using UnityEngine;

public class EnemyAutoDrop : MonoBehaviour
{
    [Header("掉落物")]
    public GameObject bigHealPrefab;
    public GameObject smallHealPrefab;

    [Header("掉落概率（0-1）")]
    public float bigHealChance = 0.05f;
    public float smallHealChance = 0.10f;

    private bool hasDropped = false;

    /// <summary>
    /// 调用这个方法来触发掉落
    /// </summary>
    public void TryDrop()
    {
        if (hasDropped) return;
        hasDropped = true;

        Vector3 pos = transform.position;

        if (bigHealPrefab != null && Random.value <= bigHealChance)
        {
            Instantiate(bigHealPrefab, pos, Quaternion.identity);
        }
        else if (smallHealPrefab != null && Random.value <= smallHealChance)
        {
            Instantiate(smallHealPrefab, pos, Quaternion.identity);
        }
    }
}
