using System.Collections;
using UnityEngine;

public class CursedSpellController : MonoBehaviour
{
    [SerializeField] private Vector2 spawnOffset = new Vector2(0, 2f);
    [SerializeField] private float impactDelay = 1f;
    [SerializeField] private float activeDuration = 1.55f;
    [Tooltip("Damage radius at scale 1. The effective radius follows the spell visual scale.")]
    [SerializeField] private float damageRadius = 1.6f;
    [SerializeField] private float damageMultiplier = 1.5f;

    private Animator anim;
    private BringerOfDeathEnemy owner;
    private Coroutine activeRoutine;
    private Vector2 targetPosition;

    private float EffectiveDamageRadius =>
        damageRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

    public void Activate(BringerOfDeathEnemy spellOwner, Transform target)
    {
        owner = spellOwner;
        targetPosition = target.position;
        transform.position = targetPosition + spawnOffset;
        gameObject.SetActive(true);

        if (anim == null)
            anim = GetComponentInChildren<Animator>(true);

        anim.Play("Idle", 0, 0);

        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(ResolveSpell());
    }

    private IEnumerator ResolveSpell()
    {
        yield return new WaitForSeconds(impactDelay);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(targetPosition, EffectiveDamageRadius);

        foreach (Collider2D obj in colliders)
        {
            if (obj.TryGetComponent(out Player player))
            {
                owner.statCtrl.DoDamage(player.statCtrl, damageMultiplier);
                break;
            }
        }

        yield return new WaitForSeconds(Mathf.Max(0, activeDuration - impactDelay));
        activeRoutine = null;
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position - (Vector3)spawnOffset, EffectiveDamageRadius);
    }
}
