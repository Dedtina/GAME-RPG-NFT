public class NightBorneMoveState : NightBorneGroundState
{
    public NightBorneMoveState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName, enemy)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
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
