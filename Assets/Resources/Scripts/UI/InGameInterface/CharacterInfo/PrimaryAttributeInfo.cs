using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrimaryAttributeInfo : MonoBehaviour
{
    public Game game;
    private Text primaryAttributeText;

    private void Start()
    {
        primaryAttributeText = transform.GetChild(0).GetComponent<Text>();
    }

    private void Update()
    {
        this.primaryAttributeText.text = "";
        if (game.player != null)
        {
            Character characterComponent = game.player.GetComponent<Character>();
            if (characterComponent != null)
            {
                string[] primaryAttributeText = new string[7];
                primaryAttributeText[0] = "Health:\t\t\t\t\t" + ((int)characterComponent.GetAttribute("Health").GetValue()).ToString() + "/" + ((int)characterComponent.GetAttribute("Health").maxValue).ToString();
                primaryAttributeText[1] = "Stamina:\t\t\t\t" + ((int)characterComponent.GetAttribute("Stamina").GetValue()).ToString() + "/" + ((int)characterComponent.GetAttribute("Stamina").maxValue).ToString();
                primaryAttributeText[2] = "Encumbrance:\t\t" + ((float)Math.Round(characterComponent.GetAttribute("Encumbrance").GetValue(), 1)).ToString() + "/" + ((float)Math.Round(characterComponent.MaxEncumbrance, 1)).ToString();
                primaryAttributeText[3] = "Action points:\t\t" + ((int)characterComponent.actionPoints).ToString() + "/" + ((int)characterComponent.GetMaxActionPoints()).ToString();
                primaryAttributeText[4] = "Turns passed: \t\t" + game.turnsPassed.ToString();
                primaryAttributeText[5] = "Experience:\t\t\t" + characterComponent.GetExperience() + "/" + characterComponent.GetXPToNextLevel();
                primaryAttributeText[6] = "Level:\t\t\t\t\t" + ((int)characterComponent.GetAttribute("Level").GetValue()).ToString() + "/" + ((int)characterComponent.GetAttribute("Level").maxValue).ToString();
                foreach (string s in primaryAttributeText)
                    this.primaryAttributeText.text += s + '\n';
            }
        }
    }
}