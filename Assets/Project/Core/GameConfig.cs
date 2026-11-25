using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameInfo
{
    public string gameId;
    public string displayName;
}

public static class GameConfig
{
    // Altere estes valores para configurar company e games
    public const string CompanyId = "open-heal";
    public const string CompanyDisplayName = "OpenHeal";
    
    // Lista de jogos disponíveis
    public static readonly GameInfo[] Games = new GameInfo[]
    {
        new GameInfo { gameId = "bubbles", displayName = "Bubbles" }
    };
}

