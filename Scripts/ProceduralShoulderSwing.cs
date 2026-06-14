using UnityEngine;

public class ProceduralShoulderSwing : MonoBehaviour
{
    public enum SwingStyle
    {
        OverheadChop,
        ForwardThrust
    }

    public Animator animator;
    public Transform shoulder;
    public Transform forearm;
    public float windupSeconds = 0.28f;
    public float strikeSeconds = 0.34f;
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

    private Quaternion baseLocalRotation;
    private Quaternion forearmBaseLocalRotation;
    private Quaternion restShoulderLocalRotation;
    private Quaternion restForearmLocalRotation;
    private float swingStartTime = -999f;
    private bool swinging;
    private bool hasRestPose;

    private void Awake()
    {
        ResolveShoulder();
    }

    public void Play()
    {
        Play(SwingStyle.OverheadChop, 1f);
    }

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

    private void ApplyForearmRotation(Quaternion from, Quaternion to, float t)
    {
        if (forearm != null)
        {
            forearm.localRotation = Quaternion.Slerp(from, to, t);
        }
    }

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




