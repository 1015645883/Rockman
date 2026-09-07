using UnityEngine;

public class BodyTrigger : MonoBehaviour
{
    public CountBomb bomb;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Bullet") || other.CompareTag("ChargeBullet"))
        {
            Bullet bullet = other.GetComponent<Bullet>();
            if (bullet != null) bullet.Deflect();
        }
        else if (other.CompareTag("FireStorm"))
        {
            bomb.Explode();
        }
        else if (other.CompareTag("IceArrow"))
        {
            RIceArrow iceArrow = other.GetComponent<RIceArrow>();
            if (iceArrow != null)
            {
                if (!bomb.isFrozen)
                    iceArrow.Freeze(bomb); // ±ù¶³Õ¨µ¯
                iceArrow.DestroyBullet();  // Ïú»Ù±ù¼ý
            }
        }
    }
}
