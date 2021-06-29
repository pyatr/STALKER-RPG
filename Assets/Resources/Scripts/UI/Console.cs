using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Console : MonoBehaviour
{
    public World world;
    int displayedLines = 25;
    //bool playerInfoWasActive = false;
    Text consoleText;
    InputField inputField;

    public void OnDisable()
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
        world = World.GetInstance();
    }

    void ProcessCommand(string s)
    {
        GameObject player = world.characterController.ControlledCharacter;
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
            if (command[0] == "aienabled")
            {
                Game.Instance.AIenabled = !Game.Instance.AIenabled;
                UpdateLog("AI enabled = " + Game.Instance.AIenabled.ToString());
                return;
            }
            if (command[0] == "giveall")
            {
                foreach (InfoBlock weapon in Game.Instance.Weapons.subBlocks)
                    world.CreateItem(weapon.name, true);
                foreach (InfoBlock LBE in Game.Instance.LBEitems.subBlocks)
                    world.CreateItem(LBE.name, true);
                foreach (InfoBlock caliber in Game.Instance.Calibers.subBlocks)
                    for (int i = 0; i < 4; i++)
                        world.CreateItem(caliber.name, true);
                foreach (InfoBlock magazine in Game.Instance.Magazines.subBlocks)
                    for (int i = 0; i < 4; i++)
                        if (!magazine.values.Contains("integral"))
                            world.CreateItem(magazine.name, true);
                foreach (InfoBlock item in Game.Instance.Items.subBlocks)
                    for (int i = 0; i < 4; i++)
                        world.CreateItem(item.name, true);
            }
            return;
        }
        if (command.Length > 1)
        {
            if (command[0] == "givecharacterpoints")
            {
                int points = int.Parse(command[1]);
                if (points > 0)
                    playerCharacterComponent.freeCharacterPoints += points;
                return;
            }
            if (command[0] == "givexp")
            {
                int xp = 0;
                if (int.TryParse(command[1], out  xp))
                    playerCharacterComponent.AddExperience(xp);
                return;
            }
            if (command[0] == "givemoney")
            {
                int money = 0;
                if (int.TryParse(command[1], out money))
                    playerCharacterComponent.money += money;
                return;
            }
            if (command[0] == "create")
            {
                world.CreateItem(command[1], true);
                return;
            }
            if (command[0] == "spawn")
            {
                world.CreateCharacter(command[1], (Vector2)player.transform.localPosition + new Vector2(0, Game.Instance.cellSize.y));
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
            if (command[0] == "move")
            {
                Direction direction = Direction.C;
                Enum.TryParse(command[1], true, out direction);
                if (direction != Direction.C)
                    playerCharacterComponent.TryMove(direction);
                return;
            }
            if (command[0] == "remember" && command[1] == "weapons")
            {
                foreach (InfoBlock weapon in Game.Instance.Weapons.subBlocks)
                    world.CreateItem(weapon.name, true);
                return;
            }
            //if (command[0] == "save")
            //{
            //    world.GetComponent<Save>().SaveGame(int.Parse(command[1]));
            //    return;
            //}
            //if (command[0] == "load")
            //{
            //    world.GetComponent<Save>().LoadGame(int.Parse(command[1]));
            //    return;
            //}
        }
        UpdateLog("Command " + s + " not recognized!");
    }

    void UpdateLog(string s)
    {
        if (s != "")
        {
            consoleText.text = "";
            world.log.UpdateLog(s);
            for (int i = Mathf.Clamp(world.log.log.Count - displayedLines, 0, world.log.log.Count - displayedLines); i < world.log.log.Count; i++)
            {
                consoleText.text += world.log.log[i];
                if (i < world.log.log.Count - 1)
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