using UnityEngine;

public class BringerOfDeathBattleState : EnemyState
{
    private readonly BringerOfDeathEnemy enemy;
    private Transform playerTransform;
    private bool isMoving;

    public BringerOfDeathBattleState(Enemy enemyBase, EnemyStateMachine stateMachine, BringerOfDeathEnemy enemy)
        : base(enemyBase, stateMachine, string.Empty)
    {
        this.enemy = enemy;
    }

    public override void Enter()
    {
        triggeredAnim = false;
        playerTransform = PlayerManager.instance.player.transform;

        float horizontalDistance = Mathf.Abs(playerTransform.position.x - enemy.BodyCenter.x);
        PlayMovementAnimation(horizontalDistance > enemy.attackRange, true);
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        base.Update();

        RaycastHit2D playerDetected = enemy.IsPlayerDetected();
        float horizontalOffset = playerTransform.position.x - enemy.BodyCenter.x;
        float horizontalDistance = Mathf.Abs(horizontalOffset);

        if (horizontalDistance > .05f)
        {
            int directionToPlayer = horizontalOffset > 0 ? 1 : -1;

            if (directionToPlayer != enemy.facingDir)
                enemy.Flip();
        }

        if (playerDetected)
            stateTimer = enemy.battleTime;

        if (horizontalDistance <= enemy.attackRange)
        {
            PlayMovementAnimation(false);
            enemy.SetZeroVelocity();

            if (Time.time > enemy.attackCooldown + enemy.lastTimeAttack)
                stateMachine.ChangeState(enemy.attackState);

            return;
        }

        if (horizontalDistance <= enemy.spellRange &&
            Time.time > enemy.spellCooldown + enemy.lastTimeSpell)
        {
            enemy.SetZeroVelocity();
            stateMachine.ChangeState(enemy.castSpellState);
            return;
        }

        if (!playerDetected && (stateTimer < 0 || Vector2.Distance(enemy.BodyCenter, playerTransform.position) > enemy.battleRange))
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        PlayMovementAnimation(true);
        enemy.SetVelocity(enemy.battleSpeed * enemy.facingDir, enemy.rb.velocity.y);
    }

    private void PlayMovementAnimation(bool moving, bool forceRestart = false)
    {
        if (!forceRestart && moving == isMoving)
            return;

        isMoving = moving;
        enemy.anim.Play(moving ? "Walk" : "Idle", 0, 0);
    }
}
