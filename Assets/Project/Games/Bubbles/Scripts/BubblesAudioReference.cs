using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BubblesGame
{
    public enum BubblesSFXenum
    {
        bubbleAppear,
        bubbleDisappear,
        hitNormal,
        hitSuper,
        hitAll,
        destroySuper,
        destroyAll
    }

    [System.Serializable]
    public class BubblesSFX : SFX<BubblesSFXenum>
    {
   
    }

    public class BubblesAudioReference : AudioReference<BubblesSFXenum, BubblesSFX>
    {
        public static BubblesAudioReference instance;

        private void Awake()
        {
            //instancia temporaria apenas para a cena do jogo
            instance = this; 
        }
    }
}
