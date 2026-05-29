public class BringerOfDeathMoveState : BringerOfDeathGroundState
{
    public BringerOfDeathMoveState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, enemy)
    {
    }

    public override void Enter()
    {
        PlayAnimation("Walk");
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        base.Update();
        enemy.SetVelocity(enemy.facingDir * enemy.moveSpeed, enemy.rb.velocity.y);

        if (enemy.IsWallDetected() || !enemy.IsGroundDetected())
        {
            enemy.Flip();
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
