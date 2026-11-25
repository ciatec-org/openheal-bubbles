using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class AudioSourceElementUI : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (AudioManager.Instance == null || AudioReferenceUI.instance == null)
        {
            Debug.LogWarning("AudioManager or AudioReferenceUI instance not found.");
            return;
        }

        AudioManager.Instance.PlayOneShot2D(AudioReferenceUI.instance.GetSFXClip(UISfxEnum.mouseHover));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (AudioManager.Instance == null || AudioReferenceUI.instance == null)
        {
            Debug.LogWarning("AudioManager or AudioReferenceUI instance not found.");
            return;
        }

        AudioManager.Instance.PlayOneShot2D(AudioReferenceUI.instance.GetSFXClip(UISfxEnum.mousePressed));
    }

}

