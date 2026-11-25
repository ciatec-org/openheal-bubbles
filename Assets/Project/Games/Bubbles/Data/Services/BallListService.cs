using System;
using System.Collections.Generic;
using UnityEngine;
using BubblesGame.Data;
using Firebase.Firestore;
using Firebase.Extensions;

namespace BubblesGame.Data.Services
{
    /// <summary>
    /// Serviço para armazenar e fazer upload da lista de bolas em chunks
    /// Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/ball_list/chunks/chunk_{index}
    /// </summary>
    public class BallListService : MonoBehaviour
    {
        private const int ChunkSize = 50;
        
        private List<BallData> cachedBalls = new List<BallData>();
        private bool hasStoredBalls = false;
        
        // Identificadores da partida
        private string userId;
        private string companyId;
        private string gameId;
        private string matchId;
        
        private FirebaseFirestore db;
        
        void Awake()
        {
            db = FirebaseFirestore.DefaultInstance;
        }
        
        /// <summary>
        /// Armazena a lista de bolas em cache (chamado após LevelPlanner.Build)
        /// </summary>
        public void StoreBallList(List<BallData> balls)
        {
            if (balls == null || balls.Count == 0)
            {
                Debug.LogWarning("[BallListService] Tentativa de armazenar lista vazia de bolas");
                return;
            }
            
            cachedBalls = new List<BallData>(balls);
            hasStoredBalls = true;
            
            Debug.Log($"[BallListService] Armazenadas {cachedBalls.Count} bolas em cache");
        }
        
        /// <summary>
        /// Configura os identificadores da partida
        /// </summary>
        public void SetMatchInfo(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
        }
        
        /// <summary>
        /// Divide a lista de bolas em chunks e salva no Firebase
        /// </summary>
        public void SaveToFirebase(Action onSuccess = null, Action<string> onError = null)
        {
            if (!hasStoredBalls || cachedBalls.Count == 0)
            {
                Debug.LogWarning("[BallListService] Nenhuma bola armazenada para salvar");
                onError?.Invoke("Nenhuma bola armazenada");
                return;
            }
            
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || 
                string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(matchId))
            {
                Debug.LogError("[BallListService] Informações da partida não configuradas");
                onError?.Invoke("Informações da partida não configuradas");
                return;
            }
            
            Debug.Log($"[BallListService] Iniciando upload de {cachedBalls.Count} bolas em chunks de {ChunkSize}");
            
            // Divide em chunks
            List<List<BallData>> chunks = DivideIntoChunks(cachedBalls, ChunkSize);
            Debug.Log($"[BallListService] Dividido em {chunks.Count} chunks");
            
            // Faz upload sequencial de cada chunk
            StartCoroutine(UploadChunksSequentially(chunks, onSuccess, onError));
        }
        
        /// <summary>
        /// Divide a lista de bolas em chunks de tamanho fixo
        /// </summary>
        private List<List<BallData>> DivideIntoChunks(List<BallData> balls, int chunkSize)
        {
            var chunks = new List<List<BallData>>();
            
            for (int i = 0; i < balls.Count; i += chunkSize)
            {
                int remaining = balls.Count - i;
                int currentChunkSize = Mathf.Min(chunkSize, remaining);
                
                var chunk = new List<BallData>();
                for (int j = 0; j < currentChunkSize; j++)
                {
                    chunk.Add(balls[i + j]);
                }
                
                chunks.Add(chunk);
            }
            
            return chunks;
        }
        
        /// <summary>
        /// Faz upload sequencial dos chunks para o Firebase
        /// </summary>
        private System.Collections.IEnumerator UploadChunksSequentially(
            List<List<BallData>> chunks, 
            Action onSuccess, 
            Action<string> onError)
        {
            int uploadedChunks = 0;
            bool hasError = false;
            string errorMessage = "";
            
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                int chunkIndex = i + 1; // chunk_1, chunk_2, etc.
                
                // Serializa o chunk para JSON
                string jsonData = SerializeChunkToJson(chunk);
                
                // Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/ball_list/chunk_{index}
                // ball_list é uma subcollection e chunk_{index} são documentos dentro dela
                DocumentReference chunkRef = db
                    .Collection("users").Document(userId)
                    .Collection("companies").Document(companyId)
                    .Collection("games").Document(gameId)
                    .Collection("matches").Document(matchId)
                    .Collection("ball_list")
                    .Document($"chunk_{chunkIndex}");
                
                // Prepara dados do documento
                var chunkData = new Dictionary<string, object>
                {
                    ["chunk_index"] = chunkIndex,
                    ["total_chunks"] = chunks.Count,
                    ["ball_count"] = chunk.Count,
                    ["balls"] = jsonData, // JSON string
                    ["created_at"] = FieldValue.ServerTimestamp
                };
                
                bool uploadCompleted = false;
                bool uploadSuccess = false;
                string uploadError = "";
                
                // Faz upload
                chunkRef.SetAsync(chunkData).ContinueWithOnMainThread(task =>
                {
                    uploadCompleted = true;
                    
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        uploadSuccess = false;
                        uploadError = task.Exception?.GetBaseException().Message ?? "Erro desconhecido";
                        Debug.LogError($"[BallListService] Erro ao fazer upload do chunk_{chunkIndex}: {uploadError}");
                    }
                    else
                    {
                        uploadSuccess = true;
                        uploadedChunks++;
                        Debug.Log($"[BallListService] Chunk_{chunkIndex} salvo com sucesso ({chunk.Count} bolas)");
                    }
                });
                
                // Aguarda upload completar
                while (!uploadCompleted)
                {
                    yield return null;
                }
                
                if (!uploadSuccess)
                {
                    hasError = true;
                    errorMessage = $"Erro ao fazer upload do chunk_{chunkIndex}: {uploadError}";
                    break;
                }
                
                // Pequeno delay entre uploads para não sobrecarregar
                yield return new WaitForSeconds(0.1f);
            }
            
            if (hasError)
            {
                Debug.LogError($"[BallListService] Falha no upload: {errorMessage}");
                onError?.Invoke(errorMessage);
            }
            else
            {
                Debug.Log($"[BallListService] ✓ Upload completo: {uploadedChunks}/{chunks.Count} chunks salvos");
                onSuccess?.Invoke();
            }
        }
        
        /// <summary>
        /// Serializa um chunk de bolas para JSON
        /// </summary>
        private string SerializeChunkToJson(List<BallData> chunk)
        {
            // JsonUtility não serializa List diretamente, precisa de wrapper
            var wrapper = new BallDataArrayWrapper { balls = chunk.ToArray() };
            return JsonUtility.ToJson(wrapper);
        }
        
        /// <summary>
        /// Wrapper para serialização JSON de array de bolas
        /// </summary>
        [Serializable]
        private class BallDataArrayWrapper
        {
            public BallData[] balls;
        }
        
        /// <summary>
        /// Limpa o cache de bolas
        /// </summary>
        public void ClearCache()
        {
            cachedBalls.Clear();
            hasStoredBalls = false;
            Debug.Log("[BallListService] Cache limpo");
        }
        
        /// <summary>
        /// Verifica se há bolas armazenadas
        /// </summary>
        public bool HasStoredBalls => hasStoredBalls && cachedBalls.Count > 0;
        
        /// <summary>
        /// Retorna o número de bolas armazenadas
        /// </summary>
        public int BallCount => cachedBalls.Count;
    }
}

