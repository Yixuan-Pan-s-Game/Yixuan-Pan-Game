using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SkeletonEnemyAI : MonoBehaviour
{
    public Animator animator;
    public RuntimeAnimatorController controllerOverride;
    public Transform target;
    public float detectRange = 9f;
    public float attackRange = 2.4f;
    public float moveSpeed = 2.1f;
    public float turnSpeed = 540f;
    public float attackCooldown = 1.4f;
    public float attackLockSeconds = 0.85f;
    public float hitDelay = 0.36f;
    public int slashDamage = 14;
    public float hitReachPadding = 0.65f;
    public float deathFallSeconds = 0.7f;
    public float deathDisappearDelay = 1.8f;
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string slashState = "Slash";

    private Rigidbody body;
    private ProceduralShoulderSwing shoulderSwing;
    private float nextAttackTime;
    private float attackLockedUntil;
    private float pendingHitTime;
    private bool hasPendingHit;
    private string currentState;
    private bool dead;

    private const int BaseLayer = 0;

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
