using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GridMapTool : MonoBehaviour
{
    public enum GridPlane
    {
        XY,
        XZ
    }

    [Title("Grid Area")]
    [SerializeField] private GridPlane gridPlane = GridPlane.XZ;

    [Tooltip("Số ô theo chiều ngang.")]
    [SerializeField] private int columns = 10;

    [Tooltip("Số ô theo chiều dọc.")]
    [SerializeField] private int rows = 10;

    [Tooltip("Kích thước mỗi ô.")]
    [SerializeField] private Vector2 cellSize = Vector2.one;

    [Tooltip("Grid sẽ lấy object này làm tâm.")]
    [SerializeField] private bool centerAtRoot = true;

    [Title("Visual")]
    [SerializeField] private Material lineMaterial;

    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);

    [SerializeField] private float lineWidth = 0.03f;

    [SerializeField] private int sortingOrder = 100;

    [SerializeField] private string sortingLayerName = "Default";

    [Title("Layer")]
    [SerializeField] private string gameObjectLayer = "Default";

    [Title("Build")]
    [SerializeField] private bool clearBeforeBuild = true;

    [SerializeField] private string lineNamePrefix = "Grid_Line_";

    private readonly List<LineRenderer> createdLines = new();

    [Button(ButtonSizes.Large)]
    public void BuildGrid()
    {
        if (columns <= 0 || rows <= 0)
        {
            Debug.LogWarning("Columns and Rows must be greater than 0.");
            return;
        }

        if (cellSize.x <= 0f || cellSize.y <= 0f)
        {
            Debug.LogWarning("Cell Size must be greater than 0.");
            return;
        }

        if (clearBeforeBuild)
        {
            ClearGrid();
        }

        int layer = LayerMask.NameToLayer(gameObjectLayer);
        if (layer == -1)
        {
            Debug.LogWarning($"Layer '{gameObjectLayer}' does not exist. Using Default layer.");
            layer = 0;
        }

        int lineIndex = 0;

        float width = columns * cellSize.x;
        float height = rows * cellSize.y;

        float startX = centerAtRoot ? -width * 0.5f : 0f;
        float endX = centerAtRoot ? width * 0.5f : width;

        float startY = centerAtRoot ? -height * 0.5f : 0f;
        float endY = centerAtRoot ? height * 0.5f : height;

        // Vertical lines
        for (int x = 0; x <= columns; x++)
        {
            float currentX = startX + x * cellSize.x;

            Vector3 from = GetPoint(currentX, startY);
            Vector3 to = GetPoint(currentX, endY);

            CreateLine(lineIndex++, from, to, layer);
        }

        // Horizontal lines
        for (int y = 0; y <= rows; y++)
        {
            float currentY = startY + y * cellSize.y;

            Vector3 from = GetPoint(startX, currentY);
            Vector3 to = GetPoint(endX, currentY);

            CreateLine(lineIndex++, from, to, layer);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(gameObject);
#endif
    }

    [Button]
    public void ClearGrid()
    {
        createdLines.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (!child.name.StartsWith(lineNamePrefix))
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private void CreateLine(int index, Vector3 localStart, Vector3 localEnd, int layer)
    {
        GameObject lineObj = new GameObject($"{lineNamePrefix}{index}");
        lineObj.transform.SetParent(transform, false);
        lineObj.layer = layer;

        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.positionCount = 2;

        line.SetPosition(0, localStart);
        line.SetPosition(1, localEnd);

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.startColor = lineColor;
        line.endColor = lineColor;

        line.numCapVertices = 0;
        line.numCornerVertices = 0;

        if (lineMaterial != null)
        {
            line.material = lineMaterial;
        }
        else
        {
            line.material = GetDefaultLineMaterial();
        }

        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;

        createdLines.Add(line);
    }

    private Vector3 GetPoint(float x, float y)
    {
        if (gridPlane == GridPlane.XY)
        {
            return new Vector3(x, y, 0f);
        }

        return new Vector3(x, 0f, y);
    }

    private Material GetDefaultLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = "Runtime Grid Line Material";
        material.color = lineColor;

        return material;
    }
}