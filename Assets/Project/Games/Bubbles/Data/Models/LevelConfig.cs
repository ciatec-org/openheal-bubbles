using System;
using UnityEngine;
using System.Collections.Generic;

namespace BubblesGame.Data
{
    /// <summary>
    /// Representa um range de zona com min e max
    /// Formato correto para Firebase: {min: float, max: float}
    /// </summary>
    [Serializable]
    public class ZoneRange
    {
        public float min;
        public float max;
        
        public ZoneRange() { }
        
        public ZoneRange(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    /// <summary>
    /// Configuração de distribuição espacial das bolas
    /// Define zonas na tela e probabilidades de spawn
    /// </summary>
    [Serializable]
    public class SpatialDistribution
    {
        public string name;
        public List<ZoneRange> xZone;  // Lista de objetos {min, max} para zonas em X
        public List<ZoneRange> yZone;  // Lista de objetos {min, max} para zonas em Y
        public float[] xDist;          // [0.4, 0.1, 0.1, 0.4] pesos/probabilidades para cada zona X
        public float[] yDist;          // [0.1, 0.3, 0.6] pesos/probabilidades para cada zona Y
        
        /// <summary>
        /// Valida se a distribuição espacial está consistente
        /// </summary>
        public bool IsValid(out string error)
        {
            error = "";
            
            if (xZone == null || xZone.Count == 0)
            {
                error = "xZone is null or empty";
                return false;
            }
            
            if (yZone == null || yZone.Count == 0)
            {
                error = "yZone is null or empty";
                return false;
            }
            
            if (xDist == null || xDist.Length == 0)
            {
                error = "xDist is null or empty";
                return false;
            }
            
            if (yDist == null || yDist.Length == 0)
            {
                error = "yDist is null or empty";
                return false;
            }
            
            if (xDist.Length != xZone.Count)
            {
                error = $"xDist.Length ({xDist.Length}) != xZone.Count ({xZone.Count})";
                return false;
            }
            
            if (yDist.Length != yZone.Count)
            {
                error = $"yDist.Length ({yDist.Length}) != yZone.Count ({yZone.Count})";
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Representa um período de cooldown no jogo
    /// Durante este período, o timer principal é pausado
    /// </summary>
    [Serializable]
    public class CoolDownPeriod
    {
        public int start_time;  // tempo em segundos quando o cooldown inicia
        public int duration;    // duração do cooldown em segundos
        
        public CoolDownPeriod() { }
        
        public CoolDownPeriod(int startTime, int duration)
        {
            this.start_time = startTime;
            this.duration = duration;
        }
        
        /// <summary>
        /// Valida se o período de cooldown é válido
        /// </summary>
        public bool IsValid(out string error)
        {
            error = "";
            
            if (start_time < 0)
            {
                error = "start_time must be >= 0";
                return false;
            }
            
            if (duration <= 0)
            {
                error = "duration must be > 0";
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Representa um objeto especial com propriedades específicas
    /// Usado para bolas com comportamentos especiais (hit, avoid, hit_all, destroy_all)
    /// </summary>
    [Serializable]
    public class SuperObject
    {
        public float time;           // tempo quando o objeto deve aparecer
        public float speed;          // velocidade específica
        public float size;           // tamanho específico
        public float direction;      // direção específica
        public float launch_coord_x; // coordenada X específica (0-1)
        public float launch_coord_y; // coordenada Y específica (0-1)
        
        public SuperObject() { }
        
        public SuperObject(float time, float speed, float size, float direction, float launchX, float launchY)
        {
            this.time = time;
            this.speed = speed;
            this.size = size;
            this.direction = direction;
            this.launch_coord_x = launchX;
            this.launch_coord_y = launchY;
        }
        
        /// <summary>
        /// Valida se o objeto especial é válido
        /// </summary>
        public bool IsValid(out string error)
        {
            error = "";
            
            if (time < 0)
            {
                error = "time must be >= 0";
                return false;
            }
            
            if (speed < 0)
            {
                error = "speed must be >= 0";
                return false;
            }
            
            if (size <= 0)
            {
                error = "size must be > 0";
                return false;
            }
            
            if (launch_coord_x < 0 || launch_coord_x > 1)
            {
                error = "launch_coord_x must be between 0 and 1";
                return false;
            }
            
            if (launch_coord_y < 0 || launch_coord_y > 1)
            {
                error = "launch_coord_y must be between 0 and 1";
                return false;
            }
            
            return true;
        }
    }

    /// <summary>
    /// Configuração de uma fase/momento do jogo
    /// Define como as bolas se comportam em um período
    /// </summary>
    [Serializable]
    public class PhaseConfig
    {
        public float minInt;      // intervalo mínimo entre spawns (segundos)
        public float maxInt;      // intervalo máximo entre spawns (segundos)
        public float minSpeed;    // velocidade mínima da bola
        public float maxSpeed;    // velocidade máxima da bola
        public float minSize;     // tamanho mínimo da bola
        public float maxSize;     // tamanho máximo da bola
        public float matureTime;  // tempo até bola ficar "madura" (ativa para hit)
        public float destroyTime; // tempo até bola se auto-destruir
        public int spatialDistIndex; // índice da distribuição espacial a usar
        
        // Direction mode configuration
        public string directionMode;        // "fixed", "range", "discrete" ou null/vazio (usa padrão)
        public float directionValue;       // Para "fixed": valor único (ex: 90)
        public float[] directionRange;     // Para "range": [min, max] (ex: [90, 120])
        public float[] directionDiscrete;  // Para "discrete": lista de valores (ex: [0, 15, 30, 45, 60])
        
        /// <summary>
        /// Valida se a configuração da fase está válida
        /// </summary>
        public bool IsValid(out string error)
        {
            error = "";
            
            if (minInt <= 0 || maxInt <= 0)
            {
                error = "minInt and maxInt must be > 0";
                return false;
            }
            
            if (minInt > maxInt)
            {
                error = $"minInt ({minInt}) cannot be > maxInt ({maxInt})";
                return false;
            }
            
            if (minSpeed < 0 || maxSpeed < 0)
            {
                error = "minSpeed and maxSpeed must be >= 0";
                return false;
            }
            
            if (minSpeed > maxSpeed)
            {
                error = $"minSpeed ({minSpeed}) cannot be > maxSpeed ({maxSpeed})";
                return false;
            }
            
            if (minSize <= 0 || maxSize <= 0)
            {
                error = "minSize and maxSize must be > 0";
                return false;
            }
            
            if (minSize > maxSize)
            {
                error = $"minSize ({minSize}) cannot be > maxSize ({maxSize})";
                return false;
            }
            
            if (matureTime < 0)
            {
                error = "matureTime must be >= 0";
                return false;
            }
            
            if (destroyTime <= 0)
            {
                error = "destroyTime must be > 0";
                return false;
            }
            
            if (spatialDistIndex < 0)
            {
                error = "spatialDistIndex must be >= 0";
                return false;
            }
            
            // Validação de direction-mode (opcional)
            if (!string.IsNullOrEmpty(directionMode))
            {
                if (directionMode == "fixed")
                {
                    if (directionValue < 0 || directionValue > 360)
                    {
                        error = "directionValue must be between 0 and 360";
                        return false;
                    }
                }
                else if (directionMode == "range")
                {
                    if (directionRange == null || directionRange.Length != 2)
                    {
                        error = "directionRange must have exactly 2 elements [min, max]";
                        return false;
                    }
                    if (directionRange[0] < 0 || directionRange[0] > 360 || directionRange[1] < 0 || directionRange[1] > 360)
                    {
                        error = "directionRange values must be between 0 and 360";
                        return false;
                    }
                    if (directionRange[0] > directionRange[1])
                    {
                        error = $"directionRange[0] ({directionRange[0]}) cannot be > directionRange[1] ({directionRange[1]})";
                        return false;
                    }
                }
                else if (directionMode == "discrete")
                {
                    if (directionDiscrete == null || directionDiscrete.Length == 0)
                    {
                        error = "directionDiscrete must have at least one value";
                        return false;
                    }
                    foreach (float val in directionDiscrete)
                    {
                        if (val < 0 || val > 360)
                        {
                            error = $"directionDiscrete value {val} must be between 0 and 360";
                            return false;
                        }
                    }
                }
                else
                {
                    error = $"Invalid directionMode: {directionMode}. Must be 'fixed', 'range', or 'discrete'";
                    return false;
                }
            }
            
            return true;
        }
    }

    /// <summary>
    /// Configuração completa de um nível
    /// Armazenada no Firebase: games/{gameId}/presets/{presetId}/levels/{levelId}
    /// </summary>
    [Serializable]
    public class LevelConfig
    {
        public int index;                              // ordem de aparição na tela
        public string name;                            // nome para exibição na tela
        public string description;
        public int totalTime;                           // duração total do jogo (segundos) - CRÍTICO
        public List<CoolDownPeriod> coolDown;           // períodos de cooldown com start_time e duration - OPCIONAL
        public int[] hitLandmarks;                      // landmarks que contam como hit [15, 16] - CRÍTICO
        public int[] destroyLandmarks;                  // landmarks que destroem bola [0] - CRÍTICO
        
        // Objetos especiais com propriedades completas - TODOS OPCIONAIS
        public List<SuperObject> areaHitSuper;          // objetos especiais tipo "hit"
        public List<SuperObject> areaAvoidSuper;        // objetos especiais tipo "avoid"
        public List<SuperObject> areaHitAllSuper;       // objetos especiais tipo "hit_all"
        public List<SuperObject> areaDestroyAllSuper;   // objetos especiais tipo "destroy_all"
        
        // Configurações procedurais - CRÍTICAS
        public SpatialDistribution[] spatialDistributions;  // distribuições espaciais disponíveis
        public PhaseConfig[] moments;                       // fases do jogo (chamado 'phases' no notebook)
        
        /// <summary>
        /// Valida se o LevelConfig tem todos os dados críticos para gerar um level
        /// </summary>
        public bool IsValidForLevelGeneration(out string error)
        {
            error = "";
            
            // CRÍTICO: totalTime deve ser > 0
            if (totalTime <= 0)
            {
                error = "totalTime must be > 0";
                return false;
            }
            
            // CRÍTICO: hitLandmarks deve existir e ter pelo menos 1 elemento
            if (hitLandmarks == null || hitLandmarks.Length == 0)
            {
                error = "hitLandmarks is required and cannot be empty";
                return false;
            }
            
            // destroyLandmarks é OPCIONAL - se vazio/null, não há colisão de destroy
            // O código já trata isso corretamente (PoseHitDetector verifica se está vazio antes de processar)
            
            // CRÍTICO: spatialDistributions deve existir e ter pelo menos 1 elemento
            if (spatialDistributions == null || spatialDistributions.Length == 0)
            {
                error = "spatialDistributions is required and cannot be empty";
                return false;
            }
            
            // CRÍTICO: moments deve existir e ter pelo menos 1 elemento
            if (moments == null || moments.Length == 0)
            {
                error = "moments is required and cannot be empty";
                return false;
            }
            
            // Valida cada distribuição espacial
            for (int i = 0; i < spatialDistributions.Length; i++)
            {
                if (spatialDistributions[i] == null)
                {
                    error = $"spatialDistributions[{i}] is null";
                    return false;
                }
                
                if (!spatialDistributions[i].IsValid(out string distError))
                {
                    error = $"spatialDistributions[{i}]: {distError}";
                    return false;
                }
            }
            
            // Valida cada momento/fase
            for (int i = 0; i < moments.Length; i++)
            {
                if (moments[i] == null)
                {
                    error = $"moments[{i}] is null";
                    return false;
                }
                
                if (!moments[i].IsValid(out string momentError))
                {
                    error = $"moments[{i}]: {momentError}";
                    return false;
                }
                
                // CRÍTICO: spatialDistIndex deve estar dentro dos bounds
                if (moments[i].spatialDistIndex >= spatialDistributions.Length)
                {
                    error = $"moments[{i}].spatialDistIndex ({moments[i].spatialDistIndex}) >= spatialDistributions.Length ({spatialDistributions.Length})";
                    return false;
                }
            }
            
            // Valida períodos de cooldown (opcional)
            if (coolDown != null)
            {
                for (int i = 0; i < coolDown.Count; i++)
                {
                    if (coolDown[i] == null)
                    {
                        error = $"coolDown[{i}] is null";
                        return false;
                    }
                    
                    if (!coolDown[i].IsValid(out string cooldownError))
                    {
                        error = $"coolDown[{i}]: {cooldownError}";
                        return false;
                    }
                }
            }
            
            // Valida objetos especiais (todos opcionais)
            if (areaHitSuper != null)
            {
                for (int i = 0; i < areaHitSuper.Count; i++)
                {
                    if (areaHitSuper[i] != null && !areaHitSuper[i].IsValid(out string hitSuperError))
                    {
                        error = $"areaHitSuper[{i}]: {hitSuperError}";
                        return false;
                    }
                }
            }
            
            if (areaAvoidSuper != null)
            {
                for (int i = 0; i < areaAvoidSuper.Count; i++)
                {
                    if (areaAvoidSuper[i] != null && !areaAvoidSuper[i].IsValid(out string avoidSuperError))
                    {
                        error = $"areaAvoidSuper[{i}]: {avoidSuperError}";
                        return false;
                    }
                }
            }
            
            if (areaHitAllSuper != null)
            {
                for (int i = 0; i < areaHitAllSuper.Count; i++)
                {
                    if (areaHitAllSuper[i] != null && !areaHitAllSuper[i].IsValid(out string hitAllSuperError))
                    {
                        error = $"areaHitAllSuper[{i}]: {hitAllSuperError}";
                        return false;
                    }
                }
            }
            
            if (areaDestroyAllSuper != null)
            {
                for (int i = 0; i < areaDestroyAllSuper.Count; i++)
                {
                    if (areaDestroyAllSuper[i] != null && !areaDestroyAllSuper[i].IsValid(out string destroyAllSuperError))
                    {
                        error = $"areaDestroyAllSuper[{i}]: {destroyAllSuperError}";
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}

