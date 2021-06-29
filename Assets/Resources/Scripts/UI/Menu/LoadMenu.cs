using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadMenu : MonoBehaviour
{
    public int slotNumber = 1;

    private void Start()
    {
        string saveSlot = "Save" + slotNumber.ToString();
        string savePath = Application.persistentDataPath + "/Saves/" + saveSlot + "/";
        Transform screenshotObject = transform.Find("Screenshot");
        Text saveInfoText = screenshotObject.Find("SaveInfo").GetComponent<Text>();
        saveInfoText.text += "Save " + slotNumber.ToString() + '\n';
        if (File.Exists(savePath + "save.sav"))
        {
            if (File.Exists(savePath + "screenshot.png"))
            {
                screenshotObject.GetComponent<Image>().enabled = true;
                byte[] imageBytes = File.ReadAllBytes(savePath + "screenshot.png");
                Texture2D screenshot = new Texture2D(1, 1);
                screenshot.LoadImage(imageBytes);
                screenshot.Apply();
                screenshotObject.GetComponent<Image>().enabled = true;
                screenshotObject.GetComponent<Image>().sprite = Sprite.Create(screenshot, new Rect(Vector2.zero, new Vector2(screenshot.width, screenshot.height)), Vector2.one);
                screenshotObject.GetComponent<RectTransform>().sizeDelta = new Vector2(screenshot.width / 4, screenshot.height / 4);
            }
            InfoBlock saveInfo = InfoBlockReader.SplitTextIntoInfoBlocks(TextTools.GetTextFromFileAsString(savePath + "save.sav"));
            saveInfoText.text += "Seed: " + saveInfo.namesValues["seed"] + '\n';
            saveInfoText.text += "Turns passed: " + saveInfo.namesValues["turns_passed"] + '\n';
            saveInfoText.text += File.GetLastWriteTime(savePath + "save.sav"); //saveInfo.namesValues["date"] + '\n';
        }
        else
        {
            screenshotObject.GetComponent<Image>().enabled = false;
            saveInfoText.text += "Save does not exist";
            //screenshotObject.Find("SaveInfo").Find("LoadButton").Find("Text").GetComponent<Text>().text = "Нет сохранения";
        }
    }

    public void LoadGame()
    {
        //int slotNumber = int.Parse(gameObject.name.Split(new char[] { '_' }, 2)[1]);
        if (slotNumber >= 1 && slotNumber < Game.Instance.maxGameSlots)
        {
            string saveSlot = "Save" + slotNumber.ToString();
            string savePath = Application.persistentDataPath + "/Saves/" + saveSlot + "/save.sav";
            if (File.Exists(savePath))
            {
                Game.Instance.loadGameOnStart = slotNumber;
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
            else
            {
                Debug.Log("Save in slot" + slotNumber.ToString() + " does not exist");
            }
        }
    }
}