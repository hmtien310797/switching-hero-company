using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class ToggleSwitch : MonoBehaviour
{
    [Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    [Header("State")]
    [SerializeField] private bool isOn = true;

    [Header("References")]
    [SerializeField] private Button clickButton;

    [Tooltip("Object sẽ trượt. Ví dụ: Slider")]
    [SerializeField] private RectTransform selector;

    [SerializeField] private TMP_Text onText;
    [SerializeField] private TMP_Text offText;

    [Header("Position")]
    [Tooltip("Empty RectTransform làm mốc ON")]
    [SerializeField] private RectTransform onPosition;

    [Tooltip("Empty RectTransform làm mốc OFF")]
    [SerializeField] private RectTransform offPosition;

    [Header("Visual")]
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(0.45f, 0.35f, 0.25f, 1f);

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.18f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Debug")]
    [SerializeField] private bool debugLog;

    [Header("Event")]
    public BoolEvent onValueChanged = new BoolEvent();

    private Tween moveTween;

    public bool IsOn => isOn;

    private void Awake()
    {
        if (clickButton == null)
            clickButton = GetComponent<Button>();

        if (clickButton != null)
        {
            clickButton.onClick.RemoveListener(Toggle);
            clickButton.onClick.AddListener(Toggle);
        }
        else
        {
            Debug.LogWarning($"[{nameof(ToggleSwitch)}] Missing Button on {name}", this);
        }

        RefreshInstant();
    }

    private void OnDestroy()
    {
        moveTween?.Kill();

        if (clickButton != null)
            clickButton.onClick.RemoveListener(Toggle);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (clickButton == null)
            clickButton = GetComponent<Button>();

        if (!Application.isPlaying)
            RefreshInstant();
    }
#endif

    public void Toggle()
    {
        SetIsOn(!isOn);
    }

    public void SetIsOn(bool value)
    {
        if (isOn == value)
            return;

        isOn = value;

        if (debugLog)
            Debug.Log($"[{nameof(ToggleSwitch)}] {name} changed: {isOn}", this);

        RefreshAnimated();
        onValueChanged?.Invoke(isOn);
    }

    public void SetIsOnWithoutNotify(bool value)
    {
        isOn = value;
        RefreshInstant();
    }

    private void RefreshInstant()
    {
        moveTween?.Kill();

        if (selector != null)
            selector.localPosition = GetTargetLocalPosition();

        RefreshTextColor();
    }

    private void RefreshAnimated()
    {
        moveTween?.Kill();

        if (selector != null)
        {
            moveTween = selector
                .DOLocalMove(GetTargetLocalPosition(), moveDuration)
                .SetEase(moveEase)
                .SetUpdate(true);
        }

        RefreshTextColor();
    }

    private Vector3 GetTargetLocalPosition()
    {
        RectTransform target = isOn ? onPosition : offPosition;

        if (target == null)
            return Vector3.zero;

        return target.localPosition;
    }

    private void RefreshTextColor()
    {
        if (onText != null)
            onText.color = isOn ? activeTextColor : inactiveTextColor;

        if (offText != null)
            offText.color = isOn ? inactiveTextColor : activeTextColor;
    }
}