using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BubblesGame.Data;
using BubblesGame.Firebase;

namespace BubblesGame
{
    /// <summary>
    /// Gerencia a UI da cena BubblesMain para seleção de presets e levels
    /// </summary>
    public class BubblesMainUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform presetPanel;
        [SerializeField] private Transform levelPanel;
        [SerializeField] private GameObject presetButtonPrefab;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Button startButton;
        [SerializeField] private ToggleGroup presetToggleGroup;
        [SerializeField] private ToggleGroup levelToggleGroup;

        [Header("Loading UI")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_Text statusText;

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        // Estado interno
        private string selectedPresetId;
        private string selectedLevelId;
        private string currentPresetId;  // Preset atual do usuário
        private string currentLevelId;   // Level atual do usuário

        // Serviços
        private FirebaseLevelLoader levelLoader;
        private GameStateService gameStateService;

        // Cache
        private List<PresetInfo> availablePresets = new List<PresetInfo>();
        private List<LevelData> currentPresetLevels = new List<LevelData>();
        
        // Transforms reais para instanciar botões (Content dentro dos painéis)
        private Transform presetContentTransform;
        private Transform levelContentTransform;
        
        // Flag para evitar loops de eventos
        private bool isUpdatingSelection = false;

        void Start()
        {
            InitializeServices();
            SetupInitialUI();
            LoadUserStateAndPresets();
        }

        void InitializeServices()
        {
            levelLoader = FindObjectOfType<FirebaseLevelLoader>();
            if (levelLoader == null)
            {
                GameObject loaderGO = new GameObject("FirebaseLevelLoader");
                levelLoader = loaderGO.AddComponent<FirebaseLevelLoader>();
            }
            gameStateService = GameStateService.Instance;
        }
        

        void SetupInitialUI()
        {
            presetContentTransform = presetPanel?.GetComponentInChildren<ToggleGroup>()?.transform;
            levelContentTransform = levelPanel?.GetComponentInChildren<ToggleGroup>()?.transform;
            
            if (presetContentTransform == null || levelContentTransform == null || 
                presetToggleGroup == null || levelToggleGroup == null ||
                presetToggleGroup == levelToggleGroup)
            {
                LogError("Configuração de UI inválida! Verifique ToggleGroups no Inspector.");
                return;
            }
            
            if (startButton != null)
            {
                startButton.interactable = false;
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            
            ShowLoading(true, "Carregando presets...");
        }

        void LoadUserStateAndPresets()
        {
            if (gameStateService != null)
            {
                gameStateService.GetOrCreateState(GameConfig.CompanyId, "bubbles",
                    onSuccess: state =>
                    {
                        currentPresetId = state.presetId;
                        currentLevelId = state.levelId;
                        LoadAvailablePresets();
                    },
                    onError: error =>
                    {
                        LogError($"Erro ao carregar estado: {error}");
                        currentPresetId = "preset_1";
                        currentLevelId = "level_1";
                        LoadAvailablePresets();
                    }
                );
            }
            else
            {
                currentPresetId = "preset_1";
                currentLevelId = "level_1";
                LoadAvailablePresets();
            }
        }

        void LoadAvailablePresets()
        {
            SetStatusText("Carregando presets...");
            levelLoader.LoadAllPresets("bubbles",
                onSuccess: presets =>
                {
                    availablePresets = presets;
                    CreatePresetButtons();
                    StartCoroutine(SelectPresetAfterCreation());
                },
                onError: error =>
                {
                    LogError($"Erro ao carregar presets: {error}");
                    ShowLoading(false);
                }
            );
        }

        IEnumerator SelectPresetAfterCreation()
        {
            yield return null;
            SelectPreset(currentPresetId);
        }

        void CreatePresetButtons()
        {
            ClearContainer(presetContentTransform);
            foreach (var preset in availablePresets)
            {
                CreatePresetButton(preset);
            }
        }

        void CreatePresetButton(PresetInfo preset)
        {
            CreateToggleButton(presetButtonPrefab, presetContentTransform, presetToggleGroup, 
                "PresetButton_", preset.PresetId, preset.Description, OnPresetSelected);
        }

        void OnPresetSelected(string presetId)
        {
            if (isUpdatingSelection || selectedPresetId == presetId) return;
            
            selectedPresetId = presetId;
            selectedLevelId = null;
            UpdateStartButtonState();
            
            ShowLoading(true, "Carregando levels...");
            levelLoader.LoadLevels("bubbles", presetId,
                onSuccess: levels =>
                {
                    currentPresetLevels = levels;
                    if (levels.Count == 0)
                    {
                        LogError($"Nenhum level encontrado para preset {presetId}");
                        ShowLoading(false);
                        return;
                    }
                    
                    CreateLevelButtons();
                    string levelToSelect = (presetId == currentPresetId) ? currentLevelId : GetFirstLevelId();
                    if (!string.IsNullOrEmpty(levelToSelect))
                    {
                        SelectLevel(levelToSelect);
                    }
                    ShowLoading(false);
                },
                onError: error =>
                {
                    LogError($"Erro ao carregar levels: {error}");
                    ShowLoading(false);
                }
            );
        }

        void CreateLevelButtons()
        {
            ClearContainer(levelContentTransform);
            foreach (var level in currentPresetLevels)
            {
                CreateLevelButton(level);
            }
        }

        void CreateLevelButton(LevelData level)
        {
            CreateToggleButton(levelButtonPrefab, levelContentTransform, levelToggleGroup, 
                "LevelButton_", level.LevelId, level.Name, OnLevelSelected);
        }
        
        void CreateToggleButton(GameObject prefab, Transform parent, ToggleGroup group, 
            string buttonPrefix, string itemId, string displayName, Action<string> onSelected)
        {
            if (prefab == null || parent == null || group == null)
            {
                LogError($"Configuração inválida para criar botão: {itemId}");
                return;
            }

            GameObject buttonObj = Instantiate(prefab, parent);
            Toggle toggle = buttonObj.GetComponent<Toggle>();
            Text label = buttonObj.GetComponentInChildren<Text>();

            if (toggle == null)
            {
                LogError($"Prefab deve ter componente Toggle! ({itemId})");
                Destroy(buttonObj);
                return;
            }

            foreach (var g in buttonObj.GetComponentsInChildren<ToggleGroup>())
            {
                DestroyImmediate(g);
            }

            toggle.group = group;
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn && !isUpdatingSelection)
                {
                    ForceToggleVisualUpdate(toggle);
                    onSelected?.Invoke(itemId);
                }
            });

