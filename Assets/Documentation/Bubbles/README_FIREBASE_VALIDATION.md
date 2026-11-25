# Firebase Data Structure - Unity Validation Guide

## Path no Firebase

```
games/bubbles/presets/{preset_id}/levels/{level_id}
```

## Estrutura do Preset

```csharp
{
  description: string,        // Ex: "Standard Preset Bubbles game"
  version: int,               // Ex: 1
  defaults?: Dictionary       // Opcional, config geral
}
```

## Estrutura do Level

```csharp
{
  description: string,                    // Ex: "Level configuration"
  totalTime: int,                         // Tempo total em segundos (ex: 120)
  coolDown: List<CoolDownPeriod>,         // Array de objetos {start_time, duration}
  hitLandmarks: int[],                    // Array - pode ter qualquer tamanho, pode ser vazio []
  destroyLandmarks: int[],                // Array - pode ter qualquer tamanho, pode ser vazio []
  
  // Opcionais - podem não existir no documento
  areaHitSuper?: List<SuperObject>,       // Array de objetos completos
  areaAvoidSuper?: List<SuperObject>,     // Array de objetos completos
  areaHitAllSuper?: List<SuperObject>,    // Array de objetos completos
  areaDestroyAllSuper?: List<SuperObject>, // Array de objetos completos
  
  spatialDistributions?: SpatialDistribution[],  // Array - pode ser vazio []
  moments?: PhaseConfig[]                        // Array - pode ser vazio [] (chamado "moments" no Firebase)
}
```

## CoolDownPeriod

```csharp
{
  start_time: int,    // Tempo em segundos quando o cooldown inicia
  duration: int       // Duração do cooldown em segundos
}
```

## SuperObject

```csharp
{
  time: float,           // Tempo quando o objeto deve aparecer
  speed: float,          // Velocidade específica
  size: float,           // Tamanho específico
  direction: float,      // Direção específica (em graus)
  launch_coord_x: float, // Coordenada X normalizada (0-1)
  launch_coord_y: float  // Coordenada Y normalizada (0-1)
}
```

## SpatialDistribution - FORMATO CRÍTICO

**⚠️ IMPORTANTE**: No Firestore, `xZone` e `yZone` são **arrays de objetos `{min, max}`**, NÃO arrays aninhados `[[min, max]]`.

### Formato Correto (Firestore):
```csharp
{
  name: string,                           // Ex: "lateral_superior"
  xZone: List<ZoneRange>,                 // Lista de objetos {min, max}
  yZone: List<ZoneRange>,                 // Lista de objetos {min, max}
  xDist: List<float>,                     // Array de probabilidades - tamanho variável
  yDist: List<float>                      // Array de probabilidades - tamanho variável
}

// ZoneRange é um objeto:
{
  min: float,  // Ex: 0.0
  max: float   // Ex: 0.35
}
```

### Exemplo Real:
```json
{
  "name": "lateral_superior",
  "xZone": [
    {"min": 0, "max": 0.35},
    {"min": 0.351, "max": 0.5},
    {"min": 0.501, "max": 0.65},
    {"min": 0.651, "max": 1}
  ],
  "yZone": [
    {"min": 0, "max": 0.35},
    {"min": 0.351, "max": 0.6},
    {"min": 0.601, "max": 1}
  ],
  "xDist": [0.4, 0.1, 0.1, 0.4],
  "yDist": [0.1, 0.3, 0.6]
}
```

### ❌ Formato INCORRETO (não usar):
```json
{
  "xZone": [[0, 0.35], [0.351, 0.5]],  // Arrays aninhados - NÃO funciona no Firestore
  "yZone": [[0, 0.35], [0.351, 0.6]]   // Arrays aninhados - NÃO funciona no Firestore
}
```

## PhaseConfig (chamado "moments" no Firebase)

```csharp
{
  minInt: float,          // Ex: 0.8 - Intervalo mínimo entre bolas
  maxInt: float,          // Ex: 1.0 - Intervalo máximo entre bolas
  minSpeed: float,        // Ex: 4.0 - Velocidade mínima
  maxSpeed: float,        // Ex: 4.0 - Velocidade máxima
  minSize: float,         // Ex: 0.8 - Tamanho mínimo
  maxSize: float,         // Ex: 0.8 - Tamanho máximo
  matureTime: float,      // Ex: 0.1 - Tempo de maturação
  destroyTime: float,     // Ex: 1.0 - Tempo até destruição
  spatialDistIndex: int    // Ex: 0 (índice em spatialDistributions)
}
```

