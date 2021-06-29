using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecondaryAttributeInfo : MonoBehaviour
{
    private Text secondaryAttributeText;
    private World world;

    private void Start()
    {
        secondaryAttributeText = transform.GetChild(0).GetComponent<Text>();
        world = World.GetInstance();
    }

    private void Update()
    {
        this.secondaryAttributeText.text = "";
        if (world.Player != null)
        {
            Character characterComponent = world.Player.GetComponent<Character>();
            if (characterComponent != null)
            {
                string[,] secondaryAttributeText = new string[2, 4];
                secondaryAttributeText[0, 0] = "STR: " + ((int)characterComponent.GetAttribute("Strength").Value).ToString();
                secondaryAttributeText[0, 1] = "DEX: " + ((int)characterComponent.GetAttribute("Dexterity").Value).ToString();
                secondaryAttributeText[0, 2] = "END: " + ((int)characterComponent.GetAttribute("Endurance").Value).ToString();
                secondaryAttributeText[0, 3] = "PER: " + ((int)characterComponent.GetAttribute("Perception").Value).ToString();
                secondaryAttributeText[1, 0] = "SOC: " + ((int)characterComponent.GetAttribute("Social").Value).ToString();
                secondaryAttributeText[1, 1] = "MRK: " + ((int)characterComponent.GetAttribute("Marksmanship").Value).ToString();
                secondaryAttributeText[1, 2] = "MED: " + ((int)characterComponent.GetAttribute("Medical").Value).ToString();
                secondaryAttributeText[1, 3] = "MEC: " + ((int)characterComponent.GetAttribute("Mechanical").Value).ToString();
                for (int i = 0; i < 4; i++)
                    this.secondaryAttributeText.text += secondaryAttributeText[0, i] + " | " + secondaryAttributeText[1, i] + '\n';
            }
        }
    }
}