using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using BubblesGame.Data;
using Core.MediaPipe;
using Core.Tracking;

namespace BubblesGame
{
    /// <summary>
    /// Visualizador debug para raios de colisão e landmarks
    /// Mostra círculos de colisão em tempo real sobre a webcam
    /// </summary>
    public class CollisionDebugVisualizer : MonoBehaviour
    {
    [Header("References")]
    [SerializeField] private LandmarkFrameProcessor frameProcessor;
    [SerializeField] private PoseHitDetector hitDetector;
    [SerializeField] private BubblesGameManager gameManager;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private RectTransform webcamRect;

        [Header("Debug Settings")]
        [SerializeField] private bool showLandmarkCircles = true;
        [SerializeField] private bool showBallCircles = true;
        [SerializeField] private bool showCollisionInfo = true;

        [Header("Visual Settings")]
        [SerializeField] private Color hitLandmarkColor = Color.green;
        [SerializeField] private Color destroyLandmarkColor = Color.red;
        [SerializeField] private Color ballHitRadiusColor = Color.cyan;
        [SerializeField] private Color ballDestroyRadiusColor = Color.magenta;
        [SerializeField] private Material circleMaterial;

        // Pools de objetos para performance
        private Queue<GameObject> circlePool = new Queue<GameObject>();
        private List<GameObject> activeCircles = new List<GameObject>();
        private UserGameSettings currentSettings;

        // UI Info
        private GameObject infoPanel;
        private Text infoText;
        
        // Texture cache para evitar memory leaks
        private Sprite circleSprite;

        void Start()
        {
            InitializeDebugVisualizer();
            CreateInfoPanel();
            
            // Conecta ao evento do FrameProcessor
            if (frameProcessor != null)
            {
                frameProcessor.OnFrameProcessed += OnFrameProcessed;
            }
        }

        void InitializeDebugVisualizer()
        {
            // Busca referências se não configuradas
            if (frameProcessor == null)
            {
                frameProcessor = FindObjectOfType<LandmarkFrameProcessor>();
                if (frameProcessor == null)
                {
                    Debug.LogWarning("[CollisionDebugVisualizer] LandmarkFrameProcessor não encontrado. Visualização de colisão ficará desativada até ser configurada.");
                }
            }
            
            if (hitDetector == null)
                hitDetector = FindObjectOfType<PoseHitDetector>();
            
            if (gameManager == null)
                gameManager = GameServices.GameManager ?? FindObjectOfType<BubblesGameManager>();

            if (overlayCanvas == null)
            {
                // Cria canvas overlay se não existir
                GameObject canvasObj = new GameObject("CollisionDebugCanvas");
                overlayCanvas = canvasObj.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = 100; // Fica por cima de tudo
                canvasObj.AddComponent<CanvasScaler>();
               // canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Busca webcam rect
            if (webcamRect == null)
            {
                RawImage webcamImage = FindObjectOfType<RawImage>();
                if (webcamImage != null)
                    webcamRect = webcamImage.rectTransform;
            }

            // Cria pool inicial de círculos
            CreateCirclePool(20);
        }

        void CreateCirclePool(int poolSize)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject circle = CreateCircleObject();
                circle.SetActive(false);
                circlePool.Enqueue(circle);
            }
        }

        GameObject CreateCircleObject()
        {
            GameObject circleObj = new GameObject("DebugCircle");
            circleObj.transform.SetParent(overlayCanvas.transform);

            Image circleImage = circleObj.AddComponent<Image>();
            // Usa sprite cached para evitar criar múltiplas texturas
            if (circleSprite == null)
                circleSprite = CreateCircleSprite();
            circleImage.sprite = circleSprite;
            circleImage.raycastTarget = false;

            RectTransform rect = circleObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            return circleObj;
        }

