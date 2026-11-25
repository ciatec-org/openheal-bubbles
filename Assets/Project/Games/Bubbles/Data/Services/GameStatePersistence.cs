using System.Collections.Generic;
using UnityEngine;
using BubblesGame.Data;

namespace BubblesGame
{
    /// <summary>
    /// Gerencia persistência do estado do jogo (ball-list e cooldown) entre partidas
    /// </summary>
    public static class GameStatePersistence
    {
        private const string KEY_PRESET_ID = "BubblesGame_SavedPresetId";
        private const string KEY_LEVEL_ID = "BubblesGame_SavedLevelId";
        private const string KEY_BALL_LIST_COUNT = "BubblesGame_BallListCount";
        private const string KEY_COOLDOWN_COUNT = "BubblesGame_CooldownCount";
        private const string KEY_BALL_LIST_PREFIX = "BubblesGame_Ball_";
        private const string KEY_COOLDOWN_PREFIX = "BubblesGame_Cooldown_";
        private const string KEY_HIT_COUNT = "BubblesGame_HitCount";

        /// <summary>
        /// Inicializador estático - registra callback de quit automaticamente
        /// </summary>
        static GameStatePersistence()
        {
            Application.quitting += OnApplicationQuitting;
        }
        /// <summary>
        /// Salva o estado atual do jogo (preset, level, ball-list e cooldown)
        /// </summary>
        public static void SaveState(string presetId, string levelId, List<BallData> ballList, List<CoolDownPeriod> cooldownPeriods, int totalHits)
        {
            if (string.IsNullOrEmpty(presetId) || string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("[GameStatePersistence] Cannot save state - presetId or levelId is empty");
                return;
            }

            // Salva preset e level
            PlayerPrefs.SetString(KEY_PRESET_ID, presetId);
            PlayerPrefs.SetString(KEY_LEVEL_ID, levelId);

            // Salva ball-list
            if (ballList != null && ballList.Count > 0)
            {
                PlayerPrefs.SetInt(KEY_BALL_LIST_COUNT, ballList.Count);
                PlayerPrefs.SetInt(KEY_HIT_COUNT, totalHits);

                // Salva cada bola como JSON (limitado a 1000 bolas por limitação do PlayerPrefs)
                int maxBalls = Mathf.Min(ballList.Count, 1000);
                for (int i = 0; i < maxBalls; i++)
                {
                    string json = JsonUtility.ToJson(ballList[i]);
                    PlayerPrefs.SetString($"{KEY_BALL_LIST_PREFIX}{i}", json);
                }
                
                Debug.Log($"[GameStatePersistence] Saved {maxBalls} balls");
            }
            else
            {
                PlayerPrefs.SetInt(KEY_BALL_LIST_COUNT, 0);
                PlayerPrefs.SetInt(KEY_HIT_COUNT, 0);
            }

            // Salva cooldown periods
            if (cooldownPeriods != null && cooldownPeriods.Count > 0)
            {
                PlayerPrefs.SetInt(KEY_COOLDOWN_COUNT, cooldownPeriods.Count);
                
                for (int i = 0; i < cooldownPeriods.Count; i++)
                {
                    string json = JsonUtility.ToJson(cooldownPeriods[i]);
                    PlayerPrefs.SetString($"{KEY_COOLDOWN_PREFIX}{i}", json);
                }
                
                Debug.Log($"[GameStatePersistence] Saved {cooldownPeriods.Count} cooldown periods");
            }
            else
            {
                PlayerPrefs.SetInt(KEY_COOLDOWN_COUNT, 0);
            }

            PlayerPrefs.Save();
            Debug.Log($"[GameStatePersistence] State saved - Preset: {presetId}, Level: {levelId}");
        }

