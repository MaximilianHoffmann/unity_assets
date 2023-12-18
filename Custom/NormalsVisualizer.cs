using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class NormalsVisualizer : MonoBehaviour
{
    public float normalLength = 0.5f; // Length of the normal lines
    public Color normalColor = Color.red; // Color of the normal lines

    private void OnDrawGizmos()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter && meshFilter.sharedMesh)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            for (int i = 0; i < vertices.Length; i++)
            {
                // Draw the normal
                Gizmos.color = normalColor;
                Vector3 vertexWorldPos = transform.TransformPoint(vertices[i]);
                Vector3 normalWorldEnd = vertexWorldPos + transform.TransformDirection(normals[i]) * normalLength;
                Gizmos.DrawLine(vertexWorldPos, normalWorldEnd);
            }
        }
    }
}