        Sprite CreateCircleSprite()
        {
            // Cria sprite de círculo simples
            int resolution = 64;
            Texture2D texture = new Texture2D(resolution, resolution);
            Color[] pixels = new Color[resolution * resolution];

            Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f - 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius && distance >= radius - 2f)
                    {
                        pixels[y * resolution + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * resolution + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
        }

        void CreateInfoPanel()
        {
            if (!showCollisionInfo) return;

            GameObject panelObj = new GameObject("CollisionInfoPanel");
            panelObj.transform.SetParent(overlayCanvas.transform);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 150);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0, 0, 0, 0.7f);

            GameObject textObj = new GameObject("InfoText");
            textObj.transform.SetParent(panelObj.transform);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);

            infoText = textObj.AddComponent<Text>();
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 12;
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.UpperLeft;

            infoPanel = panelObj;
        }

        
        void OnDestroy()
        {
            if (frameProcessor != null)
            {
                frameProcessor.OnFrameProcessed -= OnFrameProcessed;
            }
            
            // Cleanup
            ClearActiveCircles();
            
            while (circlePool.Count > 0)
            {
                GameObject circle = circlePool.Dequeue();
                if (circle != null)
                    DestroyImmediate(circle);
            }
            
            // Cleanup da texture para evitar memory leak
            if (circleSprite != null && circleSprite.texture != null)
            {
                DestroyImmediate(circleSprite.texture);
                DestroyImmediate(circleSprite);
                circleSprite = null;
            }
        }

        /// <summary>
        /// Processa frame de landmarks do FrameProcessor
        /// </summary>
        private void OnFrameProcessed(LandmarkFrameData frameData)
        {
            if (!enabled) return;

            ClearActiveCircles();

            if (frameData.IsValid)
            {
                DrawLandmarkCircles(frameData);
            }

            if (gameManager != null)
            {
                DrawBallCircles();
            }

            UpdateInfoPanel(frameData);
        }

        void ClearActiveCircles()
        {
            foreach (GameObject circle in activeCircles)
            {
                if (circle != null) // Null check para evitar erro se já foi destruído
                {
                    circle.SetActive(false);
                    circlePool.Enqueue(circle);
                }
            }
            activeCircles.Clear();
        }

        void DrawLandmarkCircles(LandmarkFrameData frameData)
        {
            if (!showLandmarkCircles || currentSettings == null || webcamRect == null) return;

            // Desenha círculos para hit landmarks (pulsos)
            if (frameData.HitLandmarks != null)
            {
                foreach (var landmark in frameData.HitLandmarks)
                {
                    if (!landmark.IsVisible) continue;

                    // Converte posição TOP-LEFT normalized para Canvas usando conversor centralizado
                    Vector2 canvasPos = CoordinateConverter.NormalizedToCanvasPosition(
                        landmark.NormalizedPosition, webcamRect);
                    
                    float radius = currentSettings.GetTotalHitSensitivity() * webcamRect.rect.width;
                    DrawCircle(canvasPos, radius, hitLandmarkColor);
                }
            }

            // Desenha círculos para destroy landmarks (nariz)
            if (frameData.DestroyLandmarks != null)
            {
                foreach (var landmark in frameData.DestroyLandmarks)
                {
                    if (!landmark.IsVisible) continue;

                    // Converte posição TOP-LEFT normalized para Canvas usando conversor centralizado
                    Vector2 canvasPos = CoordinateConverter.NormalizedToCanvasPosition(
                        landmark.NormalizedPosition, webcamRect);
                    
                    float radius = currentSettings.GetTotalDestroySensitivity() * webcamRect.rect.width;
                    DrawCircle(canvasPos, radius, destroyLandmarkColor);
                }
            }
        }

        void DrawBallCircles()
        {
            if (!showBallCircles || currentSettings == null || webcamRect == null) return;

            var activeBalls = gameManager.GetActiveBalls();
            foreach (Ball ball in activeBalls)
            {
                if (ball == null) continue;

                // Pega posição TOP-LEFT normalized da bola (0-1)
                Vector2 ballNormalizedPos = ball.GetNormalizedPosition();

                // Converte para Canvas usando conversor centralizado - MESMA lógica dos landmarks!
                Vector2 ballCanvasPos = CoordinateConverter.NormalizedToCanvasPosition(
                    ballNormalizedPos, webcamRect);

                // Círculo da bola (sempre o mesmo tamanho baseado no tamanho da bola)
                float ballRadius = ball.Data.Size * 0.02f * webcamRect.rect.width; // Converte para pixels
                DrawCircle(ballCanvasPos, ballRadius, ballHitRadiusColor);
            }
        }

        void DrawCircle(Vector2 screenPos, float radius, Color color)
        {
            GameObject circle = GetCircleFromPool();
            if (circle == null) return;

            RectTransform rect = circle.GetComponent<RectTransform>();
            Image image = circle.GetComponent<Image>();

            // Posiciona o círculo
            rect.anchoredPosition = screenPos;
            rect.sizeDelta = new Vector2(radius * 2, radius * 2);

            // Define cor
            image.color = color;

            circle.SetActive(true);
            activeCircles.Add(circle);
        }

        GameObject GetCircleFromPool()
        {
            if (circlePool.Count > 0)
            {
                return circlePool.Dequeue();
            }
            else
            {
                // Pool vazio, cria novo
                return CreateCircleObject();
            }
        }

        // REMOVIDO - Não mais necessário!
        // Todos usam CoordinateConverter.NormalizedToCanvasPosition agora

        void UpdateInfoPanel(LandmarkFrameData frameData)
        {
            if (!showCollisionInfo || infoText == null || currentSettings == null) return;

            string info = $"=== COLLISION DEBUG ===\n";
            info += $"Timestamp: {frameData.TimestampMs:F1}ms\n";
            info += $"General Sensitivity: {currentSettings.GeneralSensitivity:F3}\n";
            info += $"Hit Sensitivity: +{currentSettings.HitLandmarksSensitivity:F3}\n";
            info += $"Destroy Sensitivity: +{currentSettings.DestroyLandmarksSensitivity:F3}\n";
            info += $"Total Hit: {currentSettings.GetTotalHitSensitivity():F3}\n";
            info += $"Total Destroy: {currentSettings.GetTotalDestroySensitivity():F3}\n";

            if (gameManager != null)
            {
                var activeBalls = gameManager.GetActiveBalls();
                info += $"Active Balls: {activeBalls.Count}\n";
            }

            if (frameData.IsValid)
            {
                int visibleHitLandmarks = frameData.HitLandmarks?.Count(l => l.IsVisible) ?? 0;
                int visibleDestroyLandmarks = frameData.DestroyLandmarks?.Count(l => l.IsVisible) ?? 0;
                int totalVisible = visibleHitLandmarks + visibleDestroyLandmarks;
                int totalTracked = (frameData.HitLandmarks?.Length ?? 0) + (frameData.DestroyLandmarks?.Length ?? 0);
                
                info += $"Visible Landmarks: {totalVisible}/{totalTracked}\n";
                info += $"Hit Landmarks: {visibleHitLandmarks}\n";
                info += $"Destroy Landmarks: {visibleDestroyLandmarks}";
            }
            else
            {
                info += "Frame: INVALID";
            }

            infoText.text = info;
        }

        // Métodos públicos para controle
        public void SetSettings(UserGameSettings settings)
        {
            currentSettings = settings;
        }

        public void ToggleLandmarkCircles(bool show)
        {
            showLandmarkCircles = show;
        }

        public void ToggleBallCircles(bool show)
        {
            showBallCircles = show;
        }

        public void ToggleInfoPanel(bool show)
        {
            showCollisionInfo = show;
            if (infoPanel != null)
                infoPanel.SetActive(show);
        }

        public void SetDebugEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
            {
                ClearActiveCircles();
                if (infoPanel != null)
                    infoPanel.SetActive(false);
            }
        }


        void OnDrawGizmos()
        {
            // Gizmos adicionais para Scene view
            if (!Application.isPlaying || currentSettings == null || Camera.main == null) return;

            // Desenha bounds da tela
            Gizmos.color = Color.white;
            Vector3 bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 10));
            Vector3 bottomRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 10));
            Vector3 topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 10));
            Vector3 topLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 1, 10));

            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}