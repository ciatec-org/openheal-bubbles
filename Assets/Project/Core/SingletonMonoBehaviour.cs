using UnityEngine;

/// <summary>
/// Base class for singleton MonoBehaviours that persist across scenes.
/// Industry-standard Singleton pattern with DontDestroyOnLoad.
/// </summary>
/// <typeparam name="T">Type of the singleton class</typeparam>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    /// <summary>
    /// Singleton instance accessor
    /// </summary>
    public static T Instance
    {
        get => _instance;
    }

    /// <summary>
    /// Check if singleton instance exists
    /// </summary>
    public static bool HasInstance => _instance != null;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            
            // DontDestroyOnLoad só funciona para root GameObjects
            // Garante que o GameObject está na raiz da hierarquia
            if (transform.parent != null)
            {
                Debug.LogWarning($"[{typeof(T).Name}] GameObject is not at root level. Moving to root for DontDestroyOnLoad to work.");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            OnInitialize();
        }
        else
        {
            Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called when singleton is first initialized.
    /// Override this instead of Awake().
    /// </summary>
    protected virtual void OnInitialize()
    {
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}