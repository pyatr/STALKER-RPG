using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecondaryAttributeInfo : MonoBehaviour
{
    public Game game;

    private Character characterComponent;
    private Text secondaryAttributeText;

    private void Start()
    {
        characterComponent = game.characterController.ControlledCharacter.GetComponent<Character>();
        secondaryAttributeText = transform.GetChild(0).GetComponent<Text>();
    }

    private void Update()
    {
        this.secondaryAttributeText.text = "";
        if (game.player != null)
        {
            Character characterComponent = game.player.GetComponent<Character>();
            if (characterComponent != null)
            {
                string[,] secondaryAttributeText = new string[2, 4];
                secondaryAttributeText[0, 0] = "STR: " + ((int)characterComponent.GetAttribute("Strength").GetValue()).ToString();
                secondaryAttributeText[0, 1] = "DEX: " + ((int)characterComponent.GetAttribute("Dexterity").GetValue()).ToString();
                secondaryAttributeText[0, 2] = "END: " + ((int)characterComponent.GetAttribute("Endurance").GetValue()).ToString();
                secondaryAttributeText[0, 3] = "PER: " + ((int)characterComponent.GetAttribute("Perception").GetValue()).ToString();
                secondaryAttributeText[1, 0] = "SOC: " + ((int)characterComponent.GetAttribute("Social").GetValue()).ToString();
                secondaryAttributeText[1, 1] = "MRK: " + ((int)characterComponent.GetAttribute("Marksmanship").GetValue()).ToString();
                secondaryAttributeText[1, 2] = "MED: " + ((int)characterComponent.GetAttribute("Medical").GetValue()).ToString();
                secondaryAttributeText[1, 3] = "MEC: " + ((int)characterComponent.GetAttribute("Mechanical").GetValue()).ToString();
                for (int i = 0; i < 4; i++)
                    this.secondaryAttributeText.text += secondaryAttributeText[0, i] + " | " + secondaryAttributeText[1, i] + '\n';
            }
        }
    }
}