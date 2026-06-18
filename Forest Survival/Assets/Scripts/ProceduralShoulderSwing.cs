using UnityEngine;

// procedural shoulder swing script that owns this feature's runtime behavior.
public class ProceduralShoulderSwing : MonoBehaviour
{
    public enum SwingStyle
    {
        OverheadChop,
        ForwardThrust
    }

    // Cached component or scene reference to avoid repeated lookups: animator.
    public Animator animator;
    // Important runtime data or configuration used by this component: shoulder.
    public Transform shoulder;
    // Important runtime data or configuration used by this component: forearm.
    public Transform forearm;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: windupSeconds.
    public float windupSeconds = 0.28f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: strikeSeconds.
    public float strikeSeconds = 0.34f;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: recoverSeconds.
    public float recoverSeconds = 0.24f;
    public Vector3 windupEuler = new Vector3(108f, 0f, 0f);
    public Vector3 strikeEuler = new Vector3(0f, 0f, 0f);
    public Vector3 forearmWindupEuler = new Vector3(42f, 0f, 0f);
    public Vector3 forearmStrikeEuler = new Vector3(0f, 0f, 0f);

    [Header("Overhead Chop Tuning")]
    public Vector3 overheadWindupEuler = new Vector3(76f, 0f, 0f);
    public Vector3 overheadStrikeEuler = new Vector3(0f, 0f, 0f);
    public Vector3 overheadForearmWindupEuler = new Vector3(24f, 0f, 0f);
    public Vector3 overheadForearmStrikeEuler = new Vector3(0f, 0f, 0f);

