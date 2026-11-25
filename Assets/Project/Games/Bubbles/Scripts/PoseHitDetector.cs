using System.Collections.Generic;
using UnityEngine;
using Core.Tracking;
using Core.MediaPipe;
using BubblesGame.Data;
using System;

namespace BubblesGame
{
    /// <summary>
    /// Detecta colisão entre landmarks do MediaPipe e bolas ativas
    /// Usa coordenadas normalizadas e sensibilidades configuráveis
    /// </summary>
    public class PoseHitDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LandmarkFrameProcessor frameProcessor;
        [SerializeField] private BubblesGameManager gameManager;

        [Header("Detection Settings")]
        [SerializeField] private bool debugVisualization = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // User settings
        private UserGameSettings userSettings;
        
        // Debug visualizer
        private CollisionDebugVisualizer debugVisualizer;
        
        // Gravação de eventos (preparado para futuro)
        private IGameEventRecorder eventRecorder;
        
        // EventDataService para salvamento em chunks
        private Data.Services.EventDataService eventDataService;
        
        // PoseController para obter todos os landmarks
        private PoseController poseController;

        // Events para colisões
        public event Action<Ball, int, float> OnBallHitCollision;    // ball, landmarkIndex, hitTime
        public event Action<Ball, int, float> OnBallDestroyCollision; // ball, landmarkIndex, destroyTime

        // Landmarks principais (índices do MediaPipe Pose)
        // 0 = Nariz
        // 15 = Pulso esquerdo (left wrist)
        // 16 = Pulso direito (right wrist)

        void Start()
        {
            // Busca referências se não configuradas
            if (frameProcessor == null)
            {
                frameProcessor = FindObjectOfType<LandmarkFrameProcessor>();
                if (frameProcessor == null)
                {
                    // Cria automaticamente o LandmarkFrameProcessor se não existir
                    GameObject processorObj = new GameObject("LandmarkFrameProcessor");
                    frameProcessor = processorObj.AddComponent<LandmarkFrameProcessor>();
                    
                    // Configura o processador recém-criado
                    ConfigureFrameProcessor(frameProcessor);
                    
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning("[PoseHitDetector] LandmarkFrameProcessor não encontrado na cena. Criando automaticamente.");
                    }
                }
            }

