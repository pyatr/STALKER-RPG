using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMainMenu : MonoBehaviour
{
    private World world;

    private void Start()
    {
        world = World.GetInstance();
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

    public void OnDisable()
    {
        SwitchToMenu();
    }

    public void SwitchToMenu()
    {
        SwitchTo("MenuButtons");
    }

    public void SwitchToSettings()
    {
        SwitchTo("Settings");
    }

    public void SaveGame()
    {
        world.GetComponent<Save>().SaveGame(world.currentSlot);
    }

    public void LoadGame()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
        //world.GetComponent<Save>().LoadGame(world.currentSlot);
    }

    public void SwitchToHelp()
    {
        SwitchTo("HelpWindow");
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}