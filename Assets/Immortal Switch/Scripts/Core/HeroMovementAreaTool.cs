using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class HeroMovementAreaTool : MonoBehaviour
{
    public enum AreaSource
    {
        Manual,
        BoxCollider,
        RendererBounds
    }

    [Header("Target")]
    [SerializeField] private HeroTeamController heroTeamController;

    [Header("Area Source")]
    [SerializeField] private AreaSource areaSource = AreaSource.Manual;
    [SerializeField] private BoxCollider sourceBoxCollider;
    [SerializeField] private Renderer sourceRenderer;
    [SerializeField] private bool autoRefreshInEditor = true;

    [Header("Manual Bounds")]
    [SerializeField] private Vector2 minLimit = new Vector2(-5f, -3f); // x, z
    [SerializeField] private Vector2 maxLimit = new Vector2(5f, 3f);   // x, z

    [Header("Read Only Info")]
    [SerializeField] private float width;
    [SerializeField] private float depth;
    [SerializeField] private float area;

    [Header("Preview Mesh")]
    [SerializeField] private Transform previewPlane;
    [SerializeField] private bool updatePreviewPlane = true;
    [SerializeField] private float previewY = 0.02f;

    [Header("Visual")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color areaColor = new Color(0f, 1f, 0.2f, 0.15f);
    [SerializeField] private Color borderColor = Color.green;
    [SerializeField] private float gizmoHeight = 0.05f;

    public Vector2 MinLimit => minLimit;
    public Vector2 MaxLimit => maxLimit;

    public float Width => Mathf.Abs(maxLimit.x - minLimit.x);
    public float Depth => Mathf.Abs(maxLimit.y - minLimit.y);
    public float Area => Width * Depth;

    private Vector3 Center => new Vector3(
        (minLimit.x + maxLimit.x) * 0.5f,
        transform.position.y,
        (minLimit.y + maxLimit.y) * 0.5f
    );

    private Vector3 Size => new Vector3(
        Width,
        gizmoHeight,
        Depth
    );

    private void OnValidate()
    {
        RefreshTool();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            RefreshTool();
#endif
    }

    private void RefreshTool()
    {
        if (!Application.isPlaying && autoRefreshInEditor)
            RefreshBoundsFromSource();

        NormalizeMinMax();
        RefreshInfo();

        if (updatePreviewPlane)
            RefreshPreviewPlane();
    }

    [ContextMenu("Refresh Bounds From Source")]
    public void RefreshBoundsFromSource()
    {
        switch (areaSource)
        {
            case AreaSource.Manual:
                break;

            case AreaSource.BoxCollider:
                RefreshFromBoxCollider();
                break;

            case AreaSource.RendererBounds:
                RefreshFromRenderer();
                break;
        }

        NormalizeMinMax();
        RefreshInfo();

        if (updatePreviewPlane)
            RefreshPreviewPlane();
    }

    [ContextMenu("Apply Bounds To Hero Team Controller")]
    public void ApplyBoundsToHeroTeamController()
    {
        RefreshBoundsFromSource();

        if (heroTeamController == null)
        {
            Debug.LogWarning($"{nameof(HeroMovementAreaTool)}: HeroTeamController is null.", this);
            return;
        }

        heroTeamController.SetMovementConstraint(true);
        heroTeamController.SetMovementConstraintBounds(minLimit, maxLimit);

        Debug.Log(
            $"Applied movement bounds. Width: {Width:0.##}, Depth: {Depth:0.##}, Area: {Area:0.##} unit²",
            this
        );
    }

    private void RefreshFromBoxCollider()
    {
        if (sourceBoxCollider == null)
            return;

        Bounds bounds = sourceBoxCollider.bounds;

        minLimit = new Vector2(bounds.min.x, bounds.min.z);
        maxLimit = new Vector2(bounds.max.x, bounds.max.z);
    }

    private void RefreshFromRenderer()
    {
        if (sourceRenderer == null)
            return;

        Bounds bounds = sourceRenderer.bounds;

        minLimit = new Vector2(bounds.min.x, bounds.min.z);
        maxLimit = new Vector2(bounds.max.x, bounds.max.z);
    }

    private void RefreshPreviewPlane()
    {
        if (previewPlane == null)
            return;

        Vector3 center = new Vector3(
            (minLimit.x + maxLimit.x) * 0.5f,
            previewY,
            (minLimit.y + maxLimit.y) * 0.5f
        );

        previewPlane.position = center;
        previewPlane.rotation = Quaternion.identity;

        // Nếu previewPlane là Unity Plane mặc định:
        // Unity Plane mặc định có kích thước mesh là 10x10 unit.
        previewPlane.localScale = new Vector3(
            Width / 10f,
            1f,
            Depth / 10f
        );
    }

    private void NormalizeMinMax()
    {
        if (maxLimit.x < minLimit.x)
            (minLimit.x, maxLimit.x) = (maxLimit.x, minLimit.x);

        if (maxLimit.y < minLimit.y)
            (minLimit.y, maxLimit.y) = (maxLimit.y, minLimit.y);
    }

    private void RefreshInfo()
    {
        width = Width;
        depth = Depth;
        area = Area;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Vector3 center = Center;
        Vector3 size = Size;

        Gizmos.color = areaColor;
        Gizmos.DrawCube(center, size);

        Gizmos.color = borderColor;
        Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
        Handles.color = borderColor;

        Handles.Label(
            center + Vector3.up * 0.35f,
            $"Hero Move Area\nWidth: {Width:0.##}\nDepth: {Depth:0.##}\nArea: {Area:0.##} unit²"
        );
#endif
    }
}