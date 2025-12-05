using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnButtonUI : MonoBehaviour
{
    AudioManager audioManager;
    public void GoToScene(string sceneName)
    {
        audioManager.PlaySFX(audioManager.MenuSelect);
        SceneManager.LoadScene(sceneName);
    }

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }
}
