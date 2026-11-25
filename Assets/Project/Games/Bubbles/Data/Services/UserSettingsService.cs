using System;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

namespace BubblesGame.Data
{
    /// <summary>
    /// Serviço para gerenciar configurações de usuário no Firebase
    /// Paths: users/{user_id}/companies/{company_id}/games/{game_id}/settings/user_settings
    ///        games/{game_id}/config/settings (defaults)
    /// </summary>
    public class UserSettingsService : MonoBehaviour
    {
        private static UserSettingsService instance;
        public static UserSettingsService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UserSettingsService");
                    instance = go.AddComponent<UserSettingsService>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private FirebaseFirestore db;
        private bool isInitialized = false;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeFirestore();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        void InitializeFirestore()
        {
            try
            {
                db = FirebaseFirestore.DefaultInstance;
                isInitialized = true;
                Debug.Log("[UserSettingsService] Firebase Firestore initialized successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UserSettingsService] Failed to initialize Firestore: {e.Message}");
                isInitialized = false;
            }
        }

        /// <summary>
        /// Carrega ou cria configurações do usuário
        /// Se não existir, busca defaults e copia para o usuário
        /// </summary>
        public void GetOrCreateUserSettings(
            string userId, 
            string companyId, 
            string gameId,
            Action<UserGameSettings> onSuccess,
            Action<string> onError)
        {
            Debug.Log($"[UserSettingsService] GetOrCreateUserSettings called for user={userId}, company={companyId}, game={gameId}");
            
            if (!isInitialized)
            {
                string error = "[UserSettingsService] Firebase not initialized!";
                Debug.LogError(error);
                onError?.Invoke(error);
                return;
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(gameId))
            {
                Debug.LogError("[UserSettingsService] Invalid parameters provided");
                onError?.Invoke("Invalid parameters: userId, companyId, and gameId are required");
                return;
            }

            // Caminho correto com número ímpar de segmentos
            DocumentReference userSettingsRef = db.Collection("users").Document(userId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId)
                .Collection("settings").Document("user_settings");

            // Primeiro tenta carregar do usuário
            userSettingsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[UserSettingsService] Error getting user settings: {task.Exception}");
                    onError?.Invoke($"Failed to get user settings: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                
                if (snapshot.Exists)
                {
                    // Usuário já tem configurações
                    try
                    {
                        UserGameSettings settings = snapshot.ConvertTo<UserGameSettings>();
                        Debug.Log($"[UserSettingsService] User settings loaded: {settings}");
                        onSuccess?.Invoke(settings);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UserSettingsService] Error converting user settings: {e.Message}");
                        onError?.Invoke($"Failed to parse user settings: {e.Message}");
                    }
                }
                else
                {
                    // Usuário não tem configurações, busca defaults
                    Debug.Log("[UserSettingsService] User settings not found, loading defaults...");
                    LoadDefaultsAndCreateUserSettings(gameId, userSettingsRef, onSuccess, onError);
                }
            });
        }

        /// <summary>
        /// Carrega configurações padrão e cria para o usuário
        /// </summary>
        private void LoadDefaultsAndCreateUserSettings(
            string gameId,
            DocumentReference userSettingsRef,
            Action<UserGameSettings> onSuccess,
            Action<string> onError)
        {
            DocumentReference defaultsRef = db.Collection("games").Document(gameId).Collection("config").Document("settings");

            defaultsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                UserGameSettings settingsToCreate;

                if (task.IsFaulted || !task.Result.Exists)
                {
                    // Não tem defaults no Firebase, cria automaticamente
                    Debug.Log($"[UserSettingsService] Default settings not found for game {gameId}, creating defaults in Firebase...");
                    DefaultGameSettings defaultSettings = DefaultGameSettings.CreateDefault();
                    settingsToCreate = defaultSettings.ToUserSettings();
                    
                    // Cria os defaults no Firebase para próximas vezes
                    Dictionary<string, object> defaultsData = new Dictionary<string, object>
                    {
                        ["defaultGeneralSensitivity"] = defaultSettings.DefaultGeneralSensitivity,
                        ["defaultHitSensitivity"] = defaultSettings.DefaultHitSensitivity,
                        ["defaultDestroySensitivity"] = defaultSettings.DefaultDestroySensitivity,
                        ["version"] = defaultSettings.Version,
                        ["createdAt"] = FieldValue.ServerTimestamp,
                        ["updatedAt"] = FieldValue.ServerTimestamp
                    };
                    
                    // FORÇA a criação dos defaults - usa SetAsync em vez de UpdateAsync
                    defaultsRef.SetAsync(defaultsData).ContinueWithOnMainThread(createTask =>
                    {
                        if (createTask.IsFaulted)
                        {
                            Debug.LogError($"[UserSettingsService] FAILED to create default settings in Firebase: {createTask.Exception?.Message}");
                        }
                        else
                        {
                            Debug.Log($"[UserSettingsService] ✓ Default settings SUCCESSFULLY created in Firebase for game {gameId}");
                        }
                    });
                }
                else
                {
                    // Carrega defaults do Firebase
                    try
                    {
                        DefaultGameSettings defaults = task.Result.ConvertTo<DefaultGameSettings>();
                        settingsToCreate = defaults.ToUserSettings();
                        Debug.Log($"[UserSettingsService] Default settings loaded from Firebase: {settingsToCreate}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UserSettingsService] Error parsing default settings: {e.Message}");
                        settingsToCreate = UserGameSettings.CreateDefault();
                    }
                }

                // Cria configurações para o usuário
                CreateUserSettings(userSettingsRef, settingsToCreate, onSuccess, onError);
            });
        }

        /// <summary>
        /// Cria configurações iniciais para o usuário
        /// </summary>
        private void CreateUserSettings(
            DocumentReference userSettingsRef,
            UserGameSettings settings,
            Action<UserGameSettings> onSuccess,
            Action<string> onError)
        {
            Dictionary<string, object> settingsData = new Dictionary<string, object>
            {
                ["generalSensitivity"] = settings.GeneralSensitivity,
                ["hitLandmarksSensitivity"] = settings.HitLandmarksSensitivity,
                ["destroyLandmarksSensitivity"] = settings.DestroyLandmarksSensitivity,
                ["showCollisionRadius"] = settings.ShowCollisionRadius,
                ["enableHapticFeedback"] = settings.EnableHapticFeedback,
                ["createdAt"] = FieldValue.ServerTimestamp,
                ["updatedAt"] = FieldValue.ServerTimestamp
            };

            // FORÇA a criação das configurações do usuário
            userSettingsRef.SetAsync(settingsData).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[UserSettingsService] FAILED to create user settings: {task.Exception}");
                    onError?.Invoke($"Failed to create user settings: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                Debug.Log($"[UserSettingsService] ✓ User settings SUCCESSFULLY created: {settings}");
                onSuccess?.Invoke(settings);
            });
        }

        /// <summary>
        /// Salva configurações do usuário
        /// </summary>
        public void SaveUserSettings(
            string userId,
            string companyId,
            string gameId,
            UserGameSettings settings,
            Action onSuccess,
            Action<string> onError)
        {
            if (!isInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(gameId))
            {
                onError?.Invoke("Invalid parameters: userId, companyId, and gameId are required");
                return;
            }

            if (settings == null)
            {
                onError?.Invoke("Settings cannot be null");
                return;
            }

            // Valida configurações antes de salvar
            if (!settings.IsValid(out string validationError))
            {
                onError?.Invoke($"Invalid settings: {validationError}");
                return;
            }

            // Caminho correto com número ímpar de segmentos
            DocumentReference userSettingsRef = db.Collection("users").Document(userId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId)
                .Collection("settings").Document("user_settings");

            Dictionary<string, object> settingsData = new Dictionary<string, object>
            {
                ["generalSensitivity"] = settings.GeneralSensitivity,
                ["hitLandmarksSensitivity"] = settings.HitLandmarksSensitivity,
                ["destroyLandmarksSensitivity"] = settings.DestroyLandmarksSensitivity,
                ["showCollisionRadius"] = settings.ShowCollisionRadius,
                ["enableHapticFeedback"] = settings.EnableHapticFeedback,
                ["updatedAt"] = FieldValue.ServerTimestamp
            };

            // Usa SetAsync para criar o documento se não existir, ou UpdateAsync se existir
            userSettingsRef.SetAsync(settingsData, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[UserSettingsService] Error saving user settings: {task.Exception}");
                    onError?.Invoke($"Failed to save user settings: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                Debug.Log($"[UserSettingsService] ✓ User settings saved successfully: {settings}");
                onSuccess?.Invoke();
            });
        }

        /// <summary>
        /// Carrega apenas as configurações padrão do jogo (para admin/debug)
        /// </summary>
        public void GetDefaultSettings(
            string gameId,
            Action<DefaultGameSettings> onSuccess,
            Action<string> onError)
        {
            if (!isInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            DocumentReference defaultsRef = db.Collection("games").Document(gameId).Collection("config").Document("settings");

            defaultsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[UserSettingsService] Error getting default settings: {task.Exception}");
                    onError?.Invoke($"Failed to get default settings: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    Debug.LogWarning($"[UserSettingsService] Default settings not found for game {gameId}");
                    onSuccess?.Invoke(DefaultGameSettings.CreateDefault());
                    return;
                }

                try
                {
                    DefaultGameSettings defaults = snapshot.ConvertTo<DefaultGameSettings>();
                    Debug.Log($"[UserSettingsService] Default settings loaded: {defaults.DefaultGeneralSensitivity}");
                    onSuccess?.Invoke(defaults);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UserSettingsService] Error parsing default settings: {e.Message}");
                    onError?.Invoke($"Failed to parse default settings: {e.Message}");
                }
            });
        }

        /// <summary>
        /// Salva configurações padrão do jogo (apenas para admin)
        /// </summary>
        public void SaveDefaultSettings(
            string gameId,
            DefaultGameSettings settings,
            Action onSuccess,
            Action<string> onError)
        {
            if (!isInitialized)
            {
                onError?.Invoke("Firebase not initialized");
                return;
            }

            DocumentReference defaultsRef = db.Collection("games").Document(gameId).Collection("config").Document("settings");

            Dictionary<string, object> settingsData = new Dictionary<string, object>
            {
                ["defaultGeneralSensitivity"] = settings.DefaultGeneralSensitivity,
                ["defaultHitSensitivity"] = settings.DefaultHitSensitivity,
                ["defaultDestroySensitivity"] = settings.DefaultDestroySensitivity,
                ["version"] = settings.Version,
                ["updatedAt"] = FieldValue.ServerTimestamp
            };

            defaultsRef.SetAsync(settingsData).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[UserSettingsService] Error saving default settings: {task.Exception}");
                    onError?.Invoke($"Failed to save default settings: {task.Exception?.GetBaseException().Message}");
                    return;
                }

                Debug.Log($"[UserSettingsService] Default settings saved successfully for game {gameId}");
                onSuccess?.Invoke();
            });
        }
    }
}