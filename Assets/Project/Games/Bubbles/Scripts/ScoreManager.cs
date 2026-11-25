using UnityEngine;
using System;
using System.Collections.Generic;

namespace BubblesGame
{
    public class ScoreManager : MonoBehaviour
    {
        [Header("Base Scoring (Hit Landmarks)")]
        [SerializeField] private int baseHitScore = 10;        // Hit collision
        [SerializeField] private int baseMissScore = -2;         // Miss (destroy_time expiration)

        [Header("Base Scoring (Avoid Landmarks)")]
        [SerializeField] private int baseAvoidCollisionScore = -5;  // Avoid collision
        [SerializeField] private int baseAvoidMissScore = 0;        // Avoid miss (destroy_time expiration)

        [Header("Super Ball Multipliers")]
        [SerializeField] private int hitSuperMultiplier = 3;       // hit_super: baseHitScore × 3
        [SerializeField] private int avoidSuperMultiplier = 3;     // avoid_super: baseAvoidCollisionScore × 3

        [Header("Combo Settings")]
        [SerializeField] private bool useComboSystem = true;
        [SerializeField] private float comboTimeWindow = 2f; // Tempo para manter combo
        [SerializeField] private int comboMultiplierStep = 1; // +1 a cada X hits

        [Header("Lives System")]
        [SerializeField] private bool useLivesSystem = false;
        [SerializeField] private int startingLives = 3;

        // State
        private int currentScore = 0;
        private int lastScore = 0;
        private int currentCombo = 0;
        private int maxCombo = 0;
        private float lastHitTime = 0f;
        private int currentLives = 0;
        private int totalHits = 0;
        private int totalMisses = 0;
        private int totalBalls = 0;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnComboChanged;
        public event Action<int> OnLivesChanged;
        public event Action OnGameOver;

        // Properties
        public int CurrentScore => currentScore;
        public int LastScore => lastScore;
        public int CurrentCombo => currentCombo;
        public int MaxCombo => maxCombo;
        public int CurrentLives => currentLives;
        public int TotalHits => totalHits;
        public int TotalMisses => totalMisses;
        public float Accuracy => totalBalls > 0 ? (float)totalHits / totalBalls * 100f : 0f;

        /// <summary>
        /// Captura snapshot das landmarks do PoseController
        /// </summary>
        private System.Collections.Generic.Dictionary<int, object> GetLandmarksSnapshot()
        {
            var poseController = FindObjectOfType<Core.Tracking.PoseController>();
            if (poseController != null && poseController.SmoothedResult != null)
            {
                var result = new System.Collections.Generic.Dictionary<int, object>();
                foreach (var kvp in poseController.SmoothedResult.landmarks)
                {
                    result[kvp.Key] = new System.Collections.Generic.Dictionary<string, object> {
                        { "index", kvp.Key },
                        { "x", kvp.Value.x },
                        { "y", kvp.Value.y },
                        { "z", kvp.Value.z },
                        { "likelihood", kvp.Value.likelihood }
                    };
                }
                return result;
            }
            return null;
        }

        /// <summary>
        /// Obtém lista de bolas ativas
        /// </summary>
        private System.Collections.Generic.List<Ball> GetActiveBalls()
        {
            var gameManager = FindObjectOfType<BubblesGameManager>();
            if (gameManager != null)
            {
                return gameManager.GetActiveBalls();
            }
            return null;
        }

        void Start()
        {
            // Registra no GameServices para acesso global
            GameServices.Register(this);
            
            ResetScore();
        }
        
        void OnDestroy()
        {
            // Limpa referência ao sair
            if (GameServices.ScoreManager == this)
            {
                GameServices.Clear();
            }
        }

        void Update()
        {
            // Quebra combo se passou tempo demais sem hit
            if (useComboSystem && currentCombo > 0)
            {
                if (Time.time - lastHitTime > comboTimeWindow)
                {
                    BreakCombo();
                }
            }
        }