            if (gameManager == null)
            {
                gameManager = GameServices.GameManager;
                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<BubblesGameManager>();
                    if (gameManager == null)
                    {
                        Debug.LogError("[PoseHitDetector] BubblesGameManager não encontrado!");
                        return;
                    }
                }
            }
            
            // Conecta ao evento do FrameProcessor
            frameProcessor.OnFrameProcessed += OnFrameProcessed;
            
            // Inicializa recorder (null object por enquanto)
            eventRecorder = new NullGameEventRecorder();
            
        // Busca EventDataService (já deve estar criado pelo BubblesGameManager)
        eventDataService = FindObjectOfType<Data.Services.EventDataService>();
        if (eventDataService == null && enableDebugLogs)
        {
            Debug.LogWarning("[PoseHitDetector] EventDataService não encontrado. Eventos de colisão não serão salvos até que seja criado pelo BubblesGameManager.");
        }
            
            // Busca PoseController para obter todos os landmarks
            poseController = FindObjectOfType<PoseController>();
            if (poseController == null)
            {
                Debug.LogWarning("[PoseHitDetector] PoseController não encontrado! Não será possível capturar todos os landmarks.");
            }

            // Inicializa com configurações padrão se não foram definidas
            if (userSettings == null)
            {
                userSettings = UserGameSettings.CreateDefault();
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[PoseHitDetector] Using default sensitivity settings");
                }
            }

            // Cria canvas de debug se necessário
            if (debugVisualization)
            {
                // Inicializa debug visualizer único para a cena
                debugVisualizer = FindObjectOfType<CollisionDebugVisualizer>();
                if (debugVisualizer == null)
                {
                    GameObject debugObj = new GameObject("CollisionDebugVisualizer");
                    debugVisualizer = debugObj.AddComponent<CollisionDebugVisualizer>();
                }

                // Garante que o visualizer use as mesmas configurações de usuário
                if (userSettings == null)
                {
                    userSettings = UserGameSettings.CreateDefault();
                }
                debugVisualizer.SetSettings(userSettings);
            }
        }

        /// <summary>
        /// Define as configurações de sensibilidade do usuário
        /// </summary>
        public void SetSensitivitySettings(UserGameSettings settings)
        {
            userSettings = settings ?? UserGameSettings.CreateDefault();
            Debug.Log($"[PoseHitDetector] Sensitivity settings updated: {userSettings}");
            
            // Atualiza debug visualizer
            if (debugVisualizer != null)
            {
                debugVisualizer.SetSettings(userSettings);
            }
        }

        void OnDestroy()
        {
            if (frameProcessor != null)
            {
                frameProcessor.OnFrameProcessed -= OnFrameProcessed;
            }
        }
        
        /// <summary>
        /// Processa frame de landmarks do FrameProcessor
        /// </summary>
        private void OnFrameProcessed(LandmarkFrameData frameData)
        {
            if (!frameData.IsValid || gameManager == null) return;

            List<Ball> activeBalls = gameManager.GetActiveBalls();
            if (activeBalls == null || activeBalls.Count == 0) return;

            // Checa colisões usando dados pré-processados
            CheckHitCollisions(frameData, activeBalls);
            CheckDestroyCollisions(frameData, activeBalls);

        }

        /// <summary>
        /// Checa colisão dos hit_landmarks (pulsos) com bolas
        /// Usa dados pré-processados do FrameProcessor
        /// </summary>
        void CheckHitCollisions(LandmarkFrameData frameData, List<Ball> activeBalls)
        {
            if (frameData.HitLandmarks == null || frameData.HitLandmarks.Length == 0) return;

            foreach (Ball ball in activeBalls)
            {
                if (ball == null || !ball.IsActive) continue;

                BallData ballData = ball.Data;
                if (ballData.HitLandmarks == null || ballData.HitLandmarks.Length == 0) continue;

                // Posição da bola em coordenadas normalizadas (0-1)
                Vector2 ballNormalizedPos = ball.GetNormalizedPosition();

                // Checa cada landmark configurado para hit
                foreach (int landmarkIndex in ballData.HitLandmarks)
                {
                    // Busca landmark processado por índice
                    var processedLandmark = frameData.GetLandmarkByIndex(landmarkIndex);
                    if (!processedLandmark.HasValue || !processedLandmark.Value.IsVisible) continue;

                    var landmark = processedLandmark.Value;

                    // Calcula raios separadamente
                    float ballRadius = CalculateBallRadius(ballData);
                    float landmarkRadius = CalculateLandmarkSensitivityRadius(CollisionType.Hit);

                    // Verifica colisão em coordenadas TOP-LEFT normalized
                    if (CheckCollision(ballNormalizedPos, landmark.NormalizedPosition, ballRadius, landmarkRadius))
                    {
                        float hitTime = GetGameTime();
                        float distance = Vector2.Distance(ballNormalizedPos, landmark.NormalizedPosition);
                        
                        // Captura todos os dados do evento de colisão
                        var collisionEvent = CreateCollisionEventData(
                            ball,
                            landmarkIndex,
                            landmark.NormalizedPosition,
                            hitTime,
                            frameData.TimestampMs,
                            Data.CollisionType.Hit,
                            activeBalls
                        );
                        
                        // Grava evento completo no EventDataService
                        if (eventDataService != null)
                        {
                            eventDataService.RecordEvent(collisionEvent);
                        }
                        
                        // Mantém compatibilidade com eventRecorder antigo
                        eventRecorder.RecordCollisionEvent(collisionEvent);
                        
                        OnBallHitCollision?.Invoke(ball, landmarkIndex, hitTime);
                        OnBallHit(ball, landmarkIndex, landmark.NormalizedPosition, distance);
                        break; // Evita múltiplos hits na mesma bola no mesmo frame
                    }
                }
            }
        }

        /// <summary>
        /// Checa colisão dos destroy_landmarks (nariz) com bolas
        /// Usa dados pré-processados do FrameProcessor
        /// </summary>
        void CheckDestroyCollisions(LandmarkFrameData frameData, List<Ball> activeBalls)
        {
            if (frameData.DestroyLandmarks == null || frameData.DestroyLandmarks.Length == 0) return;

            foreach (Ball ball in activeBalls)
            {
                if (ball == null || !ball.CanBeDestroyed()) continue;

                BallData ballData = ball.Data;
                if (ballData.DestroyLandmarks == null || ballData.DestroyLandmarks.Length == 0) continue;

                // Posição da bola em coordenadas normalizadas (0-1)
                Vector2 ballNormalizedPos = ball.GetNormalizedPosition();

                // Checa cada landmark configurado para destroy
                foreach (int landmarkIndex in ballData.DestroyLandmarks)
                {
                    // Busca landmark processado por índice
                    var processedLandmark = frameData.GetLandmarkByIndex(landmarkIndex);
                    if (!processedLandmark.HasValue || !processedLandmark.Value.IsVisible) continue;

                    var landmark = processedLandmark.Value;

                    // Calcula raios separadamente
                    float ballRadius = CalculateBallRadius(ballData);
                    float landmarkRadius = CalculateLandmarkSensitivityRadius(CollisionType.Destroy);

                    // Verifica colisão usando posição pré-processada
                    if (CheckCollision(ballNormalizedPos, landmark.NormalizedPosition, ballRadius, landmarkRadius))
                    {
                        float destroyTime = GetGameTime();
                        
                        // Captura todos os dados do evento de colisão
                        var collisionEvent = CreateCollisionEventData(
                            ball,
                            landmarkIndex,
                            landmark.NormalizedPosition,
                            destroyTime,
                            frameData.TimestampMs,
                            Data.CollisionType.Destroy,
                            activeBalls
                        );
                        
                        // Grava evento completo no EventDataService
                        if (eventDataService != null)
                        {
                            eventDataService.RecordEvent(collisionEvent);
                        }
                        
                        // Mantém compatibilidade com eventRecorder antigo
                        eventRecorder.RecordCollisionEvent(collisionEvent);
                        
                        OnBallDestroyCollision?.Invoke(ball, landmarkIndex, destroyTime);
                        OnBallDestroy(ball, landmarkIndex);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Tipos de colisão para cálculo de sensibilidade
        /// </summary>
        public enum CollisionType
        {
            Hit,
            Destroy
        }

        /// <summary>
        /// Calcula o raio de sensibilidade do landmark baseado no tipo de colisão
        /// </summary>
        private float CalculateLandmarkSensitivityRadius(CollisionType type)
        {
            return type == CollisionType.Hit 
                ? userSettings.GetTotalHitSensitivity() 
                : userSettings.GetTotalDestroySensitivity();
        }

        /// <summary>
        /// Calcula o raio de colisão da bola baseado no seu tamanho
        /// </summary>
        private float CalculateBallRadius(BallData ballData)
        {
            // Raio da bola = tamanho da bola * fator base (2% da tela)
            return ballData.Size * 0.02f;
        }

        /// <summary>
        /// Verifica colisão entre bola e landmark considerando ambos os raios
        /// Ambos em coordenadas TOP-LEFT normalized (0-1)
        /// </summary>
        private bool CheckCollision(Vector2 ballPos, Vector2 landmarkPos, float ballRadius, float landmarkRadius)
        {
            float distance = Vector2.Distance(ballPos, landmarkPos);
            float totalRadius = ballRadius + landmarkRadius;
            return distance <= totalRadius;
        }


        void OnBallHit(Ball ball, int landmarkIndex, Vector2 landmarkPos, float distance)
        {
            // Passa dados REAIS da colisão antes de processar flags especiais
            ball.OnHit(landmarkIndex, landmarkPos, distance);

            // Processa flags especiais
            if (ball.Data.IsHitSuper)
            {
                // TODO: Implementar lógica de super hit
            }

            if (ball.Data.IsHitAllSuper)
            {
                // TODO: Acertar todas as bolas ativas
                HitAllBalls();
            }

        }

        void OnBallDestroy(Ball ball, int landmarkIndex)
        {
            if (ball.Data.IsDestroyAllSuper)
            {
                // TODO: Destruir todas as bolas ativas
                DestroyAllBalls();
            }

            ball.OnDestroy();
        }

        void HitAllBalls()
        {
            List<Ball> activeBalls = gameManager.GetActiveBalls();
            foreach (Ball ball in activeBalls)
            {
                if (ball != null && ball.IsActive)
                {
                    // Hit especial "hit_all" - sem dados específicos de landmark
                    ball.OnHit();
                }
            }
        }

        void DestroyAllBalls()
        {
            List<Ball> activeBalls = gameManager.GetActiveBalls();
            foreach (Ball ball in activeBalls)
            {
                if (ball != null && ball.IsActive)
                {
                    ball.OnDestroy();
                }
            }
        }

        /// <summary>
        /// Obtém o tempo atual do jogo em segundos desde o início
        /// </summary>
        private float GetGameTime()
        {
            if (GameController.Instance != null)
            {
                return GameController.Instance.GetGameTime();
            }
            return Time.time;
        }
        
        /// <summary>
        /// Cria CollisionEventData completo com todos os dados necessários
        /// </summary>
        private Data.CollisionEventData CreateCollisionEventData(
            Ball ball,
            int landmarkIndex,
            Vector2 collisionPosition,
            float hitTime,
            float timestampMs,
            Data.CollisionType type,
            List<Ball> activeBalls)
        {
            // Obtém ball_id da bola
            int ballId = ball.BallIndex >= 0 ? ball.BallIndex : (ball.Data != null ? ball.Data.BallId : -1);
            
            // Obtém todos os landmarks visíveis
            var landmarkList = GetAllLandmarksSnapshot();
            
            // Obtém todas as bolas ativas
            var ballsList = GetAllBallsSnapshot(activeBalls);
            
            return new Data.CollisionEventData
            {
                TimestampMs = timestampMs,
                ball_id = ballId,
                hit_coord_x = collisionPosition.x,
                hit_coord_y = collisionPosition.y,
                hit_time = hitTime,
                hit_landmark = landmarkIndex,
                landmark_list = landmarkList,
                balls_on_screen = ballsList,
                Type = type
            };
        }
        
        /// <summary>
        /// Obtém snapshot de todos os landmarks visíveis no momento da colisão
        /// Retorna posições em coordenadas de tela (offsets)
        /// </summary>
        private List<Data.LandmarkSnapshot> GetAllLandmarksSnapshot()
        {
            var snapshot = new List<Data.LandmarkSnapshot>();
            
            if (poseController == null || poseController.SmoothedResult == null)
            {
                return snapshot;
            }
            
            foreach (var kvp in poseController.SmoothedResult.landmarks)
            {
                int index = kvp.Key;
                var landmark = kvp.Value;
                
                // Apenas landmarks visíveis (likelihood >= threshold)
                if (landmark.likelihood < 0.5f) continue;
                
                // Converte coordenadas normalizadas para coordenadas de tela
                Vector2 normalizedPos = new Vector2(landmark.x, landmark.y);
                Vector2 screenPos = CoordinateConverter.NormalizedToScreenPosition(normalizedPos);
                
                snapshot.Add(new Data.LandmarkSnapshot
                {
                    id = index,
                    x = screenPos.x,
                    y = screenPos.y
                });
            }
            
            return snapshot;
        }
        
        /// <summary>
        /// Obtém snapshot de todas as bolas ativas no momento da colisão
        /// Retorna posições em coordenadas normalizadas (0-1)
        /// </summary>
        private List<Data.BallSnapshot> GetAllBallsSnapshot(List<Ball> activeBalls)
        {
            var snapshot = new List<Data.BallSnapshot>();
            
            if (activeBalls == null) return snapshot;
            
            foreach (var ball in activeBalls)
            {
                if (ball == null || !ball.IsActive) continue;
                
                Vector2 normalizedPos = ball.GetNormalizedPosition();
                int ballId = ball.BallIndex >= 0 ? ball.BallIndex : (ball.Data != null ? ball.Data.BallId : -1);
                
                snapshot.Add(new Data.BallSnapshot
                {
                    id = ballId,
                    x = normalizedPos.x,
                    y = normalizedPos.y
                });
            }
            
            return snapshot;
        }
        
        /// <summary>
        /// Configura automaticamente um LandmarkFrameProcessor recém-criado
        /// </summary>
        private void ConfigureFrameProcessor(LandmarkFrameProcessor processor)
        {
            // Busca PoseController na cena para conectar ao processador
            PoseController poseController = FindObjectOfType<PoseController>();
            if (poseController != null)
            {
                processor.poseController = poseController;
                
                if (enableDebugLogs)
                {
                    Debug.Log("[PoseHitDetector] LandmarkFrameProcessor configurado automaticamente com PoseController");
                }
            }
            else
            {
                Debug.LogWarning("[PoseHitDetector] PoseController não encontrado! LandmarkFrameProcessor pode não funcionar corretamente.");
            }
        }
      
        void OnDrawGizmos()
        {
          

            // Debug: desenha raio das bolas ativas
            if (gameManager != null && Application.isPlaying && userSettings != null)
            {
                List<Ball> activeBalls = gameManager.GetActiveBalls();
                foreach (Ball ball in activeBalls)
                {
                    if (ball == null) continue;

                    // Desenha raio da bola (sempre o mesmo, baseado no tamanho)
                    Gizmos.color = Color.cyan;
                    float ballRadius = CalculateBallRadius(ball.Data);
                    float ballWorldRadius = ballRadius * Screen.width / 100f; // Conversão aproximada para world space
                    Gizmos.DrawWireSphere(ball.transform.position, ballWorldRadius);
                }
                
                // Nota: Visualização de landmarks movida para CollisionDebugVisualizer
            }
        }
    }
}

