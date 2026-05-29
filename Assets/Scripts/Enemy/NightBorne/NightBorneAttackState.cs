public class NightBorneAttackState : EnemyState
{
    private NightBorneEnemy enemy;

    public NightBorneAttackState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.SetZeroVelocity();
    }

    public override void Exit()
    {
        base.Exit();

        enemy.lastTimeAttack = UnityEngine.Time.time;
    }

    public override void Update()
    {
        base.Update();

        enemy.SetZeroVelocity();

        if (triggeredAnim)
            stateMachine.ChangeState(enemy.battleState);
    }
}
