using UnityEngine;
using Sirenix.OdinInspector;

public class SelectAccount : MonoBehaviour
{
    public GameObject infoPanel;
    
    [Header("Sub Panels")]
    [SerializeField]
    private LoginPanel loginPanel;

    [SerializeField]
    private SignUpPanel signUpPanel;
    
    [Button]
    public void ShowLogin()
    {
        loginPanel.Show();
        signUpPanel.Hide();
    }

    [Button]
    public void ShowRegister()
    {
        signUpPanel.Show();
        loginPanel.Hide();
    }

    [Button]
    public void HideAll()
    {
        loginPanel.SetVisibleImmediate(false);
        signUpPanel.SetVisibleImmediate(false);
    }
    
    [Button]
    public void ShowAll()
    {
        loginPanel.SetVisibleImmediate(true);
        signUpPanel.SetVisibleImmediate(true);
    }

    public bool ValidateSignUp()
    {
        return signUpPanel.ValidateRegistrationInput();
    }

    public void SaveLoginInfo()
    {
        loginPanel.Save();
    }
}
