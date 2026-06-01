using UnityEngine;

public sealed class HealthBarController : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer fillRenderer;

    [Header("Settings")]
    [SerializeField] private float maxWidth = 1f;
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool hideWhenEmpty = true;

    private Vector2 fillOriginalSize;

    private void Awake()
    {
        if (fillRenderer != null)
        {
            fillOriginalSize = fillRenderer.size;

            if (maxWidth <= 0f)
                maxWidth = fillOriginalSize.x;
        }
        
        float yPos = transform.localPosition.y;
        transform.localPosition = new Vector3(-(maxWidth / 2f), yPos, 0);
    }

    public void SetHealthNormalized(float normalizedHealth)
    {
        normalizedHealth = Mathf.Clamp01(normalizedHealth);

        SetFill(normalizedHealth);
        UpdateVisible(normalizedHealth);
    }

    public void SetOffsetPosition(float xOffset, float yOffset)
    {
        transform.localPosition += new Vector3(xOffset, yOffset, 0);
    }

    public void SetHealth(float currentHp, float maxHp)
    {
        if (maxHp <= 0f)
        {
            SetHealthNormalized(0f);
            return;
        }

        float normalizedHealth = currentHp / maxHp;
        SetHealthNormalized(normalizedHealth);
    }

    public void ResetHealth()
    {
        SetHealthNormalized(1f);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetFill(float normalizedHealth)
    {
        if (fillRenderer == null)
            return;

        Vector2 size = fillRenderer.size;
        size.x = maxWidth * normalizedHealth;
        fillRenderer.size = size;
    }

    private void UpdateVisible(float normalizedHealth)
    {
        if (hideWhenEmpty && normalizedHealth <= 0f)
        {
            Hide();
            return;
        }

        if (hideWhenFull && normalizedHealth >= 1f)
        {
            Hide();
            return;
        }

        Show();
    }
}