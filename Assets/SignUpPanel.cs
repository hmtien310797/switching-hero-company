using System;
using System.Text.RegularExpressions;
using DG.Tweening;
using Immortal_Switch.Scripts.Localization;
using Immortal_Switch.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SignUpPanel : BouncePanel
{
    [SerializeField] private Button backBtn;
    [SerializeField] private Button registerBtn;
    [SerializeField] private TMP_InputField idInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField rePasswordInput;
    [SerializeField] private Toggle agreeToggle;
    [SerializeField] private CheckMark checkMark;
    [SerializeField] private Button rePasswordBtn;
    [SerializeField] private Button informationBtn;

    [Header("References")]
    [SerializeField]
    private LoginPanel loginPanel;

    [Header("Indicators (fill left-to-right by number of conditions met)")]
    public DifficultObject[] indicators;

    [Header("Colors")]
    public Color okColor = new Color32(255, 255, 255, 255);
    public Color notOkColor = new Color32(200, 200, 200, 255);

    [Header("Settings")]
    public int minLength = 6;
    public int maxLength = 18;
    public bool animateOnChange = true;
    public float animScale = 1.15f;
    public float animDur = 0.12f;
    
    private void Start()
    {
        rePasswordBtn.onClick.AddListener(() =>
        {
            Debug.LogWarning("[SignUpPanel] REQUIRE_SUITABLE_PASSWORD");
        });

        // Kiểm tra ID ngay khi nhập (cho phép chữ, số và dấu $)
        idInput?.onValueChanged.AddListener(OnIdInputChanged);
        passwordInput.onValueChanged.AddListener(OnPasswordChanged);
        rePasswordInput.onValueChanged.AddListener(OnRePasswordChanged);
    }

    private void OnIdInputChanged(string value)
    {
        // Chỉ cho phép chữ cái, số và dấu $
        string filtered = Regex.Replace(value, @"[^a-zA-Z0-9$]", "");

        // Giới hạn tối đa 18 ký tự
        if (filtered.Length > maxLength)
        {
            filtered = filtered.Substring(0, 18);
        }

        if (filtered != value)
        {
            idInput.text = filtered;
            // Di chuyển cursor về cuối
            idInput.caretPosition = filtered.Length;
        }
    }

    void OnPasswordChanged(string value)
    {
        // Giới hạn tối đa maxLength ký tự
        if (value.Length > maxLength)
        {
            passwordInput.text = value.Substring(0, maxLength);
            passwordInput.caretPosition = maxLength;
            value = passwordInput.text;
        }

        UpdateIndicators(value);
        bool rs = IsPasswordValid();
        rePasswordInput.interactable = rs;
        rePasswordBtn.enabled = !rs;
        informationBtn.gameObject.SetActive(!rs);
    }

    void OnRePasswordChanged(string value)
    {
        // Giới hạn tối đa maxLength ký tự
        if (value.Length > maxLength)
        {
            rePasswordInput.text = value.Substring(0, maxLength);
            rePasswordInput.caretPosition = maxLength;
            value = rePasswordInput.text;
        }

        bool rs = value.Equals(passwordInput.text);
        checkMark.ShowCheckMark(rs);
    }

    private void ResetData()
    {
        idInput.text = string.Empty;
        checkMark.ShowCheckMark(false);
        passwordInput.text = string.Empty;
        rePasswordInput.text = string.Empty;
        rePasswordInput.interactable = false;
        rePasswordBtn.enabled = true;
        agreeToggle.isOn = false;
        informationBtn.gameObject.SetActive(true);
        for (int i = 0; i < indicators.Length; i++)
        {
            indicators[i].Show(false);
        }
    }

    void UpdateIndicators(string pwd)
    {
        bool lengthOk = pwd.Length >= minLength && pwd.Length <= maxLength;
        bool hasUpper = Regex.IsMatch(pwd, "[A-Z]");
        bool hasDigit = Regex.IsMatch(pwd, "\\d");
        bool hasSpecial = Regex.IsMatch(pwd, "[^a-zA-Z0-9]"); // any non-alphanumeric

        // Số điều kiện đạt được -> sáng lần lượt từ trái qua phải (không để khoảng trống).
        int satisfied = (lengthOk ? 1 : 0) + (hasUpper ? 1 : 0)
                      + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

        for (int i = 0; i < indicators.Length; i++)
        {
            var difficultObject = indicators[i];
            if (difficultObject == null || difficultObject.highLightImage == null) continue;

            bool wasOn = difficultObject.highLightImage.activeSelf;
            bool target = i < satisfied;
            difficultObject.Show(target);

            // Chỉ chạy hiệu ứng khi chuyển off -> on, tránh rung mỗi lần gõ phím
            if (target && !wasOn && animateOnChange)
            {
                difficultObject.rectTransform.DOKill();
                difficultObject.rectTransform.DOPunchScale(Vector3.one * (animScale - 1f), animDur, 6, 0.5f);
            }
        }
    }

    private bool IsPasswordValid()
    {
        var pwd = passwordInput != null ? passwordInput.text : string.Empty;
        bool lengthOk = pwd.Length >= minLength && pwd.Length <= maxLength;
        bool hasUpper = Regex.IsMatch(pwd, "[A-Z]");
        bool hasDigit = Regex.IsMatch(pwd, "\\d");
        bool hasSpecial = Regex.IsMatch(pwd, "[^a-zA-Z0-9]"); // any non-alphanumeric
        return lengthOk && hasUpper && hasDigit && hasSpecial;
    }

    public bool ValidateRegistrationInput()
    {
        if (idInput.text.Length is < 4 or > 18)
        {
            UIManager.Instance.ShowToast(LocalizationManager.GetText("ui_username_min_max_characters"));
            return false;
        }
        
        if (string.IsNullOrEmpty(passwordInput.text) || string.IsNullOrEmpty(rePasswordInput.text) || string.IsNullOrEmpty(idInput.text))
        {
            UIManager.Instance.ShowToast(LocalizationManager.GetText("ui_refill_all_fill"));
            return false;
        }

        if (!passwordInput.text.Equals(rePasswordInput.text))
        {
            UIManager.Instance.ShowToast(LocalizationManager.GetText("ui_pass_not_equal_confirm"));
            return false;
        }

        // if (!agreeToggle.isOn)
        // {
        //     Debug.LogWarning("[SignUpPanel] AGREE_CLAUSE");
        //     return false;
        // }

        return true;
    }

    protected override void OnShow()
    {
        base.OnShow();
        ResetData();
    }
}
