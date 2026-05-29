public class BringerOfDeathGroundState : EnemyState
{
    protected readonly BringerOfDeathEnemy enemy;

    protected BringerOfDeathGroundState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, string.Empty)
    {
        this.enemy = enemy;
    }

    protected void PlayAnimation(string stateName)
    {
        triggeredAnim = false;
        enemy.anim.Play(stateName, 0, 0);
    }

    public override void Update()
    {
        base.Update();

        if (enemy.IsPlayerDetected())
            stateMachine.ChangeState(enemy.battleState);
    }
}
