using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FallRecoveryTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerFallRecovery fallRecovery = collision.GetComponentInParent<PlayerFallRecovery>();
        if (fallRecovery != null)
            fallRecovery.RecoverFromFall();
    }

    private void OnValidate()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null)
            trigger.isTrigger = true;
    }
}
