using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BubblesGame.Data;
using BubblesGame.Data.Services;
using UshiSoft;
using UI.Graphs;

namespace BubblesGame
{
    /// <summary>
    /// Gerencia a UI da cena BubblesResults com os botões de ação
    /// </summary>
    public class BubblesResultsUI : MonoBehaviour
    {
        [Header("Button References")]
        [SerializeField] private Button quitButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button menuButton;

        [Header("Match Results UI References")]
        [SerializeField] private CircularProgressDisplay progressDisplay;

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        private void Start()
        {
            SetupButtons();
            SetupResults();
        }

        private void SetupButtons()
        {
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainButtonClicked);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuButtonClicked);
            }
        }

        private void SetupResults()
        {
            var matchResults = GameStatePersistence.LoadMatchResults();
            if(matchResults == null)
            {
                LogError("No match results found to display!");
                return;
            }

            Log($"Setting up results display - Total Hits: {matchResults.totalHits}");

            //Calcula porcentagem de acertos
            float matchResultsPercentage = (float)matchResults.totalHits / matchResults.totalBalls * 100f;

            if (progressDisplay != null)
                progressDisplay.SetPercentage(matchResultsPercentage);
        }

        private void OnQuitButtonClicked()
        {
            Log("Quit button clicked - cleaning up and quitting...");
            
            // Limpa recursos antes de fechar
            CleanupBeforeQuit();
            
            // Fecha o jogo
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        private void OnPlayAgainButtonClicked()
        {
            Log("Play again button clicked - restarting game with same settings...");
            
            // Restaura estado do jogo (ball-list e cooldown) e inicia nova partida
            RestoreGameStateAndStart();
        }

        private void OnMenuButtonClicked()
        {
            Log("Menu button clicked - returning to BubblesMain...");
            
            // Limpa estado do jogo antes de voltar ao menu
            ClearGameState();
            
            // Carrega BubblesMain
            if (SceneChanger.Instance != null)
            {
                SceneChanger.Instance.ChangeScene("BubblesMain");
            }
            else
            {
                SceneManager.LoadScene("BubblesMain");
            }
        }

        private void CleanupBeforeQuit()
        {
            // Limpa estado do jogo
            ClearGameState();
            
            // Limpa recursos do Firebase se necessário
            var ballListService = FindObjectOfType<BallListService>();
            if (ballListService != null)
            {
                ballListService.ClearCache();
            }
            
            // Força garbage collection
            System.GC.Collect();
            
            Log("Cleanup completed");
        }

        private void RestoreGameStateAndStart()
        {
            // Verifica se há estado salvo
            if (!GameStatePersistence.HasSavedState())
            {
                LogError("No saved game state found! Cannot play again.");
                return;
            }

            // Restaura estado
            var savedState = GameStatePersistence.LoadState();
            if (savedState == null)
            {
                LogError("Failed to load saved game state!");
                return;
            }

            Log($"Restoring game state - Preset: {savedState.presetId}, Level: {savedState.levelId}");

            SceneManager.LoadScene("BubblesGame");

    
            /* Antigo código para restaurar estado diretamente (não funciona pois a cena precisa ser recarregada)
            // Cria nova match e inicia jogo
            MatchManager matchManager = FindObjectOfType<MatchManager>();
            if (matchManager == null)
            {
                LogError("MatchManager not found! Cannot start game.");
                return;
            }

            // Inicia nova partida com os mesmos preset/level
            matchManager.StartGame("bubbles", "BubblesGame", savedState.presetId, savedState.levelId);
            
            // Limpa estado após iniciar (bolas e cooldown já foram usados)
            ClearGameState();
            */
        }

        private void ClearGameState()
        {
            GameStatePersistence.ClearState();
            Log("Game state cleared");
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BubblesResultsUI] {message}");
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[BubblesResultsUI] {message}");
        }
    }
}

