using BubblesGame.Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BubblesGame
{
    public class BallDestroyEffects : BallEffects
    {
        [SerializeField] protected GameObject destroyAllEffect;
        [SerializeField] private Color SuperBallColorDestroyAll;
        public void PlayDestroyAllEffect(Ball ball)
        {
            base.PlayEffect(ball);
            ballColor = SuperBallColorDestroyAll;
            InstantiateEffect(normalHitEffect);

            ballColor = Color.white;
            InstantiateEffect(destroyAllEffect); 
        }

      
    }
}
