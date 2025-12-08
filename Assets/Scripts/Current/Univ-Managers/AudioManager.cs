using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    public AudioClip background;
    public AudioClip PlayCard;
    public AudioClip DrawCard;
    public AudioClip FireCaptureCard;
    public AudioClip EarthCaptureCard;
    public AudioClip MenuSelect;
    public AudioClip CoinFlip;
    public AudioClip Tie;
    public AudioClip Victory;
    public AudioClip OnStartup;

    private void Start()
    {
        musicSource.clip = background;
        musicSource.Play();

    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