        public void ResetScore()
        {
            currentScore = 0;
            currentCombo = 0;
            maxCombo = 0;
            currentLives = startingLives;
            totalHits = 0;
            totalMisses = 0;
            totalBalls = 0;
            lastHitTime = 0f;

            OnScoreChanged?.Invoke(currentScore);
            OnComboChanged?.Invoke(currentCombo);
            OnLivesChanged?.Invoke(currentLives);
        }

        public void SetTotalBalls(int count)
        {
            totalBalls = count;
        }

        /// <summary>
        /// Registra um hit na bola
        /// </summary>
        public void RegisterHit(Ball ball, int landmarkId = -1, Vector3? landmarkPos = null, float distance = 0f)
        {
            if (ball == null) return;

            // hit_all_super: acerta todas as bolas ativas
            if (ball.Data.IsHitAllSuper)
            {
                RegisterHitAllSuper(ball, landmarkId, landmarkPos);
                return;
            }

            totalHits++;
            lastHitTime = Time.time;

            // Calcula pontos base
            int points = baseHitScore;

            // hit_super: multiplica base hit por 3
            if (ball.Data.IsHitSuper)
            {
                points = baseHitScore * hitSuperMultiplier;
            }

            // avoid_super acertado por engano: base avoid collision × 3
            if (ball.Data.IsAvoidSuper)
            {
                points = baseAvoidCollisionScore * avoidSuperMultiplier;
                Debug.Log($"[ScoreManager] Hit avoid_super (mistake)! {points} points");
            }

            // Atualiza combo
            if (useComboSystem)
            {
                currentCombo++;
                if (currentCombo > maxCombo)
                {
                    maxCombo = currentCombo;
                }

                // Aplica multiplicador de combo
                int comboMultiplier = 1 + (currentCombo / comboMultiplierStep);
                points *= comboMultiplier;

                OnComboChanged?.Invoke(currentCombo);
            }

            // Adiciona pontos
            AddScore(points);

            // Registra evento de hit com dados REAIS
            var ballEventRecorder = FindObjectOfType<BubblesGame.Data.Services.BallEventRecorder>();
            if (ballEventRecorder != null && ballEventRecorder.IsCapturing)
            {
                ballEventRecorder.RecordBallEvent(
                    "ball_hit",
                    ball,
                    landmarkUsed: landmarkId >= 0 ? (int?)landmarkId : null,
                    landmarkPosition: landmarkPos,
                    distanceToCenter: distance > 0 ? (float?)distance : null,
                    currentScore: currentScore,
                    currentCombo: currentCombo,
                    activeBallsObj: GetActiveBalls(),
                    landmarks: GetLandmarksSnapshot()
                );
            }

            Debug.Log($"[ScoreManager] Hit! +{points} pontos (Combo: {currentCombo})");
        }

        /// <summary>
        /// Registra um miss (bola expirou por destroy_time)
        /// </summary>
        public void RegisterMiss(Ball ball)
        {
            if (ball == null) return;

            totalMisses++;

            int missScore = 0;

            // Determina score de missing baseado no tipo de bola
            if (ball.Data.IsHitAllSuper)
            {
                // hit_all_super miss: 0 pontos
                missScore = 0;
            }
            else if (ball.Data.IsAvoidSuper)
            {
                // avoid_super miss: base avoid miss × 1 = 0
                missScore = baseAvoidMissScore;
                Debug.Log("[ScoreManager] Avoided avoid_super correctly! No penalty");
            }
            else if (ball.Data.IsHitSuper)
            {
                // hit_super miss: base miss × 1 = -2
                missScore = baseMissScore;
            }
            else if (ball.Data.IsDestroyAllSuper)
            {
                // destroy_all_super não tem miss (só destroy collision)
                missScore = 0;
            }
            else
            {
                // Bola normal: base miss
                missScore = baseMissScore;
            }

            // Quebra combo
            if (useComboSystem)
            {
                BreakCombo();
            }

            // Aplica penalidade de missing
            if (missScore != 0)
            {
                AddScore(missScore);
            }

            // Remove vida
            if (useLivesSystem)
            {
                LoseLife();
            }

            // Registra evento de miss
            var ballEventRecorder = FindObjectOfType<BubblesGame.Data.Services.BallEventRecorder>();
            if (ballEventRecorder != null && ballEventRecorder.IsCapturing)
            {
                ballEventRecorder.RecordBallEvent(
                    "ball_missed",
                    ball,
                    currentScore: currentScore,
                    currentCombo: currentCombo,
                    activeBallsObj: GetActiveBalls(),
                    landmarks: GetLandmarksSnapshot()
                );
            }

            Debug.Log($"[ScoreManager] Miss! {missScore} pontos (Vidas: {currentLives})");
        }

