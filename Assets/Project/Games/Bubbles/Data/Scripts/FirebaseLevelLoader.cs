using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;
using BubblesGame.Data;

namespace BubblesGame.Firebase
{
    public class FirebaseLevelLoader : MonoBehaviour
    {
        private FirebaseFirestore db => FirebaseFirestore.DefaultInstance;

        public async Task<LevelData> LoadLevel(
            string presetId,
            string levelId
        )
        {
            var doc = await db
                .Collection("games")
                .Document("bubbles")
                .Collection("presets")
                .Document(presetId)
                .Collection("levels")
                .Document(levelId)
                .GetSnapshotAsync();

            if (!doc.Exists)
                return null;

            return doc.ConvertTo<LevelData>();
        }

        public void LoadLevelConfig(
            string gameId,
            string presetId,
            string levelId,
            Action<LevelConfig> onSuccess,
            Action<string> onError
        )
        {
            db.Collection("games")
                .Document(gameId)
                .Collection("presets")
                .Document(presetId)
                .Collection("levels")
                .Document(levelId)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(task.Exception?.GetBaseException().Message ?? "Unknown error");
                        return;
                    }

                    var doc = task.Result;
                    if (!doc.Exists)
                    {
                        onError?.Invoke($"Level {levelId} not found");
                        return;
                    }

