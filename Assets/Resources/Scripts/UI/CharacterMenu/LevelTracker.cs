using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelTracker : MonoBehaviour
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
            textComponent.text = ((int)player.GetAttribute("Level").Value).ToString() + "/" + ((int)player.GetAttribute("Level").MaxValue).ToString();
        else
            textComponent.text = "";
    }
}