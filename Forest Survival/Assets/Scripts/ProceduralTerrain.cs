using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
// Procedural terrain component that generates, samples, and bounds-checks the terrain mesh.
public class ProceduralTerrain : MonoBehaviour
{
    [Header("Shape")]
    // Important runtime data or configuration used by this component: resolution.
    public int resolution = 160;
    // Important runtime data or configuration used by this component: terrainSize.
    public float terrainSize = 90f;
    // Spatial value used for positioning, rotation, scale, or collision math: heightScale.
    public float heightScale = 10f;
    // Spatial value used for positioning, rotation, scale, or collision math: baseNoiseScale.
    public float baseNoiseScale = 1.4f;
    // Important runtime data or configuration used by this component: seed.
    public int seed = 37;

    [Header("Profile")]
    // Identifier or category used for lookup, routing, or state selection: ridgeStrength.
    public float ridgeStrength = 0.45f;
    // Important runtime data or configuration used by this component: bowlStrength.
    public float bowlStrength = 0.18f;
    // Important runtime data or configuration used by this component: plateauBlend.
    public float plateauBlend = 0.2f;

    [Header("Material")]
    // Asset reference used for spawning, rendering, audio, or animation: groundMaterial.
    public Material groundMaterial;

    // Important runtime data or configuration used by this component: meshFilter.
    private MeshFilter meshFilter;
    // Cached component or scene reference to avoid repeated lookups: meshCollider.
    private MeshCollider meshCollider;
    // Cached component or scene reference to avoid repeated lookups: meshRenderer.
    private MeshRenderer meshRenderer;

    // Unity lifecycle: caches local references and initializes base state when the component is created.
    private void Awake()
    {
        Generate();
    }

    // Handles the generate workflow.
    public void Generate()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (groundMaterial != null)
        {
            meshRenderer.sharedMaterial = groundMaterial;
        }

        int vertexCountPerLine = resolution + 1;
        Vector3[] vertices = new Vector3[vertexCountPerLine * vertexCountPerLine];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];

        float halfSize = terrainSize * 0.5f;
        int triangleIndex = 0;

        for (int z = 0; z <= resolution; z++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = z * vertexCountPerLine + x;
                float percentX = x / (float)resolution;
                float percentZ = z / (float)resolution;
                float localX = (percentX * terrainSize) - halfSize;
                float localZ = (percentZ * terrainSize) - halfSize;
                float height = SampleHeightLocal(localX, localZ);

                vertices[index] = new Vector3(localX, height, localZ);
                uv[index] = new Vector2(percentX * 8f, percentZ * 8f);

                if (x == resolution || z == resolution)
                {
                    continue;
                }

                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + vertexCountPerLine + 1;
                triangles[triangleIndex++] = index + vertexCountPerLine;
                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + vertexCountPerLine + 1;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "ProceduralTerrain"
        };
        mesh.indexFormat = vertices.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }

    // Calculates and returns the result for sample height world.
    public float SampleHeightWorld(float worldX, float worldZ)
    {
        Vector3 localPoint = transform.InverseTransformPoint(new Vector3(worldX, 0f, worldZ));
        return transform.TransformPoint(new Vector3(localPoint.x, SampleHeightLocal(localPoint.x, localPoint.z), localPoint.z)).y;
    }

    // Calculates and returns the result for sample normal world.
    public Vector3 SampleNormalWorld(float worldX, float worldZ)
    {
        float offset = 0.4f;
        float left = SampleHeightWorld(worldX - offset, worldZ);
        float right = SampleHeightWorld(worldX + offset, worldZ);
        float back = SampleHeightWorld(worldX, worldZ - offset);
        float forward = SampleHeightWorld(worldX, worldZ + offset);

        Vector3 tangentX = new Vector3(offset * 2f, right - left, 0f);
        Vector3 tangentZ = new Vector3(0f, forward - back, offset * 2f);
        return Vector3.Cross(tangentZ, tangentX).normalized;
    }

    // Handles the contains world position workflow.
    public bool ContainsWorldPosition(Vector3 worldPosition)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPosition);
        float halfSize = terrainSize * 0.5f;
        return localPoint.x >= -halfSize && localPoint.x <= halfSize && localPoint.z >= -halfSize && localPoint.z <= halfSize;
    }

    // Calculates and returns the result for sample height local.
    private float SampleHeightLocal(float localX, float localZ)
    {
        float sampleX = (localX + seed * 0.37f) / terrainSize;
        float sampleZ = (localZ + seed * 0.53f) / terrainSize;

        float noiseA = Mathf.PerlinNoise(sampleX * baseNoiseScale, sampleZ * baseNoiseScale);
        float noiseB = Mathf.PerlinNoise(sampleX * baseNoiseScale * 2.1f + 11.3f, sampleZ * baseNoiseScale * 2.1f + 7.7f);
        float noiseC = Mathf.PerlinNoise(sampleX * baseNoiseScale * 4.2f + 4.9f, sampleZ * baseNoiseScale * 4.2f + 15.1f);

        float blendedNoise = (noiseA * 0.55f) + (noiseB * 0.3f) + (noiseC * 0.15f);
        float ridge = 1f - Mathf.Abs((blendedNoise * 2f) - 1f);

        float radial = new Vector2(localX / terrainSize, localZ / terrainSize).magnitude;
        float bowl = Mathf.Clamp01(1f - radial * 1.35f);
        float plateau = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((bowl - 0.18f) / 0.6f));

        float height = blendedNoise * heightScale;
        height += ridge * heightScale * ridgeStrength;
        height += bowl * heightScale * bowlStrength;
        height += plateau * plateauBlend * heightScale;
        height -= radial * radial * heightScale * 0.85f;

        return height;
    }
}
