using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
    public Game game;
    int displayedLines = 25;
    //bool playerInfoWasActive = false;
    Text consoleText;
    InputField inputField;

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        inputField.text = "";
        inputField.DeactivateInputField();
        gameObject.SetActive(false);
    }

    public void RemoveBackquote()
    {
        //inputField.text = inputField.textComponent.text.Replace("`", "");
    }

    public void Start()
    {
        consoleText = transform.GetChild(0).GetComponent<Text>();
        inputField = transform.GetChild(1).GetComponent<InputField>();
        int pixelHeight = (int)GetComponent<RectTransform>().rect.height - 7 - 6;
        int fontHeight = consoleText.font.lineHeight + (int)consoleText.lineSpacing + 3;
        displayedLines = pixelHeight / fontHeight;
    }

    void ProcessCommand(string s)
    {
        GameObject player = game.characterController.ControlledCharacter;
        if (player == null)
        {
            UpdateLog("Player does not exist");
            return;
        }
        Character playerCharacterComponent = player.GetComponent<Character>();
        string[] command = s.Split(' ');
        if (command.Length == 1)
        {
            if (command[0] == "dropall")
            {
                playerCharacterComponent.DropAllItems();
                return;
            }
            if (command[0] == "tgm")
            {
                playerCharacterComponent.invulnerable = !playerCharacterComponent.invulnerable;
                UpdateLog(playerCharacterComponent.displayName + " invulnerability = " + playerCharacterComponent.invulnerable);
                return;
            }
            if (command[0] == "die")
            {
                playerCharacterComponent.Die();
                return;
            }
        }
        if (command.Length > 1)
        {
            if (command[0] == "create")
            {
                game.CreateItem(command[1], true);
                return;
            }
            if (command[0] == "givexp")
            {
                int xp = 0;
                if (int.TryParse(command[1], out xp))
                    playerCharacterComponent.AddExperience(xp);
                return;
            }
            if (command[0] == "spawn")
            {
                game.CreateCharacter(command[1], (Vector2)player.transform.localPosition + new Vector2(0, game.cellSize.y));
                return;
            }
            if (command[0] == "modattribute")
            {
                Attribute attributeToMod = playerCharacterComponent.GetAttribute(command[1]);
                if (attributeToMod != null)
                    attributeToMod.Modify(float.Parse(command[2]));
                return;
            }
            if (command[0] == "become")
            {
                playerCharacterComponent.DropAllItems();
                //playerCharacterComponent.LoadCharacter(command[1]);
                //foreach (InfoBlock faction in game.factions.subBlocks)
                //{
                //    if (faction.name == "playerfaction")
                //        if (faction.namesValues.ContainsKey("basedon"))
                //            faction.namesValues["basedon"] = playerCharacterComponent.faction;
                //}
                //game.ReloadDerivativeFactions();
                player.GetComponent<Character>().LoadFaction("playerfaction");
                return;
            }
            if (command[0] == "remember" && command[1] == "weapons")
            {
                foreach (InfoBlock weapon in game.weapons.subBlocks)                
                    game.CreateItem(weapon.name, true);                
                return;
            }
        }
        UpdateLog("Command " + s + " not recognized!");
    }

    void UpdateLog(string s)
    {
        if (s != "")
        {
            consoleText.text = "";
            game.log.UpdateLog(s);
            for (int i = Mathf.Clamp(game.log.log.Count - displayedLines, 0, game.log.log.Count - displayedLines); i < game.log.log.Count; i++)
            {
                consoleText.text += game.log.log[i];
                if (i < game.log.log.Count - 1)
                    consoleText.text += '\n';
            }
        }
    }

    void Update()
    {
        inputField.ActivateInputField();
        if (Input.GetKeyUp(KeyCode.Return))
        {
            UpdateLog(inputField.text);
            ProcessCommand(inputField.text);
            inputField.text = "";
        }
    }
}