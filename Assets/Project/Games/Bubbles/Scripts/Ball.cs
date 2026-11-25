using UnityEngine;
using UnityEngine.UI;
using BubblesGame.Data;

namespace BubblesGame
{
    public enum BallState
    {
        Inactive,   // Ainda não atingiu mature_time
        Active,     // Entre mature_time e destroy_time (pode ser acertada)
        Hit,        // Foi acertada
        Missed,     // Expirou sem ser acertada
        Destroyed   // Foi destruída (landmark de destruição)
    }

    public class Ball : MonoBehaviour
    {
        private BallData data;
        private Image image; // UI Image para Canvas
        private BallState state = BallState.Inactive;
        private float gameStartTime;
        private Vector2 velocity;
        private BubblesGameManager gameManager;
        private RectTransform webcamRect;           // Referência ao RawImage da webcam
        private RectTransform rectTransformUI;      // RectTransform desta bola (UI)
        private Vector2 normalizedPosition;         // Posição da bola em coordenadas normalizadas (0-1) no espaço da webcam
        
        // Ball identification
        private int ballIndex = -1;                 // Índice da bola no ball_list original
        
        // Collision tracking
        private float? hitTime = null;
        private float? destroyTime = null;
        
        // Bounds settings
        [Header("Ball Physics")]
        [SerializeField] private float ballRadiusNormalized = 0.02f; // Raio em coordenadas normalizadas
        private float baseBallRadiusNormalized; // Raio base para cálculo de objetos especiais

        [Header("Explosion Effects")]
        [SerializeField] private GameObject hitExplosionEffectPrefab; // BubbleHitEffect
        [SerializeField] private GameObject destroyExplosionEffectPrefab; // BubbleAvoidEffect

        [Header("Super Object Sprites")]
        [Tooltip("Sprite para objetos hit-super (arraste de Assets/Games/Bubbles/Sprites/hit-super)")]
        [SerializeField] private Sprite hitSuperSprite;
        [Tooltip("Sprite para objetos destroy-super (arraste de Assets/Games/Bubbles/Sprites/destroy-super)")]
        [SerializeField] private Sprite destroySuperSprite;
        [Tooltip("Sprite para objetos hit-all-super (arraste de Assets/Games/Bubbles/Sprites/hit-all-super)")]
        [SerializeField] private Sprite hitAllSuperSprite;
        [Tooltip("Sprite para objetos destroy-all-super (arraste de Assets/Games/Bubbles/Sprites/destroy-all-super)")]
        [SerializeField] private Sprite destroyAllSuperSprite;

        

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // Propriedades públicas
        public BallState State => state;
        public BallData Data => data;
        public bool IsActive => state == BallState.Active;
        public float? HitTime => hitTime;
        public float? DestroyTime => destroyTime;
        public int BallIndex => ballIndex;

        void Awake()
        {
            image = GetComponent<Image>();
            
            // Armazena o raio base
            baseBallRadiusNormalized = ballRadiusNormalized;
            
            // Tenta carregar os prefabs se não foram configurados
            if (hitExplosionEffectPrefab == null)
            {
                hitExplosionEffectPrefab = Resources.Load<GameObject>("BubbleHitEffect");
            }
            if (destroyExplosionEffectPrefab == null)
            {
                destroyExplosionEffectPrefab = Resources.Load<GameObject>("BubbleAvoidEffect");
            }
        }


        /// <summary>
        /// Inicializa a bola com dados do nível
        /// </summary>
        public void Initialize(BallData ballData, float startTime, RectTransform webcamRect, int index = -1)
        {
            data = ballData;
            gameStartTime = startTime;
            state = BallState.Inactive;
            this.webcamRect = webcamRect;
            ballIndex = index >= 0 ? index : (ballData != null ? ballData.BallId : -1);
            
            // Obtém referência ao GameManager para tempo centralizado
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<BubblesGameManager>();
            }

            // Configura visual
            bool isSuperObject = ballData.IsHitSuper || ballData.IsAvoidSuper || 
                                 ballData.IsHitAllSuper || ballData.IsDestroyAllSuper;

