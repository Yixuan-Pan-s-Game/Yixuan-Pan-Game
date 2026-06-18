using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
// Enemy behavior controller that handles targeting, movement, attacks, hit timing, and death flow.
public class MutantEnemyAI : MonoBehaviour
{
    // Cached component or scene reference to avoid repeated lookups: animator.
    public Animator animator;
    // Current interaction target or gameplay object being processed: target.
    public Transform target;
    // Distance or radius used for detection, interaction, or physics checks: patrolDistance.
    public float patrolDistance = 5f;
    // Distance or radius used for detection, interaction, or physics checks: detectRange.
    public float detectRange = 11f;
    // Distance or radius used for detection, interaction, or physics checks: attackRange.
    public float attackRange = 2.1f;
    // Speed or movement tuning value: walkSpeed.
    public float walkSpeed = 0.9f;
    // Speed or movement tuning value: runSpeed.
    public float runSpeed = 3.2f;
    // Speed or movement tuning value: turnSpeed.
    public float turnSpeed = 520f;
    // Layer or mask filter used by physics queries or rendering: groundMask.
    public LayerMask groundMask = ~0;
    // Distance or radius used for detection, interaction, or physics checks: groundProbeHeight.
    public float groundProbeHeight = 2.5f;
    // Distance or radius used for detection, interaction, or physics checks: groundProbeDistance.
    public float groundProbeDistance = 5f;
    // Distance or radius used for detection, interaction, or physics checks: groundedDistance.
    public float groundedDistance = 0.18f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: groundedDownForce.
    public float groundedDownForce = 3f;
    // Speed or movement tuning value: gravityAcceleration.
    public float gravityAcceleration = 32f;
    // Speed or movement tuning value: maxFallSpeed.
    public float maxFallSpeed = 18f;
    // Important runtime data or configuration used by this component: groundSkin.
    public float groundSkin = 0.03f;
    // Distance or radius used for detection, interaction, or physics checks: groundSnapDistance.
    public float groundSnapDistance = 1.15f;
    // Speed or movement tuning value: groundSnapSpeed.
    public float groundSnapSpeed = 12f;
    // Important runtime data or configuration used by this component: maxGroundAngle.
    public float maxGroundAngle = 58f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: attackCooldown.
    public float attackCooldown = 1.6f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: attackLockSeconds.
    public float attackLockSeconds = 0.95f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: hitDelay.
    public float hitDelay = 0.42f;
    // Gameplay stat that affects damage, health, healing, defense, or durability: punchDamage.
    public int punchDamage = 14;
    // Important runtime data or configuration used by this component: knockbackStrength.
    public float knockbackStrength = 0.95f;
    // Important runtime data or configuration used by this component: knockbackUpward.
    public float knockbackUpward = 0.28f;
    // Cached component or scene reference to avoid repeated lookups: visualRoot.
    public Transform visualRoot;
    // Spatial value used for positioning, rotation, scale, or collision math: visualGroundOffset.
    public float visualGroundOffset = 0f;
    // Cached component or scene reference to avoid repeated lookups: keepVisualRootAtBodyRoot.
    public bool keepVisualRootAtBodyRoot = false;
    // Runtime flag that drives control flow, UI state, or gameplay availability: alignVisibleFeetToGround.
    public bool alignVisibleFeetToGround = false;
    public Color skinColor = new Color(0.34f, 0.48f, 0.28f);
    public Color darkSkinColor = new Color(0.18f, 0.26f, 0.16f);
    // Identifier or category used for lookup, routing, or state selection: walkState.
    public string walkState = "Walk";
    // Identifier or category used for lookup, routing, or state selection: runState.
    public string runState = "Run";
    // Identifier or category used for lookup, routing, or state selection: punchState.
    public string punchState = "Punch";
    // Identifier or category used for lookup, routing, or state selection: dyingState.
    public string dyingState = "Dying";
    // Timing value or timestamp used for cooldowns, delays, or progress checks: deathDisappearDelay.
    public float deathDisappearDelay = 2.6f;

