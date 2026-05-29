using UnityEngine;
using UnityEngine.Serialization;

public class BringerOfDeathEnemy : Enemy
{
    [Header("Player detection")]
    [SerializeField] private float playerDetectionHeight = 4f;

    [Header("Spell info")]
    [Tooltip("Prefab used to create this enemy's runtime cursed spell.")]
    [FormerlySerializedAs("cursedSpell")]
    [SerializeField] private CursedSpellController cursedSpellPrefab;
    public float spellRange = 7f;
    public float spellCooldown = 4f;
    public float lastTimeSpell;

    private CursedSpellController activeCursedSpell;

    public BringerOfDeathIdleState idleState { get; private set; }
    public BringerOfDeathMoveState moveState { get; private set; }
    public BringerOfDeathBattleState battleState { get; private set; }
    public BringerOfDeathAttackState attackState { get; private set; }
    public BringerOfDeathCastSpellState castSpellState { get; private set; }
    public BringerOfDeathStunnedState stunnedState { get; private set; }
    public BringerOfDeathDeathState deathState { get; private set; }

    public Vector2 BodyCenter =>
        capsuleCD == null ? transform.position : transform.TransformPoint(capsuleCD.offset);

    protected override void Awake()
    {
        base.Awake();
        SetInitialFacingDirection(false);

        idleState = new BringerOfDeathIdleState(this, stateMachine, this);
        moveState = new BringerOfDeathMoveState(this, stateMachine, this);
        battleState = new BringerOfDeathBattleState(this, stateMachine, this);
        attackState = new BringerOfDeathAttackState(this, stateMachine, this);
        castSpellState = new BringerOfDeathCastSpellState(this, stateMachine, this);
        stunnedState = new BringerOfDeathStunnedState(this, stateMachine, this);
        deathState = new BringerOfDeathDeathState(this, stateMachine, this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override RaycastHit2D IsPlayerDetected()
    {
        Vector2 detectionSize = new Vector2(.1f, playerDetectionHeight);
        RaycastHit2D forward = Physics2D.BoxCast(
            wallCheck.position,
            detectionSize,
            0,
            facingDir * Vector2.right,
            detectPlayerForwardDistance,
            whatIsPlayer);

        if (forward)
            return forward;

        return Physics2D.BoxCast(
            wallCheck.position,
            detectionSize,
            0,
            -facingDir * Vector2.right,
            detectPlayerBehindDistance,
            whatIsPlayer);
    }

    public override void Flip()
    {
        Vector2 bodyCenterBeforeFlip = BodyCenter;
        base.Flip();
        transform.position += (Vector3)(bodyCenterBeforeFlip - BodyCenter);
    }

    public void CastCursedSpell()
    {
        if (cursedSpellPrefab == null)
        {
            Debug.LogWarning($"{name} has no cursed spell prefab assigned.", this);
            return;
        }

        if (activeCursedSpell == null)
            activeCursedSpell = Instantiate(cursedSpellPrefab);

        activeCursedSpell.Activate(this, PlayerManager.instance.player.transform);
    }

    public override void BeCounter()
    {
        base.BeCounter();
        stateMachine.ChangeState(stunnedState);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.yellow;
        float detectionWidth = detectPlayerForwardDistance + detectPlayerBehindDistance;
        Vector3 detectionCenter = wallCheck.position +
            Vector3.right * facingDir * ((detectPlayerForwardDistance - detectPlayerBehindDistance) * .5f);
        Gizmos.DrawWireCube(detectionCenter, new Vector3(detectionWidth, playerDetectionHeight, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * facingDir * attackRange);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - battleRange, transform.position.y - .2f),
            new Vector3(transform.position.x + battleRange, transform.position.y - .2f));
    }

    public override void Die()
    {
        if (activeCursedSpell != null)
            Destroy(activeCursedSpell.gameObject);

        base.Die();
        stateMachine.ChangeState(deathState);
    }
}
