using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0f, 1.35f, 0f);
    public Vector3 lookOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Orbit")]
    public float distance = 4.5f;
    public float minDistance = 2.6f;
    public float maxDistance = 7f;
    public float mouseSensitivityX = 3.2f;
    public float mouseSensitivityY = 2.2f;
    public float minPitch = 12f;
    public float maxPitch = 55f;

    [Header("Smoothing")]
    public float positionSmoothTime = 0.12f;
    public float rotationSmoothSpeed = 12f;
    public float lockFollowSpeed = 7f;

    [Header("Lock On")]
    public float lockMaxDistance = 60f;
    public KeyCode freeCursorKey = KeyCode.LeftAlt;
    public KeyCode clearLockKey = KeyCode.Mouse1;

    private Vector3 positionVelocity;
    private float yaw;
    private float pitch = 24f;
    private Transform lockedTarget;

    public Transform LockedTarget => lockedTarget;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        yaw = euler.y;
        pitch = NormalizePitch(euler.x);
        SetCursorLocked(true);
        SnapToTarget();
    }

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

    private Vector3 GetTargetPivot(Transform currentTarget)
    {
        Vector3 offset = currentTarget == target ? targetOffset : lookOffset;
        return currentTarget.position + offset;
    }

    private void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return Mathf.Clamp(angle, -89f, 89f);
    }
}