            ballData.color.a = 1;
            if (image != null)
            {
                image.color = ballData.Color;
                
                // Atribui sprite especial se for objeto especial
                if (isSuperObject)
                {
                    Sprite superSprite = GetSuperSprite(ballData);
                   
                    if (superSprite != null)
                    {
                        image.sprite = superSprite;
                    }
                }
            }
            
            // Aumenta tamanho em 1.5x para objetos especiais
            float sizeMultiplier = isSuperObject ? 1.5f : 1f;
            transform.localScale = Vector3.one * ballData.Size * sizeMultiplier;
            
            // Aumenta área de colisão em 1.5x para objetos especiais
            if (isSuperObject)
            {
                ballRadiusNormalized = baseBallRadiusNormalized * 1.5f;
            }
            else
            {
                ballRadiusNormalized = baseBallRadiusNormalized;
            }

            // Posição da bola é mantida em coordenadas TOP-LEFT normalized (0-1)
            // Mesma origem que MediaPipe e BallData do Firebase
            rectTransformUI = GetComponent<RectTransform>();
            if (rectTransformUI != null)
            {
                // Configuração de anchor para posicionamento absoluto
                rectTransformUI.anchorMin = Vector2.zero;
                rectTransformUI.anchorMax = Vector2.zero;
                rectTransformUI.pivot = new Vector2(0.5f, 0.5f);

                if (this.webcamRect != null)
                {
                    // Clamp coordenadas normalizadas para garantir que a bola apareça completamente
                    float radiusNorm = ballRadiusNormalized * ballData.Size;
                    float clampedX = Mathf.Clamp(ballData.LaunchCoord.x, radiusNorm, 1f - radiusNorm);
                    float clampedY = Mathf.Clamp(ballData.LaunchCoord.y, radiusNorm, 1f - radiusNorm);

                    // Mantém em TOP-LEFT normalized - SEM inversão!
                    // LaunchCoord já vem em top-left do Firebase
                    normalizedPosition = new Vector2(clampedX, clampedY);

                    // Atualiza posição visual inicial usando conversor centralizado
                    UpdateUIPositionFromNormalized();
                }
                else
                {
                    Debug.LogError("[Ball] WebcamRect is null! Cannot position ball correctly.");
                }
            }

            // Calcula velocidade (direction em graus)
            float angleRad = ballData.Direction * Mathf.Deg2Rad;
            velocity = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * ballData.Speed;

            // Inicia invisível (será visível no launch_time)
            if (image != null)
            {
                image.enabled = false;
            }
        }

        void Update()
        {

            if (data == null || !GameController.Instance.IsGameActive) return;

            // Usa tempo centralizado do GameManager (fallback para cálculo local se necessário)
            float currentTime = gameManager != null ? gameManager.CurrentGameTime : Time.time - gameStartTime;

            // Controle de estados baseado no tempo
            switch (state)
            {
                case BallState.Inactive:
                    CheckLaunchTime(currentTime);
                    break;

                case BallState.Active:
                    UpdateMovement();
                    CheckDestroyTime(currentTime);
                    break;

                case BallState.Hit:
                case BallState.Missed:
                case BallState.Destroyed:
                    // Estados finais - bola será destruída
                    break;
            }
        }

        private void CheckLaunchTime(float currentTime)
        {
            if (currentTime >= data.LaunchTime)
            {
                if (image != null && !image.enabled)
                {
                    image.enabled = true;
                    AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.bubbleAppear));
                }

