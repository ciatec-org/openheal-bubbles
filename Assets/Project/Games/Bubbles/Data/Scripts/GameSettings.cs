using System;
using UnityEngine;

namespace BubblesGame.Data
{
    [Serializable]
    public class GameSettings
    {
        [SerializeField] private string currentPreset;
        [SerializeField] private int currentLevel;
        [SerializeField] private int highScore;

        public string CurrentPreset
        {
            get => currentPreset;
            set => currentPreset = value;
        }

        public int CurrentLevel
        {
            get => currentLevel;
            set => currentLevel = value;
        }

        public int HighScore
        {
            get => highScore;
            set => highScore = value;
        }

        public GameSettings()
        {
            currentPreset = "preset_1";
            currentLevel = 1;
            highScore = 0;
        }
    }
}

