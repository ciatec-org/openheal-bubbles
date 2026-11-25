using UnityEngine;
using UnityEngine.UI;
using Core.MediaPipe;
using Core.Tracking;
using System.Collections.Generic;

namespace Core.UI
{
    public class PoseLandmarkUI : MonoBehaviour
    {
        [Header("Referencias")]
        public PoseController poseController;
        public RectTransform webcamRect; // O RawImage da webcam
        
        [Header("Configuracao")]
        public GameObject landmarkPrefab; // Prefab de um ponto (Image pequena)
        public Color landmarkColor = Color.green;
        public float landmarkSize = 8f;
        
        private List<GameObject> landmarkObjects = new List<GameObject>();
        private Canvas canvas;
        
        void Start()
        {
            canvas = GetComponentInParent<Canvas>();
            
            // Cria os objetos para cada landmark
            CreateLandmarkObjects();
        }
        
        void CreateLandmarkObjects()
        {
            int landmarkCount = MediaPipeBridge.LANDMARK_COUNT;
            
            for (int i = 0; i < landmarkCount; i++)
            {
                GameObject landmarkObj;
                
                if (landmarkPrefab != null)
                {
                    landmarkObj = Instantiate(landmarkPrefab, transform);
                }
                else
                {
                    // Cria um ponto simples se não há prefab
                    landmarkObj = new GameObject($"Landmark_{i}");
                    landmarkObj.transform.SetParent(transform);
                    
                    Image img = landmarkObj.AddComponent<Image>();
                    img.color = landmarkColor;
                    
                    RectTransform rect = landmarkObj.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.one * landmarkSize;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.zero;
                }
                
                landmarkObjects.Add(landmarkObj);
                landmarkObj.SetActive(false); // Inicia invisível
            }
        }
        
        void Update()
        {
            if (poseController == null)
            {
                Debug.LogWarning("[PoseLandmarkUI] PoseController é null!");
                return;
            }
            
            if (poseController.SmoothedResult == null)
            {
                Debug.LogWarning("[PoseLandmarkUI] SmoothedResult é null!");
                return;
            }
            
            if (webcamRect == null)
            {
                Debug.LogWarning("[PoseLandmarkUI] WebcamRect é null!");
                return;
            }
                
            UpdateLandmarkPositions();
        }
        
        Vector2 ConvertToWebcamRect(float xNorm, float yNorm, RectTransform rect)
        {
            float x = xNorm * rect.rect.width;
            float y = (1f - yNorm) * rect.rect.height;  // invert y

            return new Vector2(x, y);
        }

        
        void UpdateLandmarkPositions()
        {
            var landmarks = poseController.SmoothedResult.landmarks;
            
            if (landmarks == null || landmarks.Count == 0)
            {
                Debug.LogWarning("[PoseLandmarkUI] Nenhum landmark encontrado!");
                return;
            }
            
            int visibleCount = 0;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            
            for (int i = 0; i < landmarkObjects.Count; i++)
            {
                var landmarkObj = landmarkObjects[i];
                
                if (landmarks.TryGetValue(i, out var landmark) && landmark.likelihood > 0.5f)
                {
                    visibleCount++;
                    landmarkObj.SetActive(true);

                    RectTransform dot = landmarkObj.GetComponent<RectTransform>();
                    dot.anchoredPosition = ConvertToWebcamRect(landmark.x, landmark.y, webcamRect);

                    Image img = landmarkObj.GetComponent<Image>();
                    if (img != null)
                    {
                        float alpha = Mathf.Lerp(0.5f, 1f, landmark.likelihood);
                        Color color = landmarkColor;
                        color.a = alpha;
                        img.color = color;
                    }
                }
                else
                {
                    landmarkObj.SetActive(false);
                }
            }
            
            if (visibleCount == 0)
            {
                Debug.LogWarning($"[PoseLandmarkUI] Nenhum landmark visível! Total landmarks: {landmarks.Count}");
            }
        }
        
        // Método para alternar visibilidade
        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
        
        // Método para mudar cor
        public void SetLandmarkColor(Color newColor)
        {
            landmarkColor = newColor;
            foreach (var obj in landmarkObjects)
            {
                Image img = obj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = landmarkColor;
                }
            }
        }
    }
}
