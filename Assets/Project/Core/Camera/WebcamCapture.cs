using UnityEngine;
using System;
using UnityEngine.UI;

namespace Core.CameraSystem
{
    public class WebcamCapture : MonoBehaviour
    {
        public WebcamConfig config = new WebcamConfig();
        public RawImage preview;

        private WebCamTexture webcam;
        private Texture2D outputTexture;

        public event Action<Texture2D> OnFrameReady;

        void Start()
        {
            StartCamera();
        }

        public void StartCamera()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("No webcam found.");
                return;
            }

            var device = WebCamTexture.devices[config.deviceIndex];

            webcam = new WebCamTexture(device.name, config.width, config.height, config.fps);
            webcam.Play();

            outputTexture = new Texture2D(config.width, config.height, TextureFormat.RGB24, false);

            Debug.Log($"Webcam started: {device.name} {config.width}x{config.height}@{config.fps}");
        }

        void Update()
        {
            if (webcam == null || !webcam.didUpdateThisFrame) return;

            // Copia frame para Texture2D
            outputTexture.SetPixels32(webcam.GetPixels32());
            outputTexture.Apply(false);

            if (preview != null)
                preview.texture = outputTexture;

            // Dispara evento para outros sistemas
            OnFrameReady?.Invoke(outputTexture);

            // Exemplo de uso:
            var rgbBytes = Core.MediaPipe.FramePreprocessor.ToRGBBytes(outputTexture);
            // -> rgbBytes está pronto para MediaPipe
        }

        public void StopCamera()
        {
            if (webcam != null && webcam.isPlaying)
            {
                webcam.Stop();
                Debug.Log("Webcam stopped");
            }
        }

        void OnDestroy()
        {
            StopCamera();
        }
    }
}
