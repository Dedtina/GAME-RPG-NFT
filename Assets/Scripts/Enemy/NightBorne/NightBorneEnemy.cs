using UnityEngine;

public class NightBorneEnemy : Enemy
{
    public NightBorneIdleState idleState { get; private set; }
    public NightBorneMoveState moveState { get; private set; }
    public NightBorneBattleState battleState { get; private set; }
    public NightBorneAttackState attackState { get; private set; }
    public NightBorneStunnedState stunnedState { get; private set; }
    public NightBorneDeathState deathState { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        idleState = new NightBorneIdleState(this, stateMachine, "Idle", this);
        moveState = new NightBorneMoveState(this, stateMachine, "Move", this);
        battleState = new NightBorneBattleState(this, stateMachine, "Move", this);
        attackState = new NightBorneAttackState(this, stateMachine, "Attack", this);
        stunnedState = new NightBorneStunnedState(this, stateMachine, "Hurt", this);
        deathState = new NightBorneDeathState(this, stateMachine, "Death", this);
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + facingDir * attackRange, transform.position.y));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(transform.position.x - battleRange, transform.position.y - .2f),
            new Vector3(transform.position.x + battleRange, transform.position.y - .2f));
    }

    public override void BeCounter()
    {
        base.BeCounter();

        stateMachine.ChangeState(stunnedState);
    }

    public override void Die()
    {
        base.Die();

        stateMachine.ChangeState(deathState);
    }
}
