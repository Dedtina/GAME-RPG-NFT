public class NightBorneGroundState : EnemyState
{
    protected NightBorneEnemy enemy;

    public NightBorneGroundState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName)
    {
        this.enemy = enemy;
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

        bool detect = enemy.IsPlayerDetected();

        if (detect)
            stateMachine.ChangeState(enemy.battleState);
    }
}
