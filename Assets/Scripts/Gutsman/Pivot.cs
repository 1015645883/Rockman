using UnityEngine;

public class LiftPivotTrigger : MonoBehaviour
{
    private LiftController liftController;

    void Start()
    {
        liftController = GetComponentInParent<LiftController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("LiftDisableZone"))
        {
            liftController?.DisableLift();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("LiftDisableZone"))
        {
            liftController?.EnableLift();
        }
    }
}
