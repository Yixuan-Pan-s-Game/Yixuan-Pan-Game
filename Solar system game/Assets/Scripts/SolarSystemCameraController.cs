using UnityEngine;

public class SolarSystemCameraController : MonoBehaviour
{
    [SerializeField] private float moveSmoothTime = 0.35f;
    [SerializeField] private float rotationSpeed = 7f;
    [SerializeField] private Vector3 focusViewDirection = new Vector3(0.35f, 0.18f, -1f);

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private Vector3 currentVelocity;
    private Transform focusTarget;
    private float focusDistance;
    private bool returningHome;

    private void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReturnHome();
        }

        if (focusTarget != null)
        {
            MoveTowardFocus();
        }
        else if (returningHome)
        {
            MoveHome();
        }
    }

    public void FocusOn(Transform target, float distance)
    {
        focusTarget = target;
        focusDistance = Mathf.Max(0.8f, distance);
        returningHome = false;
    }

    public void ReturnHome()
    {
        focusTarget = null;
        returningHome = true;
    }

    private void MoveTowardFocus()
    {
        Vector3 viewDirection = focusViewDirection.sqrMagnitude > 0.001f
            ? focusViewDirection.normalized
            : Vector3.back;
        Vector3 targetPosition = focusTarget.position - viewDirection * focusDistance;

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, moveSmoothTime);
        RotateToLookAt(focusTarget.position);
    }

    private void MoveHome()
    {
        transform.position = Vector3.SmoothDamp(transform.position, homePosition, ref currentVelocity, moveSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, homeRotation, rotationSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, homePosition) < 0.03f &&
            Quaternion.Angle(transform.rotation, homeRotation) < 0.5f)
        {
            transform.position = homePosition;
            transform.rotation = homeRotation;
            returningHome = false;
        }
    }

    private void RotateToLookAt(Vector3 worldPoint)
    {
        Vector3 lookDirection = worldPoint - transform.position;
        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