## Validações Obrigatórias

### 1. Arrays podem ser vazios ou null
- Todos os arrays (`coolDown`, `hitLandmarks`, `spatialDistributions`, `moments`, etc.) podem ser:
  - `[]` (array vazio)
  - `null` / `undefined` (não existe)
- **SEMPRE** validar antes de usar:
  ```csharp
  var coolDown = new List<CoolDownPeriod>();
  if (data.ContainsKey("coolDown")) {
      var cooldownList = data["coolDown"] as List<object>;
      if (cooldownList != null && cooldownList.Count > 0) {
          // Parse cooldown objects
      }
  }
  ```

### 2. Tamanhos variáveis
- Arrays podem ter qualquer tamanho: 0, 1, 2, 10, 100+
- **NUNCA** assumir tamanho fixo
- **SEMPRE** verificar `Count` antes de acessar índices

### 3. Campos opcionais podem não existir
- `areaHitSuper`, `areaAvoidSuper`, `areaHitAllSuper`, `areaDestroyAllSuper` (são `List<SuperObject>`)
- `spatialDistributions`, `moments` (arrays de objetos complexos)
- **SEMPRE** verificar existência antes de usar:
  ```csharp
  if (data.ContainsKey("areaHitSuper")) {
      var areaHitSuper = ParseSuperObjectList(data, "areaHitSuper");
  }
  ```

### 4. xZone/yZone são objetos, não arrays
- **CRÍTICO**: `xZone` e `yZone` são `List<Dictionary<string, float>>` ou objetos customizados
- **NÃO** são `List<List<float>>`
- Acessar como: `zone["min"]` e `zone["max"]`, não `zone[0]` e `zone[1]`

### 5. Consistência de tamanhos
- `xDist.Count == xZone.Count`
- `yDist.Count == yZone.Count`
- `spatialDistIndex` em `moments` deve ser `< spatialDistributions.Count`

## Exemplo de Leitura (Unity C#)

