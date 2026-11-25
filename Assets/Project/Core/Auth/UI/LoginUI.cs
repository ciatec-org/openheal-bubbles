using UnityEngine;
using TMPro;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_Text messageText;
    
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private ForgotPasswordPopup forgotPasswordPopup;

    public void OnForgotPasswordButton()
    {
        if (forgotPasswordPopup != null)
        {
            forgotPasswordPopup.OpenPopup();
        }
    }

    public void OnLoginButton()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        messageText.text = "wait...";

        AuthManager.Instance.SignIn(email, password, (success, msg) =>
        {
            messageText.text = msg;
            
            if (success)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
            }
        });
    }
}
