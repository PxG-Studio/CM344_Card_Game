using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    AudioManager audioManager;
    public void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        audioManager.PlaySFX(audioManager.MenuSelect);
    }
}
