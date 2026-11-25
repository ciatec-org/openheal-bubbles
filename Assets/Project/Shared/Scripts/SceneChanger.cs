using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UshiSoft
{
    [DisallowMultipleComponent]
    public class SceneChanger : MonoBehaviour
    {
        public static SceneChanger Instance => FindAnyObjectByType<SceneChanger>();

        [SerializeField] private Color _fadeColor = Color.black;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

        private float _alpha;
        private bool _fading;

        private void OnGUI()
        {
            if (!_fading)
            {
                return;
            }

            var color = _fadeColor;
            color.a = _alpha;
            GUI.color = color;

            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        public void ChangeScene(string sceneName)
        {
            if (_fading)
            {
                return;
            }

            StartCoroutine(ChangeSceneCoroutine(sceneName));
        }

        private IEnumerator ChangeSceneCoroutine(string sceneName)
        {
            _fading = true;

            var time = 0f;
            while (time <= _fadeDuration / 2f)
            {
                _alpha = Mathf.InverseLerp(0f, _fadeDuration / 2f, time);
                time += Time.deltaTime;
                yield return null;
            }
            _alpha = 1f;

            SceneManager.LoadScene(sceneName);

            time = 0f;
            while (time <= _fadeDuration / 2f)
            {
                _alpha = Mathf.InverseLerp(_fadeDuration / 2f, 0f, time);
                time += Time.deltaTime;
                yield return null;
            }
            _alpha = 0f;

            _fading = false;
        }
    }
}