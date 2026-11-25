using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

namespace BubblesGame.Data.Services
{
    /// <summary>
    /// Classe responsável por preparar e enviar pose-data em chunks (200 frames cada)
    /// Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/pose_data/chunk_{index}
    /// </summary>
    public class PoseDataRecorder : MonoBehaviour
    {
        private const int ChunkSize = 200;
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

        // Configurar informações da partida (chamado antes de StartPoseCapture)
        public void SetMatchInfo(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
            Debug.Log($"[PoseDataRecorder] Match info configurada - userId: {userId}, matchId: {matchId}");
        }

        // Startar a captura no início do jogo
        public void StartPoseCapture(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
            isCapturing = true;
            currentChunk.Clear();
            completedChunks.Clear();
            
            Debug.Log($"[PoseDataRecorder] Captura iniciada - userId: {userId}, companyId: {companyId}, gameId: {gameId}, matchId: {matchId}");
        }

        // Chamar em cada callback de pose recebida
        public void RecordPoseFrame(Dictionary<string, object> poseFrameWithTimestamp)
        {
            if (!isCapturing) return;
            
            currentChunk.Add(poseFrameWithTimestamp);
            
            // Log apenas a cada 50 frames para reduzir spam
            if (currentChunk.Count % 50 == 0 || currentChunk.Count >= ChunkSize)
            {
                Debug.Log($"[PoseDataRecorder] Frames capturados: {TotalFramesCaptured} (chunk atual: {currentChunk.Count}/{ChunkSize})");
            }
            
            if (currentChunk.Count >= ChunkSize)
            {
                FlushChunk();
            }
        }

        // Pode ser chamado independentemente do tamanho do chunk (no EndGame, etc)
        public void FlushChunk()
        {
            if (currentChunk.Count == 0) return;
            
            // Armazena chunk completo para upload posterior
            completedChunks.Add(new List<Dictionary<string, object>>(currentChunk));
            
            // Mantém compatibilidade com callback existente
            OnChunkReadyToUpload?.Invoke(new List<Dictionary<string, object>>(currentChunk));
            
            currentChunk.Clear();
        }

        public void StopPoseCapture()
        {
            isCapturing = false;
            // Flush chunk final se houver dados pendentes
            // Isso garante que o chunk final seja adicionado aos completedChunks
            if (currentChunk.Count > 0)
            {
                FlushChunk(); // garante salvar qualquer dado remanescente
            }
            Debug.Log($"[PoseDataRecorder] Captura parada - Total chunks: {completedChunks.Count}, Frames no chunk atual: {currentChunk.Count}");
        }

        /// <summary>
        /// Salva todos os chunks de pose-data no Firebase
        /// Retorna uma coroutine que pode ser aguardada
        /// </summary>
        public System.Collections.IEnumerator SaveToFirebaseCoroutine()
        {
            // Flush chunk final se houver dados pendentes
            if (currentChunk.Count > 0)
            {
                completedChunks.Add(new List<Dictionary<string, object>>(currentChunk));
                currentChunk.Clear();
            }

            if (completedChunks.Count == 0)
            {
                Debug.Log("[PoseDataRecorder] Nenhum chunk de pose-data para salvar");
                yield break;
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || 
                string.IsNullOrEmpty(gameId) || string.IsNullOrEmpty(matchId))
            {
                Debug.LogError("[PoseDataRecorder] Informações da partida não configuradas");
                yield break;
            }

            Debug.Log($"[PoseDataRecorder] Iniciando upload de {completedChunks.Count} chunks de pose-data");

            int uploadedChunks = 0;
            bool hasError = false;
            string errorMessage = "";

            for (int i = 0; i < completedChunks.Count; i++)
            {
                var chunk = completedChunks[i];
                int chunkIndex = i + 1; // chunk_1, chunk_2, etc.

                // Path: users/{userId}/companies/{companyId}/games/{gameId}/matches/{matchId}/pose_data/chunk_{index}
                DocumentReference chunkRef = db
                    .Collection("users").Document(userId)
                    .Collection("companies").Document(companyId)
                    .Collection("games").Document(gameId)
                    .Collection("matches").Document(matchId)
                    .Collection("pose_data")
                    .Document($"chunk_{chunkIndex}");

                // Prepara dados do documento
                // Firestore aceita List<Dictionary<string, object>> diretamente
                var chunkData = new Dictionary<string, object>
                {
                    ["chunk_index"] = chunkIndex,
                    ["total_chunks"] = completedChunks.Count,
                    ["frame_count"] = chunk.Count,
                    ["frames"] = chunk, // Lista de frames diretamente
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
                        Debug.LogError($"[PoseDataRecorder] Erro ao fazer upload do chunk_{chunkIndex}: {uploadError}");
                    }
                    else
                    {
                        uploadSuccess = true;
                        uploadedChunks++;
                        Debug.Log($"[PoseDataRecorder] Chunk_{chunkIndex} salvo com sucesso ({chunk.Count} frames)");
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
                Debug.LogError($"[PoseDataRecorder] Falha no upload: {errorMessage}");
            }
            else
            {
                Debug.Log($"[PoseDataRecorder] ✓ Upload completo: {uploadedChunks}/{completedChunks.Count} chunks salvos");
            }
        }

        /// <summary>
        /// Limpa os chunks armazenados
        /// </summary>
        public void ClearChunks()
        {
            completedChunks.Clear();
            currentChunk.Clear();
            Debug.Log("[PoseDataRecorder] Chunks limpos");
        }

        /// <summary>
        /// Verifica se há chunks para salvar
        /// </summary>
        public bool HasChunksToSave => completedChunks.Count > 0 || currentChunk.Count > 0;

        /// <summary>
        /// Retorna o número total de frames capturados
        /// </summary>
        public int TotalFramesCaptured
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