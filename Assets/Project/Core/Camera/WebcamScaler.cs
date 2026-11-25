using UnityEngine;
using UnityEngine.UI;

namespace Core.CameraSystem
{
    [RequireComponent(typeof(RawImage))]
    public class WebcamScaler : MonoBehaviour
    {
        public WebcamCapture webcamCapture;        // arraste o WebcamCapture
        public RectTransform webcamRect;           // arraste o próprio RawImage

        void Start()
        {
            if (webcamCapture == null || webcamRect == null)
            {
                Debug.LogError("[WebcamScaler] Referências não configuradas.");
                return;
            }

            // Espera 0.1s para garantir que a webcam inicializou
            Invoke(nameof(ApplyScale), 0.1f);
        }

        void ApplyScale()
        {
            int camWidth = webcamCapture.config.width;
            int camHeight = webcamCapture.config.height;

            if (camWidth <= 0 || camHeight <= 0)
            {
                Debug.LogError("[WebcamScaler] Resolução inválida.");
                return;
            }

            float aspect = (float)camWidth / camHeight;

            // Pega o tamanho atual da UI (largura é a referência)
            float targetWidth = webcamRect.rect.width;
            float targetHeight = targetWidth / aspect;

            webcamRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

            Debug.Log($"[WebcamScaler] Adjusting webcam size to {camWidth}x{camHeight} (aspect = {aspect:F3})");
        }
    }
}