                // Ativa imediatamente quando aparece
                if (state == BallState.Inactive)
                {
                    state = BallState.Active;
                }
            }
        }

        private void CheckDestroyTime(float currentTime)
        {
            if (currentTime >= data.DestroyTime)
            {
                OnMiss();
            }
        }

        /// <summary>
        /// Atualiza o movimento da bola em coordenadas NORMALIZADAS dentro do webcamRect.
        /// Todo o movimento e bounce acontecem no mesmo espaço dos landmarks (0-1).
        /// </summary>
        private void UpdateMovement()
        {
            if (webcamRect == null || rectTransformUI == null) return;

            // Move em coordenadas normalizadas (0-1)
            normalizedPosition += velocity * Time.deltaTime;

            bool bounced = false;
            float radiusNorm = ballRadiusNormalized * data.Size;

            // Borda esquerda/direita
            if (normalizedPosition.x - radiusNorm <= 0f || normalizedPosition.x + radiusNorm >= 1f)
            {
                velocity.x = -velocity.x;
                bounced = true;
            }

            // Borda cima/baixo
            if (normalizedPosition.y - radiusNorm <= 0f || normalizedPosition.y + radiusNorm >= 1f)
            {
                velocity.y = -velocity.y;
                bounced = true;
            }

            // Se bateu em alguma borda, força a posição a ficar dentro do intervalo visível
            if (bounced)
            {
                float clampedX = Mathf.Clamp(normalizedPosition.x, radiusNorm, 1f - radiusNorm);
                float clampedY = Mathf.Clamp(normalizedPosition.y, radiusNorm, 1f - radiusNorm);
                normalizedPosition = new Vector2(clampedX, clampedY);
            }

            // Atualiza posição visual na UI
            UpdateUIPositionFromNormalized();
        }

        /// <summary>
        /// Converte a posição normalizada TOP-LEFT (0-1) para posição no Canvas
        /// Usa Canvas root como referência para cobrir tela inteira
        /// </summary>
        private void UpdateUIPositionFromNormalized()
        {
            if (rectTransformUI == null) return;

            // Obtém Canvas root (não webcamRect, que pode estar menor)
            Canvas canvas = rectTransformUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Ball] Canvas não encontrado!");
                return;
            }
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                Debug.LogError("[Ball] Canvas RectTransform não encontrado!");
                return;
            }

            // Debug: log apenas na primeira vez
            if (Time.frameCount == 1 || (Time.frameCount % 300 == 0))
            {
                Debug.Log($"[Ball] Canvas size: {canvasRect.rect.width}x{canvasRect.rect.height}, Normalized: {normalizedPosition}, CanvasPos: {CoordinateConverter.NormalizedToCanvasPosition(normalizedPosition, canvasRect)}");
            }

            // Usa Canvas root como referência para cobrir tela inteira
            Vector2 canvasPosition = CoordinateConverter.NormalizedToCanvasPosition(normalizedPosition, canvasRect);
            rectTransformUI.anchoredPosition = canvasPosition;
        }

        /// <summary>
        /// Chamado quando o jogador acerta a bola
        /// </summary>
        public void OnHit(int landmarkId = -1, Vector3 landmarkPos = default, float distance = 0f)
        {
            if (state != BallState.Active) return;

            state = BallState.Hit;
            
            // Registra tempo exato da colisão
            hitTime = Time.time - gameStartTime;

            // destroy_all_super: colide com QUALQUER landmark (hit ou destroy)
            if (data.IsDestroyAllSuper)
            {
                if (GameServices.ScoreManager != null)
                {
                    GameServices.ScoreManager.RegisterDestroy(this, landmarkId);
                }
            }
            else
            {
                // Notifica ScoreManager com dados REAIS da colisão
                if (GameServices.ScoreManager != null)
                {
                    GameServices.ScoreManager.RegisterHit(this, landmarkId, landmarkPos, distance);
                }
                else
                {
                    Debug.LogWarning("[Ball] ScoreManager not available in GameServices");
                }
            }

            // Instancia efeito de explosão com a cor e tamanho da bola (o som será tocado pelo próprio efeito)
            // Se landmarkPos for default(Vector3), significa que hit_all_super foi usado e o Hit é genérico
            SpawnHitExplosionEffect(landmarkPos == default);

            // Remove da lista de bolas ativas IMEDIATAMENTE
            if (gameManager != null)
            {
                gameManager.RemoveBall(this);
            }

            // Desabilita visualmente IMEDIATAMENTE
            if (image != null)
            {
                image.enabled = false;
            }

            // Desabilita GameObject e destrói
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>
        /// Chamado quando a bola expira sem ser acertada
        /// </summary>
        public void OnMiss()
        {
            if (state != BallState.Active) return;

            state = BallState.Missed;

            // Notifica ScoreManager
            if (GameServices.ScoreManager != null)
            {
                GameServices.ScoreManager.RegisterMiss(this);
            }
            else
            {
                Debug.LogWarning("[Ball] ScoreManager not available in GameServices");
            }

            // Feedback visual
            StartCoroutine(MissFeedback());
        }

        /// <summary>
        /// Chamado quando o jogador toca nos destroy_landmarks
        /// </summary>
        public void OnDestroy()
        {
            if (state != BallState.Active) return;

            state = BallState.Destroyed;
            
            // Registra tempo exato da colisão
            destroyTime = Time.time - gameStartTime;

            // Notifica ScoreManager
            if (GameServices.ScoreManager != null)
            {
                GameServices.ScoreManager.RegisterDestroy(this);
            }
            else
            {
                Debug.LogWarning("[Ball] ScoreManager not available in GameServices");
            }

            // Instancia efeito de explosão (20% maior, sem mudar a cor)
            SpawnDestroyExplosionEffect();

            // Remove da lista de bolas ativas IMEDIATAMENTE
            if (gameManager != null)
            {
                gameManager.RemoveBall(this);
            }

            // Desabilita visualmente IMEDIATAMENTE
            if (image != null)
            {
                image.enabled = false;
            }

            // Desabilita GameObject e destrói
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        /// <summary>
        /// Instancia o efeito de explosão de hit com a cor e tamanho da bola
        /// </summary>
        private void SpawnHitExplosionEffect(bool isHitAll)
        {
            if (hitExplosionEffectPrefab == null) return;
            
            // Não cria efeitos se a cena está sendo destruída
            if (this == null || gameObject == null || !gameObject.scene.isLoaded) return;

            GameObject effect = Instantiate(hitExplosionEffectPrefab);

            BallHitEffects hitEffects = effect.GetComponent<BallHitEffects>();
            if (hitEffects != null)
            {
                if(!isHitAll) hitEffects.PlayHitEffect(this);
                else hitEffects.PlayHitAllEffect(this);
            }

            // Posiciona no canvas usando o mesmo parent da bola
            RectTransform effectRect = effect.GetComponent<RectTransform>();
            if (effectRect != null)
            {
                // Determina o parent (usa o mesmo da bola ou busca Canvas)
                Transform parent = null;
                if (rectTransformUI != null && rectTransformUI.parent != null)
                {
                    parent = rectTransformUI.parent;
                }
                else
                {
                    Canvas canvas = effectRect.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        parent = canvas.transform;
                    }
                }
                
                if (parent != null)
                {
                    effect.transform.SetParent(parent, false);
                }
                
                // Obtém Canvas root para cobrir tela inteira
                Canvas canvasRoot = effectRect.GetComponentInParent<Canvas>();
                if (canvasRoot != null)
                {
                    RectTransform canvasRect = canvasRoot.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        // Usa Canvas root como referência para cobrir tela inteira
                        Vector2 canvasPos = CoordinateConverter.NormalizedToCanvasPosition(normalizedPosition, canvasRect);
                        effectRect.anchorMin = Vector2.zero;
                        effectRect.anchorMax = Vector2.zero;
                        effectRect.pivot = new Vector2(0.5f, 0.5f);
                        effectRect.anchoredPosition = canvasPos;
                    }
                }
            }

           
           
            /*
            // Aplica cor da bola
            Image img = effect.GetComponent<Image>();
            if (img != null && data != null)
            {
                img.color = data.Color;
            }
            */

           // Adiciona script de autodestruição se não existir
           if (effect.GetComponent<ExplosionEffect>() == null)
           {
               effect.AddComponent<ExplosionEffect>();
           }

           // Ajusta o tamanho da partícula
           if (data != null)
           {
               effect.transform.localScale *= data.Size;
           }

           /*
           // Ajusta o tamanho
           if (data != null)
           {
               effect.transform.localScale = Vector3.one * data.Size;
           }
           */
        }

        /// <summary>
        /// Instancia o efeito de explosão de destroy (20% maior, mantém cor original)
        /// </summary>
        private void SpawnDestroyExplosionEffect()
        {
            if (destroyExplosionEffectPrefab == null) return;
            
            // Não cria efeitos se a cena está sendo destruída
            if (this == null || gameObject == null || !gameObject.scene.isLoaded) return;

            GameObject effect = Instantiate(destroyExplosionEffectPrefab);

            BallDestroyEffects destroyEffects = effect.GetComponent<BallDestroyEffects>();
            if (destroyEffects != null)
            {
                destroyEffects.PlayDestroyAllEffect(this);
            }

            // Posiciona no canvas usando o mesmo parent da bola
            RectTransform effectRect = effect.GetComponent<RectTransform>();
            if (effectRect != null)
            {
                // Determina o parent (usa o mesmo da bola ou busca Canvas)
                Transform parent = null;
                if (rectTransformUI != null && rectTransformUI.parent != null)
                {
                    parent = rectTransformUI.parent;
                }
                else
                {
                    Canvas canvas = effectRect.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        parent = canvas.transform;
                    }
                }
                
                if (parent != null)
                {
                    effect.transform.SetParent(parent, false);
                }
                
                // Obtém Canvas root para cobrir tela inteira
                Canvas canvasRoot = effectRect.GetComponentInParent<Canvas>();
                if (canvasRoot != null)
                {
                    RectTransform canvasRect = canvasRoot.GetComponent<RectTransform>();
                    if (canvasRect != null)
                    {
                        // Usa Canvas root como referência para cobrir tela inteira
                        Vector2 canvasPos = CoordinateConverter.NormalizedToCanvasPosition(normalizedPosition, canvasRect);
                        effectRect.anchorMin = Vector2.zero;
                        effectRect.anchorMax = Vector2.zero;
                        effectRect.pivot = new Vector2(0.5f, 0.5f);
                        effectRect.anchoredPosition = canvasPos;
                    }
                }
            }
            
            // Adiciona script de autodestruição se não existir
            if (effect.GetComponent<ExplosionEffect>() == null)
            {
                effect.AddComponent<ExplosionEffect>();
            }

            // Ajusta o tamanho para ser 20% maior que a bola
            if (data != null)
            {
                float effectSize = data.Size * 1.2f; // 20% maior
                effect.transform.localScale = Vector3.one * effectSize;
            }
        }

        private System.Collections.IEnumerator MissFeedback()
        {
            // Fade out simples
            float duration = 0.2f;
            float elapsed = 0f;
            Color startColor = image != null ? image.color : Color.white;

            AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.bubbleDisappear));

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (image != null)
                {
                    image.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Retorna a posição em coordenadas TOP-LEFT normalized (0-1)
        /// Compatível com landmarks do MediaPipe e BallData do Firebase
        /// </summary>
        public Vector2 GetNormalizedPosition()
        {
            // Retorna posição TOP-LEFT normalized (0-1)
            // Mesma origem que MediaPipe e BallData!
            return normalizedPosition;
        }

        /// <summary>
        /// Verifica se está ativa e pode ser destruída por landmarks
        /// </summary>
        public bool CanBeDestroyed()
        {
            return state == BallState.Active && data.DestroyLandmarks != null && data.DestroyLandmarks.Length > 0;
        }

        /// <summary>
        /// Retorna o sprite apropriado baseado no tipo de objeto especial
        /// </summary>
        private Sprite GetSuperSprite(BallData ballData)
        {
            if (ballData.IsHitSuper) return hitSuperSprite;
            if (ballData.IsDestroyAllSuper) return destroyAllSuperSprite;
            if (ballData.IsHitAllSuper) return hitAllSuperSprite;
            if (ballData.IsAvoidSuper) return destroySuperSprite; // avoid usa destroy-super
            
            return null;
        }


        void OnDrawGizmosSelected()
        {
            // Debug: desenha direção da bola
            if (data != null && state == BallState.Active)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, velocity.normalized * 2f);
            }
        }
    }
}

