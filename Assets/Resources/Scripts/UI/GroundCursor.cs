using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GroundCursorMode
{
    Move,
    Attack
}

public class GroundCursor : MonoBehaviour
{
    public Game game;
    public Text APcost;
    private GroundCursorMode mode;
    public GroundCursorMode GroundCursorMode { get { return mode; } }

    public void SwitchCursorMode(GroundCursorMode newMode)
    {
        mode = newMode;
        switch (mode)
        {
            case GroundCursorMode.Attack:
                GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/ironsight");
                APcost.GetComponent<TextFollower>().offset = new Vector2(0, 0.56f / 2f);
                break;
            case GroundCursorMode.Move:
                GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/pathfindingcursor");
                APcost.GetComponent<TextFollower>().offset = Vector2.zero;
                break;
        }
    }


    private void Update()
    {
        if (game != null)
        {
            if (game.player == null)
                return;
            Character playerCharacter = game.player.GetComponent<Character>();
            if (playerCharacter == null)
                return;
            float APtouse = 0;
            switch (mode)
            {
                case GroundCursorMode.Attack:
                    transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                    GameObject playerWeapon = playerCharacter.weapon;
                    if (playerWeapon != null)
                    {
                        Firearm playerWeaponFirearm = playerWeapon.GetComponent<Firearm>();
                        if (playerWeaponFirearm != null)
                        {
                            APtouse = playerWeaponFirearm.GetFireMode().actionPoints;
                            APcost.text = APtouse.ToString();
                            if (APtouse <= playerCharacter.actionPoints)
                                APcost.color = new Color32(255, 212, 39, 255);
                            else
                                APcost.color = new Color32(212, 35, 75, 255);
                        }
                    }
                    break;
                case GroundCursorMode.Move:
                    Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                    int cellsX = (int)(targetPosition.x / game.cellSize.x);
                    int cellsY = (int)(targetPosition.y / game.cellSize.y);
                    int offsetX = targetPosition.x < 0 ? -1 : 1;
                    int offsetY = targetPosition.y < 0 ? -1 : 1;
                    targetPosition = new Vector2((float)Math.Round(game.cellSize.x * (cellsX + 0.5f * offsetX), 2), (float)Math.Round(game.cellSize.y * (cellsY + 0.5f * offsetY), 2));
                    transform.position = targetPosition;
                    if (playerCharacter.IsInCombat())
                    {
                        APtouse = playerCharacter.GetPathMoveCost(playerCharacter.pathFinder.FindPath(targetPosition));
                        if (APtouse <= playerCharacter.actionPoints)
                            APcost.color = new Color32(255, 212, 39, 255);
                        else
                            APcost.color = new Color32(212, 35, 75, 255);
                        APcost.text = ((int)APtouse).ToString();
                    }
                    else
                        APcost.text = "";
                    break;
            }
        }
    }
}