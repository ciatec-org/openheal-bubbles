using UnityEngine;
using Firebase.Firestore;
using Firebase.Auth;
using Firebase.Extensions;
using System;

public class MatchService : MonoBehaviour
{
    public static MatchService Instance;

    private FirebaseFirestore db;
    private FirebaseAuth auth;

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
            return;
        }
    }

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }

    /// <summary>
    /// Cria uma match sob a rota:
    /// users/{uid}/companies/{companyId}/games/{gameId}/matches/{matchId}
    ///
    /// matchId = AAAAMMDDHHMMSSmmmm (UTC).
    /// </summary>
    public void CreateMatch(string companyId, string gameId, string presetId, string levelId, Action<string> callback)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogError("Usuário não autenticado.");
            callback(null);
            return;
        }

        string userId = auth.CurrentUser.UserId;

        // AAAAMMDDHHMMSSmmmm em UTC
        string matchId = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");

        var matchData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "matchId", matchId },
            { "companyId", companyId },
            { "gameId", gameId },
            { "presetId", presetId },
            { "levelId", levelId },
            { "startedAt", DateTime.UtcNow.ToString("o") },
            { "endedAt", null },
            { "hasRawLandmarks", false },
            { "hasOffsetLandmarks", false }
        };

        DocumentReference docRef = db
            .Collection("users").Document(userId)
            .Collection("companies").Document(companyId)
            .Collection("games").Document(gameId)
            .Collection("matches").Document(matchId);

        docRef.SetAsync(matchData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"[MatchService] Match {matchId} created with preset={presetId}, level={levelId}");
                
                // CORREÇÃO: Configura SessionManager com dados da match para acesso na próxima cena
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.SetGameSession(gameId, matchId, presetId, levelId);
                    Debug.Log($"[MatchService] SessionManager configurado com preset={presetId}, level={levelId}");
                }
                
                callback(matchId);
            }
            else
            {
                Debug.LogError("Erro ao criar match: " + task.Exception);
                callback(null);
            }
        });
    }

    /// <summary>
    /// Finaliza uma match e salva os resultados
    /// Preparado para receber dados de resultado no futuro (passo 4)
    /// </summary>
    public void EndMatch(string companyId, string gameId, string matchId, Action onSuccess = null, Action<string> onError = null)
    {
        if (auth.CurrentUser == null)
        {
            onError?.Invoke("Usuário não autenticado.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        DocumentReference docRef = db
            .Collection("users").Document(userId)
            .Collection("companies").Document(companyId)
            .Collection("games").Document(gameId)
            .Collection("matches").Document(matchId);

        var updates = new System.Collections.Generic.Dictionary<string, object>
        {
            { "endedAt", DateTime.UtcNow.ToString("o") }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                Debug.Log($"[MatchService] Match {matchId} ended");
                onSuccess?.Invoke();
            }
            else
            {
                Debug.LogError($"[MatchService] Erro ao finalizar match: {task.Exception}");
                onError?.Invoke(task.Exception?.Message);
            }
        });
    }
}
