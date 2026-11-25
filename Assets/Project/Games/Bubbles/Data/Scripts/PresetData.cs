using System;
using System.Collections.Generic;
using UnityEngine;

namespace BubblesGame.Data
{
    [Serializable]
    public class PresetData
    {
        [SerializeField] private string presetId;
        [SerializeField] private string description;
        [SerializeField] private List<LevelData> levels;

        public string PresetId => presetId;
        public string Description => description;
        public List<LevelData> Levels => levels;

        public PresetData(string presetId, string description, List<LevelData> levels)
        {
            this.presetId = presetId;
            this.description = description;
            this.levels = levels;
        }

        public LevelData GetLevel(int order)
        {
            return levels?.Find(l => l.index == order);
        }
    }
}

