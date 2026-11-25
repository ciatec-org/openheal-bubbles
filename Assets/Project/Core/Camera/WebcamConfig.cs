using UnityEngine;

namespace Core.CameraSystem
{
    [System.Serializable]
    public class WebcamConfig
    {
        public int deviceIndex = 0;          // Which webcam to use
        public int width = 640;             // Resolution
        public int height = 480;             // Height
        public int fps = 30;                 // Target FPS
    }
}
