using System;
using System.Collections.Generic;
using UnityEngine;
using BubblesGame.Data;

namespace BubblesGame.Data
{
    public static class LevelPlanner
    {
        public static List<BallData> Build(
            LevelConfig config,
            int seed
        )
        {
            var rng = new System.Random(seed);
            var balls = new List<BallData>();

            int totalBalls = CalculateTotalBalls(config);
            Debug.Log($"[LevelPlanner] Calculated total balls: {totalBalls}");
            
            var slots = BuildSpatialSlots(config, totalBalls, rng);
            Debug.Log($"[LevelPlanner] Built spatial slots: {slots.Count}");

            if (slots.Count == 0)
            {
                Debug.LogError("[LevelPlanner] No spatial slots created! Check spatialDistributions configuration.");
                return balls;
            }

            Shuffle(slots, rng);

            float currentTime = 0f;
            int momentCount = config.moments.Length;
            int ballsPerMoment = slots.Count / momentCount;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                float delta = Rand(rng, slot.minInt, slot.maxInt);
                currentTime += delta;

                // Determina qual moment usar baseado na posição proporcional
                int momentIndex = Mathf.Min(i / ballsPerMoment, momentCount - 1);
                PhaseConfig moment = config.moments[momentIndex];

                balls.Add(BuildNormalBall(slot, currentTime, rng, config, moment));
            }

            InsertSupers(balls, config);

            balls.Sort((a, b) => a.LaunchTime.CompareTo(b.LaunchTime));
            Debug.Log($"[LevelPlanner] Generated {balls.Count} total balls (including supers)");
            return balls;
        }

        // ---------- core ----------

        static int CalculateTotalBalls(LevelConfig config)
        {
            int total = 0;
            
            if (config.moments == null || config.moments.Length == 0)
            {
                Debug.LogError("[LevelPlanner] No moments in config!");
                return 0;
            }
            
            float momentDuration = config.totalTime / config.moments.Length;
            Debug.Log($"[LevelPlanner] Moment duration: {momentDuration}s (totalTime: {config.totalTime}s, moments: {config.moments.Length})");

            foreach (var m in config.moments)
            {
                float avg = (m.minInt + m.maxInt) * 0.5f;
                int ballsForMoment = Mathf.FloorToInt(momentDuration / avg);
                Debug.Log($"[LevelPlanner] Moment - minInt: {m.minInt}, maxInt: {m.maxInt}, avg: {avg}, balls: {ballsForMoment}");
                total += ballsForMoment;
            }
            
            Debug.Log($"[LevelPlanner] Total balls calculated: {total}");
            return total;
        }

        static List<BallSlot> BuildSpatialSlots(
            LevelConfig config, int total, System.Random rng)
        {
            var slots = new List<BallSlot>();
            
            // Usa primeira distribuição espacial (config.spatialDistributions[0])
            if (config.spatialDistributions == null || config.spatialDistributions.Length == 0)
            {
                Debug.LogError("[LevelPlanner] No spatialDistributions in config!");
                return slots;
            }
                
            var spatialDist = config.spatialDistributions[0];
            
            if (spatialDist.xZone == null || spatialDist.xZone.Count == 0)
            {
                Debug.LogError("[LevelPlanner] spatialDist.xZone is null or empty!");
                return slots;
            }
            
            if (spatialDist.yZone == null || spatialDist.yZone.Count == 0)
            {
                Debug.LogError("[LevelPlanner] spatialDist.yZone is null or empty!");
                return slots;
            }
            
            Debug.Log($"[LevelPlanner] Building slots - xZone: {spatialDist.xZone.Count}, yZone: {spatialDist.yZone.Count}, totalBalls: {total}");

            for (int yIdx = 0; yIdx < spatialDist.yZone.Count; yIdx++)
            {
                var y = spatialDist.yZone[yIdx];
                float yWeight = yIdx < spatialDist.yDist.Length ? spatialDist.yDist[yIdx] : 1f;
                int ballsY = Mathf.RoundToInt(total * yWeight);

                for (int xIdx = 0; xIdx < spatialDist.xZone.Count; xIdx++)
                {
                    var x = spatialDist.xZone[xIdx];
                    float xWeight = xIdx < spatialDist.xDist.Length ? spatialDist.xDist[xIdx] : 1f;
                    int ballsXY = Mathf.RoundToInt(ballsY * xWeight);

                    for (int i = 0; i < ballsXY; i++)
                    {
                        slots.Add(new BallSlot
                        {
                            xMin = x.min,
                            xMax = x.max,
                            yMin = y.min,
                            yMax = y.max,
                            minInt = config.moments[0].minInt,
                            maxInt = config.moments[0].maxInt
                        });
                    }
                }
            }
            return slots;
        }

