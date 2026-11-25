using System.Collections.Generic;

namespace Core.MediaPipe
{
    [System.Serializable]
    public class PoseResult
    {
        public Dictionary<int, PoseLandmark> landmarks;
        
        /// <summary>
        /// Timestamp em milissegundos desde o início da captura
        /// </summary>
        public float timestampMs;

        public bool IsValid => landmarks != null && landmarks.Count > 0;

        public PoseResult()
        {
            landmarks = new Dictionary<int, PoseLandmark>();
            timestampMs = 0f;
        }
    }
}
