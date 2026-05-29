public class BringerOfDeathIdleState : BringerOfDeathGroundState
{
    public BringerOfDeathIdleState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, enemy)
    {
    }

    public override void Enter()
    {
        PlayAnimation("Idle");
        enemy.SetZeroVelocity();
        stateTimer = enemy.idleTime;
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }
}
