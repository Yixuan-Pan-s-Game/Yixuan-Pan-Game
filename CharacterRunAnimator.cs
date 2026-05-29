using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CharacterRunAnimator : MonoBehaviour
{
    public Transform visualRoot;
    public Animator animator;
    public float crossFadeDuration = 0.12f;
    public float fireAnimationDuration = 0.42f;
    public float runThreshold = 0.15f;

    private Rigidbody rb;
    private PlayerMovement movement;
    private ToolHoldPose holdPose = ToolHoldPose.OneHandTool;
    private bool isClimbing;
    private string currentAnimatorState;
    private float actionLockUntil;
    private string actionState;

    private const string IdleState = "Idle";
    private const string WalkState = "Walk";
    private const string WalkingState = "Walking";
    private const string JumpState = "Jump";
    private const int BaseLayer = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        CacheAnimator();
    }

    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            CacheAnimator();
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

    public void SetVisualRoot(Transform root)
    {
        visualRoot = root;
        CacheAnimator();
    }

    public Transform ResolveVisualRoot()
    {
        CacheAnimator();
        return visualRoot;
    }

    public void SetAnimator(Animator characterAnimator)
    {
        animator = characterAnimator;
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    public void SetClimbProgress(bool climbing, float progress)
    {
        isClimbing = climbing;
        if (climbing) CrossFadeAnimatorState(JumpState);
    }

    public void SetHoldPose(ToolHoldPose pose)
    {
        holdPose = pose;
        currentAnimatorState = null;
    }

    public void PlaySlash(float duration = 0.75f)
    {
        PlayActionState("Slash", duration);
    }

    public void PlayAxeChop(float duration = 0.85f)
    {
        PlayActionState("AxeChop", duration);
    }

    private void PlayActionState(string stateName, float duration)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            CacheAnimator();
        }

        if (animator == null
            || string.IsNullOrEmpty(stateName)
            || !animator.HasState(BaseLayer, Animator.StringToHash(stateName)))
        {
            return;
        }

        actionState = stateName;
        actionLockUntil = Time.time + Mathf.Max(0.02f, duration);

        if (currentAnimatorState == stateName)
        {
            animator.Play(stateName, BaseLayer, 0f);
            return;
        }

        currentAnimatorState = stateName;
        animator.CrossFadeInFixedTime(stateName, crossFadeDuration, BaseLayer);
    }

    private void UpdateAnimatorState()
    {
        float horizontalSpeed = movement != null ? movement.PlanarSpeed : rb != null ? Vector3.ProjectOnPlane(rb.velocity, Vector3.up).magnitude : 0f;
        bool isMoving = horizontalSpeed >= runThreshold;
        bool isGrounded = movement == null || movement.IsGrounded;

        if (isClimbing)
        {
            CrossFadeAnimatorState(JumpState);
            return;
        }

        if (!isGrounded)
        {
            CrossFadeAnimatorState(JumpState);
            return;
        }

        string stateName = isMoving ? ResolveAnimatorState(WalkState, WalkingState) : ResolveAnimatorState(IdleState, WalkState, WalkingState);
        CrossFadeAnimatorState(stateName);
    }

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

    private void CacheAnimator()
    {
        Transform resolvedRoot = FindBestVisualRoot(transform);
        Transform namedVisual = transform.Find("CharacterVisual");
        if (HasCharacterVisual(namedVisual))
        {
            visualRoot = namedVisual;
        }
        else if (visualRoot == null || !HasCharacterVisual(visualRoot))
        {
            visualRoot = resolvedRoot;
        }
        else if (resolvedRoot != null && animator == null)
        {
            Animator resolvedAnimator = resolvedRoot.GetComponentInChildren<Animator>(true);
            if (resolvedAnimator != null)
            {
                visualRoot = resolvedRoot;
            }
        }

        if (visualRoot != null)
        {
            Animator visualAnimator = visualRoot.GetComponentInChildren<Animator>(true);
            if (visualAnimator != null)
            {
                animator = visualAnimator;
            }
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
    }

    private static Transform FindBestVisualRoot(Transform owner)
    {
        if (owner == null)
        {
            return null;
        }

        Transform namedVisual = owner.Find("CharacterVisual");
        if (HasCharacterVisual(namedVisual))
        {
            return namedVisual;
        }

        Animator childAnimator = owner.GetComponentInChildren<Animator>(true);
        if (childAnimator != null)
        {
            Transform animatorRoot = GetTopChildUnder(owner, childAnimator.transform);
            return animatorRoot != null ? animatorRoot : childAnimator.transform;
        }

        Renderer childRenderer = owner.GetComponentInChildren<Renderer>(true);
        if (childRenderer != null)
        {
            Transform rendererRoot = GetTopChildUnder(owner, childRenderer.transform);
            return rendererRoot != null ? rendererRoot : childRenderer.transform;
        }

        return namedVisual;
    }

    private static bool HasCharacterVisual(Transform root)
    {
        return root != null
            && (root.GetComponentInChildren<Animator>(true) != null
                || root.GetComponentInChildren<Renderer>(true) != null);
    }

    private static Transform GetTopChildUnder(Transform owner, Transform child)
    {
        if (owner == null || child == null)
        {
            return null;
        }

        if (child == owner)
        {
            return owner;
        }

        Transform current = child;
        while (current.parent != null && current.parent != owner)
        {
            current = current.parent;
        }

        return current.parent == owner ? current : null;
    }
}
