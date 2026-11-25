using UnityEngine;
using Core.MediaPipe;
using System;
using System.Collections.Generic;

namespace Core.Tracking
{
    public class PoseController : MonoBehaviour
    {
        [Header("Filter Settings")]
        [Tooltip("Ative para suavizar o movimento (com pequeno atraso); desative para resposta mais imediata (mais tremida).")]
        public bool useFilter = true;

        private readonly Dictionary<int, OneEuroFilter> xFilters = new();
        private readonly Dictionary<int, OneEuroFilter> yFilters = new();
        private readonly Dictionary<int, OneEuroFilter> zFilters = new();

        public PoseResult SmoothedResult { get; private set; }
        
        /// <summary>
        /// Evento emitido após processar landmarks de um novo frame
        /// </summary>
        public event Action<PoseResult> OnPoseProcessed;

        void Awake()
        {
            SmoothedResult = new PoseResult();
        }

        public void OnPoseReceived(PoseResult result)
        {
            if (!result.IsValid) return;
            
            // Usa o timestamp do GameController se disponível
            float timestampMs = 0f;
            var gameControllerObj = GameObject.Find("GameController");
            if (gameControllerObj != null)
            {
                var gameController = gameControllerObj.GetComponent<MonoBehaviour>();
                if (gameController != null)
                {
                    // Usa reflexão para chamar GetGameTimestamp
                    var method = gameController.GetType().GetMethod("GetGameTimestamp");
                    if (method != null)
                    {
                        timestampMs = (float)method.Invoke(gameController, null);
                    }
                }
            }

            SmoothedResult.landmarks.Clear();

            foreach (var kvp in result.landmarks)
            {
                int index = kvp.Key;
                var landmark = kvp.Value;
                
                float x = landmark.x;
                float y = landmark.y;
                float z = landmark.z;

                // Espelhamento correto
                x = 1f - x; // porque a UI está flipada horizontalmente

                SmoothedResult.landmarks[index] = new PoseLandmark
                {
                    x = useFilter ? GetOrCreateFilter(xFilters, index).Filter(x) : x,
                    y = useFilter ? GetOrCreateFilter(yFilters, index).Filter(y) : y,
                    z = useFilter ? GetOrCreateFilter(zFilters, index).Filter(z) : z,
                    likelihood = landmark.likelihood
                };
            }
            
            // Usa timestamp unificado do GameController
            SmoothedResult.timestampMs = timestampMs;
            
            // Emite evento para consumidores
            OnPoseProcessed?.Invoke(SmoothedResult);
        }

        private OneEuroFilter GetOrCreateFilter(Dictionary<int, OneEuroFilter> filters, int index)
        {
            if (!filters.TryGetValue(index, out var filter))
            {
                filter = new OneEuroFilter(freq: 15f, minCutoff: 12f, beta: 8f, dCutoff: 10f);
                filters[index] = filter;
            }
            return filter;
        }

        /// <summary>
        /// Limpa todos os filtros (usado na limpeza de recursos)
        /// </summary>
        public void ClearFilters()
        {
            xFilters.Clear();
            yFilters.Clear();
            zFilters.Clear();
        }
    }
}
