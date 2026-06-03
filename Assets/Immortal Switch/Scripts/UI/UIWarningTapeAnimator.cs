using System;
using System.Collections;
using Immortal_Switch.Scripts.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UIWarningTapeAnimator : MonoBehaviour
{
    public enum WarningTapeDirection
    {
        Horizontal,
        Vertical,
        DiagonalUp,
        DiagonalDown,
        CustomAngle
    }

    [Header("References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private RawImage rawImageA;
    [SerializeField] private RawImage rawImageB;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Direction")]
    [SerializeField] private WarningTapeDirection direction = WarningTapeDirection.DiagonalDown;
    [SerializeField] private float customAngle = 0f;

    [Header("Size")]
    [SerializeField] private Vector2 horizontalSize = new Vector2(1900f, 130f);
    [SerializeField] private Vector2 verticalSize = new Vector2(1900f, 130f);
    [SerializeField] private Vector2 diagonalSize = new Vector2(1900f, 130f);

    [Header("Tile")]
    [SerializeField] private float tileRepeatX = 4f;
    [SerializeField] private float tileRepeatY = 1f;

    [Header("Scroll")]
    [SerializeField] private bool scroll = true;
    [SerializeField] private Vector2 scrollSpeedA = new Vector2(0.35f, 0f);
    [SerializeField] private Vector2 scrollSpeedB = new Vector2(-0.35f, 0f);

    [Header("Show Animation")]
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float punchScale = 1.08f;

    [Header("Auto")]
    [SerializeField] private bool playOnEnable = false;
    [SerializeField] private bool hideOnAwake = true;
    [SerializeField] private float autoHideAfter = 0f;

    public bool IsVisible => isVisible;

    private Coroutine showRoutine;
    private Coroutine hideRoutine;

    private Vector3 defaultScale;
    private bool isVisible;

    private void Reset()
    {
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        defaultScale = root.localScale;

        ApplyDirection();
        ApplyTile();
        if (hideOnAwake)
        {
            isVisible = false;
            canvasGroup.alpha = 0f;
        }
        else
        {
            isVisible = true;
            canvasGroup.alpha = 1f;
        }
        
    }

    private void Start()
    {
        GameEventManager.Subscribe<bool>(GameEvents.OnBossSpawnAnimationComplete, result =>
        {
            if (!result)
                Show();
        });
    }

    private void Update()
    {
        if (!scroll)
            return;

        ScrollRawImage(rawImageA, scrollSpeedA);
        ScrollRawImage(rawImageB, scrollSpeedB);
    }

    public void Show()
    {
        StopCurrentRoutines();

        isVisible = true;

        ApplyDirection();
        ApplyTile();

        showRoutine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopCurrentRoutines();

        isVisible = false;
        hideRoutine = StartCoroutine(HideRoutine());
    }

    [Button]
    public void Toggle()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }

    public void ShowHorizontal()
    {
        SetDirection(WarningTapeDirection.Horizontal);
        Show();
    }

    public void ShowVertical()
    {
        SetDirection(WarningTapeDirection.Vertical);
        Show();
    }

    public void ShowDiagonalUp()
    {
        SetDirection(WarningTapeDirection.DiagonalUp);
        Show();
    }

    public void ShowDiagonalDown()
    {
        SetDirection(WarningTapeDirection.DiagonalDown);
        Show();
    }

    public void ShowCustomAngle(float angle)
    {
        SetCustomAngle(angle);
        Show();
    }

    public void SetDirection(WarningTapeDirection newDirection)
    {
        direction = newDirection;
        ApplyDirection();
    }

    public void SetCustomAngle(float angle)
    {
        direction = WarningTapeDirection.CustomAngle;
        customAngle = angle;
        ApplyDirection();
    }

    public void SetScrollSpeedA(float x, float y = 0f)
    {
        scrollSpeedA = new Vector2(x, y);
    }

    public void SetScrollSpeedB(float x, float y = 0f)
    {
        scrollSpeedB = new Vector2(x, y);
    }

    private void ScrollRawImage(RawImage rawImage, Vector2 speed)
    {
        if (rawImage == null)
            return;

        Rect uv = rawImage.uvRect;
        uv.x += speed.x * Time.unscaledDeltaTime;
        uv.y += speed.y * Time.unscaledDeltaTime;
        rawImage.uvRect = uv;
    }

    private void ApplyDirection()
    {
        if (root == null)
            return;

        float angle = 0f;
        Vector2 size = horizontalSize;

        switch (direction)
        {
            case WarningTapeDirection.Horizontal:
                angle = 0f;
                size = horizontalSize;
                break;

            case WarningTapeDirection.Vertical:
                angle = 90f;
                size = verticalSize;
                break;

            case WarningTapeDirection.DiagonalUp:
                angle = 12f;
                size = diagonalSize;
                break;

            case WarningTapeDirection.DiagonalDown:
                angle = -12f;
                size = diagonalSize;
                break;

            case WarningTapeDirection.CustomAngle:
                angle = customAngle;
                size = diagonalSize;
                break;
        }

        root.localRotation = Quaternion.Euler(0f, 0f, angle);
        root.sizeDelta = size;
    }

    private void ApplyTile()
    {
        ApplyTile(rawImageA);
        ApplyTile(rawImageB);
    }

    private void ApplyTile(RawImage rawImage)
    {
        if (rawImage == null)
            return;

        Rect uv = rawImage.uvRect;
        uv.width = tileRepeatX;
        uv.height = tileRepeatY;
        rawImage.uvRect = uv;
    }

    private IEnumerator ShowRoutine()
    {
        canvasGroup.alpha = 0f;
        root.localScale = defaultScale * punchScale;

        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeInDuration);

            canvasGroup.alpha = t;
            root.localScale = Vector3.Lerp(defaultScale * punchScale, defaultScale, t);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        root.localScale = defaultScale;

        if (autoHideAfter > 0f)
        {
            yield return new WaitForSecondsRealtime(autoHideAfter);
            Hide();
        }
    }

    private IEnumerator HideRoutine()
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeOutDuration);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        root.localScale = defaultScale;
    }

    private void StopCurrentRoutines()
    {
        if (showRoutine != null)
            StopCoroutine(showRoutine);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        showRoutine = null;
        hideRoutine = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (root == null)
            root = transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        ApplyDirection();
        ApplyTile();
    }
#endif
}