            if (label != null)
            {
                label.text = displayName;
            }

            buttonObj.name = $"{buttonPrefix}{itemId}";
        }

        void OnLevelSelected(string levelId)
        {
            if (isUpdatingSelection) return;
            selectedLevelId = levelId;
            UpdateStartButtonState();
        }

        void SelectPreset(string presetId)
        {
            SelectToggle(presetContentTransform, presetToggleGroup, "PresetButton_", presetId,
                availablePresets.Count > 0 ? availablePresets[0].PresetId : null, OnPresetSelected);
        }

        void SelectLevel(string levelId)
        {
            SelectToggle(levelContentTransform, levelToggleGroup, "LevelButton_", levelId, 
                GetFirstLevelId(), OnLevelSelected);
        }
        
        void SelectToggle(Transform parent, ToggleGroup group, string buttonPrefix, string itemId, 
            string fallbackId, Action<string> onSelected)
        {
            isUpdatingSelection = true;
            
            Transform button = parent.Find($"{buttonPrefix}{itemId}");
            if (button == null && !string.IsNullOrEmpty(fallbackId))
            {
                button = parent.Find($"{buttonPrefix}{fallbackId}");
                itemId = fallbackId;
            }
            
            if (button != null)
            {
                Toggle toggle = button.GetComponent<Toggle>();
                if (toggle != null)
                {
                    if (toggle.group != group) toggle.group = group;
                    toggle.isOn = true;
                    ForceToggleVisualUpdate(toggle);
                    isUpdatingSelection = false;
                    onSelected?.Invoke(itemId);
                    return;
                }
            }
            
            isUpdatingSelection = false;
        }

        string GetFirstLevelId()
        {
            if (currentPresetLevels.Count > 0)
            {
                return currentPresetLevels[0].LevelId;
            }
            return null;
        }

        void UpdateStartButtonState()
        {
            if (startButton != null)
            {
                startButton.interactable = !string.IsNullOrEmpty(selectedPresetId) && !string.IsNullOrEmpty(selectedLevelId);
            }
        }

        void OnStartButtonClicked()
        {
            if (string.IsNullOrEmpty(selectedPresetId) || string.IsNullOrEmpty(selectedLevelId))
            {
                LogError("Preset ou Level não selecionado!");
                return;
            }
            
            if (gameStateService != null)
            {
                ShowLoading(true, "Salvando seleção...");
                gameStateService.UpdatePresetAndLevel(GameConfig.CompanyId, "bubbles", selectedPresetId, selectedLevelId,
                    onSuccess: StartGameWithMatchManager,
                    onError: error => { LogError($"Erro ao salvar: {error}"); StartGameWithMatchManager(); }
                );
            }
            else
            {
                StartGameWithMatchManager();
            }
        }

        void StartGameWithMatchManager()
        {
            MatchManager matchManager = FindObjectOfType<MatchManager>();
            if (matchManager == null)
            {
                SceneManager.LoadScene("BubblesGame");
                return;
            }
            ShowLoading(true, "Criando partida...");
            matchManager.StartGame("bubbles", "BubblesGame", selectedPresetId, selectedLevelId);
        }

        /// <summary>
        /// Força atualização visual do Toggle após mudança programática
        /// </summary>
        void ForceToggleVisualUpdate(Toggle toggle)
        {
            if (toggle == null) return;
            
            // Atualiza o Graphic do Toggle manualmente
            var graphic = toggle.graphic;
            if (graphic != null)
            {
                // Força atualização do estado visual
                graphic.SetAllDirty();
                graphic.Rebuild(CanvasUpdate.PreRender);
            }
            
            // Força atualização do Canvas após todas as mudanças
            Canvas.ForceUpdateCanvases();
            
            // Garante que o estado interno do Toggle está correto
            // Não chamamos NotifyToggleOn aqui porque ele já foi chamado automaticamente
            // quando definimos toggle.isOn = true, e chamar novamente pode causar loops
        }

        void ClearContainer(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(container.GetChild(i).gameObject);
            }
        }

        void ShowLoading(bool show, string message = "")
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }

            if (show && !string.IsNullOrEmpty(message))
            {
                SetStatusText(message);
            }
        }

        void SetStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BubblesMainUI] {message}");
            }
        }

        void LogError(string message)
        {
            Debug.LogError($"[BubblesMainUI] {message}");
        }

        // Métodos públicos para botões da UI (se necessário)
        public void OnBackButtonClicked()
        {
            // Volta para menu principal ou cena anterior
            SceneManager.LoadScene("Main");
        }

        public void OnSettingsButtonClicked()
        {
            // Abre painel de configurações (implementar se necessário)
            Log("Configurações - não implementado ainda");
        }
    }
}
