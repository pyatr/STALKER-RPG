using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CombatPanel : MonoBehaviour
{
    private Image panelBG;
    private Text actionPointsText;
    private Text thatOtherText;
    private World world;

    private void Start()
    {
        thatOtherText = transform.GetChild(0).GetComponent<Text>();
        actionPointsText = transform.GetChild(1).GetComponent<Text>();
        panelBG = GetComponent<Image>();
        world = World.GetInstance();
    }

    private void Update()
    {
        if (world.Player != null)
        {
            Character player = world.Player.GetComponent<Character>();
            if (player.IsInCombat())
            {
                thatOtherText.enabled = true;
                actionPointsText.enabled = true;
                panelBG.enabled = true;
                actionPointsText.text = "Action points: " + ((int)player.actionPoints).ToString() + "/" + ((int)player.GetMaxActionPoints()).ToString();
                if (player.enabled)
                {
                    thatOtherText.text = "Player turn";
                    thatOtherText.color = Color.white;
                    actionPointsText.color = Color.white;
                }
                else
                {
                    thatOtherText.text = "Waiting for turn";
                    thatOtherText.color = Color.yellow;
                    actionPointsText.color = Color.yellow;
                }
                return;
            }
        }
        thatOtherText.enabled = false;
        actionPointsText.enabled = false;
        panelBG.enabled = false;
    }
}