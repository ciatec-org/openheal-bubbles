using UnityEngine;
using Core.CameraSystem;
using Core.Tracking;
using Core.MediaPipe;

namespace Core.Tracking
{
    public class PoseWebSocketController : MonoBehaviour
{
    public WebcamCapture webcam;                // já existe no seu projeto
    public PoseWebSocketClient wsClient;        // o client que acabamos de criar
    public PoseController poseController;       // seu controlador de smoothing / flip

    int frameCounter = 0;

    void Start()
    {
        wsClient.OnPoseReceived += poseController.OnPoseReceived;
        webcam.OnFrameReady += OnFrame;
    }

    void OnFrame(Texture2D texture)
    {
        frameCounter++;

        // reduz consumo → envia 1 a cada 2 frames
        if (frameCounter % 2 != 0)
            return;

        wsClient.SendFrame(texture);
    }
    }
}
