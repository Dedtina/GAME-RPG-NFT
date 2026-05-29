public class BringerOfDeathStunnedState : EnemyState
{
    private readonly BringerOfDeathEnemy enemy;

    public BringerOfDeathStunnedState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, string.Empty)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        triggeredAnim = false;
        enemy.anim.Play("Stunned", 0, 0);
        enemy.SetZeroVelocity();
        stateTimer = enemy.stunnedDuration;
        enemy.fx.InvokeRepeating(nameof(enemy.fx.RedBlink), 0, .1f);
    }

    public override void Exit()
    {
        enemy.fx.Invoke(nameof(enemy.fx.CancelColorChange), 0);
    }

    public override void Update()
    {
        base.Update();
        enemy.SetZeroVelocity();

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.battleState);
    }
}