        /// <summary>
        /// Registra uma destruição (tocou destroy landmark)
        /// </summary>
        public void RegisterDestroy(Ball ball, int landmarkId = -1)
        {
            if (ball == null) return;

            // destroy_all_super: destrói TODAS as bolas
            if (ball.Data.IsDestroyAllSuper)
            {
                RegisterDestroyAllSuper(ball, landmarkId);
                return;
            }

            // Bola que deve ser evitada? Penaliza
            if (ball.Data.IsAvoidSuper)
            {
                Debug.Log("[ScoreManager] Acertou bola que deveria evitar!");
                AddScore(baseAvoidCollisionScore * avoidSuperMultiplier);
                
                if (useLivesSystem)
                {
                    LoseLife();
                }
                return;
            }

            // Pontos normais por destruição (não usado mais, mas mantido para compatibilidade)
            Debug.Log($"[ScoreManager] Destroy! (normal ball destroyed)");
        }

        /// <summary>
        /// hit_all_super: acerta todas as bolas ativas na tela
        /// </summary>
        private void RegisterHitAllSuper(Ball superBall, int landmarkId, Vector3? landmarkPos)
        {
            var activeBalls = GetActiveBalls();
            if (activeBalls == null || activeBalls.Count == 0)
            {
                Debug.Log("[ScoreManager] hit_all_super triggered but no active balls");
                return;
            }

            int totalPoints = 0;
            int ballsHit = 0;
            
            foreach (var ball in activeBalls)
            {
                if (ball != null && ball != superBall && ball.IsActive)
                {
                    // Soma pontos base de hit para cada bola ativa
                    int ballPoints = baseHitScore;
                    
                    // Se a bola for hit_super, multiplica por 3
                    if (ball.Data.IsHitSuper)
                    {
                        ballPoints = baseHitScore * hitSuperMultiplier;
                    }
                    
                    totalPoints += ballPoints;
                    totalHits++;
                    ballsHit++;
                }
            }

            AddScore(totalPoints);
            Debug.Log($"[ScoreManager] hit_all_super! Hit {ballsHit} balls for +{totalPoints} points");
            
            // Registra evento
            var ballEventRecorder = FindObjectOfType<BubblesGame.Data.Services.BallEventRecorder>();
            if (ballEventRecorder != null && ballEventRecorder.IsCapturing)
            {
                ballEventRecorder.RecordBallEvent(
                    "ball_hit_all_super",
                    superBall,
                    landmarkUsed: landmarkId >= 0 ? (int?)landmarkId : null,
                    landmarkPosition: landmarkPos,
                    currentScore: currentScore,
                    currentCombo: currentCombo,
                    activeBallsObj: GetActiveBalls(),
                    landmarks: GetLandmarksSnapshot()
                );
            }
        }

        /// <summary>
        /// destroy_all_super: quando colide com ANY landmark, destrói todas as bolas
        /// </summary>
        private void RegisterDestroyAllSuper(Ball superBall, int landmarkId)
        {
            var activeBalls = GetActiveBalls();
            if (activeBalls == null || activeBalls.Count == 0)
            {
                Debug.Log("[ScoreManager] destroy_all_super triggered but no active balls");
                return;
            }

            int totalPenalty = 0;
            int ballsDestroyed = 0;
            
            var gameManager = FindObjectOfType<BubblesGameManager>();
            
            // Cria lista temporária para evitar modificação durante iteração
            var ballsToDestroy = new List<Ball>();
            foreach (var ball in activeBalls)
            {
                if (ball != null && ball != superBall && ball.IsActive)
                {
                    ballsToDestroy.Add(ball);
                }
            }
            
            foreach (var ball in ballsToDestroy)
            {
                // Calcula miss score baseado no tipo de bola (como destroy_time expiration)
                int ballMissScore = baseMissScore;
                
                if (ball.Data.IsAvoidSuper)
                {
                    ballMissScore = baseAvoidMissScore; // 0
                }
                else if (ball.Data.IsHitSuper)
                {
                    ballMissScore = baseMissScore; // -2 (base miss × 1)
                }
                // hit_all_super e destroy_all_super não devem estar na lista de bolas normais
                
                totalPenalty += ballMissScore;
                totalMisses++;
                ballsDestroyed++;
                
                // Destroi a bola visualmente
                if (gameManager != null)
                {
                    gameManager.RemoveBall(ball);
                }
                Destroy(ball.gameObject);
            }

            AddScore(totalPenalty);
            Debug.Log($"[ScoreManager] destroy_all_super! Destroyed {ballsDestroyed} balls for {totalPenalty} points");
            
            // Registra evento
            var ballEventRecorder = FindObjectOfType<BubblesGame.Data.Services.BallEventRecorder>();
            if (ballEventRecorder != null && ballEventRecorder.IsCapturing)
            {
                ballEventRecorder.RecordBallEvent(
                    "ball_destroy_all_super",
                    superBall,
                    landmarkUsed: landmarkId >= 0 ? (int?)landmarkId : null,
                    currentScore: currentScore,
                    currentCombo: currentCombo,
                    activeBallsObj: GetActiveBalls(),
                    landmarks: GetLandmarksSnapshot()
                );
            }
        }

        void AddScore(int points)
        {
            lastScore = currentScore;
            currentScore = Mathf.Max(0, currentScore + points); // Score não pode ser negativo
            OnScoreChanged?.Invoke(currentScore);
        }

        void BreakCombo()
        {
            if (currentCombo > 0)
            {
                Debug.Log($"[ScoreManager] Combo quebrado! (Max: {currentCombo})");
                currentCombo = 0;
                OnComboChanged?.Invoke(currentCombo);
            }
        }

        void LoseLife()
        {
            currentLives--;
            OnLivesChanged?.Invoke(currentLives);

            if (currentLives <= 0)
            {
                Debug.Log("[ScoreManager] Game Over!");
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Retorna estatísticas finais da partida
        /// </summary>
        public MatchStats GetMatchStats()
        {
            // Obtém tempo total do jogo
            float gameTime = 0f;
            var gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                gameTime = gameController.GetGameTimestamp() / 1000f; // converte de ms para segundos
            }

            // Calcula bolas por minuto
            float ballsPerMin = gameTime > 0 ? (totalBalls / (gameTime / 60f)) : 0f;

            // Calcula pontos médios por acerto
            float avgPointsPerHit = totalHits > 0 ? (float)currentScore / totalHits : 0f;

            return new MatchStats
            {
                finalScore = currentScore,
                maxCombo = maxCombo,
                totalHits = totalHits,
                totalMisses = totalMisses,
                totalBalls = totalBalls,
                accuracy = Accuracy,
                totalGameTime = gameTime,
                ballsPerMinute = ballsPerMin,
                averagePointsPerHit = avgPointsPerHit
            };
        }
    }

    [Serializable]
    public struct MatchStats
    {
        public int finalScore;
        public int maxCombo;
        public int totalHits;
        public int totalMisses;
        public int totalBalls;
        public float accuracy;
        public float totalGameTime; // em segundos
        public float ballsPerMinute; // bolas por minuto
        public float averagePointsPerHit; // pontos médios por acerto
    }
}

