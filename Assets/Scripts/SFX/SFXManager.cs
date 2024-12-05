using Scripts.Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private SFXPlayer sfxPlayer;
    [SerializeField] private AudioClip btnSFX;
    [SerializeField] private AudioClip dealSFX;
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip slideSFX;

    public static SFXManager Instance;

    private void Start()
    {
        Instance = this;
    }

    public void BtnSFX()
    {
        PlaySFX(btnSFX, 1f);
    }
    
    public void DealSFX()
    {
        PlaySFX(dealSFX, 1f);
    }
    
    public void HitSFX()
    {
        PlaySFX(hitSFX, 1f);
    }
    
    public void SlideSFX()
    {
        PlaySFX(slideSFX, 0.05f);
    }

    private void PlaySFX(AudioClip clip, float volume = 0.5f)
    {
        Instantiate(sfxPlayer.gameObject, transform.position, Quaternion.identity).GetComponent<SFXPlayer>().PlaySFXWithVolume(clip, volume);
    }
}
