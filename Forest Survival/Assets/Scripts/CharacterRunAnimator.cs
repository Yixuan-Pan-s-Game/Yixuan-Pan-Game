using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
// character run animator script that owns this feature's runtime behavior.
public class CharacterRunAnimator : MonoBehaviour
{
    // Cached component or scene reference to avoid repeated lookups: visualRoot.
    public Transform visualRoot;
    // Cached component or scene reference to avoid repeated lookups: animator.
    public Animator animator;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: crossFadeDuration.
    public float crossFadeDuration = 0.12f;
    // Important runtime data or configuration used by this component: runThreshold.
    public float runThreshold = 0.15f;

    // Important runtime data or configuration used by this component: rb.
    private Rigidbody rb;
    // Important runtime data or configuration used by this component: movement.
    private PlayerMovement movement;
    // Runtime flag that drives control flow, UI state, or gameplay availability: isClimbing.
    private bool isClimbing;
    // Cached component or scene reference to avoid repeated lookups: currentAnimatorState.
    private string currentAnimatorState;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: actionLockUntil.
    private float actionLockUntil;
    // Identifier or category used for lookup, routing, or state selection: actionState.
    private string actionState;

    // Identifier or category used for lookup, routing, or state selection: IdleState.
    private const string IdleState = "Idle";
    // Identifier or category used for lookup, routing, or state selection: WalkState.
    private const string WalkState = "Walk";
    // Identifier or category used for lookup, routing, or state selection: WalkingState.
    private const string WalkingState = "Walking";
    // Identifier or category used for lookup, routing, or state selection: JumpState.
    private const string JumpState = "Jump";
    // Layer or mask filter used by physics queries or rendering: BaseLayer.
    private const int BaseLayer = 0;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        CacheAnimator();
    }

    // Unity lifecycle: synchronizes cameras, animation, or visuals after normal frame updates.
    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(actionState))
        {
            if (Time.time < actionLockUntil)
            {
                return;
            }

            actionState = null;
            currentAnimatorState = null;
        }

        UpdateAnimatorState();
    }

    // Sets state, selection, or placement data for set visual root.
    public void SetVisualRoot(Transform root)
    {
        visualRoot = root;
        CacheAnimator();
    }

    // Finds, loads, or caches the references needed for resolve visual root.
    public Transform ResolveVisualRoot()
    {
        return visualRoot;
    }

    // Sets state, selection, or placement data for set animator.
    public void SetAnimator(Animator characterAnimator)
    {
        animator = characterAnimator;
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    // Sets state, selection, or placement data for set climb progress.
    public void SetClimbProgress(bool climbing, float progress)
    {
        isClimbing = climbing;
        if (climbing)
        {
            CrossFadeAnimatorState(JumpState);
        }
    }

    // Sets state, selection, or placement data for set hold pose.
    public void SetHoldPose(ToolHoldPose pose)
    {
        currentAnimatorState = null;
    }

    // Starts or stops the animation, audio, or gameplay flow for play slash.
    public void PlaySlash(float duration = 0.75f)
    {
        PlayActionState("Slash", duration);
    }

    // Starts or stops the animation, audio, or gameplay flow for play harvest downward.
    public void PlayHarvestDownward(float duration = 0.85f)
    {
        PlayActionState("Standing Melee Attack Downward", duration);
    }

    // Starts or stops the animation, audio, or gameplay flow for play action state.
    private void PlayActionState(string stateName, float duration)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(stateName))
        {
            return;
        }

        if (!animator.HasState(BaseLayer, Animator.StringToHash(stateName)))
        {
            return;
        }

        actionState = stateName;
        actionLockUntil = Time.time + Mathf.Max(0.02f, duration);
        currentAnimatorState = stateName;
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration, BaseLayer);
    }

    // Refreshes and applies configuration or runtime state for update animator state.
    private void UpdateAnimatorState()
    {
        if (isClimbing || (movement != null && !movement.IsGrounded))
        {
            CrossFadeAnimatorState(JumpState);
            return;
        }

        float horizontalSpeed = movement != null
            ? movement.PlanarSpeed
            : rb != null ? Vector3.ProjectOnPlane(rb.velocity, Vector3.up).magnitude : 0f;
        bool isMoving = horizontalSpeed >= runThreshold;
        string stateName = isMoving
            ? ResolveAnimatorState(WalkState, WalkingState)
            : ResolveAnimatorState(IdleState, WalkState, WalkingState);

        CrossFadeAnimatorState(stateName);
    }

    // Handles the cross fade animator state workflow.
    private void CrossFadeAnimatorState(string stateName)
    {
        if (animator == null
            || string.IsNullOrEmpty(stateName)
            || currentAnimatorState == stateName
            || !animator.HasState(BaseLayer, Animator.StringToHash(stateName)))
        {
            return;
        }

        currentAnimatorState = stateName;
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration, BaseLayer);
    }

    // Finds, loads, or caches the references needed for resolve animator state.
    private string ResolveAnimatorState(params string[] stateNames)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return null;
        }

        for (int i = 0; i < stateNames.Length; i++)
        {
            string stateName = stateNames[i];
            if (!string.IsNullOrEmpty(stateName) && animator.HasState(BaseLayer, Animator.StringToHash(stateName)))
            {
                return stateName;
            }
        }

        return null;
    }

    // Finds, loads, or caches the references needed for cache animator.
    private void CacheAnimator()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.Find("CharacterVisual");
        }

        if (animator == null)
        {
            animator = visualRoot != null ? visualRoot.GetComponentInChildren<Animator>(true) : GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }
}
