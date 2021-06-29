using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrimaryAttributeInfo : MonoBehaviour
{
    private Text primaryAttributeText;
    private World world;

    private void Start()
    {
        primaryAttributeText = transform.GetChild(0).GetComponent<Text>();
        world = World.GetInstance();
    }

    private void Update()
    {
        this.primaryAttributeText.text = "";
        if (world.Player != null)
        {
            Character characterComponent = world.Player.GetComponent<Character>();
            if (characterComponent != null)
            {
                List<string> primaryAttributeText = new List<string>(7);
                primaryAttributeText.Add("Health:\t\t\t\t\t" + ((int)characterComponent.GetAttribute("Health").Value).ToString() + "/" + ((int)characterComponent.GetAttribute("Health").MaxValue).ToString());
                primaryAttributeText.Add("Stamina:\t\t\t\t" + ((int)characterComponent.GetAttribute("Stamina").Value).ToString() + "/" + ((int)characterComponent.GetAttribute("Stamina").MaxValue).ToString());
                primaryAttributeText.Add("Encumbrance:\t\t" + ((float)Math.Round(characterComponent.GetAttribute("Encumbrance").Value, 1)).ToString() + "/" + ((float)Math.Round(characterComponent.MaxEncumbrance, 1)).ToString());
                primaryAttributeText.Add("Turns passed: \t\t" + world.turnsPassed.ToString());
                primaryAttributeText.Add("Money:\t\t\t\t\t" + characterComponent.money.ToString());
                primaryAttributeText.Add("Experience:\t\t\t" + characterComponent.experience.ToString() + "/" + characterComponent.GetXPToNextLevel(characterComponent.Level + 1).ToString());
                primaryAttributeText.Add("Level:\t\t\t\t\t" + ((int)characterComponent.GetAttribute("Level").Value).ToString() + "/" + ((int)characterComponent.GetAttribute("Level").MaxValue).ToString());
                foreach (string s in primaryAttributeText)
                    this.primaryAttributeText.text += s + '\n';
            }
        }
    }
}