using UnityEngine;

namespace Core.MediaPipe
{
    public static class FramePreprocessor
    {
        // Converte Texture2D → byte[] RGB (row-major)
        public static byte[] ToRGBBytes(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            int length = pixels.Length * 3;
            byte[] buffer = new byte[length];

            int idx = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                buffer[idx++] = pixels[i].r;
                buffer[idx++] = pixels[i].g;
                buffer[idx++] = pixels[i].b;
            }

            return buffer;
        }

        // Espelhamento horizontal (caso queira usar depois)
        public static void MirrorHorizontally(Color32[] pixels, int width, int height)
        {
            int half = width / 2;

            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < half; x++)
                {
                    int left = row + x;
                    int right = row + (width - x - 1);

                    (pixels[left], pixels[right]) = (pixels[right], pixels[left]);
                }
            }
        }
    }
}
