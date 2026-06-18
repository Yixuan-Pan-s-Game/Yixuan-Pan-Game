using UnityEngine;

// Third-person camera controller for orbit movement, smoothing, cursor lock, and target lock-on.
public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    // Current interaction target or gameplay object being processed: target.
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.35f, 0f);
    public Vector3 lookOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Orbit")]
    // Distance or radius used for detection, interaction, or physics checks: distance.
    public float distance = 4.5f;
    // Distance or radius used for detection, interaction, or physics checks: minDistance.
    public float minDistance = 2.6f;
    // Distance or radius used for detection, interaction, or physics checks: maxDistance.
    public float maxDistance = 7f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: mouseSensitivityX.
    public float mouseSensitivityX = 3.2f;
    // Runtime flag that drives control flow, UI state, or gameplay availability: mouseSensitivityY.
    public float mouseSensitivityY = 2.2f;
    // Important runtime data or configuration used by this component: minPitch.
    public float minPitch = -28f;
    // Important runtime data or configuration used by this component: maxPitch.
    public float maxPitch = 68f;

    [Header("Smoothing")]
    // Timing value or timestamp used for cooldowns, delays, or progress checks: positionSmoothTime.
    public float positionSmoothTime = 0.12f;
    // Speed or movement tuning value: rotationSmoothSpeed.
    public float rotationSmoothSpeed = 12f;
    // Speed or movement tuning value: lockFollowSpeed.
    public float lockFollowSpeed = 7f;

    [Header("Lock On")]
    // Distance or radius used for detection, interaction, or physics checks: lockMaxDistance.
    public float lockMaxDistance = 60f;
    // Input setting or cached input value read from player controls: freeCursorKey.
    public KeyCode freeCursorKey = KeyCode.LeftAlt;
    // Input setting or cached input value read from player controls: clearLockKey.
    public KeyCode clearLockKey = KeyCode.Mouse1;

    // Speed or movement tuning value: positionVelocity.
    private Vector3 positionVelocity;
    // Important runtime data or configuration used by this component: yaw.
    private float yaw;
    // Important runtime data or configuration used by this component: pitch.
    private float pitch = 24f;
    // Current interaction target or gameplay object being processed: lockedTarget.
    private Transform lockedTarget;

    // Read-only state exposed to other systems: LockedTarget.
    public Transform LockedTarget => lockedTarget;

    // Unity lifecycle: resolves scene dependencies and performs the first full refresh after scene startup.
    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
        SetCursorLocked(true);
        SnapToTarget();
    }

    // Unity lifecycle: reads input and updates non-physics state once per frame.
    private void Update()
    {
        if (target == null)
        {
            return;
        }

        if (BackpackUI.IsAnyOpen)
        {
            return;
        }

        if (ForestMenuUI.IsMenuOpen)
        {
            SetCursorLocked(false);
            return;
        }

        bool freeCursorHeld = Input.GetKey(freeCursorKey);
        SetCursorLocked(!freeCursorHeld);

        if (freeCursorHeld)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryLockTargetFromMouse();
            }

            return;
        }

        if (Input.GetKeyDown(clearLockKey))
        {
            lockedTarget = null;
        }

        if (lockedTarget != null)
        {
            Vector3 lookPoint = GetTargetPivot(lockedTarget);
            Vector3 fromTarget = lookPoint - GetTargetPivot(target);
            if (fromTarget.sqrMagnitude < 0.01f || fromTarget.magnitude > lockMaxDistance)
            {
                lockedTarget = null;
                return;
            }

            Vector3 flatDirection = Vector3.ProjectOnPlane(fromTarget, Vector3.up);
            if (flatDirection.sqrMagnitude > 0.001f)
            {
                float targetYaw = Quaternion.LookRotation(flatDirection, Vector3.up).eulerAngles.y;
                float targetPitch = Mathf.Clamp(Vector3.SignedAngle(flatDirection.normalized, fromTarget.normalized, transform.right), minPitch, maxPitch);
                yaw = Mathf.LerpAngle(yaw, targetYaw, lockFollowSpeed * Time.deltaTime);
                pitch = Mathf.Lerp(pitch, Mathf.Abs(targetPitch), lockFollowSpeed * Time.deltaTime);
            }
        }
        else
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivityX;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }

    // Unity lifecycle: synchronizes cameras, animation, or visuals after normal frame updates.
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 pivot = GetTargetPivot(target);
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot - (orbitRotation * Vector3.forward * distance);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        Quaternion desiredRotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    // Sets state, selection, or placement data for snap to target.
    public void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 pivot = GetTargetPivot(target);
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = pivot - (orbitRotation * Vector3.forward * distance);
        transform.rotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        positionVelocity = Vector3.zero;
    }

    // Attempts to try lock target from mouse and returns whether the operation succeeded.
    private void TryLockTargetFromMouse()
    {
        if (Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, lockMaxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Transform candidate = hit.collider.GetComponentInParent<LockableTarget>()?.transform;
            if (candidate != null && candidate != target)
            {
                lockedTarget = candidate;
                return;
            }
        }

        lockedTarget = null;
    }

    // Calculates and returns the result for get target pivot.
    private Vector3 GetTargetPivot(Transform currentTarget)
    {
        Vector3 offset = currentTarget == target ? targetOffset : lookOffset;
        return currentTarget.position + offset;
    }

    // Sets state, selection, or placement data for set cursor locked.
    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // Handles the normalize pitch workflow.
    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return Mathf.Clamp(angle, -89f, 89f);
    }
}
