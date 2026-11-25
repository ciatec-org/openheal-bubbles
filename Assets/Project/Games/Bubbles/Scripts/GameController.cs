using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using Core.CameraSystem;
using Core.Tracking;
using BubblesGame.Data;

namespace BubblesGame
{
    /// <summary>
    /// Controlador central do jogo que gerencia o ciclo de vida completo:
    /// - Cronômetro master para timestamps unificados
    /// - Controle de início e fim de jogo
    /// - Limpeza de recursos e memória
    /// - Navegação segura entre cenas
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        // Estado do jogo
        private bool isGameActive = false;
        private bool isCleaningUp = false;
        private float levelTotalTime = 0f; // Duração total do level em segundos
        
        // Timers reutilizáveis
        private GameTimer mainTimer = new GameTimer(); // Timer principal do jogo
        private GameTimer cooldownTimer = new GameTimer(); // Timer do cooldown (independente)
        
        // Sistema de cooldown
        private List<CoolDownPeriod> coolDownPeriods = new List<CoolDownPeriod>();
        private int currentCooldownIndex = -1; // Índice do cooldown ativo (-1 se nenhum)
        private int pendingCooldownIndex = -1; // Índice do cooldown aguardando bolas desaparecerem
        private HashSet<int> processedCooldowns = new HashSet<int>(); // Cooldowns já processados (para evitar loop)

        [Header("System References")]
        [SerializeField] private BubblesGameManager gameManager;
        [SerializeField] private WebcamCapture webcamCapture;
        [SerializeField] private PoseController poseController;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private PoseDataManager poseDataManager;
        [SerializeField] private BubblesGame.Data.Services.BallEventRecorder ballEventRecorder;

        // Eventos (mantidos para compatibilidade, mas preferir GameEvents)
        public event Action<float> OnGameStarted;
        public event Action<float> OnGameEnded;

        // Propriedades públicas
        public bool IsGameActive => isGameActive;
        public float MasterStartTime => mainTimer.IsRunning ? Time.time - mainTimer.GetElapsedTime() : 0f;

        // Estado de inicialização
        private bool isLevelReady = false;
        private bool isUserSettingsReady = false;

        void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                Log("GameController initialized");
                
