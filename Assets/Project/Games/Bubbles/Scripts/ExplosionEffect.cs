using UnityEngine;

namespace BubblesGame
{
    /// <summary>
    /// Gerencia efeito de explosão: toca animação e se autodestrói
    /// </summary>
    public class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 1f; // Tempo até destruir (caso animação não dispare evento)
        [SerializeField] private bool destroyOnAnimationEnd = true;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = 100; // Alto para aparecer na frente
        
        [Header("Audio")]
        [SerializeField] private AudioClip explosionSound; // Configure no Inspector: pop.ogg para HitExplosionEffect, dumpster.ogg para DestroyExplosionEffect
        private AudioSource audioSource;
        
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private float spawnTime;
        private System.Collections.IEnumerator destroyCoroutine;
        
        void Awake()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spawnTime = Time.time;
            
            // Configura AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
            
            // Configura sorting layer para aparecer na frente
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingLayerName = sortingLayerName;
                spriteRenderer.sortingOrder = sortingOrder;
            }
            
            if (animator == null)
            {
                Debug.LogWarning("[ExplosionEffect] No Animator found!");
            }
        }
        
        void Start()
        {
            // Toca o som da explosão quando começa
            PlayExplosionSound();
            
            // Fallback: destrói após lifetime se animação não destruir antes
            destroyCoroutine = DestroyAfterLifetime();
            StartCoroutine(destroyCoroutine);
        }
        
        private System.Collections.IEnumerator DestroyAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        
        void OnDestroy()
        {
            // Cancela corrotinas para evitar criação de objetos durante destruição da cena
            if (destroyCoroutine != null)
            {
                StopCoroutine(destroyCoroutine);
            }
            StopAllCoroutines();
        }
        
        void Update()
        {
            // Verifica se animação terminou (se tiver Animator)
            if (destroyOnAnimationEnd && animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                // Se a animação terminou (normalizedTime >= 1) e não está em loop
                if (stateInfo.normalizedTime >= 1.0f && !animator.IsInTransition(0))
                {
                    Destroy(gameObject);
                }
            }
        }
        
        /// <summary>
        /// Chamado via Animation Event (se configurado no Animation Clip)
        /// </summary>
        public void OnAnimationComplete()
        {
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Toca o som da explosão (configurado no Inspector)
        /// </summary>
        private void PlayExplosionSound()
        {
            if (explosionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(explosionSound);
            }
        }
    }
}
