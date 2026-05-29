public class NightBorneDeathState : EnemyState
{
    private NightBorneEnemy enemy;

    public NightBorneDeathState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.capsuleCD.enabled = false;
        enemy.rb.velocity = UnityEngine.Vector2.zero;
        enemy.rb.gravityScale = 0;
    }
}
