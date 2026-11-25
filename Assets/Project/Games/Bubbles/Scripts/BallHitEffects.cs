using BubblesGame.Data;
using UnityEngine;

namespace BubblesGame
{
    public class BallHitEffects : BallEffects
    {
        [SerializeField] protected GameObject hitSuperEffect;
        [SerializeField] protected GameObject hitAllEffect;
        [SerializeField] protected GameObject destroySuperEffect;
        [SerializeField] protected GameObject destroyAllEffect;
        [SerializeField] private Color SuperBallColorHitSuper;
        [SerializeField] private Color SuperBallColorHitAll;
        [SerializeField] private Color SuperBallColorDestroySuper;
        [SerializeField] private Color SuperBallColorDestroyAll;


        public void PlayHitEffect(Ball ball)
        {
            base.PlayEffect(ball);

            // Determina a cor das bolas caso sejam super bolas
            if (parentBall.Data.IsAvoidSuper) ballColor = SuperBallColorDestroySuper;
            if (parentBall.Data.IsDestroyAllSuper) ballColor = SuperBallColorDestroyAll;
            if (parentBall.Data.IsHitSuper) ballColor = SuperBallColorHitSuper;
            if (parentBall.Data.IsHitAllSuper) ballColor = SuperBallColorHitAll;

            InstantiateEffect(normalHitEffect);

            // Som de hit normal
            if (!parentBall.Data.IsSuperBall)
                AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.hitNormal));

            // Efeitos especiais de HitSuper
            if (parentBall.Data.IsHitSuper)
            {
                ballColor = Color.white;
                InstantiateEffect(hitSuperEffect);
                AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.hitSuper));
                return;
            }

            // Efeitos especiais de HitAllSuper
            if (parentBall.Data.IsHitAllSuper)
            {
                ballColor = Color.white;
                InstantiateEffect(hitAllEffect);
                AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.hitAll));

                return;
            }

            // Efeitos especiais de DestroySuper
            if (parentBall.Data.IsAvoidSuper)
            {
                ballColor = Color.white;
                InstantiateEffect(destroySuperEffect);
                AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.destroySuper));
                return;
            }

            // Efeitos especiais de DestroyAllSuper
            if (parentBall.Data.IsDestroyAllSuper)
            {
                ballColor = Color.white;
                InstantiateEffect(destroyAllEffect);
                AudioManager.Instance.PlayOneShot2D(BubblesAudioReference.instance.GetSFXClip(BubblesSFXenum.destroyAll));
                return;
            }
        }


        // Efeito exclusivo para bolas HitAll
        public void PlayHitAllEffect(Ball ball)
        {

            base.PlayEffect(ball);
            ballColor = SuperBallColorHitAll; // seta a cor da particula de explosao
            InstantiateEffect(normalHitEffect);

            ballColor = Color.white; // reseta a cor da particula hit all para branco
            InstantiateEffect(hitAllEffect);
        }
}
}
