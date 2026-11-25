using UnityEngine;

namespace BubblesGame.Data
{
    /// <summary>
    /// Dados de landmarks processados para um único frame
    /// Evita processamento redundante - uma única conversão por frame
    /// </summary>
    public class LandmarkFrameData
    {
        /// <summary>
        /// Timestamp em milissegundos desde o início da captura
        /// </summary>
        public float TimestampMs { get; set; }
        
        /// <summary>
        /// Landmarks para hit (pulsos - índices 15, 16)
        /// </summary>
        public ProcessedLandmark[] HitLandmarks { get; set; }
        
        /// <summary>
        /// Landmarks para destroy (nariz - índice 0)
        /// </summary>
        public ProcessedLandmark[] DestroyLandmarks { get; set; }
        
        /// <summary>
        /// Se o frame contém dados válidos
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Retorna landmark por índice, ou null se não existir
        /// </summary>
        public ProcessedLandmark? GetLandmarkByIndex(int index)
        {
            if (HitLandmarks != null)
            {
                foreach (var landmark in HitLandmarks)
                {
                    if (landmark.Index == index && landmark.IsVisible)
                        return landmark;
                }
            }
            
            if (DestroyLandmarks != null)
            {
                foreach (var landmark in DestroyLandmarks)
                {
                    if (landmark.Index == index && landmark.IsVisible)
                        return landmark;
                }
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Landmark individual processado
    /// Contém todas as conversões pré-calculadas
    /// </summary>
    public struct ProcessedLandmark
    {
        /// <summary>
        /// Índice do landmark no MediaPipe (0-32)
        /// </summary>
        public int Index;
        
        /// <summary>
        /// Posição normalizada (0-1), com Y já invertido para Unity
        /// </summary>
        public Vector2 NormalizedPosition;
        
        /// <summary>
        /// Posição em coordenadas de tela (pixels)
        /// </summary>
        public Vector2 ScreenPosition;
        
        /// <summary>
        /// Confiança do MediaPipe (0-1)
        /// </summary>
        public float Likelihood;
        
        /// <summary>
        /// Se o landmark está visível (likelihood >= threshold)
        /// </summary>
        public bool IsVisible;
    }
}