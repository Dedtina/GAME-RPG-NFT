using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerFallRecovery : MonoBehaviour
{
    [Header("Safe position")]
    [SerializeField] private float maxSafeVerticalSpeed = 0.1f;
    [SerializeField] private Vector2 respawnOffset = new(0f, 0.25f);

    [Header("Fall detection")]
    [SerializeField] private bool useFallYLimit = true;
    [SerializeField] private float fallYLimit = -20f;
    [SerializeField] private float recoveryCooldown = 0.5f;

    [Header("Penalty")]
    [Range(0f, 1f)]
    [SerializeField] private float maxHealthDamagePercent = 0.2f;
    [SerializeField] private bool resetToIdleState = true;

    private Player player;
    private Vector3 lastSafePosition;
    private float lastRecoveryTime = -999f;

    private void Awake()
    {
        player = GetComponent<Player>();
        lastSafePosition = transform.position;
    }

    private void Update()
    {
        if (player.statCtrl == null || player.statCtrl.IsDeath())
            return;

        UpdateSafePosition();

        if (useFallYLimit && transform.position.y <= fallYLimit)
            RecoverFromFall();
    }

    public void RecoverFromFall()
    {
        if (Time.time < lastRecoveryTime + recoveryCooldown)
            return;

        if (player.statCtrl == null || player.statCtrl.IsDeath())
            return;

        lastRecoveryTime = Time.time;

        int damage = Mathf.CeilToInt(player.statCtrl.GetTotalMaxHealth() * maxHealthDamagePercent);
        player.statCtrl.TakeDamage(damage, false);

        if (player.statCtrl.IsDeath())
            return;

        transform.position = lastSafePosition + (Vector3)respawnOffset;
        Physics2D.SyncTransforms();

        if (player.rb != null)
            player.rb.velocity = Vector2.zero;

        if (resetToIdleState && player.stateMachine != null && player.idleState != null)
            player.stateMachine.ChangeState(player.idleState);
    }

    private void UpdateSafePosition()
    {
        if (!player.IsGroundDetected())
            return;

        if (player.rb != null && Mathf.Abs(player.rb.velocity.y) > maxSafeVerticalSpeed)
            return;

        lastSafePosition = transform.position;
    }
}
