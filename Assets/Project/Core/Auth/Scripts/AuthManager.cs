using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections;

public class AuthManager : SingletonMonoBehaviour<AuthManager>
{
    private FirebaseAuth auth;

    protected override void OnInitialize()
    {
        base.OnInitialize();
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        StartCoroutine(WaitForFirebaseInitialization());
    }

    private System.Collections.IEnumerator WaitForFirebaseInitialization()
    {
        // Aguarda o FirebaseInitializer terminar
        while (!FirebaseInitializer.IsInitialized)
        {
            yield return new WaitForSeconds(0.1f);
        }

        auth = FirebaseAuth.DefaultInstance;
        Debug.Log("[AuthManager] Firebase Auth initialized successfully");
    }

    public FirebaseUser CurrentUser => auth?.CurrentUser;

    public void SendPasswordResetEmail(string email, System.Action<bool, string> callback)
    {
        if (auth == null)
        {
            callback(false, "Firebase Auth not initialized");
            return;
        }
        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                callback(false, "Error sending reset email: " + task.Exception?.Message);
            }
            else
            {
                callback(true, "Password reset email sent successfully.");
            }
        });
    }

    public void SignIn(string email, string password, System.Action<bool, string> callback)
    {
        if (auth == null)
        {
            callback(false, "Firebase Auth not initialized");
            return;
        }
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                callback(false, "Error logging in: " + task.Exception?.Message);
            }
            else
            {
                var user = task.Result.User;
                if (user != null)
                {
                    CreateLoginLog(user);
                    EnsureDefaultCompanyAndGame(user);
                    
                    // Initialize SessionManager cache
                    if (SessionManager.Instance != null)
                    {
                        SessionManager.Instance.InitializeUserData();
                    }
                }

                callback(true, "Login successful.");
            }
        });
    }

    public void SignUp(string email, string password, System.Action<bool, string> callback)
    {
        if (auth == null)
        {
            callback(false, "Firebase Auth not initialized");
            return;
        }
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                callback(false, "error creating account: " + task.Exception?.Message);
            }
            else
            {
                var user = task.Result.User;

                // Create user in Firestore
                CreateUserDocument(user);
                EnsureDefaultCompanyAndGame(user);
                
                // Initialize SessionManager cache
                if (SessionManager.Instance != null)
                {
                    SessionManager.Instance.InitializeUserData();
                }

                callback(true, "account created successfully!");
            }
        });
    }

    private void CreateUserDocument(FirebaseUser user)
    {
        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;

        var data = new System.Collections.Generic.Dictionary<string, object>
        {
            { "userId", user.UserId },
            { "email", user.Email },
            { "name", "" },
            { "createdAt", System.DateTime.UtcNow.ToString("o") }
        };

        db.Collection("users").Document(user.UserId).SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Error creating user document in Firestore: " + task.Exception?.Message);
            }
            else
            {
                Debug.Log("User document created successfully in Firestore for: " + user.Email);
            }
        });
    }

    private void EnsureDefaultCompanyAndGame(FirebaseUser user)
    {
        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        const string defaultCompanyId = GameConfig.CompanyId;
        const string defaultCompanyDisplayName = GameConfig.CompanyDisplayName;

        var companyRef = db
            .Collection("users")
            .Document(user.UserId)
            .Collection("companies")
            .Document(defaultCompanyId);

        companyRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Error checking company document: " + task.Exception?.Message);
                return;
            }

            var snapshot = task.Result;

            // Se a company não existir, cria
            if (!snapshot.Exists)
            {
                var companyData = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "companyId", defaultCompanyId },
                    { "displayName", defaultCompanyDisplayName },
                    { "createdAt", System.DateTime.UtcNow.ToString("o") }
                };

                companyRef.SetAsync(companyData).ContinueWithOnMainThread(createCompanyTask =>
                {
                    if (createCompanyTask.IsCanceled || createCompanyTask.IsFaulted)
                    {
                        Debug.LogError("Error creating company document: " + createCompanyTask.Exception?.Message);
                    }
                });
            }

            // Garante todos os games configurados dentro da company
            foreach (var game in GameConfig.Games)
            {
                CreateGameIfNotExists(companyRef, game.gameId, game.displayName, user.Email);
            }
        });
    }

    private void CreateGameIfNotExists(Firebase.Firestore.DocumentReference companyRef, string gameId, string gameDisplayName, string userEmail)
    {
        var gameRef = companyRef
            .Collection("games")
            .Document(gameId);

        gameRef.GetSnapshotAsync().ContinueWithOnMainThread(gameTask =>
        {
            if (gameTask.IsCanceled || gameTask.IsFaulted)
            {
                Debug.LogError($"Error checking game document {gameId}: " + gameTask.Exception?.Message);
                return;
            }

            var gameSnapshot = gameTask.Result;
            if (gameSnapshot.Exists)
            {
                return;
            }

            var gameData = new System.Collections.Generic.Dictionary<string, object>
            {
                { "gameId", gameId },
                { "displayName", gameDisplayName },
                { "createdAt", System.DateTime.UtcNow.ToString("o") }
            };

            gameRef.SetAsync(gameData).ContinueWithOnMainThread(createGameTask =>
            {
                if (createGameTask.IsCanceled || createGameTask.IsFaulted)
                {
                    Debug.LogError($"Error creating game document {gameId}: " + createGameTask.Exception?.Message);
                }
            });
        });
    }

    private void CreateLoginLog(FirebaseUser user)
    {
        var db = Firebase.Firestore.FirebaseFirestore.DefaultInstance;
        var timestampId = System.DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff'Z'");

        var logData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "loginAt", System.DateTime.UtcNow.ToString("o") },
            { "email", user.Email },
            { "deviceId", SystemInfo.deviceUniqueIdentifier },
            { "deviceName", SystemInfo.deviceName },
            { "deviceModel", SystemInfo.deviceModel },
            { "operatingSystem", SystemInfo.operatingSystem },
            { "operatingSystemFamily", SystemInfo.operatingSystemFamily.ToString() },
            { "processorType", SystemInfo.processorType },
            { "processorCount", SystemInfo.processorCount },
            { "systemMemorySizeMb", SystemInfo.systemMemorySize },
            { "graphicsDeviceName", SystemInfo.graphicsDeviceName },
            { "graphicsMemorySizeMb", SystemInfo.graphicsMemorySize },
            { "applicationVersion", Application.version },
            { "applicationPlatform", Application.platform.ToString() },
            { "unityVersion", Application.unityVersion }
        };

        db.Collection("users").Document(user.UserId).Collection("logs").Document(timestampId).SetAsync(logData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Error creating login log: " + task.Exception?.Message);
            }
        });
    }
}
