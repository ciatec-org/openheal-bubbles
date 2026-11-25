# Sistema de Detecção de Pose - MediaPipe

## Fluxo de Processamento

```
WebcamCapture → [Frame RGB] → PoseWebSocketClient → Servidor MediaPipe
                                                           ↓
                                                    [33 Landmarks]
                                                           ↓
PoseController ← [Coordenadas 0-1] ← JSON Response ←──────┘
      ↓
[Espelhamento + Smoothing]
      ↓
LandmarkFrameProcessor → Jogos (Bubbles, etc)
```

## Captura de Câmera

**WebcamCapture** (`WebcamConfig`):
- Resolução padrão: `1280x720` @ `30 FPS`
- Formato: `TextureFormat.RGB24`
- Update: A cada frame que `webcam.didUpdateThisFrame`
- Evento: `OnFrameReady(Texture2D)`

## Envio para Servidor

**PoseWebSocketClient**:
1. Redimensiona frame para `320x240` (otimização)
2. Comprime para JPEG (qualidade 30)
3. Converte para Base64
4. Envia via WebSocket para `ws://127.0.0.1:8765`
5. Throttling: Envia 1 a cada 2 frames (reduz processamento)

**Cache**: Texture redimensionada é reutilizada para evitar alocações constantes

## Resposta do Servidor

**Formato JSON**:

```json
{
  "landmarks": [
    {"index": 0, "x": 0.5, "y": 0.3, "z": -0.1, "likelihood": 0.95},
    {"index": 1, "x": 0.52, "y": 0.28, "z": -0.12, "likelihood": 0.93},
    ...
  ]
}
```

**33 Landmarks MediaPipe Pose**: Ombros, cotovelos, pulsos, quadris, joelhos, tornozelos, olhos, etc.

## Sistema de Coordenadas

### MediaPipe (Servidor)
- **Origin**: TOP-LEFT `(0, 0)`
- **Range**: Normalizado `[0-1]`
- **X**: `0` = esquerda, `1` = direita
- **Y**: `0` = topo, `1` = baixo
- **Z**: Profundidade relativa (negativo = mais próximo da câmera)

### Após PoseController (Interno)
- **Origin**: TOP-LEFT `(0, 0)` (mantém)
- **Range**: Normalizado `[0-1]` (mantém)
- **X**: ESPELHADO `x = 1 - x` (UI espelhada)
- **Y**: Não invertido (mantém top-left)

### Canvas Unity (Visualização)
- **Origin**: BOTTOM-LEFT
- **Conversão**: `CoordinateConverter.NormalizedToCanvasPosition()`
- Inverte Y: `canvasY = (1 - normalizedY) * height`

## Processamento

### 1. PoseController
- Recebe `PoseResult` do WebSocket
- **Espelhamento**: `x = 1 - x` (linha 63)
- **Smoothing**: One Euro Filter (opcional, `useFilter = true`)
  - Parâmetros: `freq=15, minCutoff=12, beta=8, dCutoff=10`
  - Reduz tremores mas adiciona latência mínima
- Emite evento: `OnPoseProcessed(PoseResult)`

### 2. LandmarkFrameProcessor
- Filtra por `likelihood >= 0.5` (threshold configurável)
- Valida bounds: `0 <= x,y <= 1`
- Cria `ProcessedLandmark` com:
  - `NormalizedPosition`: TOP-LEFT (0-1)
  - `ScreenPosition`: BOTTOM-LEFT pixels (compatibilidade)
  - `IsVisible`: `inBounds && likelihood >= threshold`

## Resolução e Taxas

**Captura**:
- Resolução webcam: `1280x720` (configurável)
- FPS webcam: `30 FPS`

**Envio**:
- Resolução enviada: `320x240` (redimensionada)
- Taxa de envio: ~15 FPS (1 a cada 2 frames)
- Compressão: JPEG quality 30

**Latência típica**: 50-100ms (rede local + processamento)

## Espelhamento

**Por quê?** A UI da webcam está flipada horizontalmente para efeito "espelho" (usuário vê como em um espelho).

**Implementação**: 
- Único ponto: `PoseController` linha 63
- **Não** é feito no servidor
- **Não** é feito no FramePreprocessor
- Coordenadas internas já estão espelhadas após PoseController

## Normalização

**Vantagens do sistema 0-1**:
- Independente de resolução de tela
- Compatível direto com MediaPipe
- Facilita cálculos de colisão
- Reduz conversões desnecessárias

**Conversão para pixels** (quando necessário):

```csharp
float pixelsX = normalizedX * Screen.width;
float pixelsY = normalizedY * Screen.height;
```

## Performance

- **Pool de texturas**: Cache de resize evita GC
- **Throttling**: 15 FPS de envio vs 30 FPS de captura
- **Compressão**: JPEG reduz tráfego de rede
- **Filtros**: Dictionary cache evita recriação
- **Coordenadas normalizadas**: Menos cálculos de conversão

## Troubleshooting

**Landmarks tremem muito**:
- Ativar `useFilter = true` no PoseController
- Ajustar parâmetros do One Euro Filter

**Coordenadas invertidas**:
- Verificar que espelhamento está ativo: `x = 1 - x`
- Usar `CoordinateConverter` para conversões Canvas

**Latência alta**:
- Verificar rede local
- Reduzir resolução de envio (`sendWidth/sendHeight`)
- Aumentar throttling (enviar 1 a cada 3 frames)

**Landmarks não detectados**:
- Verificar iluminação
- Garantir corpo visível inteiro
- Ajustar `minLikelihood` threshold
- Verificar se a lista de landmarks enviadas pelo servidor contempla a landmark desejada

