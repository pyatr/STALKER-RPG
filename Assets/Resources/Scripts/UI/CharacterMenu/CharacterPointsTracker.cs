using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterPointsTracker : MonoBehaviour
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
            textComponent.text = player.freeCharacterPoints.ToString();
        else
            textComponent.text = "";
    }
}