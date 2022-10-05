using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GroundCursorMode
{
    Move,
    Attack,
    MeleeAttack
}

public class GroundCursor : MonoBehaviour
{
    public Text APcost;
    public Text accuracy;
    private World world;

    private GroundCursorMode mode;
    public GroundCursorMode GroundCursorMode { get { return mode; } }

    private void Start()
    {
        world = World.GetInstance();
    }

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
            case GroundCursorMode.MeleeAttack:
                GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/melee_attack_mode");
                APcost.GetComponent<TextFollower>().offset = new Vector2(0, 0.56f / 2f);
                break;
        }
    }

    private void Update()
    {
        if (world != null)
        {
            if (world.Player == null)
                return;
            Character playerCharacter = world.Player.GetComponent<Character>();
            if (playerCharacter == null)
                return;
            float APtouse = 0;
            switch (mode)
            {
                case GroundCursorMode.Attack:
                    transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                    GameObject playerWeapon = playerCharacter.Weapon;
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
                            float accuracyValue = Mathf.Max(playerCharacter.GetAccuracy(), 0);
                            float step = 9;
                            accuracy.text = accuracyValue.ToString() + "/25";
                            accuracy.color = new Color32((byte)Mathf.Max(0, 250 - accuracyValue * 9), (byte)Mathf.Min(255, 30 + step * accuracyValue), 30, 255);
                        }
                    }
                    break;
                case GroundCursorMode.Move:
                    {
                        accuracy.text = "";
                        Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                        int cellsX = (int)(targetPosition.x / Game.Instance.cellSize.x);
                        int cellsY = (int)(targetPosition.y / Game.Instance.cellSize.y);
                        int offsetX = targetPosition.x < 0 ? -1 : 1;
                        int offsetY = targetPosition.y < 0 ? -1 : 1;
                        targetPosition = new Vector2((float)Math.Round(Game.Instance.cellSize.x * (cellsX + 0.5f * offsetX), 2), (float)Math.Round(Game.Instance.cellSize.y * (cellsY + 0.5f * offsetY), 2));
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
                case GroundCursorMode.MeleeAttack:
                    transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
                    GameObject playerMeleeWeapon = playerCharacter.Weapon;
                    if (playerMeleeWeapon != null)
                    {
                        MeleeWeapon meleeWeaponComponent = playerMeleeWeapon.GetComponent<MeleeWeapon>();
                        if (meleeWeaponComponent != null)
                        {
                            APtouse = meleeWeaponComponent.actionPointsToAttack;
                            APcost.text = APtouse.ToString();
                            if (APtouse <= playerCharacter.actionPoints)
                                APcost.color = new Color32(255, 212, 39, 255);
                            else
                                APcost.color = new Color32(212, 35, 75, 255);
                            //float accuracyValue = Mathf.Max(playerCharacter.GetAccuracy(), 0);
                            //float step = 9;
                            accuracy.text = "";// accuracyValue.ToString() + "/25";
                            //accuracy.color = new Color32((byte)Mathf.Max(0, 250 - accuracyValue * 9), (byte)Mathf.Min(255, 30 + step * accuracyValue), 30, 255);
                        }
                    }
                    break;
            }
        }
    }
}