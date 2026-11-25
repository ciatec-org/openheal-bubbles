using System;

namespace BubblesGame
{
    /// <summary>
    /// Sistema centralizado de eventos para comunicação entre managers
    /// Elimina acoplamento direto e permite comunicação desacoplada
    /// </summary>
    public static class GameEvents
    {
        // Eventos de ciclo de vida do jogo
        public static event Action OnLevelReady;           // Level carregado e pronto
        public static event Action OnUserSettingsReady;    // Configurações do usuário carregadas
        public static event Action<float> OnGameStarted;    // Jogo iniciado (timestamp)
        public static event Action OnGamePaused;
        public static event Action OnGameResumed;
        public static event Action<GameEndReason> OnGameEnded;  // Jogo finalizado (razão)

        // Eventos de UI
        public static event Action OnLoadingComplete;     // Loading terminou
        public static event Action OnShowResults;          // Mostrar tela de resultados

        /// <summary>
        /// Notifica que o level está pronto para iniciar
        /// </summary>
        public static void NotifyLevelReady()
        {
            OnLevelReady?.Invoke();
        }

        /// <summary>
        /// Notifica que as configurações do usuário estão prontas
        /// </summary>
        public static void NotifyUserSettingsReady()
        {
            OnUserSettingsReady?.Invoke();
        }

        /// <summary>
        /// Notifica que o jogo iniciou
        /// </summary>
        public static void NotifyGameStarted(float startTime)
        {
            OnGameStarted?.Invoke(startTime);
        }

        /// <summary>
        /// Notifica que o jogo foi pausado
        /// </summary>
        public static void NotifyGamePaused()
        {
            OnGamePaused?.Invoke();
        }

        /// <summary>
        /// Notifica que o jogo foi retomado
        /// </summary>
        public static void NotifyGameResumed()
        {
            OnGameResumed?.Invoke();
        }

        /// <summary>
        /// Notifica que o jogo terminou
        /// </summary>
        public static void NotifyGameEnded(GameEndReason reason)
        {
            OnGameEnded?.Invoke(reason);
        }

        /// <summary>
        /// Notifica que o loading terminou
        /// </summary>
        public static void NotifyLoadingComplete()
        {
            OnLoadingComplete?.Invoke();
        }

        /// <summary>
        /// Notifica para mostrar tela de resultados
        /// </summary>
        public static void NotifyShowResults()
        {
            OnShowResults?.Invoke();
        }

        /// <summary>
        /// Limpa todos os eventos (útil para reset entre cenas)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnLevelReady = null;
            OnUserSettingsReady = null;
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameEnded = null;
            OnLoadingComplete = null;
            OnShowResults = null;
        }
    }
}

