using UnityEngine;
using TMPro;

public class SignupUI : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public TMP_Text messageText;

    public void OnSignupButton()
    {
        string email = emailInput.text;
        string pass = passwordInput.text;
        string confirm = confirmPasswordInput.text;

        if (pass != confirm)
        {
            messageText.text = "Passwords do not match.";
            return;
        }

        messageText.text = "Creating account...";

        AuthManager.Instance.SignUp(email, pass, (success, msg) =>
        {
            messageText.text = msg;

            if (success)
            {
                Debug.Log("Account created: " + AuthManager.Instance.CurrentUser.Email);
                // mudança de cena depois
            }
        });
    }
}
