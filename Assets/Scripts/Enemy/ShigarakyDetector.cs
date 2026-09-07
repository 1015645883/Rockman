using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShigarakyDetector : MonoBehaviour
{
    public Shigaraky shigaraky;      // 要通知的本体
    public string playerTag = "Player";

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && shigaraky != null)
        {
            shigaraky.SetPlayerDetected(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && shigaraky != null)
        {
            shigaraky.SetPlayerDetected(false);
        }
    }
}
