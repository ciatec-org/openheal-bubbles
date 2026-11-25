using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BubblesGame.Data;
using Firebase.Firestore;
using Firebase.Extensions;

namespace BubblesGame.Data.Services
{
    /// <summary>
    /// Classe responsável por preparar e enviar event-data em chunks (100 eventos cada)
    /// Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/event_data/chunk_{index}
    /// </summary>
    public class EventDataService : MonoBehaviour
    {
        private const int ChunkSize = 100;
        private List<Dictionary<string, object>> currentChunk = new();
        private List<List<Dictionary<string, object>>> completedChunks = new();
        private bool isCapturing = false;

        // Identificadores da partida, setados no início da partida
        public string userId;
        public string companyId;
        public string gameId;
        public string matchId;

        // Para enviar os dados (usar seu método preferido, por exemplo Firestore, REST, etc.)
        public Action<List<Dictionary<string, object>>> OnChunkReadyToUpload;

        private FirebaseFirestore db;

        void Awake()
        {
            db = FirebaseFirestore.DefaultInstance;
        }

        // Configurar informações da partida (chamado antes de StartEventCapture)
        public void SetMatchInfo(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
            Debug.Log($"[EventDataService] Match info configurada - userId: {userId}, matchId: {matchId}");
        }

        // Startar a captura no início do jogo
        public void StartEventCapture(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
            isCapturing = true;
            currentChunk.Clear();
            completedChunks.Clear();
            
            Debug.Log($"[EventDataService] Captura iniciada - userId: {userId}, companyId: {companyId}, gameId: {gameId}, matchId: {matchId}");
        }

        // Chamar em cada evento de colisão
        public void RecordEvent(CollisionEventData eventData)
        {
            if (!isCapturing) 
            {
                Debug.LogWarning($"[EventDataService] Tentativa de gravar evento mas captura não está ativa - isCapturing: {isCapturing}");
                return;
            }
            
            // Converte CollisionEventData para Dictionary
            var eventDict = ConvertEventToDictionary(eventData);
            currentChunk.Add(eventDict);
            
            // Log do primeiro evento para confirmar que está funcionando
            if (TotalEventsCaptured == 1)
            {
                Debug.Log($"[EventDataService] ✓ Primeiro evento capturado! Ball_id: {eventData.ball_id}, Landmark: {eventData.hit_landmark}, Type: {eventData.Type}");
            }
            
            // Log apenas a cada 25 eventos para reduzir spam
            if (currentChunk.Count % 25 == 0 || currentChunk.Count >= ChunkSize)
            {
                Debug.Log($"[EventDataService] Eventos capturados: {TotalEventsCaptured} (chunk atual: {currentChunk.Count}/{ChunkSize})");
            }
            
            if (currentChunk.Count >= ChunkSize)
            {
                FlushChunk();
            }
        }

        /// <summary>
        /// Converte CollisionEventData para Dictionary para serialização
        /// </summary>
        private Dictionary<string, object> ConvertEventToDictionary(CollisionEventData eventData)
        {
            var dict = new Dictionary<string, object>
            {
                ["ball_id"] = eventData.ball_id,
                ["hit_coord_x"] = eventData.hit_coord_x,
                ["hit_coord_y"] = eventData.hit_coord_y,
                ["hit_time"] = eventData.hit_time,
                ["hit_landmark"] = eventData.hit_landmark
            };

            // Converte landmark_list
            if (eventData.landmark_list != null && eventData.landmark_list.Count > 0)
            {
                var landmarkList = new List<Dictionary<string, object>>();
                foreach (var landmark in eventData.landmark_list)
                {
                    landmarkList.Add(new Dictionary<string, object>
                    {
                        ["id"] = landmark.id,
                        ["x"] = landmark.x,
                        ["y"] = landmark.y
                    });
                }
                dict["landmark_list"] = landmarkList;
            }
            else
            {
                dict["landmark_list"] = new List<Dictionary<string, object>>();
            }

            // Converte balls_on_screen
            if (eventData.balls_on_screen != null && eventData.balls_on_screen.Count > 0)
            {
                var ballsList = new List<Dictionary<string, object>>();
                foreach (var ball in eventData.balls_on_screen)
                {
                    ballsList.Add(new Dictionary<string, object>
                    {
                        ["id"] = ball.id,
                        ["x"] = ball.x,
                        ["y"] = ball.y
                    });
                }
                dict["balls_on_screen"] = ballsList;
            }
            else
            {
                dict["balls_on_screen"] = new List<Dictionary<string, object>>();
            }

            return dict;
        }

        // Pode ser chamado independentemente do tamanho do chunk (no EndGame, etc)
        public void FlushChunk()
        {
            // Sempre cria um chunk, mesmo se vazio (lista vazia de eventos)
            // Isso garante que sempre haverá pelo menos um chunk salvo
            completedChunks.Add(new List<Dictionary<string, object>>(currentChunk));
            
            // Mantém compatibilidade com callback existente
            OnChunkReadyToUpload?.Invoke(new List<Dictionary<string, object>>(currentChunk));
            
            currentChunk.Clear();
        }

