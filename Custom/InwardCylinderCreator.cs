using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class InwardCylinderCreator : MonoBehaviour
{
    public int numSegments = 64;
    public float height = 100f;
    public float radius = 200f;

    private void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = CreateCylinderMesh(numSegments, height, radius);
    }

    private Mesh CreateCylinderMesh(int segments, float height, float radius)
    {
        Mesh mesh = new Mesh();

        int vertexCount = segments * 2 + 2; // Top and bottom centers
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 6 * 2]; // Two triangles per segment, top and bottom
        Vector3[] normals = new Vector3[vertexCount];

        // Top and bottom center vertices
        vertices[vertexCount - 2] = new Vector3(0, 0, 0);
        vertices[vertexCount - 1] = new Vector3(0, height, 0);

        float angleStep = 360.0f / segments;
        int triIndex = 0;

        for (int i = 0; i < segments; i++)
        {
            // Angle calculation
            float angle = i * angleStep * Mathf.Deg2Rad;
            float nextAngle = ((i + 1) % segments) * angleStep * Mathf.Deg2Rad;

            // Bottom vertices
            vertices[i * 2] = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            vertices[i * 2 + 1] = new Vector3(Mathf.Cos(nextAngle) * radius, 0, Mathf.Sin(nextAngle) * radius);

            // Top vertices
            vertices[(i * 2) + segments * 2] = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            vertices[(i * 2 + 1) + segments * 2] = new Vector3(Mathf.Cos(nextAngle) * radius, height, Mathf.Sin(nextAngle) * radius);

            // Bottom triangles (inward facing)
            triangles[triIndex++] = vertexCount - 2;
            triangles[triIndex++] = i * 2 + 1;
            triangles[triIndex++] = i * 2;

            // Top triangles (inward facing)
            triangles[triIndex++] = vertexCount - 1;
            triangles[triIndex++] = (i * 2) + segments * 2;
            triangles[triIndex++] = (i * 2 + 1) + segments * 2;

            // Side triangles (inward facing)
            triangles[triIndex++] = (i * 2) + segments * 2;
            triangles[triIndex++] = i * 2 + 1;
            triangles[triIndex++] = i * 2;

            triangles[triIndex++] = (i * 2) + segments * 2;
            triangles[triIndex++] = (i * 2 + 1) + segments * 2;
            triangles[triIndex++] = i * 2 + 1;
        }

        // Normals (all pointing inwards)
        for (int i = 0; i < vertexCount; i++)
        {
            normals[i] = -Vector3.Normalize(vertices[i] - new Vector3(0, vertices[i].y, 0));
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

        return mesh;
    }
}
