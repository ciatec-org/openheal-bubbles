using System.Collections.Generic;
using UnityEngine;
public enum UISfxEnum
{
    mouseHover,
    mousePressed
}

[System.Serializable]
public class UISFX : SFX<UISfxEnum>
{
   
}

public class AudioReferenceUI : AudioReference<UISfxEnum, UISFX>
{
    public static AudioReferenceUI instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }

   
    }

}

