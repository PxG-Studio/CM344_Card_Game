using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    public AudioClip PlayCard;
    public AudioClip DrawCard;
    public AudioClip FireCaptureCard;
    public AudioClip EarthCaptureCard;
    public AudioClip MenuSelect;
    public AudioClip CoinFlip;
    public AudioClip Tie;

    private void Start()
    {
        musicSource.Play();

    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }
}
