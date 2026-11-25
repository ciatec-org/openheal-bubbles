using UnityEngine;
using Core.MediaPipe;
using Core.Tracking;
using Core.CameraSystem;

public class MediaPipeSetup : MonoBehaviour
{
    public WebcamCapture webcam;
    public PoseController poseController;

    private MediaPipeBridge bridge;

    void Start()
    {
        bridge = new MediaPipeBridge();

        // Quando MediaPipe terminar o processamento
        bridge.OnPoseUpdated += poseController.OnPoseReceived;

        // Quando a webcam tiver o frame pronto → envia pra MediaPipe
        webcam.OnFrameReady += (tex) =>
        {
            var rgb = Core.MediaPipe.FramePreprocessor.ToRGBBytes(tex);
            bridge.ProcessFrame(rgb, tex.width, tex.height);
        };
    }
}
