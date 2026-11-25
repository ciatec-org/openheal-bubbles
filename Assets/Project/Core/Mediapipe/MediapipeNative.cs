using System;
using System.Runtime.InteropServices;

namespace Core.MediaPipe
{
    public static class MediaPipeNative
    {
        // Inicializa o modelo nativo
        [DllImport("mediapipe_runtime", EntryPoint = "InitPose")]
        public static extern bool InitPose([MarshalAs(UnmanagedType.LPStr)] string modelPath);

        // Processa 1 frame RGB (sem espelho)
        [DllImport("mediapipe_runtime", EntryPoint = "ProcessImage")]
        public static extern IntPtr ProcessImage(
            byte[] rgbData,
            int width,
            int height
        );

        // Libera memória retornada pelo C++
        [DllImport("mediapipe_runtime", EntryPoint = "FreeResult")]
        public static extern void FreeResult(IntPtr ptr);
    }
}