        /// <summary>
        /// Carrega o estado salvo do jogo
        /// </summary>
        public static SavedGameState LoadState()
        {
            if (!HasSavedState())
            {
                return null;
            }

            string presetId = PlayerPrefs.GetString(KEY_PRESET_ID);
            string levelId = PlayerPrefs.GetString(KEY_LEVEL_ID);

            if (string.IsNullOrEmpty(presetId) || string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("[GameStatePersistence] Saved state is incomplete");
                return null;
            }

            // Carrega ball-list
            List<BallData> ballList = new List<BallData>();
            int ballCount = PlayerPrefs.GetInt(KEY_BALL_LIST_COUNT, 0);
            for (int i = 0; i < ballCount; i++)
            {
                string json = PlayerPrefs.GetString($"{KEY_BALL_LIST_PREFIX}{i}", "");
                if (!string.IsNullOrEmpty(json))
                {
                    BallData ball = JsonUtility.FromJson<BallData>(json);
                    if (ball != null)
                    {
                        ballList.Add(ball);
                    }
                }
            }

            // Carrega cooldown periods
            List<CoolDownPeriod> cooldownPeriods = new List<CoolDownPeriod>();
            int cooldownCount = PlayerPrefs.GetInt(KEY_COOLDOWN_COUNT, 0);
            for (int i = 0; i < cooldownCount; i++)
            {
                string json = PlayerPrefs.GetString($"{KEY_COOLDOWN_PREFIX}{i}", "");
                if (!string.IsNullOrEmpty(json))
                {
                    CoolDownPeriod period = JsonUtility.FromJson<CoolDownPeriod>(json);
                    if (period != null)
                    {
                        cooldownPeriods.Add(period);
                    }
                }
            }

            Debug.Log($"[GameStatePersistence] State loaded - Preset: {presetId}, Level: {levelId}, Balls: {ballList.Count}, Cooldowns: {cooldownPeriods.Count}");

            return new SavedGameState
            {
                presetId = presetId,
                levelId = levelId,
                ballList = ballList,
                cooldownPeriods = cooldownPeriods,
            };
        }

        public static MatchResult LoadMatchResults()
        {
            if (!HasSavedState())
            {
                return null;
            }

            if(!PlayerPrefs.HasKey(KEY_BALL_LIST_COUNT))
            {
                Debug.LogWarning("[GameStatePersistence] No ball list saved");
                return null;
            }

            if(!PlayerPrefs.HasKey(KEY_HIT_COUNT))
            {
                Debug.LogWarning("[GameStatePersistence] No hit count saved");
                return null;
            }

            int hitCount = PlayerPrefs.GetInt(KEY_HIT_COUNT, 0);
            int ballCount = PlayerPrefs.GetInt(KEY_BALL_LIST_COUNT, 0);

            return new MatchResult
            {
                totalHits = hitCount,
                totalBalls = ballCount,
                
            };
        }

        /// <summary>
        /// Verifica se há estado salvo
        /// </summary>
        public static bool HasSavedState()
        {
            return PlayerPrefs.HasKey(KEY_PRESET_ID) && PlayerPrefs.HasKey(KEY_LEVEL_ID);
        }

        /// <summary>
        /// Limpa o estado salvo
        /// </summary>
        public static void ClearState()
        {
            // Remove preset e level
            PlayerPrefs.DeleteKey(KEY_PRESET_ID);
            PlayerPrefs.DeleteKey(KEY_LEVEL_ID);

            // Remove ball-list
            int ballCount = PlayerPrefs.GetInt(KEY_BALL_LIST_COUNT, 0);
            for (int i = 0; i < ballCount; i++)
            {
                PlayerPrefs.DeleteKey($"{KEY_BALL_LIST_PREFIX}{i}");
            }
            PlayerPrefs.DeleteKey(KEY_BALL_LIST_COUNT);
            PlayerPrefs.DeleteKey(KEY_HIT_COUNT);

            // Remove cooldown periods
            int cooldownCount = PlayerPrefs.GetInt(KEY_COOLDOWN_COUNT, 0);
            for (int i = 0; i < cooldownCount; i++)
            {
                PlayerPrefs.DeleteKey($"{KEY_COOLDOWN_PREFIX}{i}");
            }
            PlayerPrefs.DeleteKey(KEY_COOLDOWN_COUNT);

            PlayerPrefs.Save();
            Debug.Log("[GameStatePersistence] State cleared");
        }

        /// <summary>
        /// Estrutura para armazenar estado do jogo
        /// </summary>
        public class SavedGameState
        {
            public string presetId;
            public string levelId;
            public List<BallData> ballList;
            public List<CoolDownPeriod> cooldownPeriods;
        }

        public class MatchResult
        {
            public int totalHits;
            public int totalBalls;
        }


        private static void OnApplicationQuitting()
        {
            ClearState();
        }
    }

    
}

