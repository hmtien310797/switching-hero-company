using UnityEngine;

/// <summary>
/// Hiển thị Sprite bằng MeshRenderer với shader Curved World.
/// Chỉ cần kéo Sprite vào Inspector, component sẽ tự:
/// - Tạo Mesh theo geometry/UV của Sprite.
/// - Dùng chung một Material runtime cho tất cả instance.
/// - Gán texture, tint và thông số curvature riêng bằng MaterialPropertyBlock.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class CurvedWorldSpriteRenderer : MonoBehaviour
{
    private const string ShaderName = "Custom/CurvedWorldHorizonTransparent";

    [Header("Sprite")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color color = Color.white;

    [Tooltip("Số Unity Unit tương ứng với 1 pixel. Để 0 sẽ dùng Pixels Per Unit của Sprite.")]
    [Min(0f)]
    [SerializeField] private float unitsPerPixelOverride;

    [Header("Shader")]
    [SerializeField] private float horizontalCurvature = 0.002f;
    [SerializeField] private float forwardCurvature = 0.0003f;
    [SerializeField] private float curveStartDistance;
    [SerializeField] private float horizontalStartDistance;
    [SerializeField] private float curveOffsetY;

    [Header("Renderer")]
    [SerializeField] private int sortingOrder;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh generatedMesh;
    private MaterialPropertyBlock propertyBlock;

    private static Shader cachedShader;
    private static Material sharedMaterial;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int HorizontalCurvatureId = Shader.PropertyToID("_HorizontalCurvature");
    private static readonly int ForwardCurvatureId = Shader.PropertyToID("_ForwardCurvature");
    private static readonly int CurveStartDistanceId = Shader.PropertyToID("_CurveStartDistance");
    private static readonly int HorizontalStartDistanceId = Shader.PropertyToID("_HorizontalStartDistance");
    private static readonly int CurveOffsetYId = Shader.PropertyToID("_CurveOffsetY");

    public Sprite Sprite
    {
        get => sprite;
        set
        {
            if (sprite == value)
                return;

            sprite = value;
            Refresh();
        }
    }

    public Color Color
    {
        get => color;
        set
        {
            if (color == value)
                return;

            color = value;
            ApplyMaterialProperties();
        }
    }

    private void OnEnable()
    {
        CacheComponents();
        Refresh();
    }

    private void OnValidate()
    {
        CacheComponents();
        Refresh();
    }

    private void OnDestroy()
    {
        DestroyGeneratedObject(generatedMesh);
        generatedMesh = null;
    }

    [ContextMenu("Refresh")]
    public void Refresh()
    {
        CacheComponents();

        if (!EnsureSharedMaterial())
        {
            meshRenderer.enabled = false;
            return;
        }

        RebuildMesh();
        ApplyMaterialProperties();

        meshRenderer.sortingOrder = sortingOrder;
        meshRenderer.enabled = sprite != null;
    }

    private void CacheComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        propertyBlock ??= new MaterialPropertyBlock();
    }

    private bool EnsureSharedMaterial()
    {
        if (sharedMaterial != null)
        {
            meshRenderer.sharedMaterial = sharedMaterial;
            return true;
        }

        if (cachedShader == null)
            cachedShader = Shader.Find(ShaderName);

        if (cachedShader == null)
        {
            Debug.LogError(
                $"[{nameof(CurvedWorldSpriteRenderer)}] Không tìm thấy shader '{ShaderName}'. " +
                "Hãy thêm shader vào Project Settings > Graphics > Always Included Shaders để tránh bị strip khi build.",
                this);
            return false;
        }

        sharedMaterial = new Material(cachedShader)
        {
            name = "CurvedWorldSprite_SharedRuntimeMaterial",
            hideFlags = HideFlags.HideAndDontSave
        };

        meshRenderer.sharedMaterial = sharedMaterial;
        return true;
    }

    private void RebuildMesh()
    {
        if (sprite == null)
        {
            meshFilter.sharedMesh = null;
            return;
        }

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = $"{name}_Sprite_RuntimeMesh",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
        else
        {
            generatedMesh.Clear();
        }

        Vector2[] spriteVertices = sprite.vertices;
        Vector2[] spriteUvs = sprite.uv;
        ushort[] spriteTriangles = sprite.triangles;

        float scale = unitsPerPixelOverride > 0f
            ? unitsPerPixelOverride * sprite.pixelsPerUnit
            : 1f;

        var vertices = new Vector3[spriteVertices.Length];
        for (int i = 0; i < spriteVertices.Length; i++)
        {
            Vector2 vertex = spriteVertices[i] * scale;
            vertices[i] = new Vector3(vertex.x, vertex.y, 0f);
        }

        var triangles = new int[spriteTriangles.Length];
        for (int i = 0; i < spriteTriangles.Length; i++)
            triangles[i] = spriteTriangles[i];

        generatedMesh.vertices = vertices;
        generatedMesh.uv = spriteUvs;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateBounds();
        generatedMesh.RecalculateNormals();

        meshFilter.sharedMesh = generatedMesh;
    }

    private void ApplyMaterialProperties()
    {
        if (meshRenderer == null || propertyBlock == null)
            return;

        propertyBlock.Clear();
        propertyBlock.SetTexture(MainTexId, sprite != null ? sprite.texture : null);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(HorizontalCurvatureId, horizontalCurvature);
        propertyBlock.SetFloat(ForwardCurvatureId, forwardCurvature);
        propertyBlock.SetFloat(CurveStartDistanceId, curveStartDistance);
        propertyBlock.SetFloat(HorizontalStartDistanceId, horizontalStartDistance);
        propertyBlock.SetFloat(CurveOffsetYId, curveOffsetY);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private static void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}