using UnityEngine;

/// <summary>
/// Tạo hiệu ứng đèn nhấp nháy không đồng bộ giữa nhiều Renderer dùng chung material.
/// Không clone material; sử dụng MaterialPropertyBlock để giữ batching tốt hơn.
/// Hỗ trợ shader có các property: _Color, _GlowIntensity, _RimIntensity.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public sealed class RandomLightMaterialFlicker : MonoBehaviour
{
    [Header("Brightness")]
    [SerializeField, Min(0f)] private float minBrightness = 0.72f;
    [SerializeField, Min(0f)] private float maxBrightness = 1.15f;
    [SerializeField, Min(0f)] private float baseGlowIntensity = 1.8f;
    [SerializeField, Min(0f)] private float baseRimIntensity = 3f;

    [Header("Natural Flicker")]
    [Tooltip("Tốc độ dao động sáng nhẹ. Mỗi object sẽ được random thêm để không đồng bộ.")]
    [SerializeField, Min(0.01f)] private float flickerSpeed = 7f;
    [SerializeField, Range(0f, 1f)] private float speedRandomness = 0.45f;
    [SerializeField, Range(0f, 1f)] private float noiseInfluence = 0.7f;

    [Header("Occasional Blink")]
    [Tooltip("Cho phép thỉnh thoảng đèn chớp/tắt nhanh giống đèn điện thật.")]
    [SerializeField] private bool enableRandomBlink = true;
    [SerializeField, Min(0.1f)] private float minTimeBetweenBlinks = 2.5f;
    [SerializeField, Min(0.1f)] private float maxTimeBetweenBlinks = 8f;
    [SerializeField, Min(0.01f)] private float minBlinkDuration = 0.025f;
    [SerializeField, Min(0.01f)] private float maxBlinkDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float blinkBrightness = 0.08f;
    [SerializeField, Range(0f, 1f)] private float doubleBlinkChance = 0.25f;

    [Header("Color")]
    [SerializeField] private Color lightColor = Color.white;
    [Tooltip("Giữ alpha gốc của material và chỉ nhân thêm theo độ sáng.")]
    [SerializeField] private bool multiplyAlphaByBrightness = true;

    [Header("Performance")]
    [Tooltip("Khoảng thời gian cập nhật material. 0.03-0.06 thường đủ mượt cho đèn background.")]
    [SerializeField, Min(0.01f)] private float updateInterval = 0.04f;
    [SerializeField] private bool useUnscaledTime;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int RimIntensityId = Shader.PropertyToID("_RimIntensity");

    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;

    private float randomSeed;
    private float runtimeSpeed;
    private float nextUpdateTime;
    private float nextBlinkTime;
    private float blinkEndTime;
    private float secondBlinkStartTime = -1f;
    private float secondBlinkEndTime = -1f;
    private bool isBlinking;

    private Color originalColor = Color.white;
    private float originalGlowIntensity;
    private float originalRimIntensity;
    private bool hasColor;
    private bool hasGlow;
    private bool hasRim;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        CacheMaterialDefaults();
        RandomizeRuntimeValues();
    }

    private void OnEnable()
    {
        RandomizeRuntimeValues();
        ScheduleNextBlink(CurrentTime);
        nextUpdateTime = CurrentTime + Random.Range(0f, updateInterval);
        ApplyBrightness(Random.Range(minBrightness, maxBrightness));
    }

    private void Update()
    {
        float now = CurrentTime;
        if (now < nextUpdateTime)
            return;

        nextUpdateTime = now + updateInterval;

        UpdateBlinkState(now);

        float brightness;
        if (IsInsideBlinkWindow(now))
        {
            brightness = blinkBrightness;
        }
        else
        {
            brightness = EvaluateNaturalBrightness(now);
        }

        ApplyBrightness(brightness);
    }

    private void OnDisable()
    {
        RestoreDefaults();
    }

    private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

    private void CacheMaterialDefaults()
    {
        Material material = targetRenderer.sharedMaterial;
        if (material == null)
            return;

        hasColor = material.HasProperty(ColorId);
        hasGlow = material.HasProperty(GlowIntensityId);
        hasRim = material.HasProperty(RimIntensityId);

        if (hasColor)
            originalColor = material.GetColor(ColorId);

        originalGlowIntensity = hasGlow
            ? material.GetFloat(GlowIntensityId)
            : baseGlowIntensity;

        originalRimIntensity = hasRim
            ? material.GetFloat(RimIntensityId)
            : baseRimIntensity;
    }

    private void RandomizeRuntimeValues()
    {
        randomSeed = Random.Range(0f, 10000f);

        float speedMultiplier = Random.Range(
            1f - speedRandomness,
            1f + speedRandomness
        );

        runtimeSpeed = Mathf.Max(0.01f, flickerSpeed * speedMultiplier);
    }

    private float EvaluateNaturalBrightness(float time)
    {
        // Kết hợp hai tầng Perlin Noise có tốc độ khác nhau để tránh dao động đều như sin.
        float slowNoise = Mathf.PerlinNoise(randomSeed, time * runtimeSpeed * 0.16f);
        float fastNoise = Mathf.PerlinNoise(randomSeed + 37.17f, time * runtimeSpeed * 0.73f);
        float combinedNoise = Mathf.Lerp(slowNoise, fastNoise, noiseInfluence);

        // Thêm một dao động nhỏ có phase riêng để ánh sáng trông sống hơn.
        float microPulse = Mathf.Sin(time * runtimeSpeed + randomSeed) * 0.5f + 0.5f;
        float naturalValue = Mathf.Lerp(combinedNoise, microPulse, 0.18f);

        return Mathf.Lerp(minBrightness, maxBrightness, naturalValue);
    }

    private void UpdateBlinkState(float now)
    {
        if (!enableRandomBlink)
            return;

        if (!isBlinking && now >= nextBlinkTime)
        {
            isBlinking = true;
            float duration = Random.Range(minBlinkDuration, maxBlinkDuration);
            blinkEndTime = now + duration;

            if (Random.value < doubleBlinkChance)
            {
                float gap = Random.Range(0.035f, 0.11f);
                secondBlinkStartTime = blinkEndTime + gap;
                secondBlinkEndTime = secondBlinkStartTime + Random.Range(minBlinkDuration, maxBlinkDuration);
            }
            else
            {
                secondBlinkStartTime = -1f;
                secondBlinkEndTime = -1f;
            }
        }

        float finalBlinkEnd = secondBlinkEndTime > 0f
            ? secondBlinkEndTime
            : blinkEndTime;

        if (isBlinking && now >= finalBlinkEnd)
        {
            isBlinking = false;
            ScheduleNextBlink(now);
        }
    }

    private bool IsInsideBlinkWindow(float now)
    {
        if (!isBlinking)
            return false;

        bool firstBlink = now <= blinkEndTime;
        bool secondBlink = secondBlinkStartTime > 0f &&
                           now >= secondBlinkStartTime &&
                           now <= secondBlinkEndTime;

        return firstBlink || secondBlink;
    }

    private void ScheduleNextBlink(float now)
    {
        float min = Mathf.Min(minTimeBetweenBlinks, maxTimeBetweenBlinks);
        float max = Mathf.Max(minTimeBetweenBlinks, maxTimeBetweenBlinks);
        nextBlinkTime = now + Random.Range(min, max);
    }

    private void ApplyBrightness(float brightness)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.GetPropertyBlock(propertyBlock);

        if (hasColor)
        {
            Color color = originalColor * lightColor;
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;

            if (multiplyAlphaByBrightness)
                color.a *= brightness;

            propertyBlock.SetColor(ColorId, color);
        }

        if (hasGlow)
            propertyBlock.SetFloat(GlowIntensityId, originalGlowIntensity * brightness);

        if (hasRim)
            propertyBlock.SetFloat(RimIntensityId, originalRimIntensity * brightness);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

    private void RestoreDefaults()
    {
        if (targetRenderer == null || propertyBlock == null)
            return;

        targetRenderer.GetPropertyBlock(propertyBlock);

        if (hasColor)
            propertyBlock.SetColor(ColorId, originalColor);

        if (hasGlow)
            propertyBlock.SetFloat(GlowIntensityId, originalGlowIntensity);

        if (hasRim)
            propertyBlock.SetFloat(RimIntensityId, originalRimIntensity);

        targetRenderer.SetPropertyBlock(propertyBlock);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (maxBrightness < minBrightness)
            maxBrightness = minBrightness;

        if (maxTimeBetweenBlinks < minTimeBetweenBlinks)
            maxTimeBetweenBlinks = minTimeBetweenBlinks;

        if (maxBlinkDuration < minBlinkDuration)
            maxBlinkDuration = minBlinkDuration;
    }
#endif
}
