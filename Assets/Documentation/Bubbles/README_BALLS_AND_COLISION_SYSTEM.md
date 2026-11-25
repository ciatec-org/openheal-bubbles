# Sistema de Colisão MediaPipe - Bubbles Game

## Arquitetura

Sistema de colisão entre landmarks do MediaPipe e bolas do jogo usando coordenadas normalizadas (0-1).

**Componentes principais:**
- `PoseHitDetector` - Detector de colisão
- `Ball` - Física de bounce
- `UserSettingsService` - Persistência Firebase

## Como Funciona

O sistema detecta colisões calculando a distância entre cada landmark ativo (likelihood > 0.5) e as bolas. Cada frame, o `PoseHitDetector` itera pelos landmarks configurados, converte suas posições para coordenadas normalizadas e verifica se estão dentro do raio de colisão de alguma bola. Existem dois tipos de colisão: **hit** (toca a bola) e **destroy** (estoura a bola), cada um com seu próprio conjunto de landmarks e sensibilidade. Quando uma colisão é detectada, o sistema dispara eventos específicos (`OnBallHit` ou `OnBallDestroyed`) com timestamp relativo ao início do jogo para análise posterior. As bolas registram internamente quando foram atingidas e destruídas.

### Tipos de Bola (object_type)

- **Normal**: Bola padrão, pontos base
- **hit_super**: Bola dourada, vale 3× pontos base
- **avoid_super**: Bola vermelha, deve ser evitada (perde vida e pontos se colidir)
- **hit_all_super**: Acerta todas as bolas ativas quando tocada
- **destroy_all_super**: Destrói todas as bolas ativas quando qualquer landmark colide (penalidade de miss para todas)

### Sistema de Pontuação

**Eventos:**
- **Hit**: `+pontos base` (ou `+pontos × 3` para hit_super)
- **Hit em avoid_super**: `pontos negativos × 3` + perde vida
- **Miss** (bola expira): `pontos negativos` + quebra combo + perde vida
- **hit_all_super**: soma pontos de todas as bolas ativas
- **destroy_all_super**: penalidade de miss para cada bola destruída

**Combo**: Multiplica pontos por `1 + (combo / step)`. Quebra ao errar ou deixar bola expirar.

## Coordenadas Normalizadas

Sistema usa valores 0-1 para independência de resolução:

```csharp
normalizedPos = screenPos / Screen.width
radius = sensitivity * Screen.width
```

**Sensibilidade:**
- Geral: 0.01-0.15 (base)
- Hit: 0-0.1 (adicional para hit_landmarks)
- Destroy: 0-0.1 (adicional para destroy_landmarks)
- **Cálculo final:** `Total = Geral + Específica`

## Física de Bounce

Reflexão perfeita nas bordas:

```csharp
// Vertical
if (normalizedPos.x ± radius fora dos bounds) velocity.x *= -1;

// Horizontal  
if (normalizedPos.y ± radius fora dos bounds) velocity.y *= -1;
```

## Firebase

**Estrutura de dados:**

```
games/bubbles/settings/                    # Defaults globais
  defaultGeneralSensitivity: 0.05
  defaultHitSensitivity: 0.02
  defaultDestroySensitivity: 0.01

users/{uid}/companies/{cid}/games/bubbles/settings/  # Por usuário
  generalSensitivity: 0.05
  hitLandmarksSensitivity: 0.02
  destroyLandmarksSensitivity: 0.01
```

## Debug

Ativar `debugVisualization` no `PoseHitDetector` para visualizar:
- Círculos de colisão em tempo real
- Cores diferentes para hit (verde) vs destroy (vermelho)
- Métricas no Scene view via Gizmos

## Troubleshooting

**Colisões não detectam:**
- Landmarks com `likelihood > 0.5`
- `PoseController` ativo
- Sensibilidade configurada corretamente

**Bounce não funciona:**
- `ballRadiusNormalized` configurado
- `UpdateMovement()` sendo chamado no Update

**Firebase falha:**
- Firebase inicializado antes de usar
- Permissões de escrita configuradas
- Estrutura de path correta