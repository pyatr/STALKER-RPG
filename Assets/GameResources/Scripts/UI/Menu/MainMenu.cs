using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Awake()
    {
        Game loading = Game.Instance;
    }

    public void StartNewGame()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    public void QuitGame()
    {
        Application.Quit();
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
        SwitchTo("MainMenu");
    }

    public void SwitchToSettings()
    {
        SwitchTo("Settings");
    }

    public void SwitchRecords()
    {
        SwitchTo("Records");
    }

    public void SwitchToGameLoad()
    {
        SwitchTo("GameLoadMenu");
    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
            SwitchToMenu();
    }
}