                // Notifica BubblesGameManager que está disponível (se já existir)
                NotifyManagersOfAvailability();
            }
            else
            {
                Log("Duplicate GameController destroyed");
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Notifica managers que o GameController está disponível
        /// </summary>
        void NotifyManagersOfAvailability()
        {
            // Busca BubblesGameManager e notifica
            BubblesGameManager gameManager = FindObjectOfType<BubblesGameManager>();
            if (gameManager != null)
            {
                gameManager.NotifyGameControllerAvailable();
                Log("BubblesGameManager notificado da disponibilidade do GameController");
            }
        }

        void Start()
        {
            // Busca referências dos sistemas existentes
            CacheSystemReferences();
            
            // Inscreve eventos do sistema de eventos centralizado
            GameEvents.OnLevelReady += OnLevelReadyReceived;
            GameEvents.OnUserSettingsReady += OnUserSettingsReadyReceived;
            
            // NÃO inicia automaticamente - aguarda eventos de ready
            Log("GameController initialized, waiting for level and settings to be ready...");
        }

        void Update()
        {
            if (!isGameActive) return;
            
            // Atualiza tracking de cooldown
            UpdateCooldownTracking();
            
            // Verifica se o tempo do level acabou
            if (levelTotalTime > 0f)
            {
                float currentGameTime = GetGameTimestamp() / 1000f;
                if (currentGameTime >= levelTotalTime)
                {
                    Log($"Level time up! Current: {currentGameTime:F1}s, Total: {levelTotalTime:F1}s");
                    OnTimeUp();
                }
            }
        }
        
        /// <summary>
        /// Atualiza o tracking de cooldown e controla pausa/resume do timer principal
        /// </summary>
        private void UpdateCooldownTracking()
        {
            float gameTime = mainTimer.GetElapsedTime();
            
            if (currentCooldownIndex >= 0)
            {
                // Já estamos em cooldown - verifica se deve sair
                var period = coolDownPeriods[currentCooldownIndex];
                float cooldownElapsed = cooldownTimer.GetElapsedTime();
                
                if (cooldownElapsed >= period.duration)
                {
                    // Cooldown terminou - marca como processado, retoma timer principal e para cooldown
                    processedCooldowns.Add(currentCooldownIndex);
                    cooldownTimer.Stop();
                    mainTimer.Resume();
                    currentCooldownIndex = -1;
                    Log($"Cooldown ended, main timer resumed at {gameTime:F1}s");
                }
            }
            else if (pendingCooldownIndex >= 0)
            {
                // Aguardando bolas desaparecerem antes de iniciar cooldown
                if (AreAllBallsCleared())
                {
                    // Todas as bolas desapareceram - inicia cooldown
                    currentCooldownIndex = pendingCooldownIndex;
                    var period = coolDownPeriods[currentCooldownIndex];
                    
                    // Pausa timer principal e inicia timer de cooldown
                    mainTimer.Pause();
                    cooldownTimer.Start();
                    pendingCooldownIndex = -1;
                    Log($"Cooldown started at game time {gameTime:F1}s, duration {period.duration}s");
                }
            }
            else
            {
                // Não estamos em cooldown - verifica se devemos entrar
                for (int i = 0; i < coolDownPeriods.Count; i++)
                {
                    // Ignora cooldowns já processados
                    if (processedCooldowns.Contains(i)) continue;
                    
                    var period = coolDownPeriods[i];
                    if (gameTime >= period.start_time && gameTime < period.start_time + period.duration)
                    {
                        // Verifica se há bolas ativas antes de iniciar cooldown
                        if (AreAllBallsCleared())
                        {
                            // Todas as bolas desapareceram - inicia cooldown imediatamente
                            currentCooldownIndex = i;
                            mainTimer.Pause();
                            cooldownTimer.Start();
                            Log($"Cooldown started immediately at game time {gameTime:F1}s");
                        }
                        else
                        {
                            // Aguarda bolas desaparecerem
                            pendingCooldownIndex = i;
                        }
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifica se todas as bolas foram removidas da tela
        /// </summary>
        private bool AreAllBallsCleared()
        {
            if (gameManager == null) return true;
            var activeBalls = gameManager.GetActiveBalls();
            return activeBalls == null || activeBalls.Count == 0;
        }

        void OnDestroy()
        {
            // Desinscreve eventos
            GameEvents.OnLevelReady -= OnLevelReadyReceived;
            GameEvents.OnUserSettingsReady -= OnUserSettingsReadyReceived;
            
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Retorna o timestamp atual do jogo em milissegundos desde o início
        /// Pausa durante períodos de cooldown
        /// </summary>
        public float GetGameTimestamp()
        {
            if (!isGameActive) return 0f;
            return mainTimer.GetElapsedTimeMs();
        }
        
        /// <summary>
        /// Retorna o tempo de jogo em segundos
        /// Pausa durante períodos de cooldown
        /// </summary>
        public float GetGameTime()
        {
            if (!isGameActive) return 0f;
            return mainTimer.GetElapsedTime();
        }

        /// <summary>
        /// Inicia o jogo e todos os sistemas de captura
        /// </summary>
        public void StartGame()
        {
            if (isGameActive)
            {
                Log("Game already active, ignoring StartGame call");
                return;
            }

            Log("Starting game...");
            
            // Inicia timer principal
            mainTimer.Start();
            isGameActive = true;

            // Inicia sistemas de captura
            StartDataCapture();

            float startTime = Time.time;
            // Notifica sistemas via eventos locais e centralizados
            OnGameStarted?.Invoke(startTime);
            GameEvents.NotifyGameStarted(startTime);

            Log($"Game started at timestamp: {startTime}");
        }

        /// <summary>
        /// Finaliza o jogo com limpeza completa de recursos
        /// </summary>
        public void EndGame(GameEndReason reason = GameEndReason.Manual)
        {
            if (!isGameActive || isCleaningUp)
            {
                Log("Game not active or already cleaning up, ignoring EndGame call");
                return;
            }
            
            Log($"Ending game - Reason: {reason}");
            
            // Para todos os timers
            mainTimer.Stop();
            cooldownTimer.Stop();
            
            float endTime = Time.time;
            isGameActive = false;
            isCleaningUp = true;

            StartCoroutine(EndGameSequence(reason, endTime));
        }

        /// <summary>
        /// Chamado quando o level está pronto
        /// </summary>
        private void OnLevelReadyReceived()
        {
            Log("Level ready signal received");
            isLevelReady = true;
            TryStartGame();
        }

        /// <summary>
        /// Chamado quando as configurações do usuário estão prontas
        /// </summary>
        private void OnUserSettingsReadyReceived()
        {
            Log("User settings ready signal received");
            isUserSettingsReady = true;
            TryStartGame();
        }

        /// <summary>
        /// Tenta iniciar o jogo quando todos os pré-requisitos estiverem prontos
        /// </summary>
        private void TryStartGame()
        {
            if (isGameActive)
            {
                Log("Game already active, ignoring start attempt");
                return;
            }

            if (!isLevelReady)
            {
                Log("Waiting for level to be ready...");
                return;
            }

            // User settings não são obrigatórios, mas preferimos aguardar
            // Se não estiverem prontos em 2 segundos, inicia mesmo assim
            if (!isUserSettingsReady)
            {
                Log("User settings not ready yet, but starting game anyway...");
            }

            Log("All prerequisites met, starting game...");
            StartGame();
        }

        private Core.MediaPipe.PoseWebSocketClient wsClient;
        private void CacheSystemReferences()
        {
            // Usa referências serializadas se disponíveis, senão busca via FindObjectOfType
            if (gameManager == null)
                gameManager = FindObjectOfType<BubblesGameManager>();
            
            if (webcamCapture == null)
                webcamCapture = FindObjectOfType<WebcamCapture>();
            
            if (poseController == null)
                poseController = FindObjectOfType<PoseController>();
            
            if (scoreManager == null)
                scoreManager = FindObjectOfType<ScoreManager>();
            
            if (poseDataManager == null)
            {
                poseDataManager = FindObjectOfType<PoseDataManager>();
                if (poseDataManager == null)
                {
                    var go = new GameObject("PoseDataManager");
                    poseDataManager = go.AddComponent<PoseDataManager>();
                }
            }

            if (ballEventRecorder == null)
            {
                ballEventRecorder = FindObjectOfType<BubblesGame.Data.Services.BallEventRecorder>();
                if (ballEventRecorder == null)
                {
                    var go = new GameObject("BallEventRecorder");
                    ballEventRecorder = go.AddComponent<BubblesGame.Data.Services.BallEventRecorder>();
                }
            }

            wsClient = FindObjectOfType<Core.MediaPipe.PoseWebSocketClient>();

            Log($"System references cached - GM:{gameManager != null}, WC:{webcamCapture != null}, PC:{poseController != null}, SM:{scoreManager != null}, PDM:{poseDataManager != null}, WS:{wsClient != null}, BER:{ballEventRecorder != null}");
        }

        private void StartDataCapture()
        {
            Log("Starting data capture systems...");
            // Inicia pose-data capture
            if (poseDataManager != null && poseDataManager.PoseRecorder != null)
            {
                string userId = SessionManager.GetUserId();
                string companyId = SessionManager.GetCompanyId();
                string gameId = SessionManager.GetCurrentGameId();
                string matchId = SessionManager.GetCurrentMatchId();
                
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(matchId))
                {
                    poseDataManager.PoseRecorder.StartPoseCapture(userId, companyId, gameId, matchId);
                    Log($"Pose data capture started - userId: {userId}, matchId: {matchId}");
                }
                else
                {
                    Log($"Missing session data for pose capture - userId: {userId}, matchId: {matchId}");
                }
            }
            else
            {
                Log($"PoseDataManager setup - PDM: {poseDataManager != null}, Recorder: {poseDataManager?.PoseRecorder != null}");
            }

            // Inicia ball-event capture
            if (ballEventRecorder != null)
            {
                var sess = BubblesGame.Data.GameSessionData.Instance;
                if (sess != null)
                {
                    string userId = SessionManager.GetUserId();
                    ballEventRecorder.StartEventCapture(userId, sess.CompanyId, sess.GameId, sess.MatchId);
                }
            }
            
            // Webcam já é iniciada automaticamente no Start()
            if (webcamCapture != null)
            {
                Log("Webcam capture system ready");
            }

            // PoseController já processa dados automaticamente
            if (poseController != null)
            {
                Log("Pose tracking system ready");
            }
        }

        private IEnumerator EndGameSequence(GameEndReason reason, float endTime)
        {
            Log("Starting end game sequence...");

            // 0. Notifica fim do jogo via eventos locais e centralizados (era o quinto passo anteriormente, movido para o início pois atualmente esses eventos afetam apenas a UI - necessário para um feedback de fim de jogo adequado)
            OnGameEnded?.Invoke(endTime);
            GameEvents.NotifyGameEnded(reason);

            // 1. Para processos de captura
            // Flush pose-data e ball-events antes de parar qualquer sistema
            if (poseDataManager != null && poseDataManager.PoseRecorder != null)
                poseDataManager.PoseRecorder.StopPoseCapture();
            if (ballEventRecorder != null)
                ballEventRecorder.StopEventCapture();
            
            // Para EventDataService (será salvo em SaveMatchDataCoroutine)
            var eventDataService = FindObjectOfType<BubblesGame.Data.Services.EventDataService>();
            if (eventDataService != null)
            {
                eventDataService.StopEventCapture();
            }
            
            StopDataCapture();
            yield return new WaitForEndOfFrame();

            // 2. Salva dados capturados (placeholder para implementação futura)
            yield return StartCoroutine(SaveCapturedData());

            // 3. Salva GameState caso o jogo tenha terminado naturalmente ou por tempo esgotado (permitindo replay)
            if (reason == GameEndReason.GameComplete || reason == GameEndReason.TimeUp)
                SaveGameState();

            // 4. Limpa recursos e memória
            CleanupResources();
            yield return new WaitForEndOfFrame();

            // 5. Navegação baseada na razão do fim
            HandleSceneTransition(reason);

            isCleaningUp = false;
            Log("End game sequence completed");
        }

        private void StopDataCapture()
        {
            Log("Stopping data capture systems...");

            // Para webcam
            if (webcamCapture != null)
            {
                webcamCapture.StopCamera();
                Log("Webcam capture stopped");
            }

            // PoseController para automaticamente quando não recebe dados
            if (poseController != null)
            {
                Log("Pose tracking will stop when no data is received");
            }

            // Para outros sistemas de captura futuros
        }

        private IEnumerator SaveCapturedData()
        {
            Log("Saving captured data...");

            // Salva dados da match (ball_list, finaliza match, etc.)
            if (gameManager != null)
            {
                Log("Saving match data via BubblesGameManager...");
                yield return StartCoroutine(gameManager.SaveMatchDataCoroutine());
            }
            else
            {
                Log("BubblesGameManager not found, skipping match data save");
            }

            // Placeholder para salvar outros dados capturados
            // TODO: Implementar salvamento de landmarks, eventos, etc.
            
            Log("Data saving completed");
        }

        private void CleanupResources()
        {
            Log("Cleaning up resources...");

            // Limpa todos os efeitos de explosão pendentes
            BubblesGame.ExplosionEffect[] effects = FindObjectsOfType<BubblesGame.ExplosionEffect>();
            foreach (var effect in effects)
            {
                if (effect != null && effect.gameObject != null)
                {
                    Destroy(effect.gameObject);
                }
            }
            if (effects.Length > 0)
            {
                Log($"Cleaned up {effects.Length} explosion effects");
            }

            // Limpa filtros do PoseController
            if (poseController != null)
            {
                poseController.ClearFilters();
                Log("PoseController filters cleared");
            }

            // Limpa referências de sistemas
            gameManager = null;
            webcamCapture = null;
            poseController = null;
            scoreManager = null;

            // Força garbage collection
            System.GC.Collect();

            Log("Resource cleanup completed");
        }

        private void HandleSceneTransition(GameEndReason reason)
        {
            Log($"Handling scene transition for reason: {reason}");

            switch (reason)
            {
                case GameEndReason.BackButton:
                case GameEndReason.Manual:
                    // Volta para BubblesMain
                    LoadScene("BubblesMain");
                    break;
                
                case GameEndReason.TimeUp:
                case GameEndReason.GameComplete:
                    // Salva estado do jogo antes de ir para resultados
                    // SaveGameState();
                    // Vai para tela de resultados
                    LoadScene("BubblesResults");
                    break;
                
                default:
                    LoadScene("BubblesMain");
                    break;
            }
        }

        /// <summary>
        /// Salva o estado do jogo (ball-list e cooldown) para permitir replay
        /// </summary>
        private void SaveGameState()
        {
            // Obtém preset e level do SessionManager
            string presetId = null;
            string levelId = null;
            
            if (SessionManager.Instance != null)
            {
                presetId = SessionManager.Instance.CurrentPresetId;
                levelId = SessionManager.Instance.CurrentLevelId;
            }

            if (string.IsNullOrEmpty(presetId) || string.IsNullOrEmpty(levelId))
            {
                Log("Cannot save game state - presetId or levelId is missing");
                return;
            }

            // Obtém ball-list do BallListService (que armazena a lista original)
            List<BallData> ballList = null;
            var ballListService = FindObjectOfType<BubblesGame.Data.Services.BallListService>();
            if (ballListService != null && ballListService.HasStoredBalls)
            {
                // BallListService não expõe GetBallList diretamente
                // Vamos obter do BubblesGameManager se disponível
                if (gameManager != null)
                {
                    ballList = gameManager.GetOriginalBallList();
                }
            }

            // Obtém cooldown periods
            List<CoolDownPeriod> cooldownPeriods = new List<CoolDownPeriod>(coolDownPeriods);

            // Salva usando GameStatePersistence
            GameStatePersistence.SaveState(presetId, levelId, ballList, cooldownPeriods, scoreManager.TotalHits);
            Log($"Game state saved - Preset: {presetId}, Level: {levelId}, Balls: {ballList?.Count ?? 0}, Cooldowns: {cooldownPeriods.Count}");
        }

        private void LoadScene(string sceneName)
        {
            Log($"Loading scene: {sceneName}");
            
            // Restaura timeScale antes de trocar cena
            Time.timeScale = 1f;
            
            // Carrega cena
            SceneManager.LoadScene(sceneName);
        }

        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[GameController] {message}");
            }
        }

        // Métodos públicos para integração com sistemas existentes
        
        /// <summary>
        /// Chamado pelo BubblesGameManager quando o jogo termina naturalmente
        /// </summary>
        public void OnGameCompleted()
        {
            EndGame(GameEndReason.GameComplete);
        }

        /// <summary>
        /// Chamado quando o tempo do level acaba
        /// </summary>
        public void OnTimeUp()
        {
            EndGame(GameEndReason.TimeUp);
        }

        /// <summary>
        /// Chamado pelo botão Back da UI
        /// </summary>
        public void OnBackButtonPressed()
        {
            EndGame(GameEndReason.BackButton);
        }

        /// <summary>
        /// Define o tempo total do level (sem adicionar tempo de cooldown)
        /// </summary>
        public void SetLevelTotalTime(float totalTimeInSeconds)
        {
            levelTotalTime = totalTimeInSeconds;
            Log($"Level total time set to: {totalTimeInSeconds}s");
        }
        
        /// <summary>
        /// Define os períodos de cooldown para o level
        /// </summary>
        public void SetCoolDownPeriods(List<CoolDownPeriod> periods)
        {
            coolDownPeriods = periods ?? new List<CoolDownPeriod>();
            currentCooldownIndex = -1;
            pendingCooldownIndex = -1;
            processedCooldowns.Clear();
            cooldownTimer.Stop();
            
            if (coolDownPeriods.Count > 0)
            {
                Log($"Cooldown periods set: {coolDownPeriods.Count} periods");
                
                foreach (var period in coolDownPeriods)
                {
                    Log($"  Cooldown at {period.start_time}s for {period.duration}s");
                }
            }
            else
            {
                Log("No cooldown periods set - level will run without cooldowns");
            }
        }
        
        /// <summary>
        /// Verifica se um tempo específico está em período de cooldown
        /// Retorna true apenas se há um cooldown ativo (timer rodando)
        /// </summary>
        public bool IsTimeInCooldown(float gameTime)
        {
            // Verifica se há um cooldown ativo (timer rodando)
            if (currentCooldownIndex >= 0 && cooldownTimer.IsRunning)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Retorna o tempo restante do cooldown atual (0 se não estiver em cooldown)
        /// Usa timer independente para o countdown do cooldown
        /// </summary>
        public float GetCurrentCooldownRemaining(float gameTime)
        {
            if (coolDownPeriods == null || coolDownPeriods.Count == 0) return 0f;
            if (currentCooldownIndex < 0) return 0f;
            if (!cooldownTimer.IsRunning) return 0f;
            
            var period = coolDownPeriods[currentCooldownIndex];
            float cooldownElapsed = cooldownTimer.GetElapsedTime();
            float remaining = period.duration - cooldownElapsed;
            return remaining > 0f ? remaining : 0f;
        }
    }

    /// <summary>
    /// Razões possíveis para o fim do jogo
    /// </summary>
    public enum GameEndReason
    {
        Manual,         // Chamado manualmente
        BackButton,     // Botão Back pressionado
        TimeUp,         // Tempo do level acabou
        GameComplete,   // Jogo completado naturalmente
        GameOver        // Game over (vidas esgotadas, etc.)
    }
}