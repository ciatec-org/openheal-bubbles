using System;
using System.Collections.Generic;
using UnityEngine;
using BubblesGame;

namespace BubblesGame.Data.Services
{
    /// <summary>
    /// Grava eventos de bola (hit/miss) com contexto completo para análise
    /// </summary>
    public class BallEventRecorder : MonoBehaviour
    {
        private const int BufferSize = 50;
        private List<Dictionary<string, object>> eventBuffer = new();
        private bool isCapturing = false;

        // Identificadores da partida (preenchidos automaticamente via StartEventCapture)
        [HideInInspector] public string userId;
        [HideInInspector] public string companyId;
        [HideInInspector] public string gameId;
        [HideInInspector] public string matchId;

        // Callback para enviar os eventos
        public Action<List<Dictionary<string, object>>> OnEventsReadyToUpload;

        public void StartEventCapture(string userId, string companyId, string gameId, string matchId)
        {
            this.userId = userId;
            this.companyId = companyId;
            this.gameId = gameId;
            this.matchId = matchId;
            isCapturing = true;
            eventBuffer.Clear();
        }

        /// <summary>
        /// Registra um evento de bola (hit ou miss)
        /// </summary>
        public void RecordBallEvent(
            string eventType,
            object ballObj,
            int? landmarkUsed = null,
            Vector3? landmarkPosition = null,
            float? distanceToCenter = null,
            int currentScore = 0,
            int currentCombo = 0,
            object activeBallsObj = null,
            System.Collections.Generic.Dictionary<int, object> landmarks = null)
        {
            if (!isCapturing) return;

            var evt = new Dictionary<string, object>();
            
            // Timestamp unificado
            var gameController = GameObject.FindObjectOfType(System.Type.GetType("BubblesGame.GameController, Assembly-CSharp"));
            if (gameController != null)
            {
                var method = gameController.GetType().GetMethod("GetGameTimestamp");
                evt["t"] = method != null ? (float)method.Invoke(gameController, null) : 0f;
            }
            else
            {
                evt["t"] = 0f;
            }
            
            // Tipo de evento
            evt["eventType"] = eventType;
            
            // Dados da bola usando reflexão
            var ballData = new Dictionary<string, object>();
            if (ballObj != null)
            {
                var ballType = ballObj.GetType();
                var dataProperty = ballType.GetProperty("Data");
                var currentLifetimeProperty = ballType.GetProperty("CurrentLifetime");
                var transformProperty = ballType.GetProperty("transform");
                
                if (dataProperty != null && currentLifetimeProperty != null)
                {
                    var data = dataProperty.GetValue(ballObj);
                    if (data != null)
                    {
                        var dataType = data.GetType();
                        ballData["id"] = ((MonoBehaviour)ballObj).GetInstanceID().ToString();
                        ballData["launchTime"] = dataType.GetProperty("LaunchTime")?.GetValue(data) ?? 0f;
                        ballData["destroyTime"] = dataType.GetProperty("DestroyTime")?.GetValue(data) ?? 0f;
                        ballData["currentLifetime"] = currentLifetimeProperty.GetValue(ballObj);
                        ballData["size"] = dataType.GetProperty("Size")?.GetValue(data) ?? 0f;
                        ballData["speed"] = dataType.GetProperty("Speed")?.GetValue(data) ?? 0f;
                        
                        var transform = transformProperty?.GetValue(ballObj) as Transform;
                        if (transform != null)
                        {
                            var pos = transform.position;
                            ballData["position"] = new Dictionary<string, object> {
                                { "x", pos.x },
                                { "y", pos.y }
                            };
                        }
                        
                        // Tipo da bola
                        string ballTypeStr = "normal";
                        bool isHitSuper = (bool)(dataType.GetProperty("IsHitSuper")?.GetValue(data) ?? false);
                        bool isAvoidSuper = (bool)(dataType.GetProperty("IsAvoidSuper")?.GetValue(data) ?? false);
                        if (isHitSuper) ballTypeStr = "hit_super";
                        else if (isAvoidSuper) ballTypeStr = "avoid_super";
                        ballData["type"] = ballTypeStr;
                    }
                }
            }
            evt["ball"] = ballData;
            
            // Interação (apenas para hits)
            if (eventType == "ball_hit" && landmarkUsed.HasValue)
            {
                var interaction = new Dictionary<string, object>();
                interaction["landmarkUsed"] = landmarkUsed.Value;
                
                if (landmarkPosition.HasValue)
                {
                    interaction["landmarkPosition"] = new Dictionary<string, object> {
                        { "x", landmarkPosition.Value.x },
                        { "y", landmarkPosition.Value.y },
                        { "z", landmarkPosition.Value.z }
                    };
                }
                
                if (distanceToCenter.HasValue)
                    interaction["distanceToCenter"] = distanceToCenter.Value;
                
                if (ballObj != null)
                {
                    var lifetimeProp = ballObj.GetType().GetProperty("CurrentLifetime");
                    if (lifetimeProp != null)
                    {
                        float lifetime = (float)lifetimeProp.GetValue(ballObj);
                        float reactionTime = lifetime * 1000f; // ms
                        interaction["reactionTime"] = reactionTime;
                    }
                }
                
                evt["interaction"] = interaction;
            }
            
            // Estado do jogo
            var gameState = new Dictionary<string, object>();
            gameState["score"] = currentScore;
            gameState["combo"] = currentCombo;
            
            if (activeBallsObj != null && activeBallsObj is System.Collections.IList)
            {
                var activeBalls = activeBallsObj as System.Collections.IList;
                gameState["activeBallsCount"] = activeBalls.Count;
                var ballsList = new List<Dictionary<string, object>>();
                foreach (var b in activeBalls)
                {
                    if (b != null && b != ballObj)
                    {
                        var bType = b.GetType();
                        var transformProp = bType.GetProperty("transform");
                        var lifetimeProp = bType.GetProperty("CurrentLifetime");
                        
                        if (transformProp != null && lifetimeProp != null)
                        {
                            var transform = transformProp.GetValue(b) as Transform;
                            if (transform != null)
                            {
                                var bPos = transform.position;
                                ballsList.Add(new Dictionary<string, object> {
                                    { "id", ((MonoBehaviour)b).GetInstanceID().ToString() },
                                    { "position", new Dictionary<string, object> {
                                        { "x", bPos.x },
                                        { "y", bPos.y }
                                    }},
                                    { "lifetime", lifetimeProp.GetValue(b) }
                                });
                            }
                        }
                    }
                }
                gameState["activeBalls"] = ballsList;
            }
            else
            {
                gameState["activeBallsCount"] = 0;
            }
            evt["gameState"] = gameState;
            
            // Snapshot das landmarks
            if (landmarks != null && landmarks.Count > 0)
            {
                var playerPose = new Dictionary<string, object>();
                playerPose["landmarks"] = landmarks;
                evt["playerPose"] = playerPose;
            }
            
            eventBuffer.Add(evt);
            
            if (eventBuffer.Count >= BufferSize)
            {
                FlushEvents();
            }
        }

        public void FlushEvents()
        {
            if (eventBuffer.Count == 0) return;
            OnEventsReadyToUpload?.Invoke(new List<Dictionary<string, object>>(eventBuffer));
            eventBuffer.Clear();
        }

        public void StopEventCapture()
        {
            isCapturing = false;
            FlushEvents(); // garante enviar eventos restantes
        }

        public bool IsCapturing => isCapturing;
    }
}
