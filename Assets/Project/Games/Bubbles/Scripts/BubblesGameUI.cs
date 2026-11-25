using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace BubblesGame
{
    public class BubblesGameUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private BubblesGameManager gameManager;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject cooldownPanel;
        [SerializeField] private TMP_Text cooldownCountdownText;

        [Header("Results UI")]
        [SerializeField] private TMP_Text finalScoreText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text maxComboText;
        [SerializeField] private TMP_Text totalHitsText;
        [SerializeField] private TMP_Text totalMissesText;
        [SerializeField] private TMP_Text totalBallsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text ballsPerMinText;
        [SerializeField] private TMP_Text avgPointsPerHitText;
        [SerializeField] private TMP_Text statsText; // Mantido para compatibilidade (opcional)

        [Header("Settings")]
        [SerializeField] private bool showTimer = true;
        [SerializeField] private bool showCombo = true;
        [SerializeField] private bool showLives = false;

        [Header("Score Feedback Settings")]
        [SerializeField] private float FeedbackDuration = 0.2f;
        [Space(10)]
        [SerializeField] private Color ScoreUpColor = Color.green;
        [SerializeField] private float ScoreUpScale = 1.5f;
        [Space(10)]
        [SerializeField] private Color ScoreDownColor = Color.red;
        [SerializeField] private float ScoreDownScale = 0.7f;
       

        private bool isGameRunning = false;
        private float levelTotalTime = 0f;
        private Coroutine scoreFeedbackCoroutine;

        void Start()
        {
            // Busca referências se não configuradas
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
            }

            if (gameManager == null)
            {
                gameManager = FindObjectOfType<BubblesGameManager>();
            }

            // Inscreve eventos do ScoreManager
            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged += UpdateScore;
                scoreManager.OnComboChanged += UpdateCombo;
                scoreManager.OnLivesChanged += UpdateLives;
                scoreManager.OnGameOver += ShowGameOver;
            }

            // Inscreve eventos centralizados do jogo
            GameEvents.OnGameStarted += OnGameStarted;
            GameEvents.OnGameEnded += OnGameEnded;
            GameEvents.OnLoadingComplete += OnLoadingComplete;
            GameEvents.OnShowResults += OnShowResultsReceived;
            GameEvents.OnGamePaused += OnGamePaused;
            GameEvents.OnGameResumed += OnGameResumed;

            // Inicializa UI
            ShowLoadingPanel(true);
            ShowPausePanel(false);
            ShowGameOverPanel(false);
            ShowResultsPanel(false);
            ShowSettingsPanel(false);

            UpdateScore(0);
            UpdateCombo(0);
            UpdateLives(scoreManager != null ? scoreManager.CurrentLives : 0);
        }

        void OnDestroy()
        {
            // Desinscreve eventos do ScoreManager
            if (scoreManager != null)
            {
                scoreManager.OnScoreChanged -= UpdateScore;
                scoreManager.OnComboChanged -= UpdateCombo;
                scoreManager.OnLivesChanged -= UpdateLives;
                scoreManager.OnGameOver -= ShowGameOver;
            }

            // Desinscreve eventos centralizados
            GameEvents.OnGameStarted -= OnGameStarted;
            GameEvents.OnGameEnded -= OnGameEnded;
            GameEvents.OnLoadingComplete -= OnLoadingComplete;
            GameEvents.OnShowResults -= OnShowResultsReceived;
            GameEvents.OnGamePaused -= OnGamePaused;
            GameEvents.OnGameResumed -= OnGameResumed;
        }

        /// <summary>
        /// Chamado quando o jogo inicia (via evento)
        /// </summary>
        void OnGameStarted(float startTime)
        {
            ShowLoadingPanel(false);
            isGameRunning = true;
            GameEvents.NotifyLoadingComplete();
        }

        /// <summary>
        /// Chamado quando o loading está completo
        /// </summary>
        void OnLoadingComplete()
        {
            // UI já foi atualizada no OnGameStarted
        }

        /// <summary>
        /// Chamado quando o jogo termina (via evento)
        /// </summary>
        void OnGameEnded(GameEndReason reason)
        {
            isGameRunning = false;
            ShowLoadingPanel(true);
        }

        /// <summary>
        /// Chamado quando deve mostrar resultados (via evento)
        /// </summary>
        void OnShowResultsReceived()
        {
            ShowResultsPanel(true);
        }

        /// <summary>
        /// Chamado quando o jogo é pausado (via evento)
        /// </summary>
        void OnGamePaused()
        {
            // UI já é atualizada pelo ShowPausePanel
        }

        /// <summary>
        /// Chamado quando o jogo é retomado (via evento)
        /// </summary>
        void OnGameResumed()
        {
            // UI já é atualizada pelo ShowPausePanel
        }

        void Update()
        {
            if (isGameRunning)
            {
                if (showTimer)
                {
                    UpdateTimer();
                }
                
                // Gerenciar UI de cooldown
                UpdateCooldownUI();
            }
        }

        void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"{score:N0}";

                // Inicia feedback visual
                if (scoreFeedbackCoroutine != null)
                {
                    StopCoroutine(scoreFeedbackCoroutine);
                }
                scoreFeedbackCoroutine = StartCoroutine(ScoreFeedbacks(scoreText));
            }
        }

        System.Collections.IEnumerator ScoreFeedbacks(TMP_Text tmpText)
        {
            // Determina se o score aumentou ou diminuiu
            bool scoreUp = scoreManager.CurrentScore > scoreManager.LastScore;

            // Configurações iniciais
            float scaleFrom = 1f;
            float scaleTo = scoreUp ? ScoreUpScale : ScoreDownScale;
            Color colorFrom = Color.white;
            Color colorTo = scoreUp ? ScoreUpColor : ScoreDownColor;

            // Animação de feedback
            yield return AnimateFeedback(tmpText, scaleFrom, scaleTo, colorFrom, colorTo, FeedbackDuration);

            // Animação de retorno ao estado normal
            yield return AnimateFeedback(tmpText, scaleTo, scaleFrom, colorTo, colorFrom, FeedbackDuration);

            // Restaura estado final
            tmpText.transform.localScale = Vector3.one;
            tmpText.color = Color.white;
        }

        private System.Collections.IEnumerator AnimateFeedback(TMP_Text tmpText, float fromScale, float toScale, Color fromColor, Color toColor, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                tmpText.transform.localScale = Vector3.Lerp(Vector3.one * fromScale, Vector3.one * toScale, t);
                tmpText.color = Color.Lerp(fromColor, toColor, t);
                yield return null;
            }
        }

        void UpdateCombo(int combo)
        {
            if (!showCombo || comboText == null) return;

            if (combo > 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"COMBO x{combo}";

                // Animação de escala (opcional - substituir com DOTween se disponível)
                // comboText.transform.localScale = Vector3.one * 1.2f;
                //LeanTween.scale(comboText.gameObject, Vector3.one, 0.2f).setEaseOutBack();
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }

        void UpdateLives(int lives)
        {
            if (!showLives || livesText == null) return;

            livesText.text = $"❤ {lives}";
        }

        void UpdateTimer()
        {
            if (timerText == null) return;

            // Calcula tempo restante (countdown)
            float elapsedTime = GameController.Instance != null ? 
                GameController.Instance.GetGameTimestamp() / 1000f : 0f;
            
            float remainingTime = Mathf.Max(0f, levelTotalTime - elapsedTime);
            
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        public void SetLevelTotalTime(float totalTime)
        {
            levelTotalTime = totalTime;
        }

        public void ShowLoadingPanel(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
        }

        public void ShowPausePanel(bool show)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(show);
            }

            if (show)
            {
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
        }

        public void ShowGameOverPanel(bool show)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(show);
            }

            if (show)
            {
                isGameRunning = false;
            }
        }

        public void ShowResultsPanel(bool show)
        {
            if (resultsPanel != null)
            {
                resultsPanel.SetActive(show);
            }

            if (show && scoreManager != null)
            {
                DisplayResults();
            }
        }

        public void ShowSettingsPanel(bool show)
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(show);
            }
        }

        public void ShowCooldownPanel(bool show)
        {
            if (cooldownPanel != null)
            {
                cooldownPanel.SetActive(show);
            }
        }

        public void UpdateCooldownCountdown(float remainingTime)
        {
            if (cooldownCountdownText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                cooldownCountdownText.text = $"COOLDOWN\n{seconds}";
            }
        }

        void UpdateCooldownUI()
        {
            // Validação: GameController deve existir
            if (GameController.Instance == null) 
            {
                ShowCooldownPanel(false);
                return;
            }
            
            float currentTime = GameController.Instance.GetGameTime();
            bool inCooldown = GameController.Instance.IsTimeInCooldown(currentTime);
            
            ShowCooldownPanel(inCooldown);
            
            if (inCooldown)
            {
                float remainingTime = GameController.Instance.GetCurrentCooldownRemaining(currentTime);
                UpdateCooldownCountdown(remainingTime);
            }
        }

        void ShowGameOver()
        {
            ShowGameOverPanel(true);
            // Mostra resultados após 2 segundos
            Invoke(nameof(ShowResults), 2f);
        }

        void ShowResults()
        {
            ShowGameOverPanel(false);
            ShowResultsPanel(true);
        }

        void DisplayResults()
        {
            if (scoreManager == null) return;

            MatchStats stats = scoreManager.GetMatchStats();

            // Score final
            if (finalScoreText != null)
            {
                finalScoreText.text = $"{stats.finalScore:N0}";
            }

            // Precisão
            if (accuracyText != null)
            {
                accuracyText.text = $"{stats.accuracy:F1}%";
            }

            // Combo máximo
            if (maxComboText != null)
            {
                maxComboText.text = $"x{stats.maxCombo}";
            }

            // Total de hits
            if (totalHitsText != null)
            {
                totalHitsText.text = $"{stats.totalHits}";
            }

            // Total de misses
            if (totalMissesText != null)
            {
                totalMissesText.text = $"{stats.totalMisses}";
            }

            // Total de bolas
            if (totalBallsText != null)
            {
                totalBallsText.text = $"{stats.totalBalls}";
            }

            // Tempo total (formata como MM:SS)
            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(stats.totalGameTime / 60f);
                int seconds = Mathf.FloorToInt(stats.totalGameTime % 60f);
                timeText.text = $"{minutes:00}:{seconds:00}";
            }

            // Bolas por minuto
            if (ballsPerMinText != null)
            {
                ballsPerMinText.text = $"{stats.ballsPerMinute:F1}";
            }

            // Pontos médios por hit
            if (avgPointsPerHitText != null)
            {
                avgPointsPerHitText.text = $"{stats.averagePointsPerHit:F0}";
            }

            // StatsText mantido para compatibilidade (opcional)
            if (statsText != null)
            {
                int minutes = Mathf.FloorToInt(stats.totalGameTime / 60f);
                int seconds = Mathf.FloorToInt(stats.totalGameTime % 60f);
                string timeStr = $"{minutes:00}:{seconds:00}";

                statsText.text = $"Hits: {stats.totalHits}\n" +
                                 $"Misses: {stats.totalMisses}\n" +
                                 $"Total: {stats.totalBalls}\n" +
                                 $"Tempo: {timeStr}\n" +
                                 $"Bolas/min: {stats.ballsPerMinute:F1}\n" +
                                 $"Pontos/hit: {stats.averagePointsPerHit:F0}";
            }
        }

        // Botões UI
        public void OnPauseButtonClicked()
        {
            ShowPausePanel(true);
            if (gameManager != null)
            {
                gameManager.PauseGame();
            }
        }

        public void OnResumeButtonClicked()
        {
            ShowPausePanel(false);
            if (gameManager != null)
            {
                gameManager.ResumeGame();
            }
        }

        public void OnRestartButtonClicked()
        {
            Time.timeScale = 1f;
            if (gameManager != null)
            {
                gameManager.RestartLevel();
            }
            
            if (scoreManager != null)
            {
                scoreManager.ResetScore();
            }

            ShowPausePanel(false);
            ShowGameOverPanel(false);
            ShowResultsPanel(false);
        }

        public void OnQuitButtonClicked()
        {
            // Usa o GameController para finalizar o jogo de forma segura
            if (GameController.Instance != null)
            {
                GameController.Instance.OnBackButtonPressed();
            }
            else
            {
                // Fallback se GameController não estiver disponível
                Time.timeScale = 1f;
                SceneManager.LoadScene("BubblesMain");
            }
        }

        public void OnSettingsButtonClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(!settingsPanel.activeInHierarchy);
            }
        }

        // Botões da tela de resultados
        public void OnReplayButtonClicked()
        {
            Time.timeScale = 1f;
            // Recarrega a mesma cena (BubblesGame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnNextLevelButtonClicked()
        {
            // TODO: Implementar progressão de níveis
            // Por enquanto, desabilitado
            Debug.Log("[BubblesGameUI] Próximo level - ainda não implementado");
        }

        public void OnMainMenuButtonClicked()
        {
            Time.timeScale = 1f;
            // Volta para BubblesMain
            if (GameController.Instance != null)
            {
                GameController.Instance.OnBackButtonPressed();
            }
            else
            {
                SceneManager.LoadScene("BubblesMain");
            }
        }

    }
}

