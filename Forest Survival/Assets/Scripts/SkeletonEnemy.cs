using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// Enemy behavior controller that handles targeting, movement, attacks, hit timing, and death flow.
public class SkeletonEnemyAI : MonoBehaviour
{
    // Cached component or scene reference to avoid repeated lookups: animator.
    public Animator animator;
    // Asset reference used for spawning, rendering, audio, or animation: controllerOverride.
    public RuntimeAnimatorController controllerOverride;
    // Current interaction target or gameplay object being processed: target.
    public Transform target;
    // Distance or radius used for detection, interaction, or physics checks: detectRange.
    public float detectRange = 9f;
    // Distance or radius used for detection, interaction, or physics checks: attackRange.
    public float attackRange = 2.4f;
    // Speed or movement tuning value: moveSpeed.
    public float moveSpeed = 2.1f;
    // Speed or movement tuning value: turnSpeed.
    public float turnSpeed = 540f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: attackCooldown.
    public float attackCooldown = 1.4f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: attackLockSeconds.
    public float attackLockSeconds = 0.85f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: hitDelay.
    public float hitDelay = 0.36f;
    // Gameplay stat that affects damage, health, healing, defense, or durability: slashDamage.
    public int slashDamage = 14;
    // Distance or radius used for detection, interaction, or physics checks: hitReachPadding.
    public float hitReachPadding = 0.65f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: deathFallSeconds.
    public float deathFallSeconds = 0.7f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: deathDisappearDelay.
    public float deathDisappearDelay = 1.8f;
    // Identifier or category used for lookup, routing, or state selection: idleState.
    public string idleState = "Idle";
    // Identifier or category used for lookup, routing, or state selection: walkState.
    public string walkState = "Walk";
    // Identifier or category used for lookup, routing, or state selection: slashState.
    public string slashState = "Slash";

    // Important runtime data or configuration used by this component: body.
    private Rigidbody body;
    // Important runtime data or configuration used by this component: shoulderSwing.
    private ProceduralShoulderSwing shoulderSwing;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: nextAttackTime.
    private float nextAttackTime;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: attackLockedUntil.
    private float attackLockedUntil;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: pendingHitTime.
    private float pendingHitTime;
    // Runtime flag that drives control flow, UI state, or gameplay availability: hasPendingHit.
    private bool hasPendingHit;
    // Identifier or category used for lookup, routing, or state selection: currentState.
    private string currentState;
    // Runtime flag that drives control flow, UI state, or gameplay availability: dead.
    private bool dead;

    // Layer or mask filter used by physics queries or rendering: BaseLayer.
    private const int BaseLayer = 0;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (controllerOverride != null)
            {
                animator.runtimeAnimatorController = controllerOverride;
            }
        }

        shoulderSwing = GetComponent<ProceduralShoulderSwing>();
        if (shoulderSwing == null)
        {
            shoulderSwing = gameObject.AddComponent<ProceduralShoulderSwing>();
        }

        shoulderSwing.animator = animator;
        shoulderSwing.windupSeconds = 0.12f;
        shoulderSwing.strikeSeconds = 0.18f;
        shoulderSwing.recoverSeconds = 0.2f;
    }

    // Unity lifecycle: updates physics, rigidbody movement, and collision state on the fixed timestep.
    private void FixedUpdate()
    {
        if (dead)
        {
            return;
        }

        Transform chaseTarget = ResolveTarget();
        if (chaseTarget == null)
        {
            PlayState(idleState);
            return;
        }

        Vector3 toTarget = chaseTarget.position - body.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance > detectRange)
        {
            hasPendingHit = false;
            PlayState(idleState);
            return;
        }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, lookRotation, turnSpeed * Time.fixedDeltaTime));
        }

        if (Time.time < attackLockedUntil)
        {
            PlayState(slashState);
            ResolvePendingHit(chaseTarget);
            return;
        }

        if (distance <= attackRange)
        {
            TrySlash();
            return;
        }

        Vector3 direction = toTarget.normalized;
        body.MovePosition(body.position + direction * moveSpeed * Time.fixedDeltaTime);
        PlayState(walkState);
        ResolvePendingHit(chaseTarget);
    }

    // Finds, loads, or caches the references needed for resolve target.
    private Transform ResolveTarget()
    {
        if (target != null)
        {
            return target;
        }

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        if (player != null)
        {
            target = player.transform;
            return target;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            target = taggedPlayer.transform;
        }

        return target;
    }

    // Attempts to try slash and returns whether the operation succeeded.
    private void TrySlash()
    {
        if (Time.time < nextAttackTime)
        {
            PlayState(idleState);
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        attackLockedUntil = Time.time + attackLockSeconds;
        pendingHitTime = Time.time + Mathf.Clamp(hitDelay, 0.05f, attackLockSeconds);
        hasPendingHit = true;
        if (shoulderSwing != null)
        {
            shoulderSwing.Play();
        }

        PlayState(slashState, true);
    }

    // Finds, loads, or caches the references needed for resolve pending hit.
    private void ResolvePendingHit(Transform chaseTarget)
    {
        if (!hasPendingHit || Time.time < pendingHitTime || chaseTarget == null)
        {
            return;
        }

        hasPendingHit = false;
        Vector3 toTarget = chaseTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > attackRange + hitReachPadding)
        {
            return;
        }

        if (toTarget.sqrMagnitude > 0.001f)
        {
            float facing = Vector3.Dot(transform.forward, toTarget.normalized);
            if (facing < 0.15f)
            {
                return;
            }
        }

        PlayerHealth health = chaseTarget.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(slashDamage);
        }
    }

    // Starts or stops the animation, audio, or gameplay flow for play state.
    private void PlayState(string stateName, bool forceRestart = false)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        if (!animator.HasState(BaseLayer, Animator.StringToHash(stateName)))
        {
            return;
        }

        if (!forceRestart && currentState == stateName)
        {
            return;
        }

        currentState = stateName;
        if (forceRestart)
        {
            animator.Play(stateName, BaseLayer, 0f);
            return;
        }

        animator.CrossFadeInFixedTime(stateName, 0.12f, BaseLayer);
    }

    // Handles the on enemy died workflow.
    private void OnEnemyDied(GameObject attacker)
    {
        if (dead)
        {
            return;
        }

        dead = true;
        hasPendingHit = false;
        if (animator != null)
        {
            animator.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.isKinematic = true;
        }

        StartCoroutine(FallBackAndDisappear());
    }

    // Handles the fall back and disappear workflow.
    private IEnumerator FallBackAndDisappear()
    {
        Quaternion start = transform.rotation;
        Quaternion end = start * Quaternion.Euler(-85f, 0f, 0f);
        float duration = Mathf.Max(0.05f, deathFallSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        transform.rotation = end;
        yield return new WaitForSeconds(Mathf.Max(0f, deathDisappearDelay));
        Destroy(gameObject);
    }
}
