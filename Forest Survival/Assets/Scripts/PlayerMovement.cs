using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
// Controls player input, physics movement, jumping, climbing, and terrain safety recovery.
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    // Speed or movement tuning value: moveSpeed.
    public float moveSpeed = 100f;
    // Speed or movement tuning value: sprintMultiplier.
    public float sprintMultiplier = 1.45f;
    // Speed or movement tuning value: acceleration.
    public float acceleration = 18f;
    // Important runtime data or configuration used by this component: airControlPercent.
    public float airControlPercent = 0.55f;
    // Speed or movement tuning value: rotationSpeed.
    public float rotationSpeed = 10f;
    // Speed or movement tuning value: braking.
    public float braking = 24f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: analogDeadZone.
    public float analogDeadZone = 0.2f;
    // Speed or movement tuning value: stopSpeed.
    public float stopSpeed = 0.05f;

    [Header("Collision Recovery")]
    // Important runtime data or configuration used by this component: resolveSpawnOverlaps.
    public bool resolveSpawnOverlaps = true;
    // Runtime flag that drives control flow, UI state, or gameplay availability: collisionSkin.
    public float collisionSkin = 0.03f;
    // Important runtime data or configuration used by this component: maxDepenetrationStep.
    public float maxDepenetrationStep = 0.5f;
    // Important runtime data or configuration used by this component: depenetrationPasses.
    public int depenetrationPasses = 3;
    // Layer or mask filter used by physics queries or rendering: collisionMask.
    public LayerMask collisionMask = ~0;

    [Header("Jump")]
    // Important runtime data or configuration used by this component: jumpForce.
    public float jumpForce = 7.5f;
    // Distance or radius used for detection, interaction, or physics checks: groundProbeDistance.
    public float groundProbeDistance = 0.35f;
    // Important runtime data or configuration used by this component: maxGroundAngle.
    public float maxGroundAngle = 55f;
    // Layer or mask filter used by physics queries or rendering: groundMask.
    public LayerMask groundMask = ~0;
    // Runtime flag that drives control flow, UI state, or gameplay availability: groundedDownForce.
    public float groundedDownForce = 2f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: coyoteTime.
    public float coyoteTime = 0.12f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: jumpBufferTime.
    public float jumpBufferTime = 0.15f;

    [Header("Terrain Safety")]
    // Important runtime data or configuration used by this component: rescueWhenFallingThroughTerrain.
    public bool rescueWhenFallingThroughTerrain = true;
    // Important runtime data or configuration used by this component: fallRescueDepth.
    public float fallRescueDepth = 12f;
    // Important runtime data or configuration used by this component: fallRescueHardY.
    public float fallRescueHardY = -40f;
    // Important runtime data or configuration used by this component: rescueGroundRayHeight.
    public float rescueGroundRayHeight = 120f;
    // Distance or radius used for detection, interaction, or physics checks: rescueGroundRayDistance.
    public float rescueGroundRayDistance = 260f;

    [Header("Climb")]
    // Runtime flag that drives control flow, UI state, or gameplay availability: enableClimb.
    public bool enableClimb = false;
    // Distance or radius used for detection, interaction, or physics checks: climbCheckDistance.
    public float climbCheckDistance = 0.95f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: climbMinHeight.
    public float climbMinHeight = 1.15f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: climbMaxHeight.
    public float climbMaxHeight = 2.65f;
    // Spatial value used for positioning, rotation, scale, or collision math: climbForwardOffset.
    public float climbForwardOffset = 0.65f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: climbDuration.
    public float climbDuration = 0.85f;
    // Layer or mask filter used by physics queries or rendering: climbMask.
    public LayerMask climbMask = ~0;

    // Important runtime data or configuration used by this component: rb.
    private Rigidbody rb;
    // Important runtime data or configuration used by this component: capsule.
    private CapsuleCollider capsule;
    // Cached component or scene reference to avoid repeated lookups: mainCamera.
    private Transform mainCamera;
    // Cached component or scene reference to avoid repeated lookups: cameraFollow.
    private ThirdPersonCameraFollow cameraFollow;
    // Cached component or scene reference to avoid repeated lookups: runAnimator.
    private CharacterRunAnimator runAnimator;
    // Input setting or cached input value read from player controls: moveInput.
    private Vector2 moveInput;
    // Important runtime data or configuration used by this component: jumpQueued.
    private bool jumpQueued;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastJumpPressedTime.
    private float lastJumpPressedTime = -999f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastGroundedTime.
    private float lastGroundedTime = -999f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: isGrounded.
    private bool isGrounded;
    // Runtime flag that drives control flow, UI state, or gameplay availability: isClimbing.
    private bool isClimbing;
    // Runtime flag that drives control flow, UI state, or gameplay availability: sprintHeld.
    private bool sprintHeld;
    // Spatial value used for positioning, rotation, scale, or collision math: groundNormal.
    private Vector3 groundNormal = Vector3.up;
    // Speed or movement tuning value: planarSpeed.
    private float planarSpeed;
    // Spatial value used for positioning, rotation, scale, or collision math: previousPosition.
    private Vector3 previousPosition;
    // Speed or movement tuning value: externalVelocity.
    private Vector3 externalVelocity;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastSafeGroundedPosition.
    private Vector3 lastSafeGroundedPosition;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: lastSafeGroundedTime.
    private float lastSafeGroundedTime = -999f;

    // Read-only state exposed to other systems: IsGrounded.
    public bool IsGrounded => isGrounded;
    // Read-only state exposed to other systems: IsClimbing.
    public bool IsClimbing => isClimbing;
    // Read-only state exposed to other systems: IsSprinting.
    public bool IsSprinting => sprintHeld && moveInput.y > 0.1f && moveInput.sqrMagnitude > 0.01f;
    // Read-only state exposed to other systems: PlanarSpeed.
    public float PlanarSpeed => planarSpeed;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        runAnimator = GetComponent<CharacterRunAnimator>();
        previousPosition = transform.position;
        lastSafeGroundedPosition = transform.position;
        ConfigureRigidbody();
    }

    // Unity lifecycle: restores runtime configuration or subscriptions when the component is enabled.
    private void OnEnable()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        ConfigureRigidbody();
    }

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        CacheCamera();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (mainCamera == null)
        {
            CacheCamera();
        }

        moveInput = ReadMoveInput();
        sprintHeld = ReadKey(KeyCode.W) && ReadKey(KeyCode.V);

        if (ReadJumpDown())
        {
            jumpQueued = true;
            lastJumpPressedTime = Time.time;
        }
    }

    // Unity lifecycle: updates physics, rigidbody movement, and collision state on the fixed timestep.
    private void FixedUpdate()
    {
        if (isClimbing)
        {
            planarSpeed = 0f;
            previousPosition = transform.position;
            return;
        }

        UpdateMeasuredPlanarSpeed();
        ConfigureRigidbody();
        ResolveOverlaps();
        UpdateGrounding();
        RecordSafeGroundedPosition();
        ApplyMovement();
        ApplyJump();
        RescueIfFallingThroughTerrain();
        previousPosition = transform.position;
    }

    // Refreshes and applies configuration or runtime state for update measured planar speed.
    private void UpdateMeasuredPlanarSpeed()
    {
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;
        planarSpeed = displacement.magnitude / Mathf.Max(Time.fixedDeltaTime, 0.0001f);
    }

    // Refreshes and applies configuration or runtime state for configure rigidbody.
    private void ConfigureRigidbody()
    {
        if (rb == null)
        {
            return;
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.angularDrag = 8f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.WakeUp();
    }

    // Finds, loads, or caches the references needed for resolve overlaps.
    private void ResolveOverlaps()
    {
        if (!resolveSpawnOverlaps || capsule == null)
        {
            return;
        }

        for (int pass = 0; pass < Mathf.Max(1, depenetrationPasses); pass++)
        {
            GetCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius);
            Collider[] hits = Physics.OverlapCapsule(
                bottom,
                top,
                radius + collisionSkin,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            Vector3 totalOffset = Vector3.zero;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null || !hit.enabled || hit.isTrigger || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!Physics.ComputePenetration(
                    capsule,
                    transform.position,
                    transform.rotation,
                    hit,
                    hit.transform.position,
                    hit.transform.rotation,
                    out Vector3 direction,
                    out float distance))
                {
                    continue;
                }

                if (distance <= 0.0001f || direction.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                float step = Mathf.Min(distance + collisionSkin, maxDepenetrationStep);
                totalOffset += direction.normalized * step;
            }

            if (totalOffset.sqrMagnitude < 0.000001f)
            {
                return;
            }

            Vector3 nextPosition = rb.position + totalOffset;
            rb.position = nextPosition;
            transform.position = nextPosition;
            Physics.SyncTransforms();
        }
    }

    // Calculates and returns the result for get capsule world points.
    private void GetCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius)
    {
        float radiusScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        radius = Mathf.Max(0.05f, capsule.radius * radiusScale);
        float height = Mathf.Max(radius * 2f, capsule.height * Mathf.Abs(transform.lossyScale.y));
        Vector3 center = transform.TransformPoint(capsule.center);
        float halfLine = Mathf.Max(0f, (height * 0.5f) - radius);
        bottom = center + Vector3.down * halfLine;
        top = center + Vector3.up * halfLine;
    }

    // Handles the read move input workflow.
    private Vector2 ReadMoveInput()
    {
        Vector2 keyboardInput = ReadKeyboardInput();
        if (keyboardInput.sqrMagnitude > 0.001f)
        {
            return Vector2.ClampMagnitude(keyboardInput, 1f);
        }

        Vector2 analogInput = new Vector2(ReadLegacyAxis("Horizontal"), ReadLegacyAxis("Vertical"));
        return ApplyAnalogDeadZone(analogInput);
    }

    // Refreshes and applies configuration or runtime state for apply analog dead zone.
    private Vector2 ApplyAnalogDeadZone(Vector2 input)
    {
        float deadZone = Mathf.Clamp01(analogDeadZone);
        float magnitude = input.magnitude;

        if (magnitude <= deadZone)
        {
            return Vector2.zero;
        }

        float scaledMagnitude = Mathf.InverseLerp(deadZone, 1f, Mathf.Min(magnitude, 1f));
        return input.normalized * scaledMagnitude;
    }

    // Handles the read keyboard input workflow.
    private static Vector2 ReadKeyboardInput()
    {
        Vector2 input = Vector2.zero;

        if (ReadKey(KeyCode.A) || ReadKey(KeyCode.LeftArrow))
        {
            input.x -= 1f;
        }

        if (ReadKey(KeyCode.D) || ReadKey(KeyCode.RightArrow))
        {
            input.x += 1f;
        }

        if (ReadKey(KeyCode.S) || ReadKey(KeyCode.DownArrow))
        {
            input.y -= 1f;
        }

        if (ReadKey(KeyCode.W) || ReadKey(KeyCode.UpArrow))
        {
            input.y += 1f;
        }

        return input;
    }

    // Handles the read legacy axis workflow.
    private static float ReadLegacyAxis(string axisName)
    {
        try
        {
            return Input.GetAxisRaw(axisName);
        }
        catch (System.ArgumentException)
        {
            return 0f;
        }
        catch (System.InvalidOperationException)
        {
            return 0f;
        }
    }

    // Handles the read jump down workflow.
    private static bool ReadJumpDown()
    {
        bool legacyJump = false;

        try
        {
            legacyJump = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
        }
        catch (System.ArgumentException)
        {
        }
        catch (System.InvalidOperationException)
        {
        }

        return legacyJump || ReadKeyDown(KeyCode.Space);
    }

    // Handles the read key workflow.
    private static bool ReadKey(KeyCode key)
    {
        try
        {
            return Input.GetKey(key);
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }

    // Handles the read key down workflow.
    private static bool ReadKeyDown(KeyCode key)
    {
        try
        {
            return Input.GetKeyDown(key);
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }

    // Finds, loads, or caches the references needed for cache camera.
    private void CacheCamera()
    {
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
            cameraFollow = mainCamera.GetComponent<ThirdPersonCameraFollow>();
        }
    }

    // Refreshes and applies configuration or runtime state for update grounding.
    private void UpdateGrounding()
    {
        float radius = Mathf.Max(0.05f, capsule.radius * 0.9f);
        Vector3 center = transform.TransformPoint(capsule.center);
        float bottomOffset = (capsule.height * 0.5f) - capsule.radius;
        Vector3 origin = center + (Vector3.down * bottomOffset) + (Vector3.up * 0.08f);
        float castDistance = groundProbeDistance + 0.12f;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.down, castDistance, groundMask, QueryTriggerInteraction.Ignore);
        RaycastHit bestHit = default;
        bool foundGround = false;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!IsValidStandingGround(hit.collider))
            {
                continue;
            }

            float groundAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (groundAngle > maxGroundAngle || hit.distance >= bestDistance)
            {
                continue;
            }

            bestHit = hit;
            bestDistance = hit.distance;
            foundGround = true;
        }

        isGrounded = foundGround;
        groundNormal = foundGround ? bestHit.normal : Vector3.up;
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
        }
    }

    // Calculates and returns the result for is valid standing ground.
    private static bool IsValidStandingGround(Collider collider)
    {
        if (collider == null)
        {
            return false;
        }

        if (collider is TerrainCollider || collider.GetComponentInParent<ProceduralTerrain>() != null)
        {
            return true;
        }

        string lowerName = collider.name.ToLowerInvariant();
        Transform parent = collider.transform.parent;
        while (parent != null)
        {
            lowerName += " " + parent.name.ToLowerInvariant();
            parent = parent.parent;
        }

        if (lowerName.Contains("miningisland_fallthroughsupport")
            || lowerName.Contains("terrain")
            || lowerName.Contains("ground")
            || lowerName.Contains("floor")
            || lowerName.Contains("path")
            || lowerName.Contains("island"))
        {
            return true;
        }

        if (collider.GetComponentInParent<HarvestableResource>() != null
            || collider.GetComponentInParent<LockableTarget>() != null)
        {
            return false;
        }

        if (lowerName.Contains("tree")
            || lowerName.Contains("rock")
            || lowerName.Contains("cliff")
            || lowerName.Contains("wall")
            || lowerName.Contains("ore")
            || lowerName.Contains("crystal"))
        {
            return false;
        }

        return true;
    }

    // Sets state, selection, or placement data for record safe grounded position.
    private void RecordSafeGroundedPosition()
    {
        if (!isGrounded)
        {
            return;
        }

        lastSafeGroundedPosition = rb != null ? rb.position : transform.position;
        lastSafeGroundedTime = Time.time;
    }

    // Handles the rescue if falling through terrain workflow.
    private void RescueIfFallingThroughTerrain()
    {
        if (!rescueWhenFallingThroughTerrain || rb == null || capsule == null || isGrounded)
        {
            return;
        }

        Vector3 position = rb.position;
        bool farBelowLastSafe = Time.time - lastSafeGroundedTime > 0.35f
            && position.y < lastSafeGroundedPosition.y - Mathf.Max(2f, fallRescueDepth);
        bool belowWorld = position.y < fallRescueHardY;
        if (!farBelowLastSafe && !belowWorld)
        {
            return;
        }

        Vector3 targetPosition;
        if (TryFindRescueGround(position, out RaycastHit hit))
        {
            targetPosition = hit.point + Vector3.up * GetCapsuleStandingOffset();
        }
        else
        {
            targetPosition = lastSafeGroundedPosition + Vector3.up * 0.35f;
        }

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = targetPosition;
        transform.position = targetPosition;
        Physics.SyncTransforms();
        lastSafeGroundedPosition = targetPosition;
        lastSafeGroundedTime = Time.time;
    }

    // Attempts to try find rescue ground and returns whether the operation succeeded.
    private bool TryFindRescueGround(Vector3 currentPosition, out RaycastHit bestHit)
    {
        bestHit = default;
        Physics.SyncTransforms();

        Vector3 origin = new Vector3(currentPosition.x, currentPosition.y + rescueGroundRayHeight, currentPosition.z);
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            Mathf.Max(0.15f, capsule.radius * 0.75f),
            Vector3.down,
            rescueGroundRayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore);

        bool found = false;
        float bestY = float.NegativeInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || hit.normal.y < 0.35f)
            {
                continue;
            }

            if (!IsValidStandingGround(hit.collider))
            {
                continue;
            }

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    // Calculates and returns the result for get capsule standing offset.
    private float GetCapsuleStandingOffset()
    {
        float radiusScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float radius = Mathf.Max(0.05f, capsule.radius * radiusScale);
        float height = Mathf.Max(radius * 2f, capsule.height * Mathf.Abs(transform.lossyScale.y));
        float centerY = capsule.center.y * transform.lossyScale.y;
        return Mathf.Max(0.05f, (height * 0.5f) - centerY + 0.05f);
    }

    // Refreshes and applies configuration or runtime state for apply movement.
    private void ApplyMovement()
    {
        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        Vector3 moveDirection = ResolveMoveDirection(inputDirection);
        if (isGrounded && moveDirection.sqrMagnitude > 0.001f)
        {
            moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal).normalized;
        }

        float control = isGrounded ? 1f : airControlPercent;
        Vector3 currentHorizontalVelocity = Vector3.ProjectOnPlane(rb.velocity, Vector3.up);
        float targetMoveSpeed = moveSpeed * (IsSprinting ? Mathf.Max(1f, sprintMultiplier) : 1f);
        Vector3 targetHorizontalVelocity = moveDirection * targetMoveSpeed * control;
        float response = moveDirection.sqrMagnitude > 0.001f ? acceleration : braking;
        Vector3 nextHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            response * control * Time.fixedDeltaTime);

        if (externalVelocity.sqrMagnitude > 0.0001f)
        {
            nextHorizontalVelocity += Vector3.ProjectOnPlane(externalVelocity, Vector3.up);
            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, 10f * Time.fixedDeltaTime);
        }

        if (moveDirection.sqrMagnitude < 0.001f && nextHorizontalVelocity.magnitude < stopSpeed)
        {
            nextHorizontalVelocity = Vector3.zero;
        }

        float verticalVelocity = rb.velocity.y;
        if (isGrounded && inputDirection.sqrMagnitude < 0.001f && externalVelocity.sqrMagnitude < 0.0001f)
        {
            nextHorizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            rb.angularVelocity = Vector3.zero;
        }
        else if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundedDownForce;
        }

        rb.velocity = new Vector3(nextHorizontalVelocity.x, verticalVelocity, nextHorizontalVelocity.z);

        UpdateFacing(moveDirection);
    }

    // Refreshes and applies configuration or runtime state for apply knockback.
    public void ApplyKnockback(Vector3 direction, float horizontalStrength, float upwardStrength)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = -transform.forward;
        }

        direction.Normalize();
        externalVelocity = direction * Mathf.Max(0f, horizontalStrength);

        if (rb != null)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = Mathf.Max(velocity.y, Mathf.Max(0f, upwardStrength));
            rb.velocity = velocity;
        }
    }


    // Handles the teleport to respawn workflow.
    public void TeleportToRespawn(Vector3 position, Quaternion rotation)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        isClimbing = false;
        jumpQueued = false;
        lastJumpPressedTime = -999f;
        lastGroundedTime = Time.time;
        externalVelocity = Vector3.zero;
        planarSpeed = 0f;
        previousPosition = position;
        lastSafeGroundedPosition = position;
        lastSafeGroundedTime = Time.time;

        transform.SetPositionAndRotation(position, rotation);
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
            rb.Sleep();
        }

        if (cameraFollow != null)
        {
            cameraFollow.SnapToTarget();
        }
    }
    // Finds, loads, or caches the references needed for resolve move direction.
    private Vector3 ResolveMoveDirection(Vector3 inputDirection)
    {
        if (inputDirection.sqrMagnitude < 0.001f)
        {
            return Vector3.zero;
        }

        if (mainCamera == null)
        {
            return inputDirection.normalized;
        }

        Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(mainCamera.right, Vector3.up).normalized;

        if (cameraForward.sqrMagnitude < 0.001f || cameraRight.sqrMagnitude < 0.001f)
        {
            return inputDirection.normalized;
        }

        return (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
    }

    // Refreshes and applies configuration or runtime state for update facing.
    private void UpdateFacing(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothedRotation);
        }
    }

    // Refreshes and applies configuration or runtime state for apply jump.
    private void ApplyJump()
    {
        if (!jumpQueued && Time.time - lastJumpPressedTime > jumpBufferTime)
        {
            return;
        }

        if (Time.time - lastGroundedTime <= coyoteTime)
        {
            if (enableClimb && TryStartClimb())
            {
                jumpQueued = false;
                lastJumpPressedTime = -999f;
                return;
            }

            Vector3 velocity = rb.velocity;
            velocity.y = jumpForce;
            rb.velocity = velocity;
            isGrounded = false;
            lastGroundedTime = -999f;
        }

        jumpQueued = false;
        lastJumpPressedTime = -999f;
    }

    // Attempts to try start climb and returns whether the operation succeeded.
    private bool TryStartClimb()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 chestOrigin = transform.position + Vector3.up * 1.15f;
        if (!Physics.SphereCast(chestOrigin, capsule.radius * 0.55f, forward, out RaycastHit wallHit, climbCheckDistance, climbMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (wallHit.collider.transform.IsChildOf(transform))
        {
            return false;
        }

        Vector3 wallNormal = Vector3.ProjectOnPlane(wallHit.normal, Vector3.up).normalized;
        if (wallNormal.sqrMagnitude < 0.001f)
        {
            return false;
        }

        Vector3 climbDirection = -wallNormal;
        Vector3 topProbeOrigin = wallHit.point + climbDirection * (capsule.radius + climbForwardOffset) + Vector3.up * (climbMaxHeight + 0.35f);
        float topProbeDistance = climbMaxHeight + 1.2f;

        if (!Physics.Raycast(topProbeOrigin, Vector3.down, out RaycastHit topHit, topProbeDistance, climbMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (topHit.normal.y < 0.55f)
        {
            return false;
        }

        float ledgeHeight = topHit.point.y - transform.position.y;
        if (ledgeHeight < climbMinHeight || ledgeHeight > climbMaxHeight)
        {
            return false;
        }

        Vector3 targetPosition = new Vector3(topHit.point.x, topHit.point.y + 0.05f, topHit.point.z);
        if (!HasStandingRoom(targetPosition))
        {
            return false;
        }

        StartCoroutine(ClimbToLedge(targetPosition, climbDirection));
        return true;
    }

    // Calculates and returns the result for has standing room.
    private bool HasStandingRoom(Vector3 targetPosition)
    {
        float radius = capsule.radius * 0.9f;
        Vector3 bottom = targetPosition + Vector3.up * (radius + 0.08f);
        Vector3 top = targetPosition + Vector3.up * (capsule.height - radius);
        Collider[] hits = Physics.OverlapCapsule(bottom, top, radius, climbMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }

    // Handles the climb to ledge workflow.
    private IEnumerator ClimbToLedge(Vector3 targetPosition, Vector3 climbDirection)
    {
        isClimbing = true;
        jumpQueued = false;
        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(climbDirection, Vector3.up);
        Vector3 startPosition = transform.position;
        Vector3 hangPosition = new Vector3(startPosition.x, targetPosition.y - 0.65f, startPosition.z) + climbDirection * 0.18f;
        Vector3 pullPosition = targetPosition + Vector3.up * 0.08f;

        float elapsed = 0f;
        while (elapsed < climbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / climbDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            Vector3 firstHalf = Vector3.Lerp(startPosition, hangPosition, Mathf.Clamp01(eased * 2f));
            Vector3 secondHalf = Vector3.Lerp(hangPosition, pullPosition, Mathf.Clamp01((eased - 0.5f) * 2f));
            transform.position = t < 0.5f ? firstHalf : secondHalf;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);

            if (runAnimator != null)
            {
                runAnimator.SetClimbProgress(true, t);
            }

            yield return null;
        }

        transform.position = pullPosition;
        transform.rotation = targetRotation;
        rb.isKinematic = false;
        rb.velocity = Vector3.zero;

        if (runAnimator != null)
        {
            runAnimator.SetClimbProgress(false, 0f);
        }

        isClimbing = false;
    }

}
