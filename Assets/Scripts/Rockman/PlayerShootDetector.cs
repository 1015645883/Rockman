using System.Collections;
using UnityEngine;

public class PlayerShootDetector : MonoBehaviour
{
    public PlayerMovement playerMovement;

    private IEnumerator Start()
    {
        // 延迟0.5秒获取，确保场上Player已经生成
        yield return new WaitForSeconds(0.5f);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            playerMovement = playerObj.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogWarning("未找到标签为Player的对象！");
        }
    }

    public bool IsPlayerShooting()
    {
        return playerMovement != null && playerMovement.IsShooting();
    }
}