        static BallData BuildNormalBall(
            BallSlot slot,
            float launchTime,
            System.Random rng,
            LevelConfig config,
            PhaseConfig moment
        )
        {
            // Paleta padrão de cores (pode ser extraída do config no futuro)
            Color[] defaultPalette = new Color[] {
                new Color(0.2f, 0.7f, 1.0f, 1.0f),  // Azul
                new Color(1.0f, 0.4f, 0.3f, 1.0f),  // Vermelho
                new Color(0.3f, 1.0f, 0.4f, 1.0f),  // Verde
            };
            Color c = PickColor(defaultPalette, rng);

            return new BallData
            {
                launch_time = launchTime,
                launch_coord = new Vector2Serializable
                {
                    x = Rand(rng, slot.xMin, slot.xMax),
                    y = Rand(rng, slot.yMin, slot.yMax)
                },
                speed = Rand(rng, moment.minSpeed, moment.maxSpeed),
                size = Rand(rng, moment.minSize, moment.maxSize),
                direction = ResolveDirection(moment, rng),
                mature_time = moment.matureTime,
                destroy_time = launchTime + moment.destroyTime,
                hit_landmarks = config.hitLandmarks,
                destroy_landmarks = config.destroyLandmarks,
                object_type = "normal",
                collision_policy = "hit",
                color = new ColorSerializable
                {
                    r = c.r,
                    g = c.g,
                    b = c.b,
                    a = c.a
                }
            };
        }

        static void InsertSupers(
            List<BallData> balls,
            LevelConfig config
        )
        {
            // Processa areaHitSuper
            if (config.areaHitSuper != null)
            {
                foreach (var s in config.areaHitSuper)
                {
                    AddSuperBall(balls, s, "hit_super", config);
                }
            }

            // Processa areaAvoidSuper
            if (config.areaAvoidSuper != null)
            {
                foreach (var s in config.areaAvoidSuper)
                {
                    AddSuperBall(balls, s, "avoid_super", config);
                }
            }

            // Processa areaHitAllSuper
            if (config.areaHitAllSuper != null)
            {
                foreach (var s in config.areaHitAllSuper)
                {
                    AddSuperBall(balls, s, "hit_all_super", config);
                }
            }

            // Processa areaDestroyAllSuper
            if (config.areaDestroyAllSuper != null)
            {
                foreach (var s in config.areaDestroyAllSuper)
                {
                    AddSuperBall(balls, s, "destroy_all_super", config);
                }
            }
        }

        static void AddSuperBall(
            List<BallData> balls,
            SuperObject superObj,
            string objectType,
            LevelConfig config
        )
        {
            float t = superObj.time;

            // Evita conflito de launch_time
            while (balls.Exists(b => Mathf.Abs(b.LaunchTime - t) < 0.01f))
                t += 0.1f;

            var ballData = new BallData
            {
                launch_time = t,
                launch_coord = new Vector2Serializable
                {
                    x = superObj.launch_coord_x,
                    y = superObj.launch_coord_y
                },
                speed = superObj.speed,
                size = superObj.size,
                direction = superObj.direction,
                mature_time = config.moments[0].matureTime,  // Usa do momento atual
                destroy_time = t + config.moments[0].destroyTime,  // Adiciona ao launch_time
                hit_landmarks = config.hitLandmarks,
                destroy_landmarks = config.destroyLandmarks,
                object_type = objectType,
                collision_policy = objectType.Contains("avoid") ? "avoid" : "hit",
                color = new ColorSerializable
                {
                    r = 1f,
                    g = 1f,
                    b = 1f,
                    a = 0f  // Transparente para super balls
                }
            };

            balls.Add(ballData);
        }

        // ---------- helpers ----------

        static float ResolveDirection(PhaseConfig m, System.Random rng)
        {
            if (m.directionMode == "fixed")
                return m.directionValue;

            if (m.directionMode == "range" && m.directionRange != null && m.directionRange.Length == 2)
                return Rand(rng, m.directionRange[0], m.directionRange[1]);

            if (m.directionMode == "discrete" && m.directionDiscrete != null && m.directionDiscrete.Length > 0)
            {
                int i = rng.Next(m.directionDiscrete.Length);
                return m.directionDiscrete[i];
            }

            // Fallback: direção padrão
            return 90f;
        }

        static Color PickColor(Color[] palette, System.Random rng)
        {
            if (palette == null || palette.Length == 0)
                return Color.white;
                
            int i = rng.Next(palette.Length);
            return palette[i];
        }


        static float Rand(System.Random rng, float min, float max)
            => (float)(min + rng.NextDouble() * (max - min));

        static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    class BallSlot
    {
        public float xMin, xMax, yMin, yMax;
        public float minInt, maxInt;
    }
}
