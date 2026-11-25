using UnityEngine;
using UnityEngine.UI;
using Core.CameraSystem;

public class TestWebcamDisplay : MonoBehaviour
{
    public WebcamCapture capture;
    public RawImage display;

    void Start()
    {
        capture.OnFrameReady += OnFrame;
    }

    void OnFrame(Texture2D tex)
    {
        display.texture = tex;
    }
}
