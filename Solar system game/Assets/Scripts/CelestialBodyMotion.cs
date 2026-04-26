using UnityEngine;

/// <summary>
/// Gives a planet or moon simple child-friendly self rotation and orbit motion.
/// Real projects can replace these values with more accurate scaled data.
/// </summary>
public class CelestialBodyMotion : MonoBehaviour
{
    [Header("Self Rotation")]
    [SerializeField] private Vector3 selfRotationAxis = Vector3.up;
    [SerializeField] private float selfRotationDegreesPerSecond = 20f;

    [Header("Orbit")]
    [SerializeField] private Transform orbitCenter;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private float orbitDegreesPerSecond = 10f;

    private void Update()
    {
        if (selfRotationAxis.sqrMagnitude > 0.0001f)
        {
            transform.Rotate(selfRotationAxis.normalized, selfRotationDegreesPerSecond * Time.deltaTime, Space.Self);
        }

        if (orbitCenter != null && orbitAxis.sqrMagnitude > 0.0001f)
        {
            transform.RotateAround(orbitCenter.position, orbitAxis.normalized, orbitDegreesPerSecond * Time.deltaTime);
        }
    }

    public void ConfigureRotation(float degreesPerSecond, Vector3 axis)
    {
        selfRotationDegreesPerSecond = degreesPerSecond;
        selfRotationAxis = axis;
    }

    public void ConfigureOrbit(Transform center, float degreesPerSecond, Vector3 axis)
    {
        orbitCenter = center;
        orbitDegreesPerSecond = degreesPerSecond;
        orbitAxis = axis;
    }
}
