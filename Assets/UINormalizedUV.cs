using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public sealed class UINormalizedUV : BaseMeshEffect
{
    private readonly List<UIVertex> vertices = new();

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || vertexHelper.currentVertCount == 0)
        {
            return;
        }

        vertices.Clear();
        vertexHelper.GetUIVertexStream(vertices);

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 position = vertices[i].position;

            minX = Mathf.Min(minX, position.x);
            minY = Mathf.Min(minY, position.y);
            maxX = Mathf.Max(maxX, position.x);
            maxY = Mathf.Max(maxY, position.y);
        }

        float width = Mathf.Max(maxX - minX, 0.0001f);
        float height = Mathf.Max(maxY - minY, 0.0001f);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            Vector3 position = vertex.position;

            vertex.uv1 = new Vector2(
                (position.x - minX) / width,
                (position.y - minY) / height
            );

            vertices[i] = vertex;
        }

        vertexHelper.Clear();
        vertexHelper.AddUIVertexTriangleStream(vertices);
    }
}