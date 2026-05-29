public class NightBorneIdleState : NightBorneGroundState
{
    public NightBorneIdleState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName, enemy)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateTimer = enemy.idleTime;
    }

    public override void Exit()
    {
        base.Exit();

        AudioManager.instance.PlaySFX(24, enemy.transform);
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }
}
