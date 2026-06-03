using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GroundMapBuilder : MonoBehaviour
{
    public enum BuildPlane
    {
        XY, // Dùng cho game 2D thông thường
        XZ  // Dùng cho ground nằm ngang trong scene 2.5D / 3D
    }

    [Title("Sprites")]
    [InfoBox("Kéo 4 sprite theo thứ tự: Ground_1, Ground_2, Ground_3, Ground_4")]
    [SerializeField] private List<Sprite> groundSprites = new();
    
    [SerializeField] private Transform groundParentTransform;

    [Title("Layout")]
    [SerializeField] private int columns = 2;

    [SerializeField] private BuildPlane buildPlane = BuildPlane.XY;

    [Tooltip("Kích thước mỗi tile trong world unit.")]
    [SerializeField] private Vector2 tileWorldSize = new Vector2(10f, 10f);

    [Tooltip("Xoá các tile cũ trước khi build lại.")]
    [SerializeField] private bool clearOldTiles = true;

    [Tooltip("Tên prefix của object được tạo ra.")]
    [SerializeField] private string tileNamePrefix = "Ground_";

    [Title("Sorting")]
    [SerializeField] private string sortingLayerName = "Default";

    [SerializeField] private int sortingOrder = 0;
    [Title("Layer")]
    [SerializeField] private string gameObjectLayer = "Default";

    [Button(ButtonSizes.Large)]
    private void BuildMap()
    {
        if (groundSprites == null || groundSprites.Count == 0)
        {
            Debug.LogWarning("Ground sprites is empty.");
            return;
        }

        if (columns <= 0)
        {
            Debug.LogWarning("Columns must be greater than 0.");
            return;
        }

        if (tileWorldSize.x <= 0f || tileWorldSize.y <= 0f)
        {
            Debug.LogWarning("Tile World Size must be greater than 0.");
            return;
        }

        if (clearOldTiles)
        {
            ClearChildren();
        }

        for (int i = 0; i < groundSprites.Count; i++)
        {
            Sprite sprite = groundSprites[i];

            if (sprite == null)
            {
                Debug.LogWarning($"Sprite at index {i} is null.");
                continue;
            }

            int row = i / columns;
            int col = i % columns;

            GameObject tileObj = new GameObject($"{tileNamePrefix}{i + 1}");
            tileObj.transform.SetParent(groundParentTransform, false);
            
            SetLayer(tileObj, gameObjectLayer);

            SpriteRenderer spriteRenderer = tileObj.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;

            ApplyTileSize(tileObj.transform, sprite, tileWorldSize);
            tileObj.transform.localPosition = GetTilePosition(col, row);
            tileObj.transform.localRotation = GetTileRotation();
        }
        
        

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    
    private void SetLayer(GameObject obj, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogWarning($"Layer '{layerName}' does not exist. Please create it in Project Settings > Tags and Layers.");
            return;
        }

        obj.layer = layer;
    }

    private void ApplyTileSize(Transform tileTransform, Sprite sprite, Vector2 targetWorldSize)
    {
        Vector2 spriteWorldSize = sprite.bounds.size;

        if (spriteWorldSize.x <= 0f || spriteWorldSize.y <= 0f)
        {
            tileTransform.localScale = Vector3.one;
            return;
        }

        float scaleX = targetWorldSize.x / spriteWorldSize.x;
        float scaleY = targetWorldSize.y / spriteWorldSize.y;

        if (buildPlane == BuildPlane.XY)
        {
            tileTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            // SpriteRenderer mặc định nằm trên mặt phẳng XY.
            // Xoay 90 độ quanh trục X để đặt sprite xuống mặt phẳng XZ.
            tileTransform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
    }

    private Vector3 GetTilePosition(int col, int row)
    {
        int rows = Mathf.CeilToInt((float)groundSprites.Count / columns);

        float totalWidth = columns * tileWorldSize.x;
        float totalHeight = rows * tileWorldSize.y;

        float startX = -totalWidth * 0.5f + tileWorldSize.x * 0.5f;
        float startY = totalHeight * 0.5f - tileWorldSize.y * 0.5f;

        float x = startX + col * tileWorldSize.x;

        if (buildPlane == BuildPlane.XY)
        {
            float y = startY - row * tileWorldSize.y;
            return new Vector3(x, y, 0f);
        }
        else
        {
            float z = startY - row * tileWorldSize.y;
            return new Vector3(x, 0f, z);
        }
    }

    private Quaternion GetTileRotation()
    {
        if (buildPlane == BuildPlane.XY)
        {
            return Quaternion.identity;
        }

        return Quaternion.Euler(90f, 0f, 0f);
    }

    [Button]
    private void ClearChildren()
    {
        for (int i = groundParentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = groundParentTransform.GetChild(i);

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
}