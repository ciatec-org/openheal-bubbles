using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SFX<TEnum>
{
    [SerializeField] private TEnum sfxEnum;
    [SerializeField] private List<AudioClip> clips;
    [SerializeField] private float minPitch = .8f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField, Range(0, 1)] private float volumeEqualizer = 1f;

    public TEnum SfxEnum => sfxEnum;
    public List<AudioClip> Clips => clips;
    public float MinPitch => minPitch;
    public float MaxPitch => maxPitch;
    public float VolumeEqualizer => volumeEqualizer;

}

public class SFXData
{
    public AudioClip Clip;
    public float Pitch;
    public float VolumeEqualizer;
}

public abstract class AudioReference<TEnum, TSFX> : MonoBehaviour where TSFX : SFX<TEnum>
{
    [SerializeField] protected AudioClip levelMusic;
    [SerializeField] protected List<TSFX> sfxList;
    
    protected void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("AudioManager instance not found in the scene.");
        }
        else if (levelMusic != null)
        {
            AudioManager.Instance.PlayMusic(levelMusic);
        }
    }

    public SFXData GetSFXClip(TEnum sfxEnum)
    {
        SFXData data = new SFXData();
        foreach (TSFX sfx in sfxList)
        {
            if (EqualityComparer<TEnum>.Default.Equals(sfxEnum, sfx.SfxEnum))
            {
                int randomIndex = Random.Range(0, sfx.Clips.Count);
                data.Pitch = Random.Range(sfx.MinPitch, sfx.MaxPitch);
                data.Clip = sfx.Clips[randomIndex];
                data.VolumeEqualizer = sfx.VolumeEqualizer;
                return data;
            }
        }
        data.Pitch = 1f;
        data.Clip = null;
        data.VolumeEqualizer = 1f;
        return data;
    }
}

