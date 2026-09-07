using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireResidue : MonoBehaviour
{
    public int damage = 2;
    public float damageCooldown = 1.4f;
    private float lastDamageTime;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && Time.time > lastDamageTime + damageCooldown)
        {
            HealthSystem playerHealth = other.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                int finalDamage = damage; // 默认伤害

                // 判断玩家当前角色
                string currentChar = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
                if (currentChar == "Bombman")
                {
                    finalDamage = 3; // 属性克制
                }
                if (currentChar == "Iceman" || currentChar == "Fireman")
                {
                    finalDamage = 1; // 属性减免
                }
                playerHealth.TakeDamage(finalDamage);
                lastDamageTime = Time.time;
            }
        }
    }
}
