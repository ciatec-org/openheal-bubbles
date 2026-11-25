using System;
using UnityEngine;

namespace BubblesGame.Data
{
    /// <summary>
    /// Representa informações básicas de um preset para exibição na UI
    /// </summary>
    [Serializable]
    public class PresetInfo
    {
        [SerializeField] private string presetId;
        [SerializeField] private string description;
        [SerializeField] private int version;

        public string PresetId => presetId;
        public string Description => description;
        public int Version => version;

        public PresetInfo(string presetId, string description, int version = 1)
        {
            this.presetId = presetId;
            this.description = description;
            this.version = version;
        }

        public override string ToString()
        {
            return $"PresetInfo(ID: {presetId}, Description: {description}, Version: {version})";
        }
    }
}
