using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using NativeWebSocket;
using Core.MediaPipe;   // PoseResult / PoseLandmark

namespace Core.MediaPipe
{
    [System.Serializable]
    public class PoseLandmarkWithIndex
    {
        public int index;
        public float x;
        public float y;
        public float z;
        public float likelihood;
    }

    [System.Serializable]
    public class PoseResultFromWebSocket
    {
        public PoseLandmarkWithIndex[] landmarks;
    }

    public class PoseWebSocketClient : MonoBehaviour
    {
        public string wsUrl = "ws://127.0.0.1:8765";
        public int sendWidth = 320;
        public int sendHeight = 240;
        
        private WebSocket ws;
        
        // Cache de texture para evitar alocações constantes
        private Texture2D cachedResizedTexture;
        private int lastWidth = -1;
        private int lastHeight = -1;

        public Action<PoseResult> OnPoseReceived;

    void Awake()
    {
        Debug.Log("[WebSocket] PoseWebSocketClient started !");
    }

    async void Start()
    {
        Debug.Log($"[WebSocket] Trying to connect to: {wsUrl}");
        ws = new WebSocket(wsUrl);

        ws.OnOpen += () =>
        {
            Debug.Log("[WebSocket] Connection established successfully!");
        };

        ws.OnError += (e) =>
        {
            Debug.LogError($"[WebSocket] Erro: {e}");
        };

        ws.OnClose += (e) =>
        {
            Debug.LogWarning($"[WebSocket] Connection closed. Code: {e}");
        };

        ws.OnMessage += (bytes) =>
        {
            try
            {
                string json = Encoding.UTF8.GetString(bytes);
                PoseResultFromWebSocket wsResult = JsonUtility.FromJson<PoseResultFromWebSocket>(json);
                PoseResult pose = ConvertToPoseResult(wsResult);
                OnPoseReceived?.Invoke(pose);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebSocket] Erro ao desserializar pose: {ex.Message}");
            }
        };

        try
        {
            await ws.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WebSocket] Connection failed: {ex.Message}");
        }
    }

    private PoseResult ConvertToPoseResult(PoseResultFromWebSocket wsResult)
    {
        PoseResult result = new PoseResult();

        if (wsResult?.landmarks == null)
            return result;

        foreach (var wsLandmark in wsResult.landmarks)
        {
            result.landmarks[wsLandmark.index] = new PoseLandmark
            {
                x = wsLandmark.x,
                y = wsLandmark.y,
                z = wsLandmark.z,
                likelihood = wsLandmark.likelihood
            };
        }

        return result;
    }

    private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        RenderTexture.active = rt;
        
        Graphics.Blit(source, rt);
        
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return result;
    }

    public void SendFrame(Texture2D tex)
    {
        if (ws == null || ws.State != WebSocketState.Open) 
        {
            Debug.LogWarning("[WebSocket] Attempt to send frame with no established connection");
            return;
        }

        // Usa cache de texture reutilizável
        Texture2D resized = GetOrCreateResizedTexture(tex, sendWidth, sendHeight);
        byte[] jpg = resized.EncodeToJPG(30);
        string b64 = Convert.ToBase64String(jpg);
        ws.SendText(b64);
        
        // Não destrói - reutiliza o cache
    }
    
    /// <summary>
    /// Obtém texture redimensionada do cache ou cria nova se necessário
    /// </summary>
    private Texture2D GetOrCreateResizedTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        // Verifica se precisa recriar o cache
        if (cachedResizedTexture == null || 
            lastWidth != targetWidth || 
            lastHeight != targetHeight)
        {
            // Limpa texture anterior se existir
            if (cachedResizedTexture != null)
            {
                Destroy(cachedResizedTexture);
            }
            
            // Cria nova texture cached
            cachedResizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            lastWidth = targetWidth;
            lastHeight = targetHeight;
        }
        
        // Redimensiona usando RenderTexture (mais eficiente)
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        RenderTexture.active = rt;
        
        Graphics.Blit(source, rt);
        
        cachedResizedTexture.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        cachedResizedTexture.Apply();
        
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        
        return cachedResizedTexture;
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
            ws.DispatchMessageQueue();
#endif
    }

    void OnApplicationQuit()
    {
        CleanupResources();
    }
    
    void OnDestroy()
    {
        CleanupResources();
    }
    
    private void CleanupResources()
    {
        if (ws != null)
            ws.Close();
            
        if (cachedResizedTexture != null)
        {
            Destroy(cachedResizedTexture);
            cachedResizedTexture = null;
        }
    }
    }
}
