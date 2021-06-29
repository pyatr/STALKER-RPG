using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExperienceTracker : MonoBehaviour
{
    private Text textComponent;
    private World world;

    private void Start()
    {
        textComponent = GetComponent<Text>();
        world = World.GetInstance();
    }

    private void Update()
    {
        Character player = world?.Player?.GetComponent<Character>();
        if (player != null)
            textComponent.text = player.experience + "/" + player.GetXPToNextLevel((int)player.GetAttribute("Level").Value + 1);
        else
            textComponent.text = "";
    }
}