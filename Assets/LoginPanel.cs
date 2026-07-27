using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : BouncePanel
{
    [Header("Panel Properties")]
    [SerializeField]
    private TMP_InputField idUiLabel;
    [SerializeField]
    private TMP_InputField passUiLabel;
    [SerializeField]
    private Button signUpBtn;
    [SerializeField]
    private Button signInBtn;
    [SerializeField]
    private Button forgetPassBtn;

    [Header("References")]
    [SerializeField]
    private SignUpPanel signUpPanel;

    private const string ID_LABEL = "ID";
    private const string PASS_LABEL = "PASS";

    protected void Start()
    {
        forgetPassBtn.onClick.AddListener(() =>
        {
            Debug.Log("Forget Password");
        });

        // Kiểm tra ID ngay khi nhập (cho phép chữ, số và dấu $)
        idUiLabel?.onValueChanged.AddListener(OnIdInputChanged);

        // Giới hạn password tối đa 19 ký tự
        passUiLabel?.onValueChanged.AddListener(OnPassInputChanged);
    }

    private void OnIdInputChanged(string value)
    {
        // Chỉ cho phép chữ cái, số và dấu $
        string filtered = Regex.Replace(value, @"[^a-zA-Z0-9$]", "");

        if (filtered != value)
        {
            idUiLabel.text = filtered;
            // Di chuyển cursor về cuối
            idUiLabel.caretPosition = filtered.Length;
        }
    }

    private void OnPassInputChanged(string value)
    {
        // Giới hạn tối đa 19 ký tự
        if (value.Length > 19)
        {
            passUiLabel.text = value.Substring(0, 19);
            // Di chuyển cursor về cuối
            passUiLabel.caretPosition = 19;
        }
    }

    protected override void OnShow()
    {
        base.OnShow();
        idUiLabel.text = PlayerPrefs.GetString(ID_LABEL, "");
        passUiLabel.text = PlayerPrefs.GetString(PASS_LABEL, "");

        idUiLabel.caretPosition = idUiLabel.text.Length;
        passUiLabel.caretPosition = passUiLabel.text.Length;
    }

    public void Save()
    {
        PlayerPrefs.SetString(ID_LABEL, idUiLabel.text);
        PlayerPrefs.SetString(PASS_LABEL, passUiLabel.text);
    }
}
