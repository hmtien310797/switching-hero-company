using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

[ExecuteAlways]
public class MultiSpriteCurvedGroundBuilder : MonoBehaviour
{
    [Header("Input Sprites")]
    [SerializeField] private List<Sprite> groundSprites = new();

    [Header("Grid Layout")]
    [SerializeField, Min(1)] private int columns = 2;
    [SerializeField, Min(1)] private int rows = 2;

    [Header("Ground Size")]
    [SerializeField] private float totalWidth = 20f;
    [SerializeField] private float totalDepth = 45f;

    [Header("Mesh Detail Per Tile")]
    [SerializeField, Min(1)] private int xSegmentsPerTile = 10;
    [SerializeField, Min(1)] private int zSegmentsPerTile = 10;

    [Header("Curve")]
    [SerializeField] private float curveAmount = 0.45f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Material")]
    [SerializeField] private Material materialTemplate;

    [Header("Build")]
    [SerializeField] private bool clearOldChildren = true;
    [SerializeField] private string generatedPrefix = "Curved_Ground_";

#if ODIN_INSPECTOR
    [Button("Build Curved Ground")]
#endif
    [ContextMenu("Build Curved Ground")]
    public void Build()
    {
        if (groundSprites == null || groundSprites.Count == 0)
        {
            Debug.LogWarning("No ground sprites assigned.");
            return;
        }

        if (materialTemplate == null)
        {
            Debug.LogWarning("Missing materialTemplate.");
            return;
        }

        if (clearOldChildren)
            ClearGeneratedChildren();

        float tileWidth = totalWidth / columns;
        float tileDepth = totalDepth / rows;

        int expectedCount = columns * rows;
        int count = Mathf.Min(expectedCount, groundSprites.Count);

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = groundSprites[i];
            if (sprite == null)
                continue;

            int col = i % columns;
            int row = i / columns;

            CreateTile(sprite, i, col, row, tileWidth, tileDepth);
        }
    }

    private void CreateTile(Sprite sprite, int index, int col, int row, float tileWidth, float tileDepth)
    {
        GameObject tileObject = new GameObject($"{generatedPrefix}{index + 1}");
        tileObject.transform.SetParent(transform, false);

        MeshFilter meshFilter = tileObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tileObject.AddComponent<MeshRenderer>();

        Mesh mesh = GenerateTileMesh(sprite, col, row, tileWidth, tileDepth);
        meshFilter.sharedMesh = mesh;

        Material mat = new Material(materialTemplate);
        mat.name = $"{generatedPrefix}{index + 1}_Mat";

        Texture2D texture = sprite.texture;
        mat.mainTexture = texture;

        meshRenderer.sharedMaterial = mat;
    }

    private Mesh GenerateTileMesh(Sprite sprite, int col, int row, float tileWidth, float tileDepth)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Curved Ground Tile Mesh";

        int vertX = xSegmentsPerTile + 1;
        int vertZ = zSegmentsPerTile + 1;

        Vector3[] vertices = new Vector3[vertX * vertZ];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegmentsPerTile * zSegmentsPerTile * 6];

        float xStart = -totalWidth * 0.5f + col * tileWidth;
        float xEnd = xStart + tileWidth;

        // row 0 nằm phía xa/background, row cuối nằm gần camera hơn.
        // Nếu thứ tự sprite của bạn bị ngược, đổi công thức zStart/zEnd ở đây.
        float zStart = totalDepth * 0.5f - (row + 1) * tileDepth;
        float zEnd = zStart + tileDepth;

        Rect textureRect = sprite.textureRect;
        Texture2D texture = sprite.texture;

        float uMin = textureRect.xMin / texture.width;
        float uMax = textureRect.xMax / texture.width;
        float vMin = textureRect.yMin / texture.height;
        float vMax = textureRect.yMax / texture.height;

        for (int z = 0; z < vertZ; z++)
        {
            float localZ01 = z / (float)zSegmentsPerTile;
            float zPos = Mathf.Lerp(zStart, zEnd, localZ01);

            float globalZ01 = Mathf.InverseLerp(-totalDepth * 0.5f, totalDepth * 0.5f, zPos);
            float yPos = heightCurve.Evaluate(globalZ01) * curveAmount;

            for (int x = 0; x < vertX; x++)
            {
                float localX01 = x / (float)xSegmentsPerTile;
                float xPos = Mathf.Lerp(xStart, xEnd, localX01);

                int vertexIndex = z * vertX + x;

                vertices[vertexIndex] = new Vector3(xPos, yPos, zPos);

                float u = Mathf.Lerp(uMin, uMax, localX01);
                float v = Mathf.Lerp(vMin, vMax, localZ01);
                uvs[vertexIndex] = new Vector2(u, v);
            }
        }

        int tri = 0;

        for (int z = 0; z < zSegmentsPerTile; z++)
        {
            for (int x = 0; x < xSegmentsPerTile; x++)
            {
                int i = z * vertX + x;

                triangles[tri++] = i;
                triangles[tri++] = i + vertX;
                triangles[tri++] = i + 1;

                triangles[tri++] = i + 1;
                triangles[tri++] = i + vertX;
                triangles[tri++] = i + vertX + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void ClearGeneratedChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (!child.name.StartsWith(generatedPrefix))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}