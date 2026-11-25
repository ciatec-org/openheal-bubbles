using UnityEngine;

namespace BubblesGame.Data
{
    /// <summary>
    /// Implementação Null Object do IGameEventRecorder
    /// Não faz nada - usada quando gravação está desabilitada
    /// </summary>
    public class NullGameEventRecorder : IGameEventRecorder
    {
        public void RecordLandmarkFrame(LandmarkFrameData frameData)
        {
            // Não faz nada - implementação null object
        }

        public void RecordCollisionEvent(CollisionEventData eventData)
        {
            // Não faz nada - implementação null object
        }

        public void RecordGameEvent(GameEventData eventData)
        {
            // Não faz nada - implementação null object
        }

        public void EndRecordingSession(string sessionId)
        {
            // Não faz nada - implementação null object
        }
    }
    
    /// <summary>
    /// Implementação de exemplo para arquivo local (futuro)
    /// </summary>
    public class LocalFileGameEventRecorder : IGameEventRecorder
    {
        private string sessionId;
        private string filePath;
        private bool isRecording = false;
        
        public LocalFileGameEventRecorder(string sessionId)
        {
            this.sessionId = sessionId;
            this.filePath = Application.persistentDataPath + $"/game_session_{sessionId}.json";
            // TODO: Implementar gravação em arquivo
        }

        public void RecordLandmarkFrame(LandmarkFrameData frameData)
        {
            if (!isRecording) return;
            // TODO: Serializar e gravar frameData
            Debug.Log($"[LocalFileRecorder] Recording landmark frame at {frameData.TimestampMs}ms");
        }

        public void RecordCollisionEvent(CollisionEventData eventData)
        {
            if (!isRecording) return;
            // TODO: Serializar e gravar eventData
            Debug.Log($"[LocalFileRecorder] Recording collision event: {eventData.Type} at {eventData.TimestampMs}ms");
        }

        public void RecordGameEvent(GameEventData eventData)
        {
            if (!isRecording) return;
            // TODO: Serializar e gravar eventData
            Debug.Log($"[LocalFileRecorder] Recording game event: {eventData.EventType} at {eventData.TimestampMs}ms");
        }

        public void EndRecordingSession(string sessionId)
        {
            isRecording = false;
            // TODO: Finalizar arquivo e fazer upload se necessário
            Debug.Log($"[LocalFileRecorder] Session {sessionId} ended. File: {filePath}");
        }
    }
}