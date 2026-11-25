using System.Collections.Generic;
using UnityEngine;
using BubblesGame.Data;
using BubblesGame.Data.Services;
using BubblesGame.Firebase;

namespace BubblesGame
{
    public class BubblesGameManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private Transform ballContainer;
        [SerializeField] private RectTransform webcamRect; // Referência ao RawImage da webcam

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        

        // Session data (vem do MatchManager via GameSessionData)
        private string companyId;
        private string gameId;
        private string matchId;
        private string presetId;
        private string levelId;

        // State
        private LevelData currentLevel;
        private List<Ball> activeBalls = new List<Ball>();
        private bool isGameRunning = false;
        
        // Propriedade pública para acesso ao tempo de jogo (usa GameController)
        public float CurrentGameTime => GameController.Instance != null ? 
            GameController.Instance.GetGameTimestamp() / 1000f : 0f;

        // Referência ao GameController (injetada)
        private GameController gameController;

        // Components
        private FirebaseLevelLoader levelLoader;
        private BallListService ballListService;
        private PoseDataManager poseDataManager;
        private Data.Services.EventDataService eventDataService;

        // Queue de spawn
        private Queue<BallData> spawnQueue = new Queue<BallData>();

        void Awake()
        {
            // Tenta encontrar GameController no Awake (pode ainda não estar inicializado)
            TryFindGameController();
        }

