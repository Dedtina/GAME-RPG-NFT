using UnityEngine;

public class BringerOfDeathAnimationTriggers : MonoBehaviour
{
    private BringerOfDeathEnemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<BringerOfDeathEnemy>();
    }

    public void TriggerAttack()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(enemy.attackCheck.position, enemy.attackRadius);

        foreach (Collider2D obj in colliders)
        {
            if (obj.TryGetComponent(out Player player))
                enemy.statCtrl.DoDamage(player.statCtrl);
        }
    }

    public void TriggerCastSpell()
    {
        enemy.CastCursedSpell();
    }

    public void OpenCounterArea()
    {
        enemy.OpenCounterArea();
    }

    public void CloseCounterArea()
    {
        enemy.CloseCounterArea();
    }

    public void TriggerAnim()
    {
        enemy.TriggerCurrentAnim();
    }

    public void TriggerDeathComplete()
    {
        Destroy(enemy.gameObject);
    }
}
