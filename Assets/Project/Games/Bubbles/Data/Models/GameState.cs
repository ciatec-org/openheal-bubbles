using System;

namespace BubblesGame.Data
{
    /// <summary>
    /// Representa o estado atual do jogo para um usuário
    /// Armazenado em: users/{uid}/companies/{compId}/games/{gameId}
    /// Campos: current_preset, current_level
    /// </summary>
    [Serializable]
    public class GameState
    {
        public string presetId;
        public string levelId;

        public GameState()
        {
            // Valores padrão
            presetId = "preset_1";
            levelId = "level_1";
        }

        public GameState(string presetId, string levelId)
        {
            this.presetId = presetId;
            this.levelId = levelId;
        }
    }
}

