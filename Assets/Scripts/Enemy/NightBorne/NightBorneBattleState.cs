using UnityEngine;

public class NightBorneBattleState : EnemyState
{
    private NightBorneEnemy enemy;
    private Transform playerTransform;
    private float moveDir;

    public NightBorneBattleState(Enemy enemyBase, EnemyStateMachine stateMachine, string animName, NightBorneEnemy enemy)
        : base(enemyBase, stateMachine, animName)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        base.Enter();

        playerTransform = PlayerManager.instance.player.transform;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        RaycastHit2D playerDetected = enemy.IsPlayerDetected();

        if (playerDetected)
        {
            stateTimer = enemy.battleTime;

            if (playerDetected.distance < enemy.attackRange &&
                Time.time > enemy.attackCooldown + enemy.lastTimeAttack &&
                playerDetected.rigidbody.position.x * enemy.facingDir > enemy.transform.position.x * enemy.facingDir)
            {
                stateMachine.ChangeState(enemy.attackState);
                return;
            }
        }
        else if (stateTimer < 0 || Vector2.Distance(enemy.transform.position, playerTransform.position) > enemy.battleRange)
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        moveDir = playerTransform.position.x > enemy.rb.position.x ? 1 : -1;
        enemy.SetVelocity(enemy.battleSpeed * moveDir, enemy.rb.velocity.y);
    }
}
