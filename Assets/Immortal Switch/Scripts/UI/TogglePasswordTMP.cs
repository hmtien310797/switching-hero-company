// File: TogglePasswordTMP.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class TogglePasswordTMP : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField inputField;   // kéo TMP InputField vào đây
    public Image iconImage;            // image của nút (eye icon)
    public Sprite eyeOpen;             // icon khi đang hiện mật khẩu
    public Sprite eyeClosed;           // icon khi đang ẩn mật khẩu

    bool isHidden = true;
    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Toggle);
        // đảm bảo trạng thái ban đầu là ẩn (password)
        if (inputField != null)
            SetHidden(true, false);
        UpdateIcon();
    }

    public void Toggle()
    {
        isHidden = !isHidden;
        SetHidden(isHidden, true);
        UpdateIcon();
    }

    void SetHidden(bool hide, bool reactivateField)
    {
        if (inputField == null) return;

        // lưu text để an toàn (thường không thay đổi)
        string txt = inputField.text;

        // đổi kiểu content để bật/tắt masking
        inputField.contentType = hide ? TMP_InputField.ContentType.Password : TMP_InputField.ContentType.Standard;

        // áp dụng lại các setting nội bộ của TMP khi thay contentType
        inputField.ForceLabelUpdate();
        inputField.text = txt;

        if (reactivateField)
        {
            // nếu đang focus, giữ focus và restore caret bằng ActivateInputField
            inputField.ActivateInputField();
        }
    }

    void UpdateIcon()
    {
        if (iconImage == null) return;
        iconImage.sprite = isHidden ? eyeClosed : eyeOpen;
    }

    void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Toggle);
    }
}