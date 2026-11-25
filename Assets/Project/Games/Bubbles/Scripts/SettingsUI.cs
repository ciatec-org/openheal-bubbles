using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BubblesGame.Data;
using TMPro;

namespace BubblesGame
{
    /// <summary>
    /// UI para configurar sensibilidades de colisão do usuário
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField generalSensitivityInput;
        [SerializeField] private TMP_InputField hitSensitivityInput;
        [SerializeField] private TMP_InputField destroySensitivityInput;
        
        [Header("Buttons")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;
        
        [Header("Preview")]
        [SerializeField] private Toggle showPreviewToggle;
        [SerializeField] private GameObject previewContainer;

        [Header("Audio")]
        [SerializeField] private Toggle muteSoundToggle;
        
        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI feedbackMessage;

        [Header("Panel Reference")]
        [SerializeField] private GameObject settingsPanel; // Referência opcional ao painel visual

        // State
        private UserGameSettings currentSettings;
        private string userId;
        private string companyId;
        private string gameId;
        private bool isInitialized = false;

        void Start()
        {
            InitializeUI();
            // Carrega settings diretamente do SessionManager (já inicializado no login)
            LoadCurrentSettings();
        }

        void InitializeUI()
        {
            // Configura input fields
            if (generalSensitivityInput != null)
            {
                generalSensitivityInput.contentType = TMP_InputField.ContentType.DecimalNumber;
                generalSensitivityInput.onEndEdit.AddListener(OnGeneralSensitivityChanged);
                // Safety check para placeholder
                if (generalSensitivityInput.placeholder != null)
                {
                    TextMeshProUGUI placeholderText = generalSensitivityInput.placeholder.GetComponent<TextMeshProUGUI>();
                    if (placeholderText != null)
                        placeholderText.text = "0.01 - 0.15";
                }
            }

            if (hitSensitivityInput != null)
            {
                hitSensitivityInput.contentType = TMP_InputField.ContentType.DecimalNumber;
                hitSensitivityInput.onEndEdit.AddListener(OnHitSensitivityChanged);
                // Safety check para placeholder
                if (hitSensitivityInput.placeholder != null)
                {
                    TextMeshProUGUI placeholderText = hitSensitivityInput.placeholder.GetComponent<TextMeshProUGUI>();
                    if (placeholderText != null)
                        placeholderText.text = "0.0 - 0.1";
                }
            }

            if (destroySensitivityInput != null)
            {
                destroySensitivityInput.contentType = TMP_InputField.ContentType.DecimalNumber;
                destroySensitivityInput.onEndEdit.AddListener(OnDestroySensitivityChanged);
                // Safety check para placeholder
                if (destroySensitivityInput.placeholder != null)
                {
                    TextMeshProUGUI placeholderText = destroySensitivityInput.placeholder.GetComponent<TextMeshProUGUI>();
                    if (placeholderText != null)
                        placeholderText.text = "0.0 - 0.1";
                }
            }

            // Configura botões
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(SaveSettings);
                Debug.Log("[SettingsUI] Save button listener added");
            }
            else
            {
                Debug.LogWarning("[SettingsUI] Save button not assigned!");
            }
            
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetToDefaults);
                Debug.Log("[SettingsUI] Reset button listener added");
            }
            else
            {
                Debug.LogWarning("[SettingsUI] Reset button not assigned!");
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettings);
                Debug.Log("[SettingsUI] Close button listener added");
            }
            else
            {
                Debug.LogWarning("[SettingsUI] Close button not assigned!");
            }

            // Configura preview
            if (showPreviewToggle != null)
                showPreviewToggle.onValueChanged.AddListener(OnPreviewToggleChanged);

            // Configura mute sound toggle
            if (muteSoundToggle != null)
                muteSoundToggle.onValueChanged.AddListener(OnMuteSoundToggleChanged);

            // Esconde mensagem de feedback
            HideFeedbackMessage();
        }

        void LoadCurrentSettings()
        {
            Debug.Log("[SettingsUI] LoadCurrentSettings iniciado");
            
            // Obtém dados do SessionManager (cache)
            userId = SessionManager.GetUserId();
            companyId = SessionManager.GetCompanyId();
            gameId = SessionManager.GetCurrentGameId();
            
            // Fallback para gameId se não estiver na sessão
            if (string.IsNullOrEmpty(gameId))
            {
                gameId = "bubbles";
            }

            Debug.Log($"[SettingsUI] Session data: companyId={companyId}, gameId={gameId}, userId={userId}");
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
            {
                Debug.LogError("[SettingsUI] Invalid session data! AuthGuard should prevent this.");
                currentSettings = UserGameSettings.CreateDefault();
                UpdateUI();
                return;
            }

            // Carrega do Firebase
            UserSettingsService.Instance.GetOrCreateUserSettings(
                userId, companyId, gameId,
                onSuccess: settings =>
                {
                    currentSettings = settings;
                    UpdateUI();
                    isInitialized = true;
                    Debug.Log($"[SettingsUI] Settings loaded: {settings}");
                },
                onError: error =>
                {
                    Debug.LogError($"[SettingsUI] Failed to load settings: {error}");
                    ShowFeedbackMessage($"Erro ao carregar configurações: {error}", Color.red);
                    currentSettings = UserGameSettings.CreateDefault();
                    UpdateUI();
                }
            );
        }

        void UpdateUI()
        {
            if (currentSettings == null) return;

            // Atualiza input fields
            if (generalSensitivityInput != null)
                generalSensitivityInput.text = currentSettings.GeneralSensitivity.ToString("F3");
            
            if (hitSensitivityInput != null)
                hitSensitivityInput.text = currentSettings.HitLandmarksSensitivity.ToString("F3");
            
            if (destroySensitivityInput != null)
                destroySensitivityInput.text = currentSettings.DestroyLandmarksSensitivity.ToString("F3");

            // Atualiza preview
            if (showPreviewToggle != null)
                showPreviewToggle.isOn = currentSettings.ShowCollisionRadius;
        }

        // Event handlers
        void OnGeneralSensitivityChanged(string valueText)
        {
            if (currentSettings != null && float.TryParse(valueText, out float value))
            {
                // Valida range
                value = Mathf.Clamp(value, 0.01f, 0.15f);
                currentSettings.GeneralSensitivity = value;
                
                // Atualiza o campo se o valor foi clampado
                if (generalSensitivityInput != null && !Mathf.Approximately(value, float.Parse(valueText)))
                {
                    generalSensitivityInput.text = value.ToString("F3");
                }
                
                UpdatePreview();
            }
            else if (currentSettings != null)
            {
                // Valor inválido, restaura o anterior
                if (generalSensitivityInput != null)
                    generalSensitivityInput.text = currentSettings.GeneralSensitivity.ToString("F3");
            }
        }

        void OnHitSensitivityChanged(string valueText)
        {
            if (currentSettings != null && float.TryParse(valueText, out float value))
            {
                // Valida range
                value = Mathf.Clamp(value, 0f, 0.1f);
                currentSettings.HitLandmarksSensitivity = value;
                
                // Atualiza o campo se o valor foi clampado
                if (hitSensitivityInput != null && !Mathf.Approximately(value, float.Parse(valueText)))
                {
                    hitSensitivityInput.text = value.ToString("F3");
                }
                
                UpdatePreview();
            }
            else if (currentSettings != null)
            {
                // Valor inválido, restaura o anterior
                if (hitSensitivityInput != null)
                    hitSensitivityInput.text = currentSettings.HitLandmarksSensitivity.ToString("F3");
            }
        }

        void OnDestroySensitivityChanged(string valueText)
        {
            if (currentSettings != null && float.TryParse(valueText, out float value))
            {
                // Valida range
                value = Mathf.Clamp(value, 0f, 0.1f);
                currentSettings.DestroyLandmarksSensitivity = value;
                
                // Atualiza o campo se o valor foi clampado
                if (destroySensitivityInput != null && !Mathf.Approximately(value, float.Parse(valueText)))
                {
                    destroySensitivityInput.text = value.ToString("F3");
                }
                
                UpdatePreview();
            }
            else if (currentSettings != null)
            {
                // Valor inválido, restaura o anterior
                if (destroySensitivityInput != null)
                    destroySensitivityInput.text = currentSettings.DestroyLandmarksSensitivity.ToString("F3");
            }
        }

        void OnPreviewToggleChanged(bool enabled)
        {
            if (currentSettings != null)
            {
                currentSettings.ShowCollisionRadius = enabled;
                UpdatePreview();
            }

            if (previewContainer != null)
                previewContainer.SetActive(enabled);
        }

        void UpdatePreview()
        {
            // Atualiza preview visual em tempo real
            PoseHitDetector detector = FindObjectOfType<PoseHitDetector>();
            if (detector != null && currentSettings != null)
            {
                detector.SetSensitivitySettings(currentSettings);
            }
        }

        void SaveSettings()
        {
            Debug.Log("[SettingsUI] Save button clicked");
            
            if (!isInitialized || currentSettings == null)
            {
                Debug.LogWarning("[SettingsUI] Settings not initialized, cannot save");
                ShowFeedbackMessage("Configurações não carregadas", Color.red);
                return;
            }

            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[SettingsUI] User ID is empty, cannot save");
                ShowFeedbackMessage("Usuário não identificado", Color.red);
                return;
            }

            HideFeedbackMessage();

            UserSettingsService.Instance.SaveUserSettings(
                userId, companyId, gameId, currentSettings,
                onSuccess: () =>
                {
                    ShowFeedbackMessage("Configurações salvas com sucesso!", Color.green);
                    Debug.Log("[SettingsUI] Settings saved successfully");
                },
                onError: error =>
                {
                    ShowFeedbackMessage($"Erro ao salvar: {error}", Color.red);
                    Debug.LogError($"[SettingsUI] Failed to save settings: {error}");
                }
            );
        }

        void ResetToDefaults()
        {
            Debug.Log("[SettingsUI] Reset button clicked");
            currentSettings = UserGameSettings.CreateDefault();
            UpdateUI();
            UpdatePreview();
            
            // Automaticamente salva as configurações padrão
            if (isInitialized && !string.IsNullOrEmpty(userId))
            {
                UserSettingsService.Instance.SaveUserSettings(
                    userId, companyId, gameId, currentSettings,
                    onSuccess: () =>
                    {
                        ShowFeedbackMessage("Configurações resetadas e salvas automaticamente!", Color.green);
                        Debug.Log("[SettingsUI] Settings reset to defaults and saved automatically");
                    },
                    onError: error =>
                    {
                        ShowFeedbackMessage($"Resetado mas erro ao salvar: {error}", new Color(1f, 0.65f, 0f));
                        Debug.LogError($"[SettingsUI] Settings reset but failed to save: {error}");
                    }
                );
            }
            else
            {
                ShowFeedbackMessage("Configurações resetadas para padrão", Color.yellow);
                Debug.Log("[SettingsUI] Settings reset to defaults (not saved - user not initialized)");
            }
        }

        void CloseSettings()
        {
            Debug.Log("[SettingsUI] Close button clicked");
            
            // Tenta esconder o painel através do BubblesGameUI
            BubblesGameUI gameUI = FindObjectOfType<BubblesGameUI>();
            if (gameUI != null)
            {
                gameUI.ShowSettingsPanel(false);
                Debug.Log("[SettingsUI] Settings panel hidden via BubblesGameUI");
            }
            // Fallback: se tiver referência direta ao painel
            else if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
                Debug.Log("[SettingsUI] Settings panel hidden via direct reference");
            }
            // Último recurso: desativa este GameObject
            else
            {
                Debug.LogWarning("[SettingsUI] Could not find BubblesGameUI or settingsPanel reference, deactivating self");
                gameObject.SetActive(false);
            }
        }

        void ShowFeedbackMessage(string message, Color color)
        {
            if (feedbackMessage != null)
            {
                feedbackMessage.text = message;
                feedbackMessage.color = color;
                feedbackMessage.gameObject.SetActive(true);
                // Auto-hide após 3 segundos
                Invoke(nameof(HideFeedbackMessage), 3f);
            }
        }

        void HideFeedbackMessage()
        {
            if (feedbackMessage != null)
                feedbackMessage.gameObject.SetActive(false);
        }

        string GetCurrentUserId()
        {
            return SessionManager.GetUserId();
        }

        // Métodos públicos para integração
        public void OpenSettings()
        {
            gameObject.SetActive(true);
            LoadCurrentSettings();
        }

        public void SetGameData(string userId, string companyId, string gameId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
        }


        void OnDestroy()
        {
            // Cleanup listeners dos input fields
            if (generalSensitivityInput != null)
                generalSensitivityInput.onEndEdit.RemoveAllListeners();
            if (hitSensitivityInput != null)
                hitSensitivityInput.onEndEdit.RemoveAllListeners();
            if (destroySensitivityInput != null)
                destroySensitivityInput.onEndEdit.RemoveAllListeners();
            
            // Cleanup listeners dos botões
            if (saveButton != null)
                saveButton.onClick.RemoveAllListeners();
            if (resetButton != null)
                resetButton.onClick.RemoveAllListeners();
            if (closeButton != null)
                closeButton.onClick.RemoveAllListeners();
            
            // Cleanup listener do toggle
            if (showPreviewToggle != null)
                showPreviewToggle.onValueChanged.RemoveAllListeners();

            if (muteSoundToggle != null)
                muteSoundToggle.onValueChanged.RemoveAllListeners();
        }

        // Handler para o toggle de mute sound
        void OnMuteSoundToggleChanged(bool isMuted)
        {
            var audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.SetAudioEnabled(!isMuted);
            }
        }
    }
}
