using UnityEngine;
using System.Collections.Generic;

namespace BubblesGame.Data
{
    /// <summary>
    /// Interface para gravação de eventos do jogo
    /// Preparada para futuro armazenamento (Firebase, arquivo local, etc)
    /// </summary>
    public interface IGameEventRecorder
    {
        /// <summary>
        /// Grava frame de landmarks processados
        /// </summary>
        void RecordLandmarkFrame(LandmarkFrameData frameData);
        
        /// <summary>
        /// Grava evento de colisão
        /// </summary>
        void RecordCollisionEvent(CollisionEventData eventData);
        
        /// <summary>
        /// Grava evento geral do jogo
        /// </summary>
        void RecordGameEvent(GameEventData eventData);
        
        /// <summary>
        /// Finaliza sessão de gravação
        /// </summary>
        void EndRecordingSession(string sessionId);
    }
    
    /// <summary>
    /// Snapshot de um landmark no momento da colisão
    /// </summary>
    [System.Serializable]
    public struct LandmarkSnapshot
    {
        public int id;
        public float x;
        public float y;
    }
    
    /// <summary>
    /// Snapshot de uma bola no momento da colisão
    /// </summary>
    [System.Serializable]
    public struct BallSnapshot
    {
        public int id;
        public float x;
        public float y;
    }
    
    /// <summary>
    /// Dados de evento de colisão
    /// </summary>
    public struct CollisionEventData
    {
        /// <summary>
        /// Timestamp em milissegundos desde o início da captura
        /// </summary>
        public float TimestampMs;
        
        /// <summary>
        /// ID da bola do ball_list (índice na lista original)
        /// </summary>
        public int ball_id;
        
        /// <summary>
        /// Coordenada X da colisão em coordenadas normalizadas (0-1)
        /// </summary>
        public float hit_coord_x;
        
        /// <summary>
        /// Coordenada Y da colisão em coordenadas normalizadas (0-1)
        /// </summary>
        public float hit_coord_y;
        
        /// <summary>
        /// Tempo da colisão em segundos desde o início do jogo
        /// </summary>
        public float hit_time;
        
        /// <summary>
        /// ID do landmark que colidiu
        /// </summary>
        public int hit_landmark;
        
        /// <summary>
        /// Lista de todos os landmarks visíveis no momento da colisão (com id, x, y em coordenadas de tela)
        /// </summary>
        public List<LandmarkSnapshot> landmark_list;
        
        /// <summary>
        /// Lista de todas as bolas ativas no momento da colisão (com id, x, y em coordenadas normalizadas)
        /// </summary>
        public List<BallSnapshot> balls_on_screen;
        
        /// <summary>
        /// Tipo de colisão (mantido para compatibilidade)
        /// </summary>
        public CollisionType Type;
        
        // Campos legados (mantidos para compatibilidade)
        /// <summary>
        /// @deprecated Use ball_id ao invés
        /// </summary>
        public int BallId => ball_id;
        
        /// <summary>
        /// @deprecated Use hit_landmark ao invés
        /// </summary>
        public int LandmarkIndex => hit_landmark;
        
        /// <summary>
        /// @deprecated Use hit_coord_x e hit_coord_y ao invés
        /// </summary>
        public Vector2 Position => new Vector2(hit_coord_x, hit_coord_y);
    }
    
    /// <summary>
    /// Dados de evento geral do jogo
    /// </summary>
    public struct GameEventData
    {
        /// <summary>
        /// Timestamp em milissegundos desde o início da captura
        /// </summary>
        public float TimestampMs;
        
        /// <summary>
        /// Tipo do evento
        /// </summary>
        public GameEventType EventType;
        
        /// <summary>
        /// Dados específicos do evento (JSON, etc)
        /// </summary>
        public string EventData;
        
        /// <summary>
        /// Contexto adicional
        /// </summary>
        public string Context;
    }
    
    /// <summary>
    /// Tipos de colisão para eventos
    /// </summary>
    public enum CollisionType
    {
        Hit,
        Destroy,
        Miss
    }
    
    /// <summary>
    /// Tipos de eventos do jogo
    /// </summary>
    public enum GameEventType
    {
        GameStart,
        GameEnd,
        LevelStart,
        LevelEnd,
        BallSpawn,
        BallHit,
        BallMiss,
        BallDestroy,
        ScoreChange,
        SettingsChange,
        UserAction
    }
}