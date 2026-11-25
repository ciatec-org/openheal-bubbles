using System;
using System.Collections.Generic;

namespace BubblesGame.Data
{
    [Serializable]
    public class LevelData
    {
        public int index;
        public string name;
        public string description;
        public float totalTime;
        public string levelId;  // ID real do documento no Firebase (ex: "level_01")

        public List<BallData> balls;

        public LevelData() { }

        public LevelData(int index, List<BallData> balls, string name = "", string description = "")
        {
            this.index = index;
            this.balls = balls;
            this.name = name;
            this.description = description;
        }

        // Helper properties
        public List<BallData> BallsData => balls;
        public string LevelId => !string.IsNullOrEmpty(levelId) ? levelId : $"level_{index}";
        public string Name => name;
    }
}