                    try
                    {
                        // Conversão manual usando ToDictionary (LevelConfig não tem atributos Firestore)
                        var data = doc.ToDictionary();
                        Debug.Log($"[FirebaseLevelLoader] Converting LevelConfig - Keys in document: {string.Join(", ", data.Keys)}");
                        LevelConfig config = ConvertDictionaryToLevelConfig(data, doc.Id);
                        Debug.Log($"[FirebaseLevelLoader] Conversion complete - spatialDistributions: {config.spatialDistributions?.Length ?? 0}, moments: {config.moments?.Length ?? 0}");
                        onSuccess?.Invoke(config);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[FirebaseLevelLoader] Exception during conversion: {e}");
                        onError?.Invoke($"Error converting to LevelConfig: {e.Message}");
                    }
                });
        }

        public void LoadAllPresets(
            string gameId,
            Action<List<PresetInfo>> onSuccess,
            Action<string> onError
        )
        {
            db.Collection("games")
                .Document(gameId)
                .Collection("presets")
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(task.Exception?.GetBaseException().Message ?? "Unknown error");
                        return;
                    }

                    var snapshot = task.Result;
                    var presets = new List<PresetInfo>();

                    foreach (var doc in snapshot.Documents)
                    {
                        try
                        {
                            var data = doc.ToDictionary();
                            string presetId = doc.Id;
                            string description = data.ContainsKey("description") ? data["description"].ToString() : "";
                            int version = data.ContainsKey("version") ? Convert.ToInt32(data["version"]) : 1;
                            
                            presets.Add(new PresetInfo(presetId, description, version));
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[FirebaseLevelLoader] Error parsing preset {doc.Id}: {e.Message}");
                        }
                    }

                    onSuccess?.Invoke(presets);
                });
        }

        public void LoadLevels(
            string gameId,
            string presetId,
            Action<List<LevelData>> onSuccess,
            Action<string> onError
        )
        {
            db.Collection("games")
                .Document(gameId)
                .Collection("presets")
                .Document(presetId)
                .Collection("levels")
                .GetSnapshotAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        onError?.Invoke(task.Exception?.GetBaseException().Message ?? "Unknown error");
                        return;
                    }

                    var snapshot = task.Result;
                    var levels = new List<LevelData>();

                    foreach (var doc in snapshot.Documents)
                    {
                        try
                        {
                            // Carrega apenas metadados básicos para o menu (sem LevelConfig completo)
                            var data = doc.ToDictionary();
                            
                            var level = new LevelData
                            {
                                index = data.ContainsKey("index") ? Convert.ToInt32(data["index"]) : ExtractIndexFromDoc(doc),
                                levelId = doc.Id,
                                name = data.ContainsKey("name") ? data["name"].ToString() : 
                                       data.ContainsKey("description") ? data["description"].ToString() : doc.Id,
                                description = data.ContainsKey("description") ? data["description"].ToString() : "",
                                totalTime = data.ContainsKey("totalTime") ? Convert.ToInt32(data["totalTime"]) : 0,
                                balls = new List<BallData>() // Vazio - será gerado pelo LevelPlanner quando necessário
                            };
                            
                            levels.Add(level);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[FirebaseLevelLoader] Error parsing level {doc.Id}: {e.Message}");
                        }
                    }

                    // Ordena por index
                    levels.Sort((a, b) => a.index.CompareTo(b.index));

                    onSuccess?.Invoke(levels);
                });
        }


        private int ExtractIndexFromDoc(DocumentSnapshot doc)
        {
            // Tenta extrair index do documento
            var data = doc.ToDictionary();
            
            if (data.ContainsKey("index"))
            {
                return Convert.ToInt32(data["index"]);
            }
            
            // Tenta extrair do doc.Id (ex: "level_01" -> 1, "level_1" -> 1)
            string docId = doc.Id;
            if (docId.StartsWith("level_"))
            {
                string numStr = docId.Substring(6);
                if (int.TryParse(numStr, out int index))
                {
                    return index;
                }
            }
            
            // Fallback: usa hash do docId
            return Math.Abs(docId.GetHashCode());
        }

        /// <summary>
        /// Converte Dictionary do Firestore para LevelConfig
        /// Usa conversão manual porque LevelConfig não tem atributos Firestore
        /// </summary>
        private LevelConfig ConvertDictionaryToLevelConfig(Dictionary<string, object> data, string docId)
        {
            var config = new LevelConfig();

            // Campos básicos
            config.index = data.ContainsKey("index") ? Convert.ToInt32(data["index"]) : ExtractIndexFromDocId(docId);
            config.name = data.ContainsKey("name") ? data["name"].ToString() : 
                         (data.ContainsKey("description") ? data["description"].ToString() : docId);
            config.description = data.ContainsKey("description") ? data["description"].ToString() : "";
            config.totalTime = data.ContainsKey("totalTime") ? Convert.ToInt32(data["totalTime"]) : 0;

            // hitLandmarks e destroyLandmarks
            // O Firestore pode ter "landmarks" como objeto com "hit" e "destroy", ou campos separados
            if (data.ContainsKey("hitLandmarks"))
            {
                var hitList = data["hitLandmarks"] as List<object>;
                if (hitList != null)
                {
                    config.hitLandmarks = hitList.ConvertAll(x => Convert.ToInt32(x)).ToArray();
                }
            }
            else if (data.ContainsKey("landmarks"))
            {
                // Tenta extrair de um objeto "landmarks" com "hit" e "destroy"
                var landmarksObj = data["landmarks"] as Dictionary<string, object>;
                if (landmarksObj != null && landmarksObj.ContainsKey("hit"))
                {
                    var hitList = landmarksObj["hit"] as List<object>;
                    if (hitList != null)
                    {
                        config.hitLandmarks = hitList.ConvertAll(x => Convert.ToInt32(x)).ToArray();
                    }
                }
            }

            if (data.ContainsKey("destroyLandmarks"))
            {
                var destroyList = data["destroyLandmarks"] as List<object>;
                if (destroyList != null)
                {
                    config.destroyLandmarks = destroyList.ConvertAll(x => Convert.ToInt32(x)).ToArray();
                }
            }
            else if (data.ContainsKey("landmarks"))
            {
                // Tenta extrair de um objeto "landmarks" com "hit" e "destroy"
                var landmarksObj = data["landmarks"] as Dictionary<string, object>;
                if (landmarksObj != null && landmarksObj.ContainsKey("destroy"))
                {
                    var destroyList = landmarksObj["destroy"] as List<object>;
                    if (destroyList != null)
                    {
                        config.destroyLandmarks = destroyList.ConvertAll(x => Convert.ToInt32(x)).ToArray();
                    }
                }
            }

            // Inicializa destroyLandmarks como array vazio se não encontrou (é opcional)
            if (config.destroyLandmarks == null)
            {
                config.destroyLandmarks = new int[0];
            }
            // hitLandmarks é obrigatório - se não encontrou, será validado depois

            // coolDown - verifica tanto "coolDown" quanto "cooldowns"
            if (data.ContainsKey("coolDown"))
            {
                var cooldownList = data["coolDown"] as List<object>;
                if (cooldownList != null)
                {
                    config.coolDown = new List<CoolDownPeriod>();
                    foreach (var item in cooldownList)
                    {
                        var dict = item as Dictionary<string, object>;
                        if (dict != null)
                        {
                            config.coolDown.Add(new CoolDownPeriod(
                                Convert.ToInt32(dict["start_time"]),
                                Convert.ToInt32(dict["duration"])
                            ));
                        }
                    }
                }
            }
            else if (data.ContainsKey("cooldowns"))
            {
                var cooldownList = data["cooldowns"] as List<object>;
                if (cooldownList != null)
                {
                    config.coolDown = new List<CoolDownPeriod>();
                    foreach (var item in cooldownList)
                    {
                        var dict = item as Dictionary<string, object>;
                        if (dict != null)
                        {
                            config.coolDown.Add(new CoolDownPeriod(
                                Convert.ToInt32(dict["start_time"]),
                                Convert.ToInt32(dict["duration"])
                            ));
                        }
                    }
                }
            }

            // spatialDistributions - verifica tanto "spatialDistributions" (plural) quanto "spatialDistribution" (singular)
            List<object> distList = null;
            if (data.ContainsKey("spatialDistributions"))
            {
                distList = data["spatialDistributions"] as List<object>;
                Debug.Log($"[FirebaseLevelLoader] Found 'spatialDistributions' (plural) - Type: {data["spatialDistributions"]?.GetType()}, Count: {(distList?.Count ?? 0)}");
            }
            else if (data.ContainsKey("spatialDistribution"))
            {
                // Se for singular, pode ser um objeto único ou uma lista com um único elemento
                var singleDist = data["spatialDistribution"];
                Debug.Log($"[FirebaseLevelLoader] Found 'spatialDistribution' (singular) - Type: {singleDist?.GetType()}");
                
                if (singleDist is List<object>)
                {
                    distList = singleDist as List<object>;
                    Debug.Log($"[FirebaseLevelLoader] spatialDistribution is List<object> with {distList.Count} items");
                }
                else if (singleDist is Dictionary<string, object>)
                {
                    // Se for um objeto único, cria uma lista com ele
                    distList = new List<object> { singleDist };
                    Debug.Log($"[FirebaseLevelLoader] spatialDistribution is Dictionary<string, object>, wrapping in list");
                    
                    // Log das chaves do dicionário para debug
                    var distDict = singleDist as Dictionary<string, object>;
                    Debug.Log($"[FirebaseLevelLoader] spatialDistribution keys: {string.Join(", ", distDict.Keys)}");
                }
                else
                {
                    Debug.LogWarning($"[FirebaseLevelLoader] spatialDistribution is unexpected type: {singleDist?.GetType()}");
                }
            }
            else
            {
                Debug.LogWarning("[FirebaseLevelLoader] Neither 'spatialDistributions' nor 'spatialDistribution' found in document!");
            }

            if (distList != null && distList.Count > 0)
            {
                var distributions = new List<SpatialDistribution>();
                foreach (var item in distList)
                {
                    var dict = item as Dictionary<string, object>;
                    if (dict != null)
                    {
                        Debug.Log($"[FirebaseLevelLoader] Processing spatialDistribution item - Keys: {string.Join(", ", dict.Keys)}");
                        
                        var dist = new SpatialDistribution();
                        dist.name = dict.ContainsKey("name") ? dict["name"].ToString() : "";

                        // xZone: List<ZoneRange> - aceita tanto "xZone" quanto "xZones"
                        // Firestore usa: x_start_zone, x_end_zone, x_weight
                        string xZoneKey = dict.ContainsKey("xZone") ? "xZone" : (dict.ContainsKey("xZones") ? "xZones" : null);
                        if (xZoneKey != null)
                        {
                            var xZoneList = dict[xZoneKey] as List<object>;
                            Debug.Log($"[FirebaseLevelLoader] {xZoneKey} found - Type: {dict[xZoneKey]?.GetType()}, IsList: {xZoneList != null}, Count: {(xZoneList?.Count ?? 0)}");
                            
                            if (xZoneList != null && xZoneList.Count > 0)
                            {
                                dist.xZone = new List<ZoneRange>();
                                var xDistList = new List<float>();
                                
                                foreach (var zoneObj in xZoneList)
                                {
                                    var zoneDict = zoneObj as Dictionary<string, object>;
                                    if (zoneDict != null)
                                    {
                                        // Tenta formato Firestore: x_start_zone, x_end_zone, x_weight
                                        if (zoneDict.ContainsKey("x_start_zone") && zoneDict.ContainsKey("x_end_zone"))
                                        {
                                            float start = Convert.ToSingle(zoneDict["x_start_zone"]);
                                            float end = Convert.ToSingle(zoneDict["x_end_zone"]);
                                            dist.xZone.Add(new ZoneRange(start, end));
                                            
                                            // Extrai weight se existir
                                            if (zoneDict.ContainsKey("x_weight"))
                                            {
                                                xDistList.Add(Convert.ToSingle(zoneDict["x_weight"]));
                                            }
                                        }
                                        // Tenta formato código: min, max
                                        else if (zoneDict.ContainsKey("min") && zoneDict.ContainsKey("max"))
                                        {
                                            dist.xZone.Add(new ZoneRange(
                                                Convert.ToSingle(zoneDict["min"]),
                                                Convert.ToSingle(zoneDict["max"])
                                            ));
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"[FirebaseLevelLoader] Invalid {xZoneKey} item - Keys: {string.Join(", ", zoneDict.Keys)}");
                                        }
                                    }
                                }
                                
                                // Converte List<float> para float[]
                                if (xDistList.Count > 0)
                                {
                                    dist.xDist = xDistList.ToArray();
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[FirebaseLevelLoader] Neither 'xZone' nor 'xZones' found in spatialDistribution!");
                        }

                        // yZone: List<ZoneRange> - aceita tanto "yZone" quanto "yZones"
                        // Firestore usa: y_start_zone, y_end_zone, y_weight
                        string yZoneKey = dict.ContainsKey("yZone") ? "yZone" : (dict.ContainsKey("yZones") ? "yZones" : null);
                        if (yZoneKey != null)
                        {
                            var yZoneList = dict[yZoneKey] as List<object>;
                            Debug.Log($"[FirebaseLevelLoader] {yZoneKey} found - Type: {dict[yZoneKey]?.GetType()}, IsList: {yZoneList != null}, Count: {(yZoneList?.Count ?? 0)}");
                            
                            if (yZoneList != null && yZoneList.Count > 0)
                            {
                                dist.yZone = new List<ZoneRange>();
                                var yDistList = new List<float>();
                                
                                foreach (var zoneObj in yZoneList)
                                {
                                    var zoneDict = zoneObj as Dictionary<string, object>;
                                    if (zoneDict != null)
                                    {
                                        // Tenta formato Firestore: y_start_zone, y_end_zone, y_weight
                                        if (zoneDict.ContainsKey("y_start_zone") && zoneDict.ContainsKey("y_end_zone"))
                                        {
                                            float start = Convert.ToSingle(zoneDict["y_start_zone"]);
                                            float end = Convert.ToSingle(zoneDict["y_end_zone"]);
                                            dist.yZone.Add(new ZoneRange(start, end));
                                            
                                            // Extrai weight se existir
                                            if (zoneDict.ContainsKey("y_weight"))
                                            {
                                                yDistList.Add(Convert.ToSingle(zoneDict["y_weight"]));
                                            }
                                        }
                                        // Tenta formato código: min, max
                                        else if (zoneDict.ContainsKey("min") && zoneDict.ContainsKey("max"))
                                        {
                                            dist.yZone.Add(new ZoneRange(
                                                Convert.ToSingle(zoneDict["min"]),
                                                Convert.ToSingle(zoneDict["max"])
                                            ));
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"[FirebaseLevelLoader] Invalid {yZoneKey} item - Keys: {string.Join(", ", zoneDict.Keys)}");
                                        }
                                    }
                                }
                                
                                // Converte List<float> para float[]
                                if (yDistList.Count > 0)
                                {
                                    dist.yDist = yDistList.ToArray();
                                }
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[FirebaseLevelLoader] Neither 'yZone' nor 'yZones' found in spatialDistribution!");
                        }

                        // xDist: float[] - se não foi extraído dos xZones, tenta campo separado
                        if (dist.xDist == null || dist.xDist.Length == 0)
                        {
                            if (dict.ContainsKey("xDist"))
                            {
                                var xDistList = dict["xDist"] as List<object>;
                                if (xDistList != null && xDistList.Count > 0)
                                {
                                    dist.xDist = xDistList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                }
                            }
                        }

                        // yDist: float[] - se não foi extraído dos yZones, tenta campo separado
                        if (dist.yDist == null || dist.yDist.Length == 0)
                        {
                            if (dict.ContainsKey("yDist"))
                            {
                                var yDistList = dict["yDist"] as List<object>;
                                if (yDistList != null && yDistList.Count > 0)
                                {
                                    dist.yDist = yDistList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                }
                            }
                        }

                        // Log do resultado antes de validar
                        Debug.Log($"[FirebaseLevelLoader] SpatialDistribution parsed - xZone: {dist.xZone?.Count ?? 0}, yZone: {dist.yZone?.Count ?? 0}, xDist: {dist.xDist?.Length ?? 0}, yDist: {dist.yDist?.Length ?? 0}");

                        // Só adiciona se tiver dados válidos
                        if (dist.xZone != null && dist.xZone.Count > 0 && 
                            dist.yZone != null && dist.yZone.Count > 0 &&
                            dist.xDist != null && dist.xDist.Length > 0 &&
                            dist.yDist != null && dist.yDist.Length > 0)
                        {
                            distributions.Add(dist);
                            Debug.Log("[FirebaseLevelLoader] SpatialDistribution added successfully!");
                        }
                        else
                        {
                            Debug.LogError($"[FirebaseLevelLoader] SpatialDistribution validation failed - xZone: {dist.xZone?.Count ?? 0}, yZone: {dist.yZone?.Count ?? 0}, xDist: {dist.xDist?.Length ?? 0}, yDist: {dist.yDist?.Length ?? 0}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[FirebaseLevelLoader] spatialDistribution item is not Dictionary - Type: {item?.GetType()}");
                    }
                }
                config.spatialDistributions = distributions.Count > 0 ? distributions.ToArray() : null;
                Debug.Log($"[FirebaseLevelLoader] Final spatialDistributions count: {config.spatialDistributions?.Length ?? 0}");
            }
            
            if (config.spatialDistributions == null || config.spatialDistributions.Length == 0)
            {
                Debug.LogWarning("[FirebaseLevelLoader] spatialDistributions is null or empty after conversion!");
            }

            // moments (array de PhaseConfig)
            if (data.ContainsKey("moments"))
            {
                var momentsList = data["moments"] as List<object>;
                if (momentsList != null && momentsList.Count > 0)
                {
                    var moments = new List<PhaseConfig>();
                    foreach (var item in momentsList)
                    {
                        var dict = item as Dictionary<string, object>;
                        if (dict != null)
                        {
                            var moment = new PhaseConfig();
                            
                            // Mapeia campos snake_case do Firestore para camelCase do código
                            moment.minInt = dict.ContainsKey("minInt") ? Convert.ToSingle(dict["minInt"]) : 
                                          (dict.ContainsKey("min_int") ? Convert.ToSingle(dict["min_int"]) : 0f);
                            moment.maxInt = dict.ContainsKey("maxInt") ? Convert.ToSingle(dict["maxInt"]) : 
                                          (dict.ContainsKey("max_int") ? Convert.ToSingle(dict["max_int"]) : 0f);
                            moment.minSpeed = dict.ContainsKey("minSpeed") ? Convert.ToSingle(dict["minSpeed"]) : 
                                            (dict.ContainsKey("min_speed") ? Convert.ToSingle(dict["min_speed"]) : 0f);
                            moment.maxSpeed = dict.ContainsKey("maxSpeed") ? Convert.ToSingle(dict["maxSpeed"]) : 
                                            (dict.ContainsKey("max_speed") ? Convert.ToSingle(dict["max_speed"]) : 0f);
                            moment.minSize = dict.ContainsKey("minSize") ? Convert.ToSingle(dict["minSize"]) : 
                                           (dict.ContainsKey("min_size") ? Convert.ToSingle(dict["min_size"]) : 0f);
                            moment.maxSize = dict.ContainsKey("maxSize") ? Convert.ToSingle(dict["maxSize"]) : 
                                           (dict.ContainsKey("max_size") ? Convert.ToSingle(dict["max_size"]) : 0f);
                            moment.matureTime = dict.ContainsKey("matureTime") ? Convert.ToSingle(dict["matureTime"]) : 
                                              (dict.ContainsKey("mature_time") ? Convert.ToSingle(dict["mature_time"]) : 0f);
                            moment.destroyTime = dict.ContainsKey("destroyTime") ? Convert.ToSingle(dict["destroyTime"]) : 
                                               (dict.ContainsKey("destroy_time") ? Convert.ToSingle(dict["destroy_time"]) : 0f);
                            moment.spatialDistIndex = dict.ContainsKey("spatialDistIndex") ? Convert.ToInt32(dict["spatialDistIndex"]) : 
                                                     (dict.ContainsKey("spatial_dist_index") ? Convert.ToInt32(dict["spatial_dist_index"]) : 0);

                            // Direction mode - Firestore usa "direction_mode"
                            moment.directionMode = dict.ContainsKey("directionMode") ? dict["directionMode"].ToString() : 
                                                  (dict.ContainsKey("direction_mode") ? dict["direction_mode"].ToString() : null);
                            
                            // Direction value - Firestore pode ter "direction_value" como número ou array
                            if (dict.ContainsKey("directionValue"))
                            {
                                var dirVal = dict["directionValue"];
                                if (dirVal is List<object>)
                                {
                                    // Se for array, é "discrete" ou "range"
                                    var dirList = dirVal as List<object>;
                                    if (moment.directionMode == "discrete")
                                    {
                                        moment.directionDiscrete = dirList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                    }
                                    else if (moment.directionMode == "range")
                                    {
                                        moment.directionRange = dirList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                    }
                                }
                                else
                                {
                                    moment.directionValue = Convert.ToSingle(dirVal);
                                }
                            }
                            else if (dict.ContainsKey("direction_value"))
                            {
                                var dirVal = dict["direction_value"];
                                if (dirVal is List<object>)
                                {
                                    // Se for array, é "discrete" ou "range"
                                    var dirList = dirVal as List<object>;
                                    if (moment.directionMode == "discrete")
                                    {
                                        moment.directionDiscrete = dirList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                    }
                                    else if (moment.directionMode == "range")
                                    {
                                        moment.directionRange = dirList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                    }
                                }
                                else
                                {
                                    moment.directionValue = Convert.ToSingle(dirVal);
                                }
                            }
                            
                            // Fallback para campos separados (formato código)
                            if (dict.ContainsKey("directionRange"))
                            {
                                var rangeList = dict["directionRange"] as List<object>;
                                if (rangeList != null && rangeList.Count > 0)
                                {
                                    moment.directionRange = rangeList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                }
                            }

                            if (dict.ContainsKey("directionDiscrete"))
                            {
                                var discreteList = dict["directionDiscrete"] as List<object>;
                                if (discreteList != null && discreteList.Count > 0)
                                {
                                    moment.directionDiscrete = discreteList.ConvertAll(x => Convert.ToSingle(x)).ToArray();
                                }
                            }

                            moments.Add(moment);
                        }
                    }
                    config.moments = moments.Count > 0 ? moments.ToArray() : null;
                }
            }
            
            if (config.moments == null || config.moments.Length == 0)
            {
                Debug.LogWarning("[FirebaseLevelLoader] moments is null or empty after conversion!");
            }

            // SuperObjects (opcionais) - Firestore usa "superObjects" como objeto com chaves
            if (data.ContainsKey("superObjects"))
            {
                var superObjDict = data["superObjects"] as Dictionary<string, object>;
                if (superObjDict != null)
                {
                    config.areaHitSuper = ConvertSuperObjectList(superObjDict, "areaHitSuper");
                    config.areaAvoidSuper = ConvertSuperObjectList(superObjDict, "areaAvoidSuper");
                    config.areaHitAllSuper = ConvertSuperObjectList(superObjDict, "areaHitAllSuper");
                    config.areaDestroyAllSuper = ConvertSuperObjectList(superObjDict, "areaDestroyAllSuper");
                }
            }
            else
            {
                // Fallback para formato direto (se existir)
                config.areaHitSuper = ConvertSuperObjectList(data, "areaHitSuper");
                config.areaAvoidSuper = ConvertSuperObjectList(data, "areaAvoidSuper");
                config.areaHitAllSuper = ConvertSuperObjectList(data, "areaHitAllSuper");
                config.areaDestroyAllSuper = ConvertSuperObjectList(data, "areaDestroyAllSuper");
            }

            return config;
        }

        private List<SuperObject> ConvertSuperObjectList(Dictionary<string, object> data, string key)
        {
            if (!data.ContainsKey(key))
                return null;

            var list = data[key] as List<object>;
            if (list == null)
                return null;

            var result = new List<SuperObject>();
            foreach (var item in list)
            {
                var dict = item as Dictionary<string, object>;
                if (dict != null)
                {
                    // Mapeia campos do Firestore (snake_case) para o formato do código
                    result.Add(new SuperObject(
                        dict.ContainsKey("time") ? Convert.ToSingle(dict["time"]) : 
                        (dict.ContainsKey("launch_time") ? Convert.ToSingle(dict["launch_time"]) : 0f),
                        dict.ContainsKey("speed") ? Convert.ToSingle(dict["speed"]) : 0f,
                        dict.ContainsKey("size") ? Convert.ToSingle(dict["size"]) : 0f,
                        dict.ContainsKey("direction") ? Convert.ToSingle(dict["direction"]) : 0f,
                        dict.ContainsKey("launch_coord_x") ? Convert.ToSingle(dict["launch_coord_x"]) : 0f,
                        dict.ContainsKey("launch_coord_y") ? Convert.ToSingle(dict["launch_coord_y"]) : 0f
                    ));
                }
            }
            return result.Count > 0 ? result : null;
        }

        private int ExtractIndexFromDocId(string docId)
        {
            if (docId.StartsWith("level_"))
            {
                string numStr = docId.Substring(6);
                if (int.TryParse(numStr, out int index))
                {
                    return index;
                }
            }
            return Math.Abs(docId.GetHashCode());
        }
    }
}
