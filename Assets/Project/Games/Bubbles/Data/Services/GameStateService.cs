using System;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

namespace BubblesGame.Data
{
    /// <summary>
    /// Gerencia o estado atual do jogo para o usuário (preset e level atuais)
    /// </summary>
    public class GameStateService : MonoBehaviour
    {
        public static GameStateService Instance { get; private set; }

        private FirebaseFirestore db;
        private FirebaseAuth auth;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            StartCoroutine(InitializeFirebase());
        }

        private System.Collections.IEnumerator InitializeFirebase()
        {
            Debug.Log("[GameStateService] Starting Firebase initialization...");
            
            // Aguarda um frame para garantir que a cena carregou
            yield return null;
            
            // Aguarda mais um pouco para garantir que o Firebase está pronto
            yield return new WaitForSeconds(0.5f);
            
            int maxRetries = 10;
            int retryCount = 0;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    db = FirebaseFirestore.DefaultInstance;
                    auth = FirebaseAuth.DefaultInstance;
                    
                    if (db != null && auth != null)
                    {
                        Debug.Log("[GameStateService] Firebase initialized successfully");
                        yield break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[GameStateService] Waiting for Firebase... ({retryCount + 1}/{maxRetries}): {e.Message}");
                }
                
                retryCount++;
                yield return new WaitForSeconds(0.2f);
            }
            
            Debug.LogWarning("[GameStateService] Failed to initialize Firebase after retries - will use fallback mode");
        }

        /// <summary>
        /// Carrega ou cria o estado atual do jogo
        /// Se não existir, cria com valores padrão (preset_1, level_1)
        /// </summary>
        public void GetOrCreateState(string companyId, string gameId, Action<GameState> onSuccess, Action<string> onError = null)
        {
            Debug.Log($"[GameStateService] GetOrCreateState called for company={companyId}, game={gameId}");
            
            if (db == null || auth == null)
            {
                string error = "[GameStateService] Firebase not initialized!";
                Debug.LogError(error);
                onError?.Invoke(error);
                return;
            }

            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                string error = "[GameStateService] User not authenticated! AuthGuard should prevent this.";
                Debug.LogError(error);
                onError?.Invoke(error);
                return;
            }
            
            Debug.Log($"[GameStateService] User authenticated: {user.UserId}");

            DocumentReference gameRef = db.Collection("users").Document(user.UserId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId);

            gameRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke($"Error loading state: {task.Exception?.Message}");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                
                GameState state;
                if (snapshot.Exists && snapshot.ContainsField("current_preset") && snapshot.ContainsField("current_level"))
                {
                    // Lê estado existente
                    state = new GameState(
                        snapshot.GetValue<string>("current_preset"),
                        snapshot.GetValue<string>("current_level")
                    );
                    Debug.Log($"[GameStateService] State loaded - Preset: {state.presetId}, Level: {state.levelId}");
                }
                else
                {
                    // Cria estado padrão
                    state = new GameState(); // preset_1, level_1
                    Debug.Log($"[GameStateService] Creating default state - Preset: {state.presetId}, Level: {state.levelId}");
                    
                    // Salva estado padrão
                    SaveState(companyId, gameId, state, 
                        () => Debug.Log("[GameStateService] Default state saved"),
                        error => Debug.LogWarning($"[GameStateService] Failed to save default state: {error}")
                    );
                }

                onSuccess?.Invoke(state);
            });
        }

        /// <summary>
        /// Salva o estado atual do jogo
        /// </summary>
        public void SaveState(string companyId, string gameId, GameState state, Action onSuccess = null, Action<string> onError = null)
        {
            if (db == null || auth == null)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                onError?.Invoke("User not authenticated");
                return;
            }

            DocumentReference gameRef = db.Collection("users").Document(user.UserId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId);

            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { "current_preset", state.presetId },
                { "current_level", state.levelId },
                { "updatedAt", DateTime.UtcNow.ToString("o") }
            };

            gameRef.UpdateAsync(data).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke($"Error saving state: {task.Exception?.Message}");
                    return;
                }

                Debug.Log($"[GameStateService] State saved - Preset: {state.presetId}, Level: {state.levelId}");
                onSuccess?.Invoke();
            });
        }

        /// <summary>
        /// Atualiza apenas o levelId atual
        /// </summary>
        public void UpdateLevel(string companyId, string gameId, string newLevelId, Action onSuccess = null, Action<string> onError = null)
        {
            GetOrCreateState(companyId, gameId, state =>
            {
                state.levelId = newLevelId;
                SaveState(companyId, gameId, state, onSuccess, onError);
            }, onError);
        }

        /// <summary>
        /// Atualiza o preset e level
        /// </summary>
        public void UpdatePresetAndLevel(string companyId, string gameId, string newPresetId, string newLevelId, Action onSuccess = null, Action<string> onError = null)
        {
            GameState state = new GameState(newPresetId, newLevelId);
            SaveState(companyId, gameId, state, onSuccess, onError);
        }
    }
}

