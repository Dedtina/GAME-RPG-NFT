using UnityEngine;

public class BringerOfDeathDeathState : EnemyState
{
    private readonly BringerOfDeathEnemy enemy;

    public BringerOfDeathDeathState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, string.Empty)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        triggeredAnim = false;
        enemy.anim.Play("Death", 0, 0);
        enemy.capsuleCD.enabled = false;
        enemy.rb.velocity = Vector2.zero;
        enemy.rb.gravityScale = 0;
    }

    public override void Exit()
    {
    }
}