        public void StopEventCapture()
        {
            isCapturing = false;
            // Flush chunk final se houver dados pendentes
            // Isso garante que o chunk final seja adicionado aos completedChunks
            if (currentChunk.Count > 0)
            {
                FlushChunk(); // garante salvar qualquer dado remanescente
            }
            Debug.Log($"[EventDataService] Captura parada - Total chunks: {completedChunks.Count}, Eventos no chunk atual: {currentChunk.Count}");
        }

        /// <summary>
        /// Salva todos os chunks de event-data no Firebase
        /// Retorna uma coroutine que pode ser aguardada
        /// Sempre cria pelo menos um chunk (mesmo vazio) para garantir que event_data existe
        /// </summary>
        public System.Collections.IEnumerator SaveToFirebaseCoroutine()
        {
            // Flush chunk final se houver dados pendentes (mesmo se vazio)
            // Isso garante que sempre haverá pelo menos um chunk salvo
            if (currentChunk.Count > 0 || completedChunks.Count == 0)
            {
                // Se não há chunks completos e o chunk atual está vazio, cria chunk vazio
                completedChunks.Add(new List<Dictionary<string, object>>(currentChunk));
                currentChunk.Clear();
            }

            // Sempre salva pelo menos um chunk (mesmo vazio)
            if (completedChunks.Count == 0)
            {
                // Cria chunk vazio para garantir que event_data existe
                completedChunks.Add(new List<Dictionary<string, object>>());
                Debug.Log("[EventDataService] Criando chunk vazio (nenhum evento de colisão)");
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || 
                string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(matchId))
            {
                Debug.LogError("[EventDataService] Informações da partida não configuradas");
                yield break;
            }

            Debug.Log($"[EventDataService] Iniciando upload de {completedChunks.Count} chunks de event-data");

            int uploadedChunks = 0;
            bool hasError = false;
            string errorMessage = "";
            
            // Estrutura para rastrear uploads paralelos
            var uploadTasks = new List<System.Threading.Tasks.Task>();
            var uploadResults = new bool[completedChunks.Count];
            var uploadErrors = new string[completedChunks.Count];

            // Inicia todos os uploads em paralelo
            for (int i = 0; i < completedChunks.Count; i++)
            {
                var chunk = completedChunks[i];
                int chunkIndex = i + 1; // chunk_1, chunk_2, etc.
                int currentIndex = i; // Captura para closure

                // Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/event_data/chunk_{index}
                DocumentReference chunkRef = db
                    .Collection("users").Document(userId)
                    .Collection("companies").Document(companyId)
                    .Collection("games").Document(gameId)
                    .Collection("matches").Document(matchId)
                    .Collection("event_data")
                    .Document($"chunk_{chunkIndex}");

                // Prepara dados do documento
                var chunkData = new Dictionary<string, object>
                {
                    ["chunk_index"] = chunkIndex,
                    ["total_chunks"] = completedChunks.Count,
                    ["event_count"] = chunk.Count,
                    ["events"] = chunk, // Lista de eventos diretamente
                    ["created_at"] = FieldValue.ServerTimestamp
                };

                // Faz upload assíncrono
                var task = chunkRef.SetAsync(chunkData).ContinueWithOnMainThread(uploadTask =>
                {
                    if (uploadTask.IsFaulted || uploadTask.IsCanceled)
                    {
                        uploadResults[currentIndex] = false;
                        uploadErrors[currentIndex] = uploadTask.Exception?.GetBaseException().Message ?? "Erro desconhecido";
                        Debug.LogError($"[EventDataService] Erro ao fazer upload do chunk_{chunkIndex}: {uploadErrors[currentIndex]}");
                    }
                    else
                    {
                        uploadResults[currentIndex] = true;
                        uploadedChunks++;
                        Debug.Log($"[EventDataService] Chunk_{chunkIndex} salvo com sucesso ({chunk.Count} eventos)");
                    }
                });
                
                uploadTasks.Add(task);
            }

            // Aguarda todos os uploads completarem
            while (uploadTasks.Any(t => !t.IsCompleted))
            {
                yield return null;
            }

            // Verifica resultados
            for (int i = 0; i < uploadResults.Length; i++)
            {
                if (!uploadResults[i])
                {
                    hasError = true;
                    errorMessage = $"Erro ao fazer upload do chunk_{i + 1}: {uploadErrors[i]}";
                    break;
                }
            }

            if (hasError)
            {
                Debug.LogError($"[EventDataService] Falha no upload: {errorMessage}");
            }
            else
            {
                Debug.Log($"[EventDataService] ✓ Upload completo: {uploadedChunks}/{completedChunks.Count} chunks salvos");
            }
        }

        /// <summary>
        /// Limpa os chunks armazenados
        /// </summary>
        public void ClearChunks()
        {
            completedChunks.Clear();
            currentChunk.Clear();
            Debug.Log("[EventDataService] Chunks limpos");
        }

        /// <summary>
        /// Verifica se há chunks para salvar
        /// </summary>
        public bool HasChunksToSave => completedChunks.Count > 0 || currentChunk.Count > 0;

        /// <summary>
        /// Retorna o número total de eventos capturados
        /// </summary>
        public int TotalEventsCaptured
        {
            get
            {
                int total = currentChunk.Count;
                foreach (var chunk in completedChunks)
                {
                    total += chunk.Count;
                }
                return total;
            }
        }

        public bool IsCapturing => isCapturing;
    }
}

