using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class MutantEnemyAI : MonoBehaviour
{
    public Animator animator;
    public Transform target;
    public float patrolDistance = 5f;
    public float detectRange = 11f;
    public float attackRange = 2.1f;
    public float walkSpeed = 0.9f;
    public float runSpeed = 3.2f;
    public float turnSpeed = 520f;
    public LayerMask groundMask = ~0;
    public float groundProbeHeight = 2.5f;
    public float groundProbeDistance = 5f;
    public float groundedDistance = 0.18f;
    public float groundedDownForce = 3f;
    public float gravityAcceleration = 32f;
    public float maxFallSpeed = 18f;
    public float groundSkin = 0.03f;
    public float groundSnapDistance = 1.15f;
    public float groundSnapSpeed = 12f;
    public float maxGroundAngle = 58f;
    public float attackCooldown = 1.6f;
    public float attackLockSeconds = 0.95f;
    public float hitDelay = 0.42f;
    public int punchDamage = 14;
    public float knockbackStrength = 0.95f;
    public float knockbackUpward = 0.28f;
    public Transform visualRoot;
    public float visualGroundOffset = 0f;
    public bool keepVisualRootAtBodyRoot = false;
    public bool alignVisibleFeetToGround = false;
    public Color skinColor = new Color(0.34f, 0.48f, 0.28f);
    public Color darkSkinColor = new Color(0.18f, 0.26f, 0.16f);
    public string walkState = "Walk";
    public string runState = "Run";
    public string punchState = "Punch";
    public string dyingState = "Dying";
    public float deathDisappearDelay = 2.6f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private Material runtimeSkinMaterial;
    private Vector3 patrolOrigin;
    private Vector3 patrolA;
    private Vector3 patrolB;
    private Vector3 patrolTarget;
    private float nextAttackTime;
    private float attackLockedUntil;
    private float pendingHitTime;
    private bool hasPendingHit;
    private string currentState;
    private Vector3 groundNormal = Vector3.up;
    private bool hasGround;
    private bool isGrounded;
    private float targetRootY;
    private float groundY;
    private bool dead;

    private const int BaseLayer = 0;

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

    private void LateUpdate()
    {
        KeepVisualRootGrounded();
        AlignVisibleFeetToGround();
    }

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

    private float GetRootToCapsuleBottomOffset()
    {
        if (capsule == null)
        {
            return 0f;
        }

        float height = Mathf.Max(capsule.height, capsule.radius * 2f);
        return (height * 0.5f) - capsule.center.y;
    }

    private void StopHorizontalMotion()
    {
        Vector3 velocity = body.velocity;
        body.velocity = new Vector3(0f, ResolveVerticalVelocity(velocity.y), 0f);
    }

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

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;
    }

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