        void Start()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Starting initialization...");
            }

            // Tenta novamente encontrar GameController no Start
            if (gameController == null)
            {
                TryFindGameController();
            }

            // Se ainda não encontrou, aguarda um frame e tenta novamente
            if (gameController == null)
            {
                StartCoroutine(WaitForGameController());
            }
            else
            {
                // Se encontrou, inscreve eventos imediatamente
                SubscribeToGameControllerEvents();
            }
            
            // Valida referências
            if (ballPrefab == null)
            {
                Debug.LogWarning("[BubblesGameManager] Ball prefab não configurado! Continuando sem prefab para permitir settings.");
                // Não retorna mais - permite que o GameSessionData seja criado mesmo sem prefab
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Ball prefab configurado corretamente");
            }

            if (ballContainer == null)
            {
                ballContainer = transform; // Usa o próprio transform se não configurado
                if (enableDebugLogs)
                {
                    Debug.Log("[BubblesGameManager] Ball container definido como próprio transform");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Ball container configurado");
            }

            // Busca webcam rect se não configurado
            if (webcamRect == null)
            {
                UnityEngine.UI.RawImage webcamImage = FindObjectOfType<UnityEngine.UI.RawImage>();
                if (webcamImage != null)
                {
                    webcamRect = webcamImage.rectTransform;
                    if (enableDebugLogs)
                    {
                        Debug.Log("[BubblesGameManager] WebcamRect encontrado automaticamente");
                    }
                }
                else
                {
                    Debug.LogWarning("[BubblesGameManager] WebcamRect não encontrado! Bolas podem não aparecer corretamente.");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] WebcamRect configurado");
            }

            // Busca ou cria o loader
            levelLoader = FindObjectOfType<FirebaseLevelLoader>();
            if (levelLoader == null)
            {
                GameObject loaderObj = new GameObject("FirebaseLevelLoader");
                levelLoader = loaderObj.AddComponent<FirebaseLevelLoader>();
                if (enableDebugLogs)
                {
                    Debug.Log("[BubblesGameManager] FirebaseLevelLoader criado dinamicamente");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] FirebaseLevelLoader encontrado na cena");
            }
            
            // Busca ou cria o BallListService
            ballListService = FindObjectOfType<BallListService>();
            if (ballListService == null)
            {
                GameObject ballListServiceObj = new GameObject("BallListService");
                ballListService = ballListServiceObj.AddComponent<BallListService>();
                if (enableDebugLogs)
                {
                    Debug.Log("[BubblesGameManager] BallListService criado dinamicamente");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] BallListService encontrado na cena");
            }
            
            // Busca PoseDataManager (singleton, pode estar em outra cena)
            poseDataManager = PoseDataManager.Instance;
            if (poseDataManager == null)
            {
                poseDataManager = FindObjectOfType<PoseDataManager>();
            }
            
            if (poseDataManager == null && enableDebugLogs)
            {
                Debug.LogWarning("[BubblesGameManager] PoseDataManager não encontrado - pose-data não será salvo");
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] PoseDataManager encontrado");
            }
            
            // Inicializa EventDataService cedo para evitar problemas de ordem
            InitializeEventDataService();
            
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] EventDataService inicializado");
            }

            // Aguarda Firebase estar pronto antes de carregar dados
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Iniciando coroutine WaitForFirebaseAndLoad");
            }
            StartCoroutine(WaitForFirebaseAndLoad());
        }

        /// <summary>
        /// Tenta encontrar o GameController
        /// </summary>
        void TryFindGameController()
        {
            gameController = GameController.Instance;
            if (gameController == null)
            {
                // Tenta buscar diretamente na cena como fallback
                gameController = FindObjectOfType<GameController>();
                if (gameController != null && enableDebugLogs)
                {
                    Debug.Log("[BubblesGameManager] GameController encontrado via FindObjectOfType");
                }
                else
                {
                    // Se não encontrou, tenta criar automaticamente
                    CreateGameControllerIfMissing();
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] GameController encontrado via Instance");
            }
        }

        /// <summary>
        /// Cria o GameController se não existir na cena
        /// </summary>
        void CreateGameControllerIfMissing()
        {
            // Verifica novamente se realmente não existe
            if (FindObjectOfType<GameController>() == null)
            {
                Debug.LogWarning("[BubblesGameManager] GameController não encontrado na cena! Criando automaticamente...");
                GameObject gameControllerObj = new GameObject("GameController");
                gameController = gameControllerObj.AddComponent<GameController>();
                Debug.Log("[BubblesGameManager] GameController criado automaticamente");
            }
        }

        /// <summary>
        /// Inscreve eventos do GameController
        /// </summary>
        void SubscribeToGameControllerEvents()
        {
            if (gameController != null)
            {
                gameController.OnGameStarted += OnGameControllerStarted;
                if (enableDebugLogs)
                {
                    Debug.Log("[BubblesGameManager] Eventos do GameController inscritos");
                }
            }
        }
        
        /// <summary>
        /// Desinscreve eventos
        /// </summary>
        void UnsubscribeFromEvents()
        {
            if (gameController != null)
            {
                gameController.OnGameStarted -= OnGameControllerStarted;
            }
        }

        /// <summary>
        /// Aguarda o GameController estar disponível
        /// </summary>
        System.Collections.IEnumerator WaitForGameController()
        {
            int attempts = 0;
            const int maxAttempts = 10;
            
            while (gameController == null && attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.1f);
                TryFindGameController();
                attempts++;
            }

            if (gameController == null)
            {
                Debug.LogError("[BubblesGameManager] GameController não encontrado após múltiplas tentativas! O jogo pode não funcionar corretamente.");
            }
            else
            {
                // Inscreve eventos agora que encontrou
                SubscribeToGameControllerEvents();
            }
        }

        private System.Collections.IEnumerator WaitForFirebaseAndLoad()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Aguardando Firebase inicializar...");
            }
            // Aguarda um pouco para dar tempo do Firebase inicializar
            yield return new WaitForSeconds(1f);
            
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Timeout do Firebase atingido, carregando dados da sessão");
            }
            // Carrega dados da sessão (que por sua vez carregará o nível)
            LoadSessionData();
        }

        void LoadSessionData()
        {
            Debug.Log("[BubblesGameManager] LoadSessionData iniciado");
            
            // Valores básicos do SessionManager (já configurados pelo MatchManager)
            companyId = GameConfig.CompanyId;
            gameId = SessionManager.Instance != null ? SessionManager.Instance.CurrentGameId : "bubbles";
            matchId = SessionManager.Instance != null ? SessionManager.Instance.CurrentMatchId : System.DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");
            
            // CORREÇÃO: Pega preset/level do SessionManager (cache local) em vez do Firebase
            // O MatchManager já configurou esses valores antes de carregar esta cena
            if (SessionManager.Instance != null && 
                !string.IsNullOrEmpty(SessionManager.Instance.CurrentPresetId) && 
                !string.IsNullOrEmpty(SessionManager.Instance.CurrentLevelId))
            {
                presetId = SessionManager.Instance.CurrentPresetId;
                levelId = SessionManager.Instance.CurrentLevelId;
                
            Debug.Log($"[BubblesGameManager] Session data loaded - Match: {matchId}, Preset: {presetId}, Level: {levelId}");
                
                InitializeGameSession(presetId, levelId);
            }
            else
            {
                // Fallback: tenta carregar do Firebase GameState
                Debug.LogWarning("[BubblesGameManager] SessionManager não tem preset/level, tentando Firebase...");
                
                if (GameStateService.Instance != null)
                {
                    GameStateService.Instance.GetOrCreateState(companyId, gameId,
                        onSuccess: state =>
                        {
                            presetId = state.presetId;
                            levelId = state.levelId;
                            Debug.Log($"[BubblesGameManager] State loaded from Firebase - Preset: {presetId}, Level: {levelId}");
                            InitializeGameSession(presetId, levelId);
                        },
                        onError: error =>
                        {
                            Debug.LogWarning($"[BubblesGameManager] Failed to load state, using defaults: {error}");
                            presetId = "preset_1";
                            levelId = "level_1";
                            InitializeGameSession(presetId, levelId);
                        }
                    );
                }
                else
                {
                    // Último fallback
                    Debug.LogWarning("[BubblesGameManager] Usando valores padrão");
                    presetId = "preset_1";
                    levelId = "level_1";
                    InitializeGameSession(presetId, levelId);
                }
            }
        }

        /// <summary>
        /// Carrega configurações de sensibilidade do usuário
        /// </summary>
        void LoadUserSettings()
        {
            // Pega userId do sistema de autenticação (assumindo que existe)
            string userId = GetCurrentUserId();
            
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning("[BubblesGameManager] User ID not available, using default settings");
                ApplyDefaultSettings();
                return;
            }

            UserSettingsService.Instance.GetOrCreateUserSettings(
                userId, companyId, gameId,
                onSuccess: settings =>
                {
            Debug.Log($"[BubblesGameManager] User settings loaded: {settings}");
            ApplyUserSettings(settings);
                },
                onError: error =>
                {
                    Debug.LogError($"[BubblesGameManager] Failed to load user settings: {error}");
                    ApplyDefaultSettings();
                }
            );
        }

        /// <summary>
        /// Aplica configurações do usuário ao sistema de colisão
        /// </summary>
        void ApplyUserSettings(UserGameSettings settings)
        {
            PoseHitDetector detector = FindObjectOfType<PoseHitDetector>();
            if (detector != null)
            {
                detector.SetSensitivitySettings(settings);
                Debug.Log("[BubblesGameManager] User settings applied to PoseHitDetector");
            }
            else
            {
                Debug.LogWarning("[BubblesGameManager] PoseHitDetector not found, settings not applied");
            }

            // Notifica que as configurações do usuário estão prontas
            GameEvents.NotifyUserSettingsReady();
        }

        /// <summary>
        /// Aplica configurações padrão
        /// </summary>
        void ApplyDefaultSettings()
        {
            UserGameSettings defaultSettings = UserGameSettings.CreateDefault();
            ApplyUserSettings(defaultSettings);
        }

        /// <summary>
        /// Obtém o ID do usuário atual do cache (SessionManager)
        /// </summary>
        string GetCurrentUserId()
        {
            string userId = SessionManager.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogError("[BubblesGameManager] No user data in SessionManager! AuthGuard should prevent this.");
            }
            return userId;
        }
        
        /// <summary>
        /// Inicializa o EventDataService cedo para evitar problemas de ordem
        /// </summary>
        void InitializeEventDataService()
        {
            if (eventDataService == null)
            {
                eventDataService = FindObjectOfType<Data.Services.EventDataService>();
                if (eventDataService == null)
                {
                    GameObject eventServiceObj = new GameObject("EventDataService");
                    eventDataService = eventServiceObj.AddComponent<Data.Services.EventDataService>();
                    Debug.Log("[BubblesGameManager] EventDataService criado automaticamente");
                }
                else
                {
                    Debug.Log("[BubblesGameManager] EventDataService encontrado na cena");
                }
            }
        }

        /// <summary>
        /// Inicializa a sessão de jogo e carrega configurações
        /// </summary>
        void InitializeGameSession(string presetId, string levelId)
        {
            // Atualiza SessionManager com dados da sessão
            SessionManager.Instance?.SetGameSession(gameId, matchId, presetId, levelId);
            
            Debug.Log($"[BubblesGameManager] Game session initialized - Preset: {presetId}, Level: {levelId}");
            
            // Carrega configurações do usuário
            LoadUserSettings();
            
            // Só carrega level se tiver ballPrefab configurado
            if (ballPrefab != null)
            {
                LoadLevel();
            }
            else
            {
                Debug.LogWarning("[BubblesGameManager] Ball prefab não configurado, pulando LoadLevel()");
            }
        }

        void LoadLevel()
        {
            // Verifica se há estado salvo para restaurar (PlayAgain)
            var savedState = GameStatePersistence.LoadState();
            if (savedState != null && savedState.ballList != null && savedState.ballList.Count > 0)
            {
                Debug.Log($"[BubblesGameManager] Restoring saved game state - {savedState.ballList.Count} balls, {savedState.cooldownPeriods?.Count ?? 0} cooldowns");
                RestoreLevelFromSavedState(savedState);
                GameStatePersistence.ClearState(); // Limpa estado salvo após restaurar
                return;
            }

            Debug.Log($"[BubblesGameManager] Loading level config: {presetId}/{levelId}");

            // Carrega LevelConfig e gera bolas proceduralmente
            levelLoader.LoadLevelConfig(gameId, presetId, levelId,
                onSuccess: levelConfig =>
                {
                    Debug.Log($"[BubblesGameManager] Config carregado - Total Time: {levelConfig.totalTime}s, Phases: {levelConfig.moments?.Length ?? 0}");
                    
                    // Validação do LevelConfig antes de gerar bolas
                    if (!levelConfig.IsValidForLevelGeneration(out string validationError))
                    {
                        Debug.LogError($"[BubblesGameManager] LevelConfig inválido: {validationError}");
                        return;
                    }
                    
                    // Debug: verifica estruturas críticas
                    Debug.Log($"[BubblesGameManager] SpatialDistributions: {levelConfig.spatialDistributions?.Length ?? 0}");
                    if (levelConfig.spatialDistributions != null && levelConfig.spatialDistributions.Length > 0)
                    {
                        var firstDist = levelConfig.spatialDistributions[0];
                        Debug.Log($"[BubblesGameManager] First SpatialDist - xZone: {firstDist.xZone?.Count ?? 0}, yZone: {firstDist.yZone?.Count ?? 0}, xDist: {firstDist.xDist?.Length ?? 0}, yDist: {firstDist.yDist?.Length ?? 0}");
                    }
                    
                    Debug.Log($"[BubblesGameManager] Moments: {levelConfig.moments?.Length ?? 0}");
                    if (levelConfig.moments != null && levelConfig.moments.Length > 0)
                    {
                        var firstMoment = levelConfig.moments[0];
                        Debug.Log($"[BubblesGameManager] First Moment - minInt: {firstMoment.minInt}, maxInt: {firstMoment.maxInt}, minSpeed: {firstMoment.minSpeed}, maxSpeed: {firstMoment.maxSpeed}");
                    }
                    
                    // Informa o tempo total e cooldowns ao GameController e UI
                    if (GameController.Instance != null)
                    {
                        GameController.Instance.SetLevelTotalTime(levelConfig.totalTime);
                        GameController.Instance.SetCoolDownPeriods(levelConfig.coolDown);
                    }
                    
                    var gameUI = FindObjectOfType<BubblesGameUI>();
                    if (gameUI != null)
                    {
                        gameUI.SetLevelTotalTime(levelConfig.totalTime);
                    }
                    
                    // Gera bolas proceduralmente usando seed baseado no matchId
                    int seed = !string.IsNullOrEmpty(matchId) ? matchId.GetHashCode() : 0;
                    Debug.Log($"[BubblesGameManager] Generating balls with seed: {seed}");
                    
                    // Gera lista completa de bolas usando LevelPlanner
                    List<BallData> balls = LevelPlanner.Build(levelConfig, seed);
                    
                    // Validação crítica: todas as bolas devem estar calculadas
                    if (balls == null || balls.Count == 0)
                    {
                        Debug.LogError("[BubblesGameManager] CRITICAL: No balls generated! Cannot start level.");
                        return;
                    }
                    
                    // Validação adicional: verifica se todas as bolas têm dados válidos
                    int validBalls = 0;
                    foreach (var ball in balls)
                    {
                        if (ball != null && ball.LaunchTime >= 0)
                        {
                            validBalls++;
                        }
                    }
                    
                    if (validBalls != balls.Count)
                    {
                        Debug.LogError($"[BubblesGameManager] CRITICAL: {balls.Count - validBalls} invalid balls detected!");
                        return;
                    }
                    
                    Debug.Log($"[BubblesGameManager] ✓ PRE-CALCULATION COMPLETE: {validBalls} balls generated and validated");
                    
                    // Atribui ball_id a cada bola (índice na lista)
                    for (int i = 0; i < balls.Count; i++)
                    {
                        balls[i].ball_id = i;
                    }
                    
                    // Armazena lista de bolas no BallListService para upload posterior
                    if (ballListService != null)
                    {
                        string userId = GetCurrentUserId();
                        ballListService.SetMatchInfo(userId, companyId, gameId, matchId);
                        ballListService.StoreBallList(balls);
                        Debug.Log($"[BubblesGameManager] Lista de {balls.Count} bolas armazenada no BallListService");
                    }
                    
                    // Configura PoseDataRecorder
                    if (poseDataManager != null && poseDataManager.PoseRecorder != null)
                    {
                        string userId = GetCurrentUserId();
                        poseDataManager.PoseRecorder.SetMatchInfo(userId, companyId, gameId, matchId);
                    }
                    else
                    {
                        Debug.LogWarning("[BubblesGameManager] PoseDataRecorder não encontrado, pose-data não será salva");
                    }
                    
                    // Configura EventDataService (já deve estar inicializado)
                    if (eventDataService != null)
                    {
                        // Obtém dados do SessionManager (mesmo padrão dos outros serviços)
                        string userId = SessionManager.GetUserId();
                        string companyId = SessionManager.GetCompanyId();
                        string gameId = SessionManager.GetCurrentGameId();
                        string matchId = SessionManager.GetCurrentMatchId();
                        
                        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(matchId))
                        {
                            eventDataService.SetMatchInfo(userId, companyId, gameId, matchId);
                            eventDataService.StartEventCapture(userId, companyId, gameId, matchId);
                            Debug.Log("[BubblesGameManager] EventDataService configurado e iniciado");
                        }
                        else
                        {
                            Debug.LogWarning($"[BubblesGameManager] Dados de sessão incompletos para EventDataService - userId: {userId}, matchId: {matchId}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BubblesGameManager] EventDataService não encontrado, eventos de colisão não serão salvos");
                    }
                    
                    // Cria LevelData com as bolas geradas usando levelId real do Firebase
                    currentLevel = new LevelData(1, balls, levelId, "Generated Level");
                    OnLevelLoaded();
                },
                onError: error =>
                {
                    Debug.LogError($"[BubblesGameManager] Erro ao carregar level config: {error}");
                }
            );
        }

        /// <summary>
        /// Restaura o level a partir do estado salvo (PlayAgain)
        /// </summary>
        void RestoreLevelFromSavedState(GameStatePersistence.SavedGameState savedState)
        {
            if (savedState == null || savedState.ballList == null || savedState.ballList.Count == 0)
            {
                Debug.LogError("[BubblesGameManager] Cannot restore - saved state is invalid");
                return;
            }

            Debug.Log($"[BubblesGameManager] Restoring level from saved state - {savedState.ballList.Count} balls");

            // Cria LevelData com as bolas salvas
            currentLevel = new LevelData(1, new List<BallData>(savedState.ballList), savedState.levelId, "Restored Level");

            // Restaura cooldown periods no GameController
            if (GameController.Instance != null && savedState.cooldownPeriods != null)
            {
                GameController.Instance.SetCoolDownPeriods(savedState.cooldownPeriods);
                Debug.Log($"[BubblesGameManager] Restored {savedState.cooldownPeriods.Count} cooldown periods");
            }

            // Armazena lista de bolas no BallListService
            if (ballListService != null)
            {
                string userId = GetCurrentUserId();
                ballListService.SetMatchInfo(userId, companyId, gameId, matchId);
                ballListService.StoreBallList(savedState.ballList);
                Debug.Log($"[BubblesGameManager] Restored {savedState.ballList.Count} balls to BallListService");
            }

            // Carrega configurações do level para obter totalTime
            levelLoader.LoadLevelConfig(gameId, savedState.presetId, savedState.levelId,
                onSuccess: levelConfig =>
                {
                    if (GameController.Instance != null)
                    {
                        GameController.Instance.SetLevelTotalTime(levelConfig.totalTime);
                    }
                    
                    var gameUI = FindObjectOfType<BubblesGameUI>();
                    if (gameUI != null)
                    {
                        gameUI.SetLevelTotalTime(levelConfig.totalTime);
                    }

                    OnLevelLoaded();
                },
                onError: error =>
                {
                    Debug.LogError($"[BubblesGameManager] Erro ao carregar level config para restore: {error}");
                    // Continua mesmo assim com os dados salvos
                    OnLevelLoaded();
                }
            );
        }


        void OnLevelLoaded()
        {
            if (currentLevel == null || currentLevel.balls == null || currentLevel.balls.Count == 0)
            {
                Debug.LogError("[BubblesGameManager] Level vazio ou inválido!");
                return;
            }

            Debug.Log($"[BubblesGameManager] Level carregado com {currentLevel.balls.Count} bolas");

            // Ordena bolas por launch_time
            currentLevel.balls.Sort((a, b) => a.LaunchTime.CompareTo(b.LaunchTime));

            // Preenche fila de spawn
            spawnQueue.Clear(); // Garante que a fila está limpa
            foreach (var ballData in currentLevel.balls)
            {
                spawnQueue.Enqueue(ballData);
            }

            // Validação crítica: fila deve estar completamente preenchida
            if (spawnQueue.Count != currentLevel.balls.Count)
            {
                Debug.LogError($"[BubblesGameManager] CRITICAL: Spawn queue mismatch! Expected {currentLevel.balls.Count}, got {spawnQueue.Count}");
                return;
            }

            Debug.Log($"[BubblesGameManager] ✓ SPAWN QUEUE READY: {spawnQueue.Count} balls queued and ready for game start");

            // Notifica ScoreManager do total de bolas
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.SetTotalBalls(currentLevel.balls.Count);
            }

            // Notifica que o level está pronto - GameController iniciará o jogo
            GameEvents.NotifyLevelReady();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BubblesGameManager] ✓ LEVEL READY: All {spawnQueue.Count} balls pre-calculated and queued. Game can start safely.");
            }
        }

        /// <summary>
        /// Chamado quando o GameController inicia o jogo
        /// </summary>
        void OnGameControllerStarted(float startTime)
        {
            isGameRunning = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[BubblesGameManager] Jogo iniciado pelo GameController! {spawnQueue.Count} bolas na fila");
            }
        }

        void Update()
        {
            // Só processa se o jogo estiver rodando E o GameController estiver ativo
            if (!isGameRunning || gameController == null || !gameController.IsGameActive) 
                return;

            float currentTime = CurrentGameTime;
            
            // Não spawna bolas durante períodos de cooldown (se existirem)
            if (gameController.IsTimeInCooldown(currentTime))
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0) // Log a cada segundo aproximadamente
                {
                    Debug.Log($"[BubblesGameManager] In cooldown period at game time {currentTime:F1}s");
                }
                return;
            }

            // Spawna bolas no momento correto
            while (spawnQueue.Count > 0 && spawnQueue.Peek().LaunchTime <= currentTime)
            {
                SpawnBall(spawnQueue.Dequeue());
            }

            // Remove bolas que já foram destruídas
            activeBalls.RemoveAll(ball => ball == null);

            // Verifica se o jogo acabou naturalmente (todas as bolas foram spawnadas e destruídas)
            // O GameController também pode terminar o jogo por tempo ou outras razões
            if (spawnQueue.Count == 0 && activeBalls.Count == 0 && isGameRunning)
            {
                // Notifica GameController que o jogo terminou naturalmente
                if (gameController != null)
                {
                    gameController.OnGameCompleted();
                }
            }
        }

        void SpawnBall(BallData ballData)
        {
            GameObject ballObj = Instantiate(ballPrefab, ballContainer);
            Ball ball = ballObj.GetComponent<Ball>();

            if (ball == null)
            {
                Debug.LogError("[BubblesGameManager] Ball prefab não tem componente Ball!");
                Destroy(ballObj);
                return;
            }

            ball.Initialize(ballData, GameController.Instance?.MasterStartTime ?? Time.time, webcamRect, ballData.BallId);
            activeBalls.Add(ball);

            if (enableDebugLogs)
            {
                Debug.Log($"[BubblesGameManager] Bola spawnada no tempo {ballData.LaunchTime}");
            }
        }

        /// <summary>
        /// Método público para salvar dados da match (chamado pelo GameController)
        /// O GameController orquestra todo o salvamento quando detecta fim do jogo
        /// Retorna uma coroutine que pode ser aguardada
        /// </summary>
        public System.Collections.IEnumerator SaveMatchDataCoroutine()
        {
            isGameRunning = false;
            
            if (enableDebugLogs)
            {
                Debug.Log("[BubblesGameManager] Salvando dados da match...");
            }
            
            bool saveCompleted = false;
            bool saveSuccess = false;
            
            // Salva lista de bolas no Firebase
            if (ballListService != null && ballListService.HasStoredBalls)
            {
                Debug.Log("[BubblesGameManager] Iniciando upload da lista de bolas...");
                ballListService.SaveToFirebase(
                    onSuccess: () =>
                    {
                        Debug.Log($"[BubblesGameManager] ✓ Lista de bolas salva com sucesso");
                        saveSuccess = true;
                        saveCompleted = true;
                    },
                    onError: error =>
                    {
                        Debug.LogError($"[BubblesGameManager] Erro ao salvar lista de bolas: {error}");
                        saveSuccess = false;
                        saveCompleted = true;
                    }
                );
                
                // Aguarda conclusão do upload
                while (!saveCompleted)
                {
                    yield return null;
                }
            }
            else
            {
                Debug.Log("[BubblesGameManager] Nenhuma bola para salvar");
                saveSuccess = true;
                saveCompleted = true;
            }
            
            // Salva pose_data (landmarks) no Firebase
            if (poseDataManager == null)
            {
                Debug.LogWarning("[BubblesGameManager] PoseDataManager não encontrado - tentando buscar novamente...");
                poseDataManager = PoseDataManager.Instance;
                if (poseDataManager == null)
                {
                    poseDataManager = FindObjectOfType<PoseDataManager>();
                }
            }
            
            if (poseDataManager != null && poseDataManager.PoseRecorder != null)
            {
                var recorder = poseDataManager.PoseRecorder;
                
                // Verifica se há chunks para salvar (incluindo chunk final que pode estar em currentChunk)
                bool hasChunks = recorder.HasChunksToSave;
                int totalFrames = recorder.TotalFramesCaptured;
                bool isCapturing = recorder.IsCapturing;
                
                Debug.Log($"[BubblesGameManager] PoseDataRecorder status - HasChunksToSave: {hasChunks}, TotalFrames: {totalFrames}, IsCapturing: {isCapturing}");
                Debug.Log($"[BubblesGameManager] Match info - userId: {recorder.userId}, companyId: {recorder.companyId}, gameId: {recorder.gameId}, matchId: {recorder.matchId}");
                
                // Sempre tenta salvar, mesmo se HasChunksToSave retornar false
                // O SaveToFirebaseCoroutine() fará a verificação final e logará o motivo
                Debug.Log("[BubblesGameManager] Iniciando upload de pose-data...");
                yield return StartCoroutine(recorder.SaveToFirebaseCoroutine());
                Debug.Log("[BubblesGameManager] Upload de pose-data concluído");
            }
            else
            {
                Debug.LogWarning($"[BubblesGameManager] PoseDataManager ou PoseRecorder não disponível - poseDataManager: {poseDataManager != null}, PoseRecorder: {poseDataManager?.PoseRecorder != null}");
            }
            
            // Salva event_data (eventos de colisão) no Firebase
            if (eventDataService == null)
            {
                eventDataService = FindObjectOfType<Data.Services.EventDataService>();
            }
            
            if (eventDataService != null)
            {
                // Para captura antes de salvar
                eventDataService.StopEventCapture();
                
                // Verifica status antes de salvar
                int totalEvents = eventDataService.TotalEventsCaptured;
                bool isCapturing = eventDataService.IsCapturing;
                
                Debug.Log($"[BubblesGameManager] EventDataService status - TotalEvents: {totalEvents}, IsCapturing: {isCapturing}");
                
                // Sempre salva (mesmo se vazio, cria chunk vazio)
                Debug.Log("[BubblesGameManager] Iniciando upload de event-data...");
                yield return StartCoroutine(eventDataService.SaveToFirebaseCoroutine());
                Debug.Log("[BubblesGameManager] Upload de event-data concluído");
            }
            else
            {
                Debug.LogWarning("[BubblesGameManager] EventDataService não disponível - eventos de colisão não serão salvos");
            }
            
            // Finaliza a match no Firebase
            if (!string.IsNullOrEmpty(matchId) && MatchService.Instance != null)
            {
                bool matchFinalized = false;
                MatchService.Instance.EndMatch(companyId, gameId, matchId,
                    onSuccess: () =>
                    {
                        Debug.Log($"[BubblesGameManager] Match {matchId} finalizada com sucesso");
                        matchFinalized = true;
                    },
                    onError: error =>
                    {
                        Debug.LogError($"[BubblesGameManager] Erro ao finalizar match: {error}");
                        matchFinalized = true;
                    }
                );
                
                // Aguarda finalização da match
                while (!matchFinalized)
                {
                    yield return null;
                }
            }
            
            Debug.Log("[BubblesGameManager] Salvamento de dados da match concluído");
        }

        private System.Collections.IEnumerator ShowResultsAfterDelay()
        {
            // Aguarda 1-2 segundos para dar sensação de conclusão
            yield return new WaitForSeconds(1.5f);

            // Notifica via evento centralizado
            GameEvents.NotifyShowResults();
        }

        /// <summary>
        /// Retorna todas as bolas atualmente ativas (para detecção de hit)
        /// </summary>
        public List<Ball> GetActiveBalls()
        {
            return activeBalls.FindAll(ball => ball != null && ball.IsActive);
        }

        /// <summary>
        /// Remove uma bola específica da lista de bolas ativas
        /// Chamado imediatamente quando a bola é destruída
        /// </summary>
        public void RemoveBall(Ball ball)
        {
            if (activeBalls.Contains(ball))
            {
                activeBalls.Remove(ball);
                Debug.Log($"[BubblesGameManager] Ball removed from active list immediately");
            }
        }

        /// <summary>
        /// Retorna a lista original de bolas do level (para salvar estado)
        /// </summary>
        public List<BallData> GetOriginalBallList()
        {
            if (currentLevel != null && currentLevel.balls != null)
            {
                return new List<BallData>(currentLevel.balls);
            }
            return null;
        }

        /// <summary>
        /// Para o jogo (pause)
        /// </summary>
        public void PauseGame()
        {
            isGameRunning = false;
            Time.timeScale = 0f;
            GameEvents.NotifyGamePaused();
        }

        /// <summary>
        /// Resume o jogo
        /// </summary>
        public void ResumeGame()
        {
            isGameRunning = true;
            Time.timeScale = 1f;
            GameEvents.NotifyGameResumed();
        }

        /// <summary>
        /// Reinicia o nível
        /// </summary>
        public void RestartLevel()
        {
            // Limpa bolas ativas
            foreach (var ball in activeBalls)
            {
                if (ball != null) Destroy(ball.gameObject);
            }
            activeBalls.Clear();
            spawnQueue.Clear();

            // Recarrega o nível
            LoadLevel();
        }

        void OnDestroy()
        {
            // Desinscreve eventos
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Método público para permitir que outros sistemas notifiquem quando GameController estiver disponível
        /// </summary>
        public void NotifyGameControllerAvailable()
        {
            if (gameController == null)
            {
                TryFindGameController();
                if (gameController != null)
                {
                    SubscribeToGameControllerEvents();
                }
            }
        }

        void OnDrawGizmos()
        {
            // Debug: mostra área de jogo (viewport)
            if (Camera.main != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane + 10f));
                Vector3 bottomRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, Camera.main.nearClipPlane + 10f));
                Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane + 10f));
                Vector3 topLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, Camera.main.nearClipPlane + 10f));

                Gizmos.DrawLine(bottomLeft, bottomRight);
                Gizmos.DrawLine(bottomRight, topRight);
                Gizmos.DrawLine(topRight, topLeft);
                Gizmos.DrawLine(topLeft, bottomLeft);
            }
        }
    }
}

