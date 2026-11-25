using System.ComponentModel;
using UnityEngine;

public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    [SerializeField, Range(0, 1)] private float musicVolume = 1f;
    [SerializeField, Range(0, 1)] private float sfxVolume = 1f;
    private bool audioEnabled = true;
    private AudioSource musicInstance;

    protected override void Awake()
    {
        base.Awake();
        CreateMusicInstance();
    }

    public void SetAudioEnabled(bool enabled)
    {
        audioEnabled = enabled;
        if (!audioEnabled && musicInstance.isPlaying)
        {
            musicInstance.Pause();
        }
        else if (audioEnabled && !musicInstance.isPlaying && musicInstance.clip != null)
        {
            musicInstance.Play();
        }
    }

    /// <summary>
    /// Toca um SFX 2D uma única vez
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="pitch"></param>
    private void PlayOneShot2D(AudioClip clip, float pitch = 1f, float volumeEqualizer = 1f)
    {
        if (!audioEnabled || sfxVolume == 0)
            return;

        if (clip == null)
        {
            Debug.LogWarning("No SFX clip provided to play.");
            return;
        }

        // Cria um GameObject temporário para o SFX
        GameObject sfxInstance = new GameObject("SFXInstance");

        // Adiciona um AudioSource ao GameObject e configura o clip
        AudioSource audioSource = sfxInstance.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.pitch = pitch;
        audioSource.volume = sfxVolume * volumeEqualizer;
        audioSource.Play();

        // Destroi o GameObject após o áudio terminar de tocar
        Destroy(sfxInstance, clip.length);
    }

    public void PlayOneShot2D(SFXData sfxData)
    {
        PlayOneShot2D(sfxData.Clip, sfxData.Pitch, sfxData.VolumeEqualizer);
    }

    /// <summary>
    /// Toca uma musica em loop
    /// </summary>
    /// <param name="clip"></param>
    public void PlayMusic(AudioClip clip)
    {
        if (!audioEnabled)
            return;

        if (musicInstance.clip == clip && musicInstance.isPlaying)
        {
            Debug.LogWarning("Music is already playing this clip.");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("No music clip provided to play.");
            return;
        }

        musicInstance.clip = clip;
        musicInstance.volume = musicVolume;
        musicInstance.loop = true;
        musicInstance.Play();
    }


    /// <summary>
    /// Seta o volume da música, entre 0 e 1.
    /// </summary>
    /// <param name="volume"></param>
    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        musicVolume = volume;
        if (musicInstance != null)
        {
            musicInstance.volume = musicVolume;
        }
    }

    /// <summary>
    /// Seta o volume dos SFX, entre 0 e 1.
    /// </summary>
    /// <param name="volume"></param>
    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        sfxVolume = volume;
    }

    private void CreateMusicInstance()
    {
        if (musicInstance == null)
        {
            // Instancia um GameObject para o AudioSource de música
            GameObject musicInstanceGameObject = new GameObject("MusicInstance");
            musicInstanceGameObject.transform.SetParent(this.transform);

            // Adiciona o AudioSource ao GameObject e faz o cache
            musicInstance = musicInstanceGameObject.AddComponent<AudioSource>();

            // Configurações iniciais do AudioSource
            musicInstance.loop = true;
            musicInstance.playOnAwake = false;
        }
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        // Atualiza o volume do AudioSource de música no editor
        if (musicInstance != null)
        {
            musicInstance.volume = musicVolume;
        }
    }

#endif

}

