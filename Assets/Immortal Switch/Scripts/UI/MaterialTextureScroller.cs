using UnityEngine;

public class MaterialTextureScroller : MonoBehaviour
{
    private Renderer targetRenderer;
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0f);

    private Material runtimeMaterial;
    private Vector2 currentOffset;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }
        
        runtimeMaterial = targetRenderer.material;
        currentOffset = runtimeMaterial.mainTextureOffset;
    }

    private void Update()
    {
        ScrollOffset(Time.deltaTime);
    }

    private void ScrollOffset(float deltaTime)
    {
        currentOffset += scrollSpeed * deltaTime;

        // Tránh giá trị offset tăng vô hạn.
        currentOffset.x = Mathf.Repeat(currentOffset.x, 1f);
        currentOffset.y = Mathf.Repeat(currentOffset.y, 1f);

        runtimeMaterial.mainTextureOffset = currentOffset;
    }
}