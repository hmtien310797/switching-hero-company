using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HeroJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum JoystickMode
    {
        Fixed,
        Floating
    }

    private HeroTeamController heroTeamController;

    [Header("References")]
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private Canvas canvas;

    [Header("Settings")]
    [SerializeField] private JoystickMode joystickMode = JoystickMode.Fixed;
    [SerializeField] private float radius = 120f;
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private bool hideWhenIdle = false;
    [SerializeField] private float fullInputDistance = 40f;
    [SerializeField] private bool snapToFullInput = false;

    [Header("Output")]
    [SerializeField] private Vector2 input;
    [SerializeField] private float inputMagnitude;
    
    [Header("Handle Animation")]
    [SerializeField] private bool rotateHandleToDirection = true;
    [SerializeField] private bool scaleHandleByInput = true;
    [SerializeField] private bool scaleBackgroundByInput = true;

    [SerializeField] private float handleRotateSmoothSpeed = 20f;
    [SerializeField] private float handleMaxScaleBonus = 0.18f;
    [SerializeField] private float backgroundMaxScaleBonus = 0.08f;

    private Vector3 handleDefaultScale;
    private Vector3 backgroundDefaultScale;
    public Vector2 Input => input;
    public float InputMagnitude => inputMagnitude;
    public bool IsDragging { get; private set; }

    private Camera uiCamera;
    private Vector2 startAnchoredPosition;

    private void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (joystickRoot == null)
            joystickRoot = transform as RectTransform;

        if (background == null)
            background = joystickRoot;
        
        if (handle != null)
            handleDefaultScale = handle.localScale;

        if (background != null)
            backgroundDefaultScale = background.localScale;

        startAnchoredPosition = joystickRoot.anchoredPosition;

        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        SetVisualActive(!hideWhenIdle);
        ResetHandle();
    }

    public void SetTarget(HeroTeamController target)
    {
        heroTeamController = target;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        IsDragging = true;

        if (joystickMode == JoystickMode.Floating)
        {
            SetJoystickPosition(eventData);
            SetVisualActive(true);
        }

        UpdateInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        IsDragging = false;

        input = Vector2.zero;
        inputMagnitude = 0f;

        ResetHandle();

        heroTeamController?.ClearMoveInput();

        if (joystickMode == JoystickMode.Floating)
        {
            joystickRoot.anchoredPosition = startAnchoredPosition;

            if (hideWhenIdle)
                SetVisualActive(false);
        }
    }

    private void UpdateInput(PointerEventData eventData)
    {
        if (background == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            uiCamera,
            out Vector2 localPoint
        );

        // Vị trí dùng để hiển thị cục handle.
        // Handle không được đi quá vòng joystick.
        Vector2 visualPoint = localPoint;

        if (visualPoint.magnitude > radius)
            visualPoint = visualPoint.normalized * radius;

        // Input dùng để điều khiển hero.
        // fullInputDistance càng nhỏ thì kéo nhẹ càng nhanh đạt input max.
        Vector2 rawInput = localPoint / fullInputDistance;

        if (rawInput.magnitude > 1f)
            rawInput.Normalize();

        if (rawInput.magnitude < deadZone)
        {
            rawInput = Vector2.zero;
        }
        else if (snapToFullInput)
        {
            rawInput = rawInput.normalized;
        }

        input = rawInput;
        inputMagnitude = input.magnitude;

        UpdateHandleVisual(visualPoint, input);

        heroTeamController?.SetMoveInput(input);
    }
    
    
    private void UpdateHandleVisual(Vector2 visualPoint, Vector2 moveInput)
    {
        float magnitude = moveInput.magnitude;

        if (handle != null)
        {
            handle.anchoredPosition = visualPoint;

            if (rotateHandleToDirection && moveInput.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;

                Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);

                handle.localRotation = Quaternion.Lerp(
                    handle.localRotation,
                    targetRotation,
                    Time.unscaledDeltaTime * handleRotateSmoothSpeed
                );
            }

            if (scaleHandleByInput)
            {
                float scaleBonus = magnitude * handleMaxScaleBonus;
                handle.localScale = handleDefaultScale * (1f + scaleBonus);
            }
        }

        if (background != null && scaleBackgroundByInput)
        {
            float scaleBonus = magnitude * backgroundMaxScaleBonus;
            background.localScale = backgroundDefaultScale * (1f + scaleBonus);
        }
    }

    private void SetJoystickPosition(PointerEventData eventData)
    {
        if (joystickRoot == null)
            return;

        RectTransform parentRect = joystickRoot.parent as RectTransform;

        if (parentRect == null)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            uiCamera,
            out Vector2 localPoint
        );

        joystickRoot.anchoredPosition = localPoint;
    }

    private void ResetHandle()
    {
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
            handle.localRotation = Quaternion.identity;
            handle.localScale = handleDefaultScale;
        }

        if (background != null)
        {
            background.localScale = backgroundDefaultScale;
        }
    }

    private void SetVisualActive(bool active)
    {
        if (joystickRoot != null)
            joystickRoot.gameObject.SetActive(active);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        radius = Mathf.Max(1f, radius);
        deadZone = Mathf.Clamp01(deadZone);
    }
#endif
}