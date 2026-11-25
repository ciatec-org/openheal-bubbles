using UnityEngine;

namespace BubblesGame.Data
{
    /// <summary>
    /// Singleton para passar dados de sessão entre cenas
    /// </summary>
    public class GameSessionData : MonoBehaviour
    {
        public static GameSessionData Instance { get; private set; }

        public string CompanyId { get; set; }
        public string GameId { get; set; }
        public string MatchId { get; set; }
        public string PresetId { get; set; }
        public string LevelId { get; set; }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetSessionData(string companyId, string gameId, string matchId, string presetId, string levelId)
        {
            CompanyId = companyId;
            GameId = gameId;
            MatchId = matchId;
            PresetId = presetId;
            LevelId = levelId;

            Debug.Log($"[GameSessionData] Session set - Match: {matchId}, Preset: {presetId}, Level: {levelId}");
        }

        public void ClearSession()
        {
            CompanyId = null;
            GameId = null;
            MatchId = null;
            PresetId = null;
            LevelId = null;
        }

        public bool HasValidSession()
        {
            return !string.IsNullOrEmpty(MatchId) && !string.IsNullOrEmpty(PresetId) && !string.IsNullOrEmpty(LevelId);
        }
    }
}

