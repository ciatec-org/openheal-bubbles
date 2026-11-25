# Bubbles Game - Sistema de Detecção de Pose

Jogo de fisioterapia que usa MediaPipe para detectar movimentos corporais e estourar bolas na tela.

## Fluxo do Sistema

```
1. CAPTURA
   WebcamCapture (1280x720@30fps)
         ↓
   [Frame RGB]
         ↓

2. DETECÇÃO
   PoseWebSocketClient (redimensiona 320x240, JPEG)
         ↓
   Servidor MediaPipe (ws://127.0.0.1:8765)
         ↓
   [33 Landmarks: x,y,z, likelihood]
         ↓

3. PROCESSAMENTO
   PoseController (espelhamento x=1-x + smoothing)
         ↓
   LandmarkFrameProcessor (filtra likelihood>0.5)
         ↓
   [ProcessedLandmark: coordenadas 0-1 TOP-LEFT]
         ↓

4. JOGO
   Firebase → BubblesGameManager carrega LevelData
         ↓
   Spawna Ball com BallData (tempo, posição, velocidade, landmarks)
         ↓
   PoseHitDetector detecta colisão (distância landmark-bola)
         ↓
   ScoreManager calcula pontos (hit/miss/combo)
         ↓
   GameController gerencia tempo e finaliza
```

## Como Funciona

1. **Câmera captura** frames da webcam a 30 FPS
2. **Cliente WebSocket** redimensiona e envia frames a ~15 FPS para servidor MediaPipe
3. **Servidor retorna** 33 landmarks (coordenadas 0-1 do corpo)
4. **PoseController** espelha horizontalmente e aplica smoothing
5. **GameManager carrega** nível do Firebase (lista de bolas com timings)
6. **Bolas spawnam** no tempo correto e se movem com física de bounce
7. **Detector verifica** colisões entre landmarks e bolas cada frame
8. **ScoreManager** registra hits/miss e calcula pontuação com combo
9. **GameController** termina jogo por tempo ou quando todas bolas acabam

## Arquitetura

### Core (Sistemas Base)
- **WebcamCapture**: Captura de câmera
- **PoseWebSocketClient**: Comunicação com servidor MediaPipe
- **PoseController**: Processamento de landmarks (smoothing + espelhamento)
- **PoseResult**: Estrutura de dados dos 33 landmarks

### Game (Lógica do Jogo)
- **BubblesGameManager**: Orquestração principal, spawn de bolas
- **Ball**: Física (movimento, bounce), estado (active/hit/destroyed)
- **PoseHitDetector**: Sistema de colisão landmark-bola
- **ScoreManager**: Pontuação, combo, vidas
- **GameController**: Tempo, estados do jogo, cooldowns

### Data (Firebase)
- **FirebaseLevelLoader**: Carrega configuração de níveis
- **BallListService**: Gera lista de bolas procedural ou carrega do Firebase
- **UserSettingsService**: Configurações de sensibilidade por usuário
- **BallEventRecorder**: Registra eventos de colisão para análise
- **SessionManager**: Gerencia dados da sessão (companyId, matchId, etc)

### Shared (Utilitários)
- **CoordinateConverter**: Conversão coordenadas TOP-LEFT ↔ BOTTOM-LEFT
- **LandmarkFrameProcessor**: Filtra e valida landmarks
- **GameEvents**: Sistema de eventos global

## Coordenadas

**Sistema interno** (MediaPipe, colisão, bolas):
- Origin: TOP-LEFT (0,0)
- Range: Normalizado [0-1]
- X espelhado após PoseController

**Unity Canvas** (visualização):
- Origin: BOTTOM-LEFT
- Conversão via `CoordinateConverter`

## Tipos de Bola

- **Normal**: +pontos base
- **hit_super**: +pontos × 3
- **avoid_super**: -pontos × 3, perde vida
- **hit_all_super**: Acerta todas bolas ativas
- **destroy_all_super**: Destrói todas bolas (penalidades)

## Pontuação

- **Hit**: +pontos (× combo)
- **Miss**: -pontos, quebra combo, perde vida
- **Combo**: Multiplica por `1 + (combo / step)`

## Firebase

```
games/bubbles/
  presets/{presetId}/levels/{levelId}/  # Configuração do nível
  settings/                              # Defaults globais

users/{uid}/companies/{cid}/games/bubbles/
  settings/        # Sensibilidades personalizadas
  matches/{mid}/   # Dados da partida
```

## Performance

- Throttling: 15 FPS envio vs 30 FPS captura
- Cache: Texturas redimensionadas reutilizadas
- Coordenadas normalizadas: Menos conversões
- Filtros: Dictionary cache evita recriação
- Compressão: JPEG quality 30

