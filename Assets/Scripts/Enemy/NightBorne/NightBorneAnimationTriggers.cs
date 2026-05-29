using UnityEngine;

public class NightBorneAnimationTriggers : MonoBehaviour
{
    private Enemy enemy;

    [Header("Death explosion")]
    [SerializeField] private float deathExplosionRadius = 2f;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    public void TriggerAnim()
    {
        enemy.TriggerCurrentAnim();
    }

    public void TriggerAttack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackRadius);

        foreach (Collider2D obj in colliders)
        {
            if (obj.TryGetComponent(out Player player))
            {
                enemy.statCtrl.DoDamage(player.statCtrl);
                enemy.statCtrl.DoFireDamage(player.statCtrl);
            }
        }
    }

    public void TriggerDeathExplosion()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.transform.position, deathExplosionRadius);

        foreach (Collider2D obj in colliders)
        {
            if (obj.TryGetComponent(out Player player))
                enemy.statCtrl.DoDamage(player.statCtrl, 2f);
        }
    }

    public void TriggerDeathComplete()
    {
        Destroy(enemy.gameObject);
    }

    public void OpenCounterArea()
    {
        enemy.OpenCounterArea();
    }

    public void CloseCounterArea()
    {
        enemy.CloseCounterArea();
    }
}
