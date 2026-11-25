using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // Chamado por GameLauncher (compatibilidade)
    public void StartGame(string gameId, string sceneName)
    {
        StartGame(gameId, sceneName, "preset_1", "level_1");
    }

    // Versão completa com preset e level customizados
    public void StartGame(string gameId, string sceneName, string presetId, string levelId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("[MatchManager] gameId vazio.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[MatchManager] sceneName vazio.");
            return;
        }

        if (string.IsNullOrEmpty(presetId))
        {
            Debug.LogWarning("[MatchManager] presetId vazio, usando padrão.");
            presetId = "preset_1";
        }

        if (string.IsNullOrEmpty(levelId))
        {
            Debug.LogWarning("[MatchManager] levelId vazio, usando padrão.");
            levelId = "level_1";
        }

        string companyId = GameConfig.CompanyId;

        Debug.Log($"[MatchManager] Starting game {gameId} with preset {presetId} and level {levelId}...");

        MatchService.Instance.CreateMatch(companyId, gameId, presetId, levelId, matchId =>
        {
            if (matchId != null)
            {
                Debug.Log($"[MatchManager] Match created: {matchId} for game {gameId} (preset: {presetId}, level: {levelId})");

                // Carrega a cena do jogo - o jogo se encarregará de gerenciar seu estado
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError("[MatchManager] Failed to create match.");
            }
        });
    }

}
