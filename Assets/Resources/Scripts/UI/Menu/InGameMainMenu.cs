using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMainMenu : MonoBehaviour
{
    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }

    private void SwitchTo(string menuName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject currentObject = transform.GetChild(i).gameObject;
            if (currentObject.name != "Background")
                currentObject.SetActive(currentObject.name == menuName);
        }
    }

    public void SwitchToMenu()
    {
        SwitchTo("MenuButtons");
    }

    public void SwitchToSettings()
    {
        SwitchTo("Settings");
    }

    public void SwitchToGameLoad()
    {
        SwitchTo("GameLoadMenu");
    }

    public void SwitchToHelp()
    {
        SwitchTo("HelpWindow");
    }
}