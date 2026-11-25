using UnityEngine;

namespace BubblesGame
{
    /// <summary>
    /// Timer reutilizável com controle de pause/resume/stop/reset
    /// Pode ser usado para timer principal, cooldown timer, etc.
    /// </summary>
    public class GameTimer
    {
        private float startTime = 0f;
        private float pausedTimeAccumulator = 0f;
        private float pauseStartTime = 0f;
        private bool isRunning = false;
        private bool isPaused = false;

        /// <summary>
        /// Retorna se o timer está rodando
        /// </summary>
        public bool IsRunning => isRunning && !isPaused;

        /// <summary>
        /// Retorna se o timer está pausado
        /// </summary>
        public bool IsPaused => isPaused;

        /// <summary>
        /// Retorna se o timer está parado (não rodando e não pausado)
        /// </summary>
        public bool IsStopped => !isRunning && !isPaused;

        /// <summary>
        /// Inicia o timer (ou reinicia se já estava rodando)
        /// </summary>
        public void Start()
        {
            startTime = Time.time;
            pausedTimeAccumulator = 0f;
            pauseStartTime = 0f;
            isRunning = true;
            isPaused = false;
        }

        /// <summary>
        /// Pausa o timer (mantém o tempo atual)
        /// </summary>
        public void Pause()
        {
            if (!isRunning || isPaused) return;

            // Marca quando a pausa começou
            pauseStartTime = Time.time;
            isPaused = true;
        }

        /// <summary>
        /// Retoma o timer após pausa
        /// </summary>
        public void Resume()
        {
            if (!isRunning || !isPaused) return;

            // Acumula o tempo que esteve pausado
            if (pauseStartTime > 0f)
            {
                pausedTimeAccumulator += Time.time - pauseStartTime;
            }
            pauseStartTime = 0f;
            isPaused = false;
        }

        /// <summary>
        /// Para o timer completamente (reset)
        /// </summary>
        public void Stop()
        {
            isRunning = false;
            isPaused = false;
            startTime = 0f;
            pausedTimeAccumulator = 0f;
            pauseStartTime = 0f;
        }

        /// <summary>
        /// Reseta o timer para zero mas mantém rodando
        /// </summary>
        public void Reset()
        {
            startTime = Time.time;
            pausedTimeAccumulator = 0f;
            pauseStartTime = 0f;
        }

        /// <summary>
        /// Retorna o tempo decorrido em segundos (desconsidera pausas)
        /// </summary>
        public float GetElapsedTime()
        {
            if (!isRunning) return 0f;

            float totalTime = Time.time - startTime;
            
            if (isPaused)
            {
                // Se está pausado, calcula até o momento da pausa
                float timeUntilPause = pauseStartTime - startTime;
                return timeUntilPause - pausedTimeAccumulator;
            }
            else
            {
                // Se está rodando, calcula tempo atual menos pausas acumuladas
                return totalTime - pausedTimeAccumulator;
            }
        }

        /// <summary>
        /// Retorna o tempo decorrido em milissegundos
        /// </summary>
        public float GetElapsedTimeMs()
        {
            return GetElapsedTime() * 1000f;
        }
    }
}

