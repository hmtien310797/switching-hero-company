using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class VegetationSpriteRenderer : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int UseVegetationId = Shader.PropertyToID("_UseVegetation");
    private static readonly int UseWindId = Shader.PropertyToID("_UseWind");

    [Header("Vegetation")]
    [SerializeField] private Sprite sprite;
    [SerializeField] private Material sharedMaterial;

    [Header("Material Setup")]
    [SerializeField] private bool autoAssignAtlasTexture = true;
    [SerializeField] private bool enableMeshWind = true;

    [Header("Size")]
    [SerializeField] private bool useSpriteSize = true;
    [SerializeField] private Vector2 customSize = Vector2.one;

    [Header("Bounds")]
    [SerializeField, Min(0f)] private float boundsPadding = 0.5f;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh generatedMesh;

    private void Awake()
    {
        Initialize();
        Rebuild();
    }

    private void OnEnable()
    {
        Initialize();
        Rebuild();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Initialize();
        Rebuild();
    }
#endif

    public void SetSprite(Sprite newSprite)
    {
        if (sprite == newSprite)
        {
            return;
        }

        sprite = newSprite;
        Rebuild();
    }

    private void Initialize()
    {
        meshFilter ??= GetComponent<MeshFilter>();
        meshRenderer ??= GetComponent<MeshRenderer>();

        if (sharedMaterial != null)
        {
            meshRenderer.sharedMaterial = sharedMaterial;
        }
    }

    private void ApplySharedMaterialSettings()
    {
        if (sharedMaterial == null || sprite == null)
        {
            return;
        }

        // Tất cả sprite phải nằm trong cùng một atlas texture.
        if (autoAssignAtlasTexture && sharedMaterial.HasProperty(MainTexId))
        {
            sharedMaterial.SetTexture(MainTexId, sprite.texture);
        }

        // Mỗi mesh đã tự chọn sprite bằng UV atlas, nên không dùng lớp overlay nữa.
        if (sharedMaterial.HasProperty(UseVegetationId))
        {
            sharedMaterial.SetFloat(UseVegetationId, 0f);
        }

        if (sharedMaterial.HasProperty(UseWindId))
        {
            sharedMaterial.SetFloat(UseWindId, enableMeshWind ? 1f : 0f);
        }
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        Initialize();

        if (sprite == null)
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }

            return;
        }

        ApplySharedMaterialSettings();

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh
            {
                name = $"{name}_VegetationMesh"
            };
        }

        Rect spriteRect = sprite.rect;
        float pixelsPerUnit = Mathf.Max(sprite.pixelsPerUnit, 0.0001f);

        Vector2 size = useSpriteSize
            ? new Vector2(spriteRect.width / pixelsPerUnit,
                spriteRect.height / pixelsPerUnit)
            : customSize;

        Vector2 pivot = new Vector2(
            sprite.pivot.x / Mathf.Max(spriteRect.width, 0.0001f),
            sprite.pivot.y / Mathf.Max(spriteRect.height, 0.0001f));

        float left = -pivot.x * size.x;
        float right = left + size.x;
        float bottom = -pivot.y * size.y;
        float top = bottom + size.y;

        Vector3[] vertices =
        {
            new(left, bottom, 0f),
            new(left, top, 0f),
            new(right, top, 0f),
            new(right, bottom, 0f)
        };

        Vector2[] localUvs =
        {
            new(0f, 0f),
            new(0f, 1f),
            new(1f, 1f),
            new(1f, 0f)
        };

        int[] triangles =
        {
            0, 1, 2,
            0, 2, 3
        };

        Color32[] colors =
        {
            new(255, 255, 255, 255),
            new(255, 255, 255, 255),
            new(255, 255, 255, 255),
            new(255, 255, 255, 255)
        };

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.uv = GetAtlasUvs();
        generatedMesh.uv2 = localUvs;
        generatedMesh.colors32 = colors;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateBounds();

        Bounds bounds = generatedMesh.bounds;
        bounds.Expand(boundsPadding * 2f);
        generatedMesh.bounds = bounds;

        meshFilter.sharedMesh = generatedMesh;
    }

    private Vector2[] GetAtlasUvs()
    {
        Rect textureRect = sprite.textureRect;
        Texture texture = sprite.texture;

        float inverseWidth = 1f / Mathf.Max(texture.width, 1);
        float inverseHeight = 1f / Mathf.Max(texture.height, 1);

        float uMin = textureRect.xMin * inverseWidth;
        float uMax = textureRect.xMax * inverseWidth;
        float vMin = textureRect.yMin * inverseHeight;
        float vMax = textureRect.yMax * inverseHeight;

        return new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMin, vMax),
            new Vector2(uMax, vMax),
            new Vector2(uMax, vMin)
        };
    }

    private void OnDestroy()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }
}
