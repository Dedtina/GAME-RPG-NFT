public class BringerOfDeathCastSpellState : EnemyState
{
    private readonly BringerOfDeathEnemy enemy;

    public BringerOfDeathCastSpellState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, string.Empty)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        triggeredAnim = false;
        enemy.anim.Play("CastSpell", 0, 0);
        enemy.SetZeroVelocity();
    }

    public override void Exit()
    {
        enemy.lastTimeSpell = UnityEngine.Time.time;
    }

    public override void Update()
    {
        base.Update();
        enemy.SetZeroVelocity();

        if (triggeredAnim)
            stateMachine.ChangeState(enemy.battleState);
    }
}
