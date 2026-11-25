using UnityEngine;

namespace BubblesGame
{
    /// <summary>
    /// Service Locator para eliminar FindObjectOfType excessivos
    /// Centraliza referências dos principais serviços do jogo
    /// </summary>
    public static class GameServices
    {
        // Principais serviços do jogo
        public static ScoreManager ScoreManager { get; private set; }
        public static BubblesGameManager GameManager { get; private set; }
        public static LandmarkFrameProcessor FrameProcessor { get; private set; }
        public static BubblesGameUI GameUI { get; private set; }
        
        // Estado de inicialização
        public static bool IsInitialized { get; private set; }
        
        /// <summary>
        /// Registra o ScoreManager
        /// </summary>
        public static void Register(ScoreManager scoreManager)
        {
            ScoreManager = scoreManager;
            Debug.Log("[GameServices] ScoreManager registered");
            CheckInitialization();
        }
        
        /// <summary>
        /// Registra o BubblesGameManager
        /// </summary>
        public static void Register(BubblesGameManager gameManager)
        {
            GameManager = gameManager;
            Debug.Log("[GameServices] BubblesGameManager registered");
            CheckInitialization();
        }
        
        /// <summary>
        /// Registra o LandmarkFrameProcessor
        /// </summary>
        public static void Register(LandmarkFrameProcessor frameProcessor)
        {
            FrameProcessor = frameProcessor;
            Debug.Log("[GameServices] LandmarkFrameProcessor registered");
            CheckInitialization();
        }
        
        /// <summary>
        /// Registra o BubblesGameUI
        /// </summary>
        public static void Register(BubblesGameUI gameUI)
        {
            GameUI = gameUI;
            Debug.Log("[GameServices] BubblesGameUI registered");
            CheckInitialization();
        }
        
        /// <summary>
        /// Limpa todas as referências (chamado ao sair da cena)
        /// </summary>
        public static void Clear()
        {
            ScoreManager = null;
            GameManager = null;
            FrameProcessor = null;
            GameUI = null;
            IsInitialized = false;
            Debug.Log("[GameServices] All services cleared");
        }
        
        /// <summary>
        /// Verifica se os serviços essenciais estão registrados
        /// </summary>
        private static void CheckInitialization()
        {
            // Considera inicializado se tem pelo menos ScoreManager e GameManager
            bool wasInitialized = IsInitialized;
            IsInitialized = ScoreManager != null && GameManager != null;
            
            if (IsInitialized && !wasInitialized)
            {
                Debug.Log("[GameServices] Core services initialized!");
            }
        }
        
        /// <summary>
        /// Busca e registra serviços automaticamente (fallback)
        /// Usado apenas se os serviços não se registraram sozinhos
        /// </summary>
        public static void AutoRegisterMissingServices()
        {
            if (ScoreManager == null)
            {
                ScoreManager = Object.FindObjectOfType<ScoreManager>();
                if (ScoreManager != null)
                    Debug.LogWarning("[GameServices] ScoreManager auto-registered via FindObjectOfType");
            }
            
            if (GameManager == null)
            {
                GameManager = Object.FindObjectOfType<BubblesGameManager>();
                if (GameManager != null)
                    Debug.LogWarning("[GameServices] BubblesGameManager auto-registered via FindObjectOfType");
            }
            
            if (FrameProcessor == null)
            {
                FrameProcessor = Object.FindObjectOfType<LandmarkFrameProcessor>();
                if (FrameProcessor != null)
                    Debug.LogWarning("[GameServices] LandmarkFrameProcessor auto-registered via FindObjectOfType");
            }
            
            if (GameUI == null)
            {
                GameUI = Object.FindObjectOfType<BubblesGameUI>();
                if (GameUI != null)
                    Debug.LogWarning("[GameServices] BubblesGameUI auto-registered via FindObjectOfType");
            }
            
            CheckInitialization();
        }
    }
}