using UnityEngine;
using Firebase.Auth;

/// <summary>
/// Centralized session data manager using Singleton pattern.
/// Caches user and game session data in memory for performance.
/// Industry-standard approach: PlayerPrefs (persistent) + Singletons (session).
/// </summary>
public class SessionManager : SingletonMonoBehaviour<SessionManager>
{
    // User Data Cache
    public string UserId { get; private set; }
    public string UserEmail { get; private set; }
    public string CompanyId { get; private set; }

    // Game Session Cache
    public string CurrentGameId { get; private set; }
    public string CurrentMatchId { get; private set; }
    public string CurrentPresetId { get; private set; }
    public string CurrentLevelId { get; private set; }
    public int CurrentLandmarks { get; private set; }

    protected override void OnInitialize()
    {
        base.OnInitialize();
        Debug.Log("[SessionManager] Initialized - Ready for data caching");
    }

    #region User Data Management

    /// <summary>
    /// Initialize user data from Firebase Auth.
    /// Call this after successful authentication.
    /// </summary>
    public void InitializeUserData()
    {
        var user = FirebaseAuth.DefaultInstance?.CurrentUser;
        if (user != null)
        {
            UserId = user.UserId;
            UserEmail = user.Email;
            CompanyId = GameConfig.CompanyId; // Default company
            
            Debug.Log($"[SessionManager] User data initialized: {UserEmail} ({UserId})");
        }
        else
        {
            Debug.LogWarning("[SessionManager] Cannot initialize user data - no authenticated user");
        }
    }

    /// <summary>
    /// Check if user data is valid and available
    /// </summary>
    public bool HasValidUserData()
    {
        return !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(UserEmail);
    }

    #endregion

    #region Game Session Management

    /// <summary>
    /// Set current game session data.
    /// Call this when starting a new game or match.
    /// </summary>
    public void SetGameSession(string gameId, string matchId, string presetId, string levelId, int landmarks = 0)
    {
        CurrentGameId = gameId;
        CurrentMatchId = matchId;
        CurrentPresetId = presetId;
        CurrentLevelId = levelId;
        CurrentLandmarks = landmarks;
        
        Debug.Log($"[SessionManager] Game session set: {gameId} - {presetId}/{levelId} (Match: {matchId})");
    }

    /// <summary>
    /// Update current level and landmarks during gameplay
    /// </summary>
    public void UpdateLevel(string levelId, int landmarks)
    {
        CurrentLevelId = levelId;
        CurrentLandmarks = landmarks;
        
        Debug.Log($"[SessionManager] Level updated: {levelId} (Landmarks: {landmarks})");
    }

    /// <summary>
    /// Clear game session data (keep user data)
    /// </summary>
    public void ClearGameSession()
    {
        CurrentGameId = null;
        CurrentMatchId = null;
        CurrentPresetId = null;
        CurrentLevelId = null;
        CurrentLandmarks = 0;
        
        Debug.Log("[SessionManager] Game session cleared");
    }

    /// <summary>
    /// Check if game session is valid and available
    /// </summary>
    public bool HasValidGameSession()
    {
        return !string.IsNullOrEmpty(CurrentGameId) && 
               !string.IsNullOrEmpty(CurrentPresetId) && 
               !string.IsNullOrEmpty(CurrentLevelId);
    }

    #endregion

    #region Complete Reset

    /// <summary>
    /// Clear all cached data (user + game session)
    /// </summary>
    public void ClearAll()
    {
        // Clear user data
        UserId = null;
        UserEmail = null;
        CompanyId = null;
        
        // Clear game session
        ClearGameSession();
        
        Debug.Log("[SessionManager] All data cleared");
    }

    #endregion

    #region Static Helper Methods (Clean Access)

    /// <summary>
    /// Static helper to get current user ID
    /// </summary>
    public static string GetUserId()
    {
        return Instance?.UserId;
    }

    /// <summary>
    /// Static helper to get current user email
    /// </summary>
    public static string GetUserEmail()
    {
        return Instance?.UserEmail;
    }

    /// <summary>
    /// Static helper to get current company ID
    /// </summary>
    public static string GetCompanyId()
    {
        return Instance?.CompanyId;
    }

    /// <summary>
    /// Static helper to get current game ID
    /// </summary>
    public static string GetCurrentGameId()
    {
        return Instance?.CurrentGameId;
    }

    /// <summary>
    /// Static helper to get current match ID
    /// </summary>
    public static string GetCurrentMatchId()
    {
        return Instance?.CurrentMatchId;
    }

    /// <summary>
    /// Static helper to get current preset ID
    /// </summary>
    public static string GetCurrentPresetId()
    {
        return Instance?.CurrentPresetId;
    }

    /// <summary>
    /// Static helper to get current level ID
    /// </summary>
    public static string GetCurrentLevelId()
    {
        return Instance?.CurrentLevelId;
    }

    /// <summary>
    /// Static helper to check if user data is available
    /// </summary>
    public static bool IsUserDataValid()
    {
        return Instance?.HasValidUserData() ?? false;
    }

    /// <summary>
    /// Static helper to check if game session is available
    /// </summary>
    public static bool IsGameSessionValid()
    {
        return Instance?.HasValidGameSession() ?? false;
    }

    #endregion
}