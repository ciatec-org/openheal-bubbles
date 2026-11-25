using UnityEngine;
using Firebase;
using Firebase.Extensions;

public class FirebaseInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var status = task.Result;

            if (status == DependencyStatus.Available)
            {
                IsInitialized = true;
                Debug.Log("Successfully initialized Firebase.");
            }
            else
            {
                Debug.LogError("Problems with Firebase dependencies: " + status);
            }
        });
    }
}