    // Important runtime data or configuration used by this component: body.
    private Rigidbody body;
    // Important runtime data or configuration used by this component: capsule.
    private CapsuleCollider capsule;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: runtimeSkinMaterial.
    private Material runtimeSkinMaterial;
    // Important runtime data or configuration used by this component: patrolOrigin.
    private Vector3 patrolOrigin;
    // Important runtime data or configuration used by this component: patrolA.
    private Vector3 patrolA;
    // Important runtime data or configuration used by this component: patrolB.
    private Vector3 patrolB;
    // Current interaction target or gameplay object being processed: patrolTarget.
    private Vector3 patrolTarget;
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
    // Spatial value used for positioning, rotation, scale, or collision math: groundNormal.
    private Vector3 groundNormal = Vector3.up;
    // Runtime flag that drives control flow, UI state, or gameplay availability: hasGround.
    private bool hasGround;
    // Runtime flag that drives control flow, UI state, or gameplay availability: isGrounded.
    private bool isGrounded;
    // Cached component or scene reference to avoid repeated lookups: targetRootY.
    private float targetRootY;
    // Important runtime data or configuration used by this component: groundY.
    private float groundY;
    // Runtime flag that drives control flow, UI state, or gameplay availability: dead.
    private bool dead;

    // Layer or mask filter used by physics queries or rendering: BaseLayer.
    private const int BaseLayer = 0;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        ConfigureBody();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (visualRoot == null)
            {
                visualRoot = animator.transform;
            }
        }

        ApplyFallbackMaterial();
        CacheVisualGroundOffset();

        patrolOrigin = transform.position;
        Vector3 right = transform.right;
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }

        right.Normalize();
        patrolA = patrolOrigin - right * Mathf.Max(0.1f, patrolDistance * 0.5f);
        patrolB = patrolOrigin + right * Mathf.Max(0.1f, patrolDistance * 0.5f);
        patrolTarget = patrolB;
    }

    // Unity lifecycle: updates physics, rigidbody movement, and collision state on the fixed timestep.
    private void FixedUpdate()
    {
        if (dead)
        {
            StopHorizontalMotion();
            return;
        }

        ConfigureBody();
        UpdateGrounding();
        StabilizeGroundContact();
        Transform chaseTarget = ResolveTarget();
        if (chaseTarget != null)
        {
            Vector3 toTarget = chaseTarget.position - body.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= detectRange)
            {
                HandleChase(chaseTarget, toTarget, distance);
                ResolvePendingHit(chaseTarget);
                return;
            }
        }

        hasPendingHit = false;
        Patrol();
    }

    // Refreshes and applies configuration or runtime state for configure body.
    private void ConfigureBody()
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (capsule == null)
        {
            capsule = GetComponent<CapsuleCollider>();
        }

        if (body == null)
        {
            return;
        }

        body.isKinematic = false;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.WakeUp();
    }

    // Unity lifecycle: synchronizes cameras, animation, or visuals after normal frame updates.
    private void LateUpdate()
    {
        KeepVisualRootGrounded();
        AlignVisibleFeetToGround();
    }

    // Handles the handle chase workflow.
    private void HandleChase(Transform chaseTarget, Vector3 toTarget, float distance)
    {
        FaceDirection(toTarget);

        if (Time.time < attackLockedUntil)
        {
            StopHorizontalMotion();
            PlayState(punchState);
            return;
        }

        if (distance <= attackRange)
        {
            TryPunch(chaseTarget);
            return;
        }

        Vector3 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
        MoveAlongGround(direction, runSpeed);
        PlayState(runState);
    }

    // Handles the patrol workflow.
    private void Patrol()
    {
        Vector3 toPatrol = patrolTarget - body.position;
        toPatrol.y = 0f;
        if (toPatrol.magnitude <= 0.35f)
        {
            patrolTarget = Vector3.Distance(patrolTarget, patrolA) < 0.1f ? patrolB : patrolA;
            toPatrol = patrolTarget - body.position;
            toPatrol.y = 0f;
        }

        if (toPatrol.sqrMagnitude > 0.001f)
        {
            Vector3 direction = toPatrol.normalized;
            FaceDirection(direction);
            MoveAlongGround(direction, walkSpeed);
        }

        PlayState(walkState);
    }

    // Handles the stabilize ground contact workflow.
    private void StabilizeGroundContact()
    {
        if (!hasGround || body == null)
        {
            return;
        }

        float delta = targetRootY - body.position.y;
        bool belowGround = delta > 0f;
        bool closeEnoughToSnap = Mathf.Abs(delta) <= groundSnapDistance;
        if (!belowGround && !closeEnoughToSnap)
        {
            return;
        }

        Vector3 position = body.position;
        position.y = belowGround
            ? targetRootY
            : Mathf.MoveTowards(position.y, targetRootY, groundSnapSpeed * Time.fixedDeltaTime);

        body.position = position;
        transform.position = position;

        Vector3 velocity = body.velocity;
        if (velocity.y < 0f || belowGround)
        {
            velocity.y = -groundedDownForce;
            body.velocity = velocity;
        }

        isGrounded = Mathf.Abs(targetRootY - body.position.y) <= groundedDistance;
        Physics.SyncTransforms();
    }

    // Handles the move along ground workflow.
    private void MoveAlongGround(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();
        if (isGrounded)
        {
            direction = Vector3.ProjectOnPlane(direction, groundNormal).normalized;
        }

        Vector3 horizontalVelocity = direction * speed;
        Vector3 velocity = body.velocity;
        float verticalVelocity = ResolveVerticalVelocity(velocity.y);
        body.velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
    }

    // Finds, loads, or caches the references needed for resolve vertical velocity.
    private float ResolveVerticalVelocity(float currentVerticalVelocity)
    {
        if (isGrounded && currentVerticalVelocity < 0f)
        {
            return -groundedDownForce;
        }

        if (!isGrounded)
        {
            return Mathf.MoveTowards(
                currentVerticalVelocity,
                -Mathf.Abs(maxFallSpeed),
                Mathf.Abs(gravityAcceleration) * Time.fixedDeltaTime);
        }

        return currentVerticalVelocity;
    }

    // Refreshes and applies configuration or runtime state for update grounding.
    private void UpdateGrounding()
    {
        hasGround = false;
        isGrounded = false;
        groundNormal = Vector3.up;

        Vector3 origin = body.position + Vector3.up * Mathf.Max(0.1f, groundProbeHeight);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundProbeDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) > maxGroundAngle)
            {
                continue;
            }

            hasGround = true;
            groundY = hit.point.y;
            targetRootY = hit.point.y + GetRootToCapsuleBottomOffset() + Mathf.Max(0f, groundSkin);
            isGrounded = Mathf.Abs(body.position.y - targetRootY) <= groundedDistance;
            groundNormal = hit.normal;
            return;
        }
    }

    // Calculates and returns the result for get root to capsule bottom offset.
    private float GetRootToCapsuleBottomOffset()
    {
        if (capsule == null)
        {
            return 0f;
        }

        float height = Mathf.Max(capsule.height, capsule.radius * 2f);
        return (height * 0.5f) - capsule.center.y;
    }

    // Starts or stops the animation, audio, or gameplay flow for stop horizontal motion.
    private void StopHorizontalMotion()
    {
        Vector3 velocity = body.velocity;
        body.velocity = new Vector3(0f, ResolveVerticalVelocity(velocity.y), 0f);
    }

    // Attempts to try punch and returns whether the operation succeeded.
    private void TryPunch(Transform chaseTarget)
    {
        if (Time.time < nextAttackTime)
        {
            StopHorizontalMotion();
            PlayState(walkState);
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        attackLockedUntil = Time.time + attackLockSeconds;
        pendingHitTime = Time.time + Mathf.Clamp(hitDelay, 0.05f, attackLockSeconds);
        hasPendingHit = true;
        PlayState(punchState, true);
        ResolvePendingHit(chaseTarget);
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
        StopHorizontalMotion();
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        if (animator != null && animator.HasState(BaseLayer, Animator.StringToHash(dyingState)))
        {
            animator.Play(dyingState, BaseLayer, 0f);
        }

        Destroy(gameObject, deathDisappearDelay);
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
        if (toTarget.magnitude > attackRange + 0.65f)
        {
            return;
        }

        PlayerHealth health = chaseTarget.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(punchDamage);
        }

        PlayerMovement movement = chaseTarget.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            Vector3 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
            movement.ApplyKnockback(direction, knockbackStrength, knockbackUpward);
        }
        else
        {
            Rigidbody targetBody = chaseTarget.GetComponent<Rigidbody>();
            if (targetBody != null && !targetBody.isKinematic)
            {
                Vector3 direction = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : transform.forward;
                targetBody.AddForce((direction * knockbackStrength) + Vector3.up * knockbackUpward, ForceMode.VelocityChange);
            }
        }
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

    // Handles the face direction workflow.
    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        body.MoveRotation(Quaternion.RotateTowards(body.rotation, lookRotation, turnSpeed * Time.fixedDeltaTime));
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

    // Finds, loads, or caches the references needed for cache visual ground offset.
    private void CacheVisualGroundOffset()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (keepVisualRootAtBodyRoot)
        {
            visualGroundOffset = 0f;
            Vector3 localPosition = visualRoot.localPosition;
            localPosition.y = 0f;
            visualRoot.localPosition = localPosition;
            return;
        }

        visualGroundOffset = visualRoot.localPosition.y;
    }

    // Handles the keep visual root grounded workflow.
    private void KeepVisualRootGrounded()
    {
        if (visualRoot == null)
        {
            return;
        }

        Vector3 localPosition = visualRoot.localPosition;
        localPosition.y = visualGroundOffset;
        visualRoot.localPosition = localPosition;
    }

    // Handles the align visible feet to ground workflow.
    private void AlignVisibleFeetToGround()
    {
        if (!alignVisibleFeetToGround || visualRoot == null || !hasGround)
        {
            return;
        }

        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = default;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        if (!found)
        {
            return;
        }

        float targetFeetY = groundY + groundSkin;
        float correction = targetFeetY - bounds.min.y;
        if (Mathf.Abs(correction) < 0.001f || Mathf.Abs(correction) > 2.5f)
        {
            return;
        }

        Vector3 position = visualRoot.position;
        position.y += correction;
        visualRoot.position = position;
        visualGroundOffset = visualRoot.localPosition.y;
    }

    // Refreshes and applies configuration or runtime state for apply fallback material.
    private void ApplyFallbackMaterial()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        runtimeSkinMaterial = CreateRuntimeMaterial();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = runtimeSkinMaterial;
                continue;
            }

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] == null || IsPlainWhiteMaterial(materials[j]))
                {
                    materials[j] = runtimeSkinMaterial;
                }
            }

            renderer.sharedMaterials = materials;
        }
    }

    // Creates or rebuilds the runtime objects, assets, or UI for create runtime material.
    private Material CreateRuntimeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.name = "MutantFallbackSkin_Runtime";
        SetMaterialColor(material, skinColor);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", skinColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", skinColor);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.28f);
        }

        return material;
    }

    // Sets state, selection, or placement data for set material color.
    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;
    }

    // Calculates and returns the result for is plain white material.
    private static bool IsPlainWhiteMaterial(Material material)
    {
        if (material == null)
        {
            return true;
        }

        Texture texture = null;
        if (material.HasProperty("_BaseMap"))
        {
            texture = material.GetTexture("_BaseMap");
        }

        if (texture == null && material.HasProperty("_MainTex"))
        {
            texture = material.GetTexture("_MainTex");
        }

        Color color = material.HasProperty("_BaseColor")
            ? material.GetColor("_BaseColor")
            : material.color;

        return texture == null && color.r > 0.82f && color.g > 0.82f && color.b > 0.82f;
    }
}
