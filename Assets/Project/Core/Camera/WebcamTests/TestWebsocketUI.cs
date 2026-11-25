using UnityEngine;
using UnityEngine.UI;
using Core.UI;
using Core.Tracking;

public class TestWebsocketUI : MonoBehaviour
{
    [Header("Referencias da Cena")]
    public PoseController poseController;
    public RawImage webcamRawImage; // O RawImage que mostra a webcam
    public Canvas canvas;
    
    [Header("Configuracao dos Landmarks")]
    public bool showLandmarks = true;
    public Color landmarkColor = Color.green;
    public float landmarkSize = 8f;
    
    private PoseLandmarkUI landmarkUI;
    
    void Start()
    {
        SetupLandmarkUI();
    }
    
    void SetupLandmarkUI()
    {
        // Cria um GameObject para os landmarks
        GameObject landmarkContainer = new GameObject("LandmarkContainer");
        
        // Adiciona RectTransform primeiro
        RectTransform containerRect = landmarkContainer.AddComponent<RectTransform>();
        
        // Define o parent depois de ter o RectTransform
        containerRect.SetParent(canvas.transform, false);
        
        // Adiciona o componente PoseLandmarkUI
        landmarkUI = landmarkContainer.AddComponent<PoseLandmarkUI>();
        
        // Configura as referencias
        landmarkUI.poseController = poseController;
        landmarkUI.webcamRect = webcamRawImage.rectTransform;
        landmarkUI.landmarkColor = landmarkColor;
        landmarkUI.landmarkSize = landmarkSize;
        
        // Posiciona o container sobre a webcam
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        
        // Garante que está na frente
        containerRect.SetAsLastSibling();
        
        landmarkContainer.SetActive(showLandmarks);
        
        Debug.Log("[TestWebsocketUI] Landmark UI configurado!");
    }
    
    // Removido Update() - landmarks sempre ligados
    
    public void ToggleLandmarks()
    {
        if (landmarkUI != null)
        {
            landmarkUI.ToggleVisibility();
            showLandmarks = landmarkUI.gameObject.activeSelf;
            Debug.Log($"[TestWebsocketUI] Landmarks: {(showLandmarks ? "Visíveis" : "Ocultos")}");
        }
    }
    
    public void CycleLandmarkColor()
    {
        Color[] colors = { Color.green, Color.red, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        int currentIndex = System.Array.IndexOf(colors, landmarkColor);
        int nextIndex = (currentIndex + 1) % colors.Length;
        
        landmarkColor = colors[nextIndex];
        
        if (landmarkUI != null)
        {
            landmarkUI.SetLandmarkColor(landmarkColor);
        }
        
        Debug.Log($"[TestWebsocketUI] Cor dos landmarks alterada para: {landmarkColor}");
    }
}
