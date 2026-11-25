using UnityEngine;
using System;

namespace Core.MediaPipe
{
    public class MediaPipeBridge
    {
        public const int LANDMARK_COUNT = 33;

        // Evento: quando a pipeline retornar landmarks
        public event Action<PoseResult> OnPoseUpdated;

        // Chamada pública usada pelo WebcamCapture
        public void ProcessFrame(byte[] rgbBytes, int width, int height)
        {
            // Placeholder: simula processamento
            var result = FakeProcess(rgbBytes, width, height);

            // Dispara evento
            OnPoseUpdated?.Invoke(result);
        }

        // Modo simulação até a DLL entrar em cena
        private PoseResult FakeProcess(byte[] bytes, int width, int height)
        {
            var pose = new PoseResult();

            // Preenche com valores "neutros"
            for (int i = 0; i < LANDMARK_COUNT; i++)
            {
                pose.landmarks[i] = new PoseLandmark
                {
                    x = 0.5f,
                    y = 0.5f,
                    z = 0,
                    likelihood = 0
                };
            }

            return pose;
        }
    }
}
