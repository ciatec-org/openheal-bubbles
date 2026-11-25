using UnityEngine;

// Coloque este script no botão.
// Configure gameId e sceneName no inspetor e chame OnClick() no evento do botão.
public class GameLauncher : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private MatchManager matchManager;
    

    [Header("Config deste botão")]
    [SerializeField] private string gameId = "";
    [SerializeField] private string sceneName = "SteadyMotionMain";

    public void OnClick()
    {
        if (matchManager == null)
        {
            Debug.LogError("GameLauncher: MatchManager não configurado no inspetor.");
            return;
        }

        if (string.IsNullOrEmpty(gameId))
        {
            Debug.LogError("GameLauncher: gameId vazio.");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("GameLauncher: sceneName vazio.");
            return;
        }

        matchManager.StartGame(gameId, sceneName);
    }
}


