using UnityEngine;
using TMPro;

public class ForgotPasswordPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private UnityEngine.UI.Button sendButton;
    [SerializeField] private UnityEngine.UI.Button closeButton;

    private void Start()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButton);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    public void OpenPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }

        if (emailInput != null)
        {
            emailInput.text = "";
        }

        if (messageText != null)
        {
            messageText.text = "";
        }
    }

    public void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void OnSendButton()
    {
        if (emailInput == null || AuthManager.Instance == null)
        {
            return;
        }

        string email = emailInput.text;

        if (string.IsNullOrEmpty(email))
        {
            if (messageText != null)
            {
                messageText.text = "Please enter your email address.";
            }
            return;
        }

        // Cancela qualquer Invoke anterior
        CancelInvoke();

        if (messageText != null)
        {
            messageText.text = "Sending reset email...";
        }

        AuthManager.Instance.SendPasswordResetEmail(email, (success, msg) =>
        {
            if (messageText != null)
            {
                messageText.text = msg;
            }

            if (success)
            {
                // Limpa a mensagem e fecha o popup após 2 segundos
                Invoke(nameof(ClearMessageAndClose), 2f);
            }
        });
    }

    private void ClearMessageAndClose()
    {
        if (messageText != null)
        {
            messageText.text = "";
        }
        ClosePopup();
    }
}