    // Spatial value used for positioning, rotation, scale, or collision math: baseLocalRotation.
    private Quaternion baseLocalRotation;
    // Spatial value used for positioning, rotation, scale, or collision math: forearmBaseLocalRotation.
    private Quaternion forearmBaseLocalRotation;
    // Spatial value used for positioning, rotation, scale, or collision math: restShoulderLocalRotation.
    private Quaternion restShoulderLocalRotation;
    // Spatial value used for positioning, rotation, scale, or collision math: restForearmLocalRotation.
    private Quaternion restForearmLocalRotation;
    // Timing value or timestamp used for cooldowns, delays, or progress checks: swingStartTime.
    private float swingStartTime = -999f;
    // Important runtime data or configuration used by this component: swinging.
    private bool swinging;
    // Runtime flag that drives control flow, UI state, or gameplay availability: hasRestPose.
    private bool hasRestPose;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        ResolveShoulder();
    }

    // Starts or stops the animation, audio, or gameplay flow for play.
    public void Play()
    {
        Play(SwingStyle.OverheadChop, 1f);
    }

    // Starts or stops the animation, audio, or gameplay flow for play.
    public void Play(SwingStyle style, float speedScale)
    {
        ResolveShoulder();
        if (shoulder == null)
        {
            return;
        }

        ConfigureStyle(style, speedScale);
        CaptureRestPoseIfNeeded();
        baseLocalRotation = restShoulderLocalRotation;
        forearmBaseLocalRotation = restForearmLocalRotation;
        shoulder.localRotation = baseLocalRotation;
        if (forearm != null)
        {
            forearm.localRotation = restForearmLocalRotation;
        }
        swingStartTime = Time.time;
        swinging = true;
    }

    // Refreshes and applies configuration or runtime state for configure style.
    private void ConfigureStyle(SwingStyle style, float speedScale)
    {
        float scale = Mathf.Max(0.2f, speedScale);
        if (style == SwingStyle.ForwardThrust)
        {
            windupSeconds = 0.26f / scale;
            strikeSeconds = 0.32f / scale;
            recoverSeconds = 0.24f / scale;
            windupEuler = new Vector3(-78f, -10f, -16f);
            strikeEuler = new Vector3(32f, 4f, 8f);
            forearmWindupEuler = new Vector3(-30f, 0f, 0f);
            forearmStrikeEuler = new Vector3(24f, 0f, 0f);
            return;
        }

        windupSeconds = 0.28f / scale;
        strikeSeconds = 0.34f / scale;
        recoverSeconds = 0.24f / scale;
        windupEuler = overheadWindupEuler;
        strikeEuler = overheadStrikeEuler;
        forearmWindupEuler = overheadForearmWindupEuler;
        forearmStrikeEuler = overheadForearmStrikeEuler;
    }

    // Unity lifecycle: synchronizes cameras, animation, or visuals after normal frame updates.
    private void LateUpdate()
    {
        if (!swinging || shoulder == null)
        {
            return;
        }

        float elapsed = Time.time - swingStartTime;
        float windup = Mathf.Max(0.01f, windupSeconds);
        float strike = Mathf.Max(0.01f, strikeSeconds);
        float recover = Mathf.Max(0.01f, recoverSeconds);

        if (elapsed < windup)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / windup);
            shoulder.localRotation = Quaternion.Slerp(baseLocalRotation, baseLocalRotation * Quaternion.Euler(windupEuler), t);
            ApplyForearmRotation(forearmBaseLocalRotation, forearmBaseLocalRotation * Quaternion.Euler(forearmWindupEuler), t);
            return;
        }

        elapsed -= windup;
        if (elapsed < strike)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / strike);
            shoulder.localRotation = Quaternion.Slerp(baseLocalRotation * Quaternion.Euler(windupEuler), baseLocalRotation * Quaternion.Euler(strikeEuler), t);
            ApplyForearmRotation(forearmBaseLocalRotation * Quaternion.Euler(forearmWindupEuler), forearmBaseLocalRotation * Quaternion.Euler(forearmStrikeEuler), t);
            return;
        }

        elapsed -= strike;
        if (elapsed < recover)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / recover);
            shoulder.localRotation = Quaternion.Slerp(baseLocalRotation * Quaternion.Euler(strikeEuler), baseLocalRotation, t);
            ApplyForearmRotation(forearmBaseLocalRotation * Quaternion.Euler(forearmStrikeEuler), forearmBaseLocalRotation, t);
            return;
        }

        shoulder.localRotation = baseLocalRotation;
        if (forearm != null)
        {
            forearm.localRotation = forearmBaseLocalRotation;
        }
        swinging = false;
    }

    // Handles the reset pose workflow.
    public void ResetPose()
    {
        ResolveShoulder();
        CaptureRestPoseIfNeeded();
        if (shoulder != null)
        {
            shoulder.localRotation = restShoulderLocalRotation;
        }

        if (forearm != null)
        {
            forearm.localRotation = restForearmLocalRotation;
        }

        swinging = false;
    }

    // Refreshes and applies configuration or runtime state for apply forearm rotation.
    private void ApplyForearmRotation(Quaternion from, Quaternion to, float t)
    {
        if (forearm != null)
        {
            forearm.localRotation = Quaternion.Slerp(from, to, t);
        }
    }

    // Finds, loads, or caches the references needed for resolve shoulder.
    private void ResolveShoulder()
    {
        if (shoulder != null)
        {
            return;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null && animator.isHuman)
        {
            shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            if (shoulder == null)
            {
                shoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            }
        }

        if (shoulder == null)
        {
            shoulder = FindLikelyRightArm(transform);
        }

        CaptureRestPoseIfNeeded();
    }

    // Handles the capture rest pose if needed workflow.
    private void CaptureRestPoseIfNeeded()
    {
        if (hasRestPose || shoulder == null)
        {
            return;
        }

        restShoulderLocalRotation = shoulder.localRotation;
        restForearmLocalRotation = forearm != null ? forearm.localRotation : Quaternion.identity;
        hasRestPose = true;
    }

    // Finds, loads, or caches the references needed for find likely right arm.
    private static Transform FindLikelyRightArm(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform best = null;
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            string name = transforms[i].name.ToLowerInvariant();
            bool rightSide = name.Contains("right") || name.Contains("_r") || name.Contains(".r") || name.Contains(" r ");
            bool armBone = name.Contains("upperarm") || name.Contains("arm") || name.Contains("shoulder");
            if (rightSide && armBone)
            {
                if (name.Contains("upper") || name.Contains("shoulder"))
                {
                    return transforms[i];
                }

                best = transforms[i];
            }
        }

        return best;
    }
}




