using UnityEngine;

namespace BubblesGame
{
    /// <summary>
    /// Conversor centralizado de coordenadas
    /// 
    /// SISTEMA INTERNO (MediaPipe, BallData, Colisão):
    ///   - Origin: TOP-LEFT
    ///   - Range: [0-1] normalized
    ///   - X: 0 = esquerda, 1 = direita
    ///   - Y: 0 = TOPO, 1 = baixo
    /// 
    /// SISTEMA UNITY UI (Canvas/RectTransform):
    ///   - Origin: BOTTOM-LEFT
    ///   - Units: pixels
    ///   - X: 0 = esquerda, width = direita
    ///   - Y: 0 = BAIXO, height = topo
    /// 
    /// Este é o ÚNICO lugar onde a inversão de Y acontece!
    /// </summary>
    public static class CoordinateConverter
    {
        /// <summary>
        /// Converte posição normalizada (top-left, 0-1) para pixels do Canvas (bottom-left)
        /// </summary>
        /// <param name="normalizedTopLeft">Posição normalized com origin top-left (0-1)</param>
        /// <param name="canvasRect">RectTransform do canvas/container</param>
        /// <returns>Posição em pixels com origin bottom-left</returns>
        public static Vector2 NormalizedToCanvasPosition(Vector2 normalizedTopLeft, RectTransform canvasRect)
        {
            if (canvasRect == null)
            {
                Debug.LogError("[CoordinateConverter] canvasRect is null!");
                return Vector2.zero;
            }

            return new Vector2(
                normalizedTopLeft.x * canvasRect.rect.width,
                (1f - normalizedTopLeft.y) * canvasRect.rect.height  // ← ÚNICA inversão de Y aqui!
            );
        }

        /// <summary>
        /// Converte posição normalizada (top-left, 0-1) para pixels de tela (bottom-left)
        /// Usa Screen.width e Screen.height como referência
        /// </summary>
        public static Vector2 NormalizedToScreenPosition(Vector2 normalizedTopLeft)
        {
            return new Vector2(
                normalizedTopLeft.x * Screen.width,
                (1f - normalizedTopLeft.y) * Screen.height  // ← ÚNICA inversão de Y aqui!
            );
        }

        /// <summary>
        /// Converte posição do Canvas (bottom-left, pixels) para normalizada (top-left, 0-1)
        /// Útil para converter input/touch para sistema interno
        /// </summary>
        public static Vector2 CanvasToNormalizedPosition(Vector2 canvasPosition, RectTransform canvasRect)
        {
            if (canvasRect == null)
            {
                Debug.LogError("[CoordinateConverter] canvasRect is null!");
                return Vector2.zero;
            }

            return new Vector2(
                canvasPosition.x / canvasRect.rect.width,
                1f - (canvasPosition.y / canvasRect.rect.height)  // ← Inverte Y de volta
            );
        }

        /// <summary>
        /// Valida se coordenada normalizada está dentro dos limites válidos (0-1)
        /// </summary>
        public static bool IsNormalizedInBounds(Vector2 normalized)
        {
            return normalized.x >= 0f && normalized.x <= 1f &&
                   normalized.y >= 0f && normalized.y <= 1f;
        }

        /// <summary>
        /// Clamp coordenada normalizada para garantir que está em 0-1
        /// </summary>
        public static Vector2 ClampNormalized(Vector2 normalized)
        {
            return new Vector2(
                Mathf.Clamp01(normalized.x),
                Mathf.Clamp01(normalized.y)
            );
        }

        /// <summary>
        /// Converte posição normalizada TOP-LEFT (0-1) para World Position 3D
        /// Usado para posicionar efeitos 3D (SpriteRenderer) no espaço do Canvas
        /// </summary>
        /// <param name="normalizedTopLeft">Posição normalized TOP-LEFT (0-1)</param>
        /// <param name="canvasRect">RectTransform do canvas/container</param>
        /// <param name="camera">Câmera para conversão world space</param>
        /// <returns>Posição world space 3D</returns>
        public static Vector3 NormalizedToWorldPosition(Vector2 normalizedTopLeft, RectTransform canvasRect, Camera camera)
        {
            if (canvasRect == null)
            {
                Debug.LogError("[CoordinateConverter] canvasRect is null!");
                return Vector3.zero;
            }

            if (camera == null)
            {
                Debug.LogError("[CoordinateConverter] camera is null!");
                return Vector3.zero;
            }

            // 1. TOP-LEFT normalized → Canvas pixels (BOTTOM-LEFT)
            Vector2 canvasPos = NormalizedToCanvasPosition(normalizedTopLeft, canvasRect);
            
            // 2. Canvas local position → World position
            Vector3 canvasWorldPos = canvasRect.TransformPoint(canvasPos);
            
            // 3. World position → Screen position
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camera, canvasWorldPos);
            
            // 4. Screen position → World position (Z=0 para câmera ortográfica 2D)
            Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            
            return worldPos;
        }
    }
}