```csharp
using Firebase.Firestore;
using System.Collections.Generic;

public class LevelConfig
{
    public string description;
    public int totalTime;
    public List<CoolDownPeriod> coolDown;
    public int[] hitLandmarks;
    public int[] destroyLandmarks;
    public List<SuperObject> areaHitSuper;
    public List<SuperObject> areaAvoidSuper;
    public List<SuperObject> areaHitAllSuper;
    public List<SuperObject> areaDestroyAllSuper;
    public SpatialDistribution[] spatialDistributions;
    public PhaseConfig[] moments;
}

public class CoolDownPeriod
{
    public int start_time;
    public int duration;
}

public class SuperObject
{
    public float time;
    public float speed;
    public float size;
    public float direction;
    public float launch_coord_x;
    public float launch_coord_y;
}

public class ZoneRange
{
    public float min;
    public float max;
}

public class SpatialDistribution
{
    public string name;
    public List<ZoneRange> xZone;  // ⚠️ Lista de objetos, não List<List<float>>
    public List<ZoneRange> yZone;  // ⚠️ Lista de objetos, não List<List<float>>
    public float[] xDist;
    public float[] yDist;
}

public class PhaseConfig
{
    public float minInt;
    public float maxInt;
    public float minSpeed;
    public float maxSpeed;
    public float minSize;
    public float maxSize;
    public float matureTime;
    public float destroyTime;
    public int spatialDistIndex;
}

// Função de leitura
public async Task<LevelConfig> LoadLevel(string presetId, string levelId)
{
    DocumentReference levelRef = FirebaseFirestore.DefaultInstance
        .Collection("games")
        .Document("bubbles")
        .Collection("presets")
        .Document(presetId)
        .Collection("levels")
        .Document(levelId);
    
    DocumentSnapshot snapshot = await levelRef.GetSnapshotAsync();
    
    if (!snapshot.Exists)
    {
        throw new Exception($"Level {levelId} not found");
    }
    
    Dictionary<string, object> data = snapshot.ToDictionary();
    LevelConfig level = new LevelConfig();
    
    // Campos obrigatórios
    level.description = data.GetValue<string>("description") ?? "";
    level.totalTime = data.GetValue<int>("totalTime");
    
    // Arrays - sempre validar
    level.coolDown = new List<CoolDownPeriod>();
    if (data.ContainsKey("coolDown"))
    {
        var cooldownList = data["coolDown"] as List<object>;
        if (cooldownList != null)
        {
            foreach (var item in cooldownList)
            {
                var dict = item as Dictionary<string, object>;
                if (dict != null)
                {
                    level.coolDown.Add(new CoolDownPeriod {
                        start_time = Convert.ToInt32(dict["start_time"]),
                        duration = Convert.ToInt32(dict["duration"])
                    });
                }
            }
        }
    }
    
    level.hitLandmarks = data.ContainsKey("hitLandmarks") 
        ? (data["hitLandmarks"] as List<object>)?.Select(x => Convert.ToInt32(x)).ToArray()
        : new int[0];
    
    level.destroyLandmarks = data.ContainsKey("destroyLandmarks")
        ? (data["destroyLandmarks"] as List<object>)?.Select(x => Convert.ToInt32(x)).ToArray()
        : new int[0];
    
    // SuperObjects opcionais - código simplificado, ver FirebaseLevelLoader.cs para implementação completa
    level.areaHitSuper = ParseSuperObjectList(data, "areaHitSuper");
    level.areaAvoidSuper = ParseSuperObjectList(data, "areaAvoidSuper");
    level.areaHitAllSuper = ParseSuperObjectList(data, "areaHitAllSuper");
    level.areaDestroyAllSuper = ParseSuperObjectList(data, "areaDestroyAllSuper");
    
    // SpatialDistributions - FORMATO CRÍTICO
    level.spatialDistributions = new List<SpatialDistribution>();
    if (data.ContainsKey("spatialDistributions"))
    {
        var spatialDists = data.GetValue<List<object>>("spatialDistributions");
        if (spatialDists != null)
        {
            foreach (var distObj in spatialDists)
            {
                var distDict = distObj as Dictionary<string, object>;
                if (distDict == null) continue;
                
                SpatialDistribution dist = new SpatialDistribution();
                dist.name = distDict.GetValue<string>("name") ?? "";
                
                // ⚠️ xZone e yZone são List<Dictionary<string, float>>
                var xZoneList = distDict.GetValue<List<object>>("xZone");
                dist.xZone = new List<ZoneRange>();
                if (xZoneList != null)
                {
                    foreach (var zoneObj in xZoneList)
                    {
                        var zoneDict = zoneObj as Dictionary<string, object>;
                        if (zoneDict == null) continue;
                        
                        ZoneRange zone = new ZoneRange();
                        zone.min = Convert.ToSingle(zoneDict["min"]);
                        zone.max = Convert.ToSingle(zoneDict["max"]);
                        dist.xZone.Add(zone);
                    }
                }
                
                // Mesmo para yZone
                var yZoneList = distDict.GetValue<List<object>>("yZone");
                dist.yZone = new List<ZoneRange>();
                if (yZoneList != null)
                {
                    foreach (var zoneObj in yZoneList)
                    {
                        var zoneDict = zoneObj as Dictionary<string, object>;
                        if (zoneDict == null) continue;
                        
                        ZoneRange zone = new ZoneRange();
                        zone.min = Convert.ToSingle(zoneDict["min"]);
                        zone.max = Convert.ToSingle(zoneDict["max"]);
                        dist.yZone.Add(zone);
                    }
                }
                
                dist.xDist = distDict.GetValue<List<float>>("xDist") ?? new List<float>();
                dist.yDist = distDict.GetValue<List<float>>("yDist") ?? new List<float>();
                
                // Validação de consistência
                if (dist.xDist.Count != dist.xZone.Count)
                {
                    Debug.LogWarning($"xDist.Count ({dist.xDist.Count}) != xZone.Count ({dist.xZone.Count})");
                }
                if (dist.yDist.Count != dist.yZone.Count)
                {
                    Debug.LogWarning($"yDist.Count ({dist.yDist.Count}) != yZone.Count ({dist.yZone.Count})");
                }
                
                level.spatialDistributions.Add(dist);
            }
        }
    }
    
    // PhaseConfigs (chamado "moments" no Firebase)
    var phasesList = new List<PhaseConfig>();
    if (data.ContainsKey("moments"))
    {
        var momentsList = data["moments"] as List<object>;
        if (momentsList != null)
        {
            foreach (var momentObj in momentsList)
            {
                var momentDict = momentObj as Dictionary<string, object>;
                if (momentDict == null) continue;
                
                PhaseConfig phase = new PhaseConfig {
                    minInt = Convert.ToSingle(momentDict["minInt"]),
                    maxInt = Convert.ToSingle(momentDict["maxInt"]),
                    minSpeed = Convert.ToSingle(momentDict["minSpeed"]),
                    maxSpeed = Convert.ToSingle(momentDict["maxSpeed"]),
                    minSize = Convert.ToSingle(momentDict["minSize"]),
                    maxSize = Convert.ToSingle(momentDict["maxSize"]),
                    matureTime = Convert.ToSingle(momentDict["matureTime"]),
                    destroyTime = Convert.ToSingle(momentDict["destroyTime"]),
                    spatialDistIndex = Convert.ToInt32(momentDict["spatialDistIndex"])
                };
                
                // Validação de índice
                if (phase.spatialDistIndex >= level.spatialDistributions.Length)
                {
                    Debug.LogError($"spatialDistIndex {phase.spatialDistIndex} >= spatialDistributions.Length {level.spatialDistributions.Length}");
                }
                
                phasesList.Add(phase);
            }
        }
    }
    level.moments = phasesList.ToArray();
    
    return level;
}

// Helper method
private List<SuperObject> ParseSuperObjectList(Dictionary<string, object> data, string key)
{
    if (!data.ContainsKey(key)) return null;
    
    var list = data[key] as List<object>;
    if (list == null) return null;
    
    var result = new List<SuperObject>();
    foreach (var obj in list)
    {
        var dict = obj as Dictionary<string, object>;
        if (dict != null)
        {
            result.Add(new SuperObject {
                time = Convert.ToSingle(dict["time"]),
                speed = Convert.ToSingle(dict["speed"]),
                size = Convert.ToSingle(dict["size"]),
                direction = Convert.ToSingle(dict["direction"]),
                launch_coord_x = Convert.ToSingle(dict["launch_coord_x"]),
                launch_coord_y = Convert.ToSingle(dict["launch_coord_y"])
            });
        }
    }
    return result;
}
```

