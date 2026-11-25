using System;
using UnityEngine;
using Firebase.Firestore;

namespace BubblesGame.Data
{
    /// <summary>
    /// Configurações de sensibilidade personalizáveis pelo usuário
    /// Armazenadas no Firebase em users/{user_id}/companies/{company_id}/games/{game_id}/settings/user_settings
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class UserGameSettings
    {
        [Header("Sensitivity Settings")]
        [SerializeField] private float generalSensitivity = 0.10f;      // 10% da largura da tela (AUMENTADO para jogabilidade)
        [SerializeField] private float hitLandmarksSensitivity = 0.10f;  // +10% adicional para hit landmarks (AUMENTADO)
        [SerializeField] private float destroyLandmarksSensitivity = 0.05f; // +5% adicional para destroy landmarks (AUMENTADO)

        [Header("Visual Settings")]
        [SerializeField] private bool showCollisionRadius = true;        // Mostrar raios de colisão no debug
        [SerializeField] private bool enableHapticFeedback = true;       // Vibração no mobile (futuro)

        // Construtor padrão para desserialização
        public UserGameSettings() { }

        // Construtor com valores
        public UserGameSettings(float general, float hit, float destroy)
        {
            generalSensitivity = general;
            hitLandmarksSensitivity = hit;
            destroyLandmarksSensitivity = destroy;
            showCollisionRadius = true;
            enableHapticFeedback = true;
        }

        // Properties públicas
        [FirestoreProperty("generalSensitivity")]
        public float GeneralSensitivity 
        { 
            get => generalSensitivity; 
            set => generalSensitivity = Mathf.Clamp(value, 0.01f, 0.15f); 
        }

        [FirestoreProperty("hitLandmarksSensitivity")]
        public float HitLandmarksSensitivity 
        { 
            get => hitLandmarksSensitivity; 
            set => hitLandmarksSensitivity = Mathf.Clamp(value, 0f, 0.1f); 
        }

        [FirestoreProperty("destroyLandmarksSensitivity")]
        public float DestroyLandmarksSensitivity 
        { 
            get => destroyLandmarksSensitivity; 
            set => destroyLandmarksSensitivity = Mathf.Clamp(value, 0f, 0.1f); 
        }

        [FirestoreProperty("showCollisionRadius")]
        public bool ShowCollisionRadius 
        { 
            get => showCollisionRadius; 
            set => showCollisionRadius = value; 
        }

        [FirestoreProperty("enableHapticFeedback")]
        public bool EnableHapticFeedback 
        { 
            get => enableHapticFeedback; 
            set => enableHapticFeedback = value; 
        }

        /// <summary>
        /// Calcula sensibilidade total para hit landmarks
        /// </summary>
        public float GetTotalHitSensitivity()
        {
            return generalSensitivity + hitLandmarksSensitivity;
        }

        /// <summary>
        /// Calcula sensibilidade total para destroy landmarks
        /// </summary>
        public float GetTotalDestroySensitivity()
        {
            return generalSensitivity + destroyLandmarksSensitivity;
        }

        /// <summary>
        /// Valida se as configurações estão dentro dos limites aceitáveis
        /// </summary>
        public bool IsValid(out string error)
        {
            error = "";

            if (generalSensitivity < 0.01f || generalSensitivity > 0.15f)
            {
                error = "General sensitivity must be between 0.01 and 0.15";
                return false;
            }

            if (hitLandmarksSensitivity < 0f || hitLandmarksSensitivity > 0.1f)
            {
                error = "Hit landmarks sensitivity must be between 0 and 0.1";
                return false;
            }

            if (destroyLandmarksSensitivity < 0f || destroyLandmarksSensitivity > 0.1f)
            {
                error = "Destroy landmarks sensitivity must be between 0 and 0.1";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Cria uma cópia das configurações
        /// </summary>
        public UserGameSettings Clone()
        {
            return new UserGameSettings(generalSensitivity, hitLandmarksSensitivity, destroyLandmarksSensitivity)
            {
                showCollisionRadius = this.showCollisionRadius,
                enableHapticFeedback = this.enableHapticFeedback
            };
        }

        /// <summary>
        /// Retorna configurações padrão do sistema
        /// </summary>
        public static UserGameSettings CreateDefault()
        {
            return new UserGameSettings(0.10f, 0.10f, 0.05f); // AUMENTADO para jogabilidade real
        }

        public override string ToString()
        {
            return $"UserGameSettings(General: {generalSensitivity:F3}, Hit: {hitLandmarksSensitivity:F3}, Destroy: {destroyLandmarksSensitivity:F3})";
        }
    }

    /// <summary>
    /// Configurações padrão do jogo (carregadas do Firebase)
    /// Armazenadas em games/{game_id}/config/settings
    /// </summary>
    [Serializable]
    [FirestoreData]
    public class DefaultGameSettings
    {
        [SerializeField] private float defaultGeneralSensitivity = 0.05f;
        [SerializeField] private float defaultHitSensitivity = 0.02f;
        [SerializeField] private float defaultDestroySensitivity = 0.01f;
        [SerializeField] private string version = "1.0";

        // Construtor padrão para desserialização do Firebase
        public DefaultGameSettings() { }

        // Properties
        [FirestoreProperty("defaultGeneralSensitivity")]
        public float DefaultGeneralSensitivity 
        { 
            get => defaultGeneralSensitivity; 
            set => defaultGeneralSensitivity = value; 
        }

        [FirestoreProperty("defaultHitSensitivity")]
        public float DefaultHitSensitivity 
        { 
            get => defaultHitSensitivity; 
            set => defaultHitSensitivity = value; 
        }

        [FirestoreProperty("defaultDestroySensitivity")]
        public float DefaultDestroySensitivity 
        { 
            get => defaultDestroySensitivity; 
            set => defaultDestroySensitivity = value; 
        }

        [FirestoreProperty("version")]
        public string Version 
        { 
            get => version; 
            set => version = value; 
        }

        /// <summary>
        /// Converte para UserGameSettings
        /// </summary>
        public UserGameSettings ToUserSettings()
        {
            return new UserGameSettings(defaultGeneralSensitivity, defaultHitSensitivity, defaultDestroySensitivity);
        }

        /// <summary>
        /// Cria configurações padrão do sistema
        /// </summary>
        public static DefaultGameSettings CreateDefault()
        {
            return new DefaultGameSettings
            {
                defaultGeneralSensitivity = 0.10f,  // AUMENTADO
                defaultHitSensitivity = 0.10f,       // AUMENTADO
                defaultDestroySensitivity = 0.05f,   // AUMENTADO
                version = "1.0"
            };
        }
    }
}