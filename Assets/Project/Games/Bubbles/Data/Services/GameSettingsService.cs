using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase.Auth;

namespace BubblesGame.Data
{
    public class GameSettingsService : MonoBehaviour
    {
        private FirebaseFirestore db;
        private FirebaseAuth auth;

        void Start()
        {
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
        }

        /// <summary>
        /// Carrega as configurações do jogo para o usuário atual
        /// </summary>
        public void LoadGameSettings(string companyId, string gameId, Action<GameSettings> onSuccess, Action<string> onError)
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

            DocumentReference settingsRef = db.Collection("users").Document(user.UserId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId)
                .Collection("game-settings").Document("settings");

            settingsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke($"Error loading settings: {task.Exception?.Message}");
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                
                GameSettings settings;
                if (snapshot.Exists)
                {
                    // Deserializa do Firestore
                    settings = new GameSettings
                    {
                        CurrentPreset = snapshot.GetValue<string>("currentPreset"),
                        CurrentLevel = snapshot.GetValue<int>("currentLevel"),
                        HighScore = snapshot.GetValue<int>("highScore")
                    };
                }
                else
                {
                    // Primeira vez, cria settings padrão
                    settings = new GameSettings();
                    SaveGameSettings(companyId, gameId, settings, null, null);
                }

                onSuccess?.Invoke(settings);
            });
        }

        /// <summary>
        /// Salva as configurações do jogo para o usuário atual
        /// </summary>
        public void SaveGameSettings(string companyId, string gameId, GameSettings settings, Action onSuccess, Action<string> onError)
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

            DocumentReference settingsRef = db.Collection("users").Document(user.UserId)
                .Collection("companies").Document(companyId)
                .Collection("games").Document(gameId)
                .Collection("game-settings").Document("settings");

            var data = new Dictionary<string, object>
            {
                { "currentPreset", settings.CurrentPreset },
                { "currentLevel", settings.CurrentLevel },
                { "highScore", settings.HighScore }
            };

            settingsRef.SetAsync(data).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    onError?.Invoke($"Error saving settings: {task.Exception?.Message}");
                    return;
                }

                onSuccess?.Invoke();
            });
        }

        /// <summary>
        /// Atualiza apenas o nível atual
        /// </summary>
        public void UpdateCurrentLevel(string companyId, string gameId, int newLevel, Action onSuccess, Action<string> onError)
        {
            LoadGameSettings(companyId, gameId, settings =>
            {
                settings.CurrentLevel = newLevel;
                SaveGameSettings(companyId, gameId, settings, onSuccess, onError);
            }, onError);
        }

        /// <summary>
        /// Atualiza o high score se o novo score for maior
        /// </summary>
        public void UpdateHighScore(string companyId, string gameId, int newScore, Action onSuccess, Action<string> onError)
        {
            LoadGameSettings(companyId, gameId, settings =>
            {
                if (newScore > settings.HighScore)
                {
                    settings.HighScore = newScore;
                    SaveGameSettings(companyId, gameId, settings, onSuccess, onError);
                }
                else
                {
                    onSuccess?.Invoke();
                }
            }, onError);
        }
    }
}

