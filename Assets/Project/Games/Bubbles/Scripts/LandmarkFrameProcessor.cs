using UnityEngine;
using System;
using Core.Tracking;
using Core.MediaPipe;
using BubblesGame.Data;
using System.Collections.Generic;

namespace BubblesGame
{
    /// <summary>
    /// Processador central de landmarks - elimina redundância
    /// Converte landmarks do PoseController em dados prontos para uso
    /// </summary>
    public class LandmarkFrameProcessor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] public PoseController poseController;
        
        [Header("Settings")]
        [SerializeField] private float minLikelihood = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // Índices dos landmarks que usamos no jogo
        private static readonly int[] HIT_LANDMARK_INDICES = { 15, 16 };    // Pulsos
        private static readonly int[] DESTROY_LANDMARK_INDICES = { 0 };     // Nariz
        
        /// <summary>
        /// Evento central - todos os componentes consomem daqui
        /// </summary>
        public event Action<LandmarkFrameData> OnFrameProcessed;
        
        // Cache reutilizável para evitar alocações
        private LandmarkFrameData frameDataCache;
        private List<ProcessedLandmark> hitLandmarksCache;
        private List<ProcessedLandmark> destroyLandmarksCache;
        
        // Gravação de eventos (preparado para futuro)
        private IGameEventRecorder eventRecorder;

        void Awake()
        {
            // Inicializa caches
            frameDataCache = new LandmarkFrameData();
            hitLandmarksCache = new List<ProcessedLandmark>();
            destroyLandmarksCache = new List<ProcessedLandmark>();
            
            // Inicializa recorder (null object por enquanto)
            eventRecorder = new NullGameEventRecorder();
        }

        void Start()
        {
            // Busca PoseController se não configurado
            if (poseController == null)
            {
                poseController = FindObjectOfType<PoseController>();
                if (poseController == null)
                {
                    Debug.LogError("[LandmarkFrameProcessor] PoseController não encontrado!");
                    return;
                }
            }
            
            // Escuta evento do PoseController
            poseController.OnPoseProcessed += ProcessPoseResult;
            
            if (enableDebugLogs)
            {
                Debug.Log("[LandmarkFrameProcessor] Conectado ao PoseController");
            }
        }
        
        void OnDestroy()
        {
            if (poseController != null)
            {
                poseController.OnPoseProcessed -= ProcessPoseResult;
            }
        }

        /// <summary>
        /// Processa resultado do PoseController e emite evento com dados prontos
        /// </summary>
        private void ProcessPoseResult(PoseResult poseResult)
        {
            if (!poseResult.IsValid)
            {
                // Emite frame inválido
                frameDataCache.IsValid = false;
                frameDataCache.TimestampMs = poseResult.timestampMs;
                OnFrameProcessed?.Invoke(frameDataCache);
                return;
            }
            
            // Limpa caches
            hitLandmarksCache.Clear();
            destroyLandmarksCache.Clear();
            
            // Processa hit landmarks
            foreach (int index in HIT_LANDMARK_INDICES)
            {
                if (poseResult.landmarks.TryGetValue(index, out var landmark))
                {
                    var processed = ProcessLandmark(landmark, index);
                    hitLandmarksCache.Add(processed);
                }
            }
            
            // Processa destroy landmarks
            foreach (int index in DESTROY_LANDMARK_INDICES)
            {
                if (poseResult.landmarks.TryGetValue(index, out var landmark))
                {
                    var processed = ProcessLandmark(landmark, index);
                    destroyLandmarksCache.Add(processed);
                }
            }
            
            // Monta resultado final
            frameDataCache.TimestampMs = poseResult.timestampMs;
            frameDataCache.HitLandmarks = hitLandmarksCache.ToArray();
            frameDataCache.DestroyLandmarks = destroyLandmarksCache.ToArray();
            frameDataCache.IsValid = true;
            
            // Grava frame para futuro armazenamento
            eventRecorder.RecordLandmarkFrame(frameDataCache);
            
            // Grava pose-data para Firebase (se disponível)
            var poseDataManager = PoseDataManager.Instance;
            if (poseDataManager != null && poseDataManager.PoseRecorder != null)
            {
                if (poseDataManager.PoseRecorder.IsCapturing)
                {
                    var frame = new System.Collections.Generic.Dictionary<string, object>();
                    frame["t"] = poseResult.timestampMs;
                    var lmList = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                    foreach (var kvp in poseResult.landmarks)
                    {
                        lmList.Add(new System.Collections.Generic.Dictionary<string, object> {
                            { "index", kvp.Key },
                            { "x", kvp.Value.x },
                            { "y", kvp.Value.y },
                            { "z", kvp.Value.z },
                            { "likelihood", kvp.Value.likelihood }
                        });
                    }
                    frame["landmarks"] = lmList;
                    poseDataManager.PoseRecorder.RecordPoseFrame(frame);
                }
            }
            
            // Emite evento
            OnFrameProcessed?.Invoke(frameDataCache);
        }
        
        /// <summary>
        /// Processa um landmark individual
        /// Mantém coordenadas em TOP-LEFT normalized (0-1)
        /// Conversão para UI acontece apenas na visualização!
        /// </summary>
        private ProcessedLandmark ProcessLandmark(PoseLandmark landmark, int index)
        {
            // Mantém coordenadas TOP-LEFT normalized (0-1)
            // MediaPipe já vem em top-left, apenas fazemos clamp
            Vector2 normalizedTopLeft = new Vector2(
                Mathf.Clamp01(landmark.x),
                Mathf.Clamp01(landmark.y)
            );
            
            // Landmark só é visível se estiver DENTRO da tela E com likelihood suficiente
            bool isInBounds = CoordinateConverter.IsNormalizedInBounds(normalizedTopLeft);
            bool isVisible = isInBounds && landmark.likelihood >= minLikelihood;
            
            // Conversão para pixels de tela usando conversor centralizado
            Vector2 screenPos = CoordinateConverter.NormalizedToScreenPosition(normalizedTopLeft);
            
            return new ProcessedLandmark
            {
                Index = index,
                NormalizedPosition = normalizedTopLeft,  // TOP-LEFT normalized!
                ScreenPosition = screenPos,               // BOTTOM-LEFT pixels (só para compatibilidade)
                Likelihood = landmark.likelihood,
                IsVisible = isVisible
            };
        }
        
        /// <summary>
        /// Permite ajustar threshold de likelihood em runtime
        /// </summary>
        public void SetMinLikelihood(float threshold)
        {
            minLikelihood = Mathf.Clamp01(threshold);
        }
        
        /// <summary>
        /// Define o recorder de eventos (para futuro armazenamento)
        /// </summary>
        public void SetEventRecorder(IGameEventRecorder recorder)
        {
            eventRecorder = recorder ?? new NullGameEventRecorder();
        }
    }
}