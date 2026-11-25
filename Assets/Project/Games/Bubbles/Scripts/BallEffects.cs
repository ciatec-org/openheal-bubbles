using UnityEngine;

namespace BubblesGame
{
    public class BallEffects : MonoBehaviour
    {
        [SerializeField] protected GameObject normalHitEffect;
       
        protected Ball parentBall;
        protected Color ballColor;
        protected GameObject instantiatedEffect;
    
        protected virtual void PlayEffect(Ball ball)
        {
            parentBall = ball;

            if(parentBall.Data != null) 
                ballColor = parentBall.Data.Color;
            else
                ballColor = Color.white;
        }

        protected void InstantiateEffect(GameObject effect)
        {

            instantiatedEffect = Instantiate(effect, parentBall.transform.position, Quaternion.identity);
            ParticleSystem particle1 = instantiatedEffect.GetComponent<ParticleSystem>();
            ParticleSystem particle2 = null;

            if (instantiatedEffect.transform.childCount > 0)
                particle2 = instantiatedEffect.transform.GetChild(0).GetComponent<ParticleSystem>();

            if (particle1 != null)
            {
                var mainModule = particle1.main;
                mainModule.startColor = ballColor;
            }

            if (particle2 != null)
            {
                var mainModule = particle2.main;
                mainModule.startColor = ballColor;
            }

            Destroy(instantiatedEffect, 3);
        }
    }
}
