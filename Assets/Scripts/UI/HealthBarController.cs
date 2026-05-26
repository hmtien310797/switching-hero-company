using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fillRenderer;

    private float originalWidth;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (fillRenderer == null)
            fillRenderer = GetComponent<SpriteRenderer>();

        originalWidth = fillRenderer.size.x;
        originalLocalPosition = fillRenderer.transform.localPosition;
    }

    public void SetHealth(float healthRatio)
    {
        if (fillRenderer == null)
            return;

        healthRatio = Mathf.Clamp01(healthRatio);

        var size = fillRenderer.size;
        size.x = originalWidth * healthRatio;
        fillRenderer.size = size;

        // Nếu pivot của sprite fill đang nằm giữa, dùng đoạn này để giữ mép trái cố định.
        // Nếu pivot đã là Left Center thì có thể bỏ đoạn dưới.
        var pos = originalLocalPosition;
        pos.x = originalLocalPosition.x - (originalWidth - size.x) * 0.5f;
        fillRenderer.transform.localPosition = pos;
    }

    public void PreSetHealth()
    {
        SetHealth(1f);
    }
}