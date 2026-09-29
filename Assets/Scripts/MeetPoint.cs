using UnityEngine;

public class MeetingPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 通知关卡管理器
            FindObjectOfType<SpecialLevelManager>()
                .OnCharacterReachedMeetingPoint();
        }
    }
}