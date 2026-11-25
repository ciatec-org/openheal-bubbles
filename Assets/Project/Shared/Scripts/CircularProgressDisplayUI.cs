using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Graphs
{
    /// <summary>
    /// Componente unificado para exibir progresso circular
    /// Suporta background, fill e ring em um único componente
    /// </summary>
    public class CircularProgressDisplay : MonoBehaviour
    {
        [Header("Image Components")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image ringImage;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Progress Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float targetValue = 0.75f;
        [SerializeField] private string labelFormat = "{0}%";
        [SerializeField] private bool showLabel = true;

        [Header("Visual Style")]
        [SerializeField] private ProgressType progressType = ProgressType.Fill;
        [Range(0.1f, 1f)]
        [SerializeField] private float ringMaxFill = 0.9f; // Para deixar gap no ring

        [Header("Animation")]
        [SerializeField] private bool animate = true;
        [SerializeField] private float animationSpeed = 1.5f;
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Timing")]
        [SerializeField] private float startDelay = 0f;
        [SerializeField] private bool fadeIn = true;
        [SerializeField] private float fadeSpeed = 3f;

        [Header("Effects")]
        [SerializeField] private bool pulseEffect = false;
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float pulseAmount = 0.03f;

        public enum ProgressType
        {
            Fill,           // Only fill
            Ring,           // Only ring
            FillAndRing     // Fill + Ring combined
        }

        private float currentValue;
        private Coroutine animationCoroutine;

        #region Unity Lifecycle

        void Awake()
        {
            ConfigureImageComponents();
        }

        void OnEnable()
        {
            ResetDisplay();
            StartAnimation();
        }

        void Update()
        {
            if (pulseEffect)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = Vector3.one * scale;
            }
        }

        void OnValidate()
        {
            // Update display in editor
            if (Application.isPlaying) return;
            
            UpdateVisualComponents();
            SetDisplayValue(targetValue, false);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Set the progress value (0-1)
        /// </summary>
        public void SetValue(float value, bool animated = true)
        {
            targetValue = Mathf.Clamp01(value);
            
            if (animated && animate && gameObject.activeInHierarchy)
            {
                StartAnimation();
            }
            else
            {
                SetDisplayValue(targetValue, false);
            }
        }

        /// <summary>
        /// Set the progress value in percentage (0-100)
        /// </summary>
        public void SetPercentage(float percentage, bool animated = true)
        {
            SetValue(percentage / 100f, animated);
        }

        /// <summary>
        /// Get the current progress value
        /// </summary>
        public float GetValue() => currentValue;

        /// <summary>
        /// Get the current progress value in percentage
        /// </summary>
        public float GetPercentage() => currentValue * 100f;

        #endregion

        #region Private Methods

        private void ConfigureImageComponents()
        {
            // Configure backgroundImage
            if (backgroundImage != null)
            {
                backgroundImage.type = Image.Type.Simple;
            }

            // Configure fillImage for radial fill
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = 2; // Top
                fillImage.fillClockwise = true;
            }

            // Configure ringImage for radial fill
            if (ringImage != null)
            {
                ringImage.type = Image.Type.Filled;
                ringImage.fillMethod = Image.FillMethod.Radial360;
                ringImage.fillOrigin = 2; // Top
                ringImage.fillClockwise = true;
            }
        }

        private void ResetDisplay()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            currentValue = 0f;
            SetDisplayValue(0f, false);

            if (canvasGroup != null && fadeIn)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void StartAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(AnimateProgress());
        }

        private IEnumerator AnimateProgress()
        {
            // Delay inicial
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            float startValue = currentValue;
            float timer = 0f;
            float duration = animate ? (1f / animationSpeed) : 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float easedT = animate ? animationCurve.Evaluate(t) : 1f;

                float value = Mathf.Lerp(startValue, targetValue, easedT);
                SetDisplayValue(value, true);

                // Fade in
                if (fadeIn && canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(
                        canvasGroup.alpha, 1f, Time.deltaTime * fadeSpeed
                    );
                }

                yield return null;
            }

            // Garantir valor final exato
            SetDisplayValue(targetValue, true);
            
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            animationCoroutine = null;
        }

        private void SetDisplayValue(float value, bool updateComponents)
        {
            currentValue = value;

            if (updateComponents)
            {
                UpdateVisualComponents();
            }

            // Atualizar label
            if (progressLabel != null && showLabel)
            {
                int percentage = Mathf.RoundToInt(value * 100f);
                progressLabel.text = string.Format(labelFormat, percentage);
            }
        }

        private void UpdateVisualComponents()
        {
            switch (progressType)
            {
                case ProgressType.Fill:
                    UpdateFillOnly();
                    break;
                case ProgressType.Ring:
                    UpdateRingOnly();
                    break;
                case ProgressType.FillAndRing:
                    UpdateFillAndRing();
                    break;
            }
        }

        private void UpdateFillOnly()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentValue;
                fillImage.gameObject.SetActive(true);
            }

            if (ringImage != null)
            {
                ringImage.gameObject.SetActive(false);
            }
        }

        private void UpdateRingOnly()
        {
            if (ringImage != null)
            {
                ringImage.fillAmount = currentValue * ringMaxFill;
                ringImage.gameObject.SetActive(true);
            }

            if (fillImage != null)
            {
                fillImage.gameObject.SetActive(false);
            }
        }

        private void UpdateFillAndRing()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = currentValue;
                fillImage.gameObject.SetActive(true);
            }

            if (ringImage != null)
            {
                ringImage.fillAmount = currentValue * ringMaxFill;
                ringImage.gameObject.SetActive(true);
            }
        }

        #endregion

        #region Editor Helpers

        [System.Serializable]
        public class ProgressColors
        {
            public Color backgroundColor = Color.gray;
            public Color fillColor = Color.blue;
            public Color ringColor = Color.cyan;
            public Color textColor = Color.white;
        }

        /// <summary>
        /// Aplica cores padrão aos componentes
        /// </summary>
        public void ApplyColors(ProgressColors colors)
        {
            if (backgroundImage != null) backgroundImage.color = colors.backgroundColor;
            if (fillImage != null) fillImage.color = colors.fillColor;
            if (ringImage != null) ringImage.color = colors.ringColor;
            if (progressLabel != null) progressLabel.color = colors.textColor;
        }

        #endregion
    }
}