## Checklist de Validação

Verifique se o código Unity:

- [ ] Valida arrays vazios antes de usar (não assume que sempre existem)
- [ ] Valida tamanhos de arrays antes de acessar índices
- [ ] Verifica existência de campos opcionais antes de usar
- [ ] **xZone e yZone são tratados como List<ZoneRange> ou List<Dictionary>, NÃO List<List<float>>**
- [ ] Acessa `zone["min"]` e `zone["max"]`, não `zone[0]` e `zone[1]`
- [ ] Valida consistência: `xDist.Count == xZone.Count` e `yDist.Count == yZone.Count`
- [ ] Valida `spatialDistIndex < spatialDistributions.Count` antes de usar
- [ ] Trata erros de conversão de tipos (float/int)
- [ ] Usa `??` ou verificação de null para valores opcionais
- [ ] Não assume tamanhos fixos para arrays

## Erros Comuns

### ❌ Erro 1: Tratar xZone como array aninhado
```csharp
// ERRADO
List<List<float>> xZone = data.GetValue<List<List<float>>>("xZone");
float min = xZone[0][0];  // Isso NÃO funciona!
```

### ✅ Correto: Tratar xZone como lista de objetos
```csharp
// CORRETO
List<ZoneRange> xZone = ParseZoneList(data.GetValue<List<object>>("xZone"));
float min = xZone[0].min;
```

### ❌ Erro 2: Assumir que array sempre tem elementos
```csharp
// ERRADO
var firstCoolDown = coolDown[0];  // Pode dar IndexOutOfRangeException
```

### ✅ Correto: Validar antes
```csharp
// CORRETO
if (coolDown != null && coolDown.Count > 0) {
    var firstCoolDown = coolDown[0];
    // firstCoolDown tem start_time e duration
}
```

### ❌ Erro 3: Não validar índices
```csharp
// ERRADO
SpatialDistribution dist = spatialDistributions[moment.spatialDistIndex];
```

### ✅ Correto: Validar índice
```csharp
// CORRETO
if (moment.spatialDistIndex < spatialDistributions.Count) {
    SpatialDistribution dist = spatialDistributions[moment.spatialDistIndex];
}
```

