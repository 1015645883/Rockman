using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BluesSpecial : MonoBehaviour
{
    public GameObject shieldPrefab;  // 盾牌预制体
    public Transform shieldSpawnPoint;  // 盾牌生成点，建议挂在角色正前方

    private GameObject currentShield;
    private PlayerMovement playerMovement;

    public float chargeSpeedMultiplier = 1.5f; // 比洛克人快 50%

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (shieldPrefab == null)
        {
            Debug.LogWarning("请在检查器赋值盾牌预制体！");
        }
        if (shieldSpawnPoint == null)
        {
            Debug.LogWarning("请指定盾牌生成点！");
        }
    }

    void Update()
    {
        if (playerMovement == null)
            return;

        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite == null)
            return;

        Vector3 shieldLocalPos = shieldSpawnPoint.localPosition;

        if (playerSprite.flipX)
        {
            // 玩家翻转了，盾牌放右边（比如 x=正数）
            shieldLocalPos.x = Mathf.Abs(shieldLocalPos.x);
        }
        else
        {
            // 玩家未翻转，盾牌放左边（比如 x=负数）
            shieldLocalPos.x = -Mathf.Abs(shieldLocalPos.x);
        }
        shieldSpawnPoint.localPosition = shieldLocalPos;

        if (playerMovement.isJumping && !playerMovement.isShooting)
        {
            if (currentShield == null && shieldPrefab != null)
            {
                // 不指定父物体，盾牌独立于玩家
                currentShield = Instantiate(shieldPrefab, shieldSpawnPoint.position, Quaternion.identity);
            }
            else if (currentShield != null)
            {
                // 让盾牌跟随shieldSpawnPoint位置，但不挂在玩家下
                currentShield.transform.position = shieldSpawnPoint.position;
            }
        }
        else
        {
            if (currentShield != null)
            {
                Destroy(currentShield);
                currentShield = null;
            }
        }
    }


    // 你要确保布鲁斯的蓄力速度逻辑用下面这个方法获取
    public float GetChargeSpeed(float baseChargeSpeed)
    {
        return baseChargeSpeed * chargeSpeedMultiplier;
    }
}