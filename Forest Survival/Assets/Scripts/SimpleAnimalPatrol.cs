using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// Animal behavior component for patrol, harvesting, or interaction state.
public class SimpleAnimalPatrol : MonoBehaviour
{
    [Header("Patrol")]
    // Distance or radius used for detection, interaction, or physics checks: patrolDistance.
    public float patrolDistance = 4f;
    // Speed or movement tuning value: moveSpeed.
    public float moveSpeed = 0.8f;
    // Speed or movement tuning value: turnSpeed.
    public float turnSpeed = 360f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: waitAtEndSeconds.
    public float waitAtEndSeconds = 1f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: waitJitterSeconds.
    public float waitJitterSeconds = 0f;
    // Speed or movement tuning value: speedJitter.
    public float speedJitter = 0f;

    [Header("Animation")]
    // Cached component or scene reference to avoid repeated lookups: animator.
    public Animator animator;
    // Asset reference used for spawning, rendering, audio, or animation: controllerOverride.
    public RuntimeAnimatorController controllerOverride;
    // Identifier or category used for lookup, routing, or state selection: idleState.
    public string idleState = "idle";
    // Identifier or category used for lookup, routing, or state selection: walkState.
    public string walkState = "walk_forward";
    // Runtime flag that drives control flow, UI state, or gameplay availability: useBlendParameters.
    public bool useBlendParameters;
    // Important runtime data or configuration used by this component: verticalParameter.
    public string verticalParameter = "Vert";
    // Identifier or category used for lookup, routing, or state selection: stateParameter.
    public string stateParameter = "State";
    // Identifier or category used for lookup, routing, or state selection: idleVerticalValue.
    public float idleVerticalValue = 0f;
    // Important runtime data or configuration used by this component: walkVerticalValue.
    public float walkVerticalValue = 0.5f;
    // Identifier or category used for lookup, routing, or state selection: walkStateValue.
    public float walkStateValue = 0f;

    // Timing value or timestamp used for cooldowns, delays, or progress checks: startPosition.
    private Vector3 startPosition;
    // Spatial value used for positioning, rotation, scale, or collision math: leftPoint.
    private Vector3 leftPoint;
    // Spatial value used for positioning, rotation, scale, or collision math: rightPoint.
    private Vector3 rightPoint;
    // Current interaction target or gameplay object being processed: targetPoint.
    private Vector3 targetPoint;
    // Important runtime data or configuration used by this component: body.
    private Rigidbody body;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: waitTimer.
    private float waitTimer;
    // Important runtime data or configuration used by this component: walking.
    private bool walking;
    // Speed or movement tuning value: currentMoveSpeed.
    private float currentMoveSpeed;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null && controllerOverride != null)
        {
            animator.runtimeAnimatorController = controllerOverride;
        }
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        startPosition = transform.position;
        Vector3 halfPatrol = transform.right * (patrolDistance * 0.5f);
        leftPoint = startPosition - halfPatrol;
        rightPoint = startPosition + halfPatrol;
        targetPoint = rightPoint;
        currentMoveSpeed = ResolveMoveSpeed();
        ApplyAnimation(false);
    }

    // Unity lifecycle: updates physics, rigidbody movement, and collision state on the fixed timestep.
    private void FixedUpdate()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            SetWalking(false);
            return;
        }

        Vector3 current = body.position;
        Vector3 flatTarget = new Vector3(targetPoint.x, current.y, targetPoint.z);
        Vector3 toTarget = flatTarget - current;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            targetPoint = targetPoint == rightPoint ? leftPoint : rightPoint;
            waitTimer = ResolveWaitTime();
            currentMoveSpeed = ResolveMoveSpeed();
            SetWalking(false);
            return;
        }

        Vector3 direction = toTarget.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        body.MovePosition(current + direction * currentMoveSpeed * Time.fixedDeltaTime);
        SetWalking(true);
    }

    // Sets state, selection, or placement data for set walking.
    private void SetWalking(bool isWalking)
    {
        if (walking == isWalking)
        {
            return;
        }

        walking = isWalking;
        ApplyAnimation(walking);
    }

    // Finds, loads, or caches the references needed for resolve wait time.
    private float ResolveWaitTime()
    {
        if (waitJitterSeconds <= 0f)
        {
            return waitAtEndSeconds;
        }

        return Mathf.Max(0f, waitAtEndSeconds + Random.Range(-waitJitterSeconds, waitJitterSeconds));
    }

    // Finds, loads, or caches the references needed for resolve move speed.
    private float ResolveMoveSpeed()
    {
        if (speedJitter <= 0f)
        {
            return moveSpeed;
        }

        return Mathf.Max(0.05f, moveSpeed + Random.Range(-speedJitter, speedJitter));
    }

    // Refreshes and applies configuration or runtime state for apply animation.
    private void ApplyAnimation(bool isWalking)
    {
        if (useBlendParameters)
        {
            SetBlendParameters(isWalking);
            return;
        }

        PlayState(isWalking ? walkState : idleState);
    }

    // Sets state, selection, or placement data for set blend parameters.
    private void SetBlendParameters(bool isWalking)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(verticalParameter))
        {
            animator.SetFloat(verticalParameter, isWalking ? walkVerticalValue : idleVerticalValue);
        }

        if (!string.IsNullOrEmpty(stateParameter))
        {
            animator.SetFloat(stateParameter, isWalking ? walkStateValue : 0f);
        }
    }

    // Starts or stops the animation, audio, or gameplay flow for play state.
    private void PlayState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        animator.CrossFadeInFixedTime(stateName, 0.15f);
    }
}
