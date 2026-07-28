using UnityEngine;

/// <summary>
/// Hiển thị một Sprite bằng MeshRenderer với shader Curved World + hiệu ứng sao.
/// Chỉ cần gắn component vào GameObject rỗng, component sẽ tự:
/// - Thêm MeshFilter và MeshRenderer.
/// - Tạo Mesh theo geometry/UV của Sprite nền.
/// - Tạo và dùng chung Material runtime.
/// - Gán Sprite nền, Sprite ngôi sao và toàn bộ thông số shader bằng MaterialPropertyBlock.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public sealed class CurvedWorldSpriteStarsRenderer : MonoBehaviour
{
    private const string ShaderName = "Custom/CurvedWorldSpriteStars";

    [Header("Sprites")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Sprite starSprite;
    [SerializeField] private Color color = Color.white;

    [Tooltip("Số Unity Unit tương ứng với 1 pixel. Để 0 sẽ dùng Pixels Per Unit của Sprite.")]
    [Min(0f)]
    [SerializeField] private float unitsPerPixelOverride;

    [Header("Curved World")]
    [SerializeField] private float horizontalCurvature = 0.002f;
    [SerializeField] private float forwardCurvature = 0.0003f;
    [SerializeField] private float curveStartDistance;
    [SerializeField] private float horizontalStartDistance;
    [SerializeField] private float curveOffsetY;

    [Header("Stars")]
    [SerializeField] private Color starColor = Color.white;
    [SerializeField, Range(1f, 120f)] private float gridDensity = 45f;
    [SerializeField, Range(0f, 1f)] private float starThreshold = 0.78f;
    [SerializeField, Range(0.02f, 1f)] private float starScale = 0.3f;
    [SerializeField, Range(0f, 10f)] private float starBrightness = 2.5f;
    [SerializeField, Range(0f, 10f)] private float twinkleSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float twinkleMinimum = 0.05f;
    [SerializeField, Range(0f, 1f)] private float starRegionBottom = 0.67f;
    [SerializeField, Range(0f, 0.5f)] private float starRegionFade = 0.1f;
    [SerializeField] private bool starsEnabled = true;

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
    private static readonly int StarTexId = Shader.PropertyToID("_StarTex");
    private static readonly int StarSpriteRectId = Shader.PropertyToID("_StarSpriteRect");
    private static readonly int StarColorId = Shader.PropertyToID("_StarColor");
    private static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
    private static readonly int StarThresholdId = Shader.PropertyToID("_StarThreshold");
    private static readonly int StarScaleId = Shader.PropertyToID("_StarScale");
    private static readonly int StarBrightnessId = Shader.PropertyToID("_StarBrightness");
    private static readonly int TwinkleSpeedId = Shader.PropertyToID("_TwinkleSpeed");
    private static readonly int TwinkleMinId = Shader.PropertyToID("_TwinkleMin");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int StarRegionYId = Shader.PropertyToID("_StarRegionY");
    private static readonly int StarFadeId = Shader.PropertyToID("_StarFade");
    private static readonly int StarEnabledId = Shader.PropertyToID("_StarEnabled");

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

    public Sprite StarSprite
    {
        get => starSprite;
        set
        {
            if (starSprite == value)
                return;

            starSprite = value;
            ApplyMaterialProperties();
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

    public MeshRenderer Renderer => meshRenderer;

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

    /// <summary>
    /// Khởi tạo hoặc thay đổi Sprite nền và Sprite ngôi sao bằng code.
    /// </summary>
    public void Initialize(Sprite backgroundSprite, Sprite newStarSprite)
    {
        sprite = backgroundSprite;
        starSprite = newStarSprite;
        Refresh();
    }

    public void SetStarsEnabled(bool enabled)
    {
        starsEnabled = enabled;
        ApplyMaterialProperties();
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
                $"[{nameof(CurvedWorldSpriteStarsRenderer)}] Không tìm thấy shader '{ShaderName}'. " +
                "Hãy thêm shader vào Project Settings > Graphics > Always Included Shaders để tránh bị strip khi build.",
                this);
            return false;
        }

        sharedMaterial = new Material(cachedShader)
        {
            name = "CurvedWorldSpriteStars_SharedRuntimeMaterial",
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
                name = $"{name}_CurvedWorldSpriteStars_RuntimeMesh",
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

        propertyBlock.SetTexture(MainTexId, sprite != null ? sprite.texture : Texture2D.whiteTexture);
        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetFloat(HorizontalCurvatureId, horizontalCurvature);
        propertyBlock.SetFloat(ForwardCurvatureId, forwardCurvature);
        propertyBlock.SetFloat(CurveStartDistanceId, curveStartDistance);
        propertyBlock.SetFloat(HorizontalStartDistanceId, horizontalStartDistance);
        propertyBlock.SetFloat(CurveOffsetYId, curveOffsetY);

        ApplyStarSprite(propertyBlock);
        propertyBlock.SetColor(StarColorId, starColor);
        propertyBlock.SetFloat(GridSizeId, gridDensity);
        propertyBlock.SetFloat(StarThresholdId, starThreshold);
        propertyBlock.SetFloat(StarScaleId, starScale);
        propertyBlock.SetFloat(StarBrightnessId, starBrightness);
        propertyBlock.SetFloat(TwinkleSpeedId, twinkleSpeed);
        propertyBlock.SetFloat(TwinkleMinId, twinkleMinimum);
        propertyBlock.SetFloat(AspectId, CalculateAspectRatio());
        propertyBlock.SetFloat(StarRegionYId, starRegionBottom);
        propertyBlock.SetFloat(StarFadeId, starRegionFade);
        propertyBlock.SetFloat(StarEnabledId, starsEnabled && starSprite != null ? 1f : 0f);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyStarSprite(MaterialPropertyBlock block)
    {
        if (starSprite == null || starSprite.texture == null)
        {
            block.SetTexture(StarTexId, Texture2D.whiteTexture);
            block.SetVector(StarSpriteRectId, new Vector4(0f, 0f, 1f, 1f));
            return;
        }

        Texture texture = starSprite.texture;
        Rect textureRect = starSprite.textureRect;

        block.SetTexture(StarTexId, texture);
        block.SetVector(
            StarSpriteRectId,
            new Vector4(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height));
    }

    private float CalculateAspectRatio()
    {
        if (sprite == null)
            return 1f;

        Rect rect = sprite.rect;
        return rect.height > Mathf.Epsilon ? rect.width / rect.height : 1f;
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