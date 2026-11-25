using UnityEngine;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class AuthGuard : MonoBehaviour
{
    private const string LoginSceneName = "Login";
    
    void Start()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser == null)
        {
            SceneManager.LoadScene(LoginSceneName);
        }
    }
}
