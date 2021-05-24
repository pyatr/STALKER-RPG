using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public Game game;

    private GameObject _controlledCharacter = null;
    public GameObject ControlledCharacter { get { return _controlledCharacter; } }
    //private GameObject crosshair = null;

    void Start()
    {
        game = GetComponent<Game>();
    }

    public void SetCharacterControl(GameObject character)
    {
        if (_controlledCharacter != null)
            _controlledCharacter.GetComponent<AI>().enabled = true;
        _controlledCharacter = character;
        character.GetComponent<AI>().enabled = false;
        Camera.main.GetComponent<FocusOnGameObject>().ChangeFocusObject(ControlledCharacter);
    }

    public Direction GetDirectionFromNumPad(bool heldDown = true)
    {
        for (int keyCode = 257; keyCode < 266; keyCode++)
            if (heldDown)
                if (Input.GetKey((KeyCode)keyCode))
                    return (Direction)(keyCode - 257);
                else
                if (Input.GetKeyUp((KeyCode)keyCode))
                    return (Direction)(keyCode - 257);
        return Direction.C;
    }

    public void TryWait(int turnNumber)
    {
        if (turnNumber > 0)
        {
            ControlledCharacter.GetComponent<Character>().waitingTurns = turnNumber;
            game.GiveTurnToNextCharacter(ControlledCharacter.GetComponent<Character>());
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (game.CurrentUiMode == InGameUI.Menu)
                game.SwitchUIMode(InGameUI.Interface);
            else if (game.CurrentUiMode == InGameUI.Interface)
                game.SwitchUIMode(InGameUI.Menu);
            return;
        }
        if (Input.GetKeyUp(KeyCode.BackQuote))
        {
            if (game.CurrentUiMode == InGameUI.Console)
                game.SwitchUIMode(InGameUI.Interface);
            else if (game.CurrentUiMode == InGameUI.Interface)
                game.SwitchUIMode(InGameUI.Console);
            return;
        }
        if (ControlledCharacter == null)
            return;
        Character characterComponent = ControlledCharacter.GetComponent<Character>();
        if (!characterComponent)
            return;
        if (!characterComponent.enabled)
            return;
        if ((Input.GetKeyUp(KeyCode.LeftControl) || characterComponent.IsInCombat()) && characterComponent.waitingTurns > 0)
        {
            characterComponent.waitingTurns = 0;
            game.UpdateLog("Stopped waiting.");
            return;
        }
        bool inCombat = characterComponent.IsInCombat();
        GameObject equippedItem = characterComponent.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
        Firearm firearmComponent = null;
        if (equippedItem != null)
        {
            firearmComponent = equippedItem.GetComponent<Firearm>();
            //if (!equippedItem.GetComponent<Repairkit>() || !equippedItem.GetComponent<Medkit>())
            //    characterComponent.usingKit = false;
        }
        //else
        //    characterComponent.usingKit = false;
        characterComponent.CalculateEncumbrance();
        if (Input.GetKeyUp(KeyCode.Tab) && game.CurrentUiMode == InGameUI.Inventory)
        {
            game.SwitchUIMode(InGameUI.Interface);
            return;
        }
        if (game.CurrentUiMode == InGameUI.Interface)
        {
            Cursor.visible = false;
            if (Input.GetKeyUp(KeyCode.F2))
            {
                game.gameInterfaceObject.SetActive(!game.gameInterfaceObject.activeSelf);
                game.groundCursor.gameObject.SetActive(game.gameInterfaceObject.activeSelf);
                return;
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                game.SwitchUIMode(InGameUI.Inventory);
                return;
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                game.UpdateLog("Finished turn");
                characterComponent.EndTurn();
                //game.GiveTurnToNextCharacter(characterComponent);
                //return;
            }
            if (Input.GetKeyUp(KeyCode.B))
            {
                if (firearmComponent)
                    firearmComponent.SwitchFireMod();
            }
            if (Input.GetKeyUp(KeyCode.R))
                characterComponent.ReloadWeapon();
            if (Input.GetKeyUp(KeyCode.E))
            {
                if (firearmComponent)
                {
                    if (firearmComponent.jammed)
                    {
                        if (characterComponent.UseActionPoints(2))
                        {
                            float condition = firearmComponent.Condition / firearmComponent.GetComponent<Item>().GetMaxCondition() * 25;
                            float unjamChance = characterComponent.GetAttribute("Mechanical").value;
                            if (UnityEngine.Random.Range(condition - firearmComponent.reliability, condition) < unjamChance)
                            {
                                firearmComponent.jammed = false;
                                game.UpdateLog("Your gun is no longer jammed.");
                            }
                            else
                                game.UpdateLog("Your fail to unjam your gun.");
                        }
                        else
                            game.UpdateLog("Not enough AP to unjam the gun (2).");
                    }
                    else
                        game.UpdateLog("Your gun doesn't need to be unjammed.");
                }
                Repairkit repairkit = equippedItem.GetComponent<Repairkit>();
                if (repairkit)
                {
                    //if (characterComponent.UseActionPoints(4))
                    //{
                    //characterComponent.usingKit = true;
                    if (!inCombat)
                        repairkit.OnUse(inCombat);
                    else
                        game.UpdateLog("Can not use repair kits in combat");
                    //}
                    //else
                    //    game.UpdateLog("Not enough AP to use repair kit (4).");
                }
                Medkit medkit = equippedItem.GetComponent<Medkit>();
                if (medkit)
                {
                    if (characterComponent.UseActionPoints(4))
                    {
                        //characterComponent.usingKit = true;
                        medkit.OnUse(inCombat);
                    }
                    else
                        game.UpdateLog("Not enough AP to use medkit (4).");
                }
            }
            if (Input.GetKeyUp(KeyCode.Q))
                game.UpdateLog(game.DistanceFromToInCells(characterComponent.transform.position, game.groundCursor.position).ToString() + " cells");
            if (Input.GetKeyUp(KeyCode.Keypad0) && !inCombat)
                TryWait(10);
            if (Input.GetKeyUp(KeyCode.KeypadPeriod) && !inCombat)
                TryWait(20);
            if (Input.GetKeyUp(KeyCode.KeypadEnter) && !inCombat)
                TryWait(50);
            Cursor.visible = false;
            if (game.GroundCursorMode == GroundCursorMode.Move)
            {
                if (Input.GetMouseButtonUp(0))
                    characterComponent.MoveOnPathTo(game.groundCursor.position);
                //Direction direction;
                //if (Input.GetKey(KeyCode.LeftControl))
                //    direction = GetDirectionFromNumPad(false);
                //else
                //direction = GetDirectionFromNumPad();
                //if (Input.GetKey(KeyCode.LeftAlt))
                //    if (Input.GetKey(KeyCode.Keypad5))
                //        Camera.main.GetComponent<FocusOnGameObject>().ResetOffset();
                //    else if (Game.PointIsOnScreen((Vector2)controlledCharacter.transform.position - game.cellSize * characterComponent.DirectionToNumbers(direction)))
                //        Camera.main.GetComponent<FocusOnGameObject>().ModOffset(characterComponent.DirectionToNumbers(direction) / 4);
                //if (direction != Direction.C)
                //    characterComponent.TryMove(direction);
                if (Input.GetMouseButtonUp(1) && !Input.GetKey(KeyCode.LeftAlt))
                {
                    if (firearmComponent)
                    {
                        if (!firearmComponent.jammed)
                        {
                            game.observedTarget = null;
                            game.GroundCursorMode = GroundCursorMode.Attack;
                        }
                        else
                            game.UpdateLog("Your gun is jammed!");
                    }
                }
                return;
            }
            if (game.GroundCursorMode == GroundCursorMode.Attack)
            {
                game.observedTarget = null;
                Vector2Int cursorTilePosition = game.ObjectPositionToCell(game.groundCursor);
                foreach (Character character in game.activeCharacters)
                {
                    if (character.gameObject != ControlledCharacter)
                    {
                        if (cursorTilePosition == game.ObjectPositionToCell(character.transform))
                        {
                            game.observedTarget = character;
                            break;
                        }
                    }
                }
                if (characterComponent.performingAction)
                    return;
                if (Input.GetMouseButtonUp(1))
                    game.GroundCursorMode = GroundCursorMode.Move;
                if (Input.GetMouseButtonUp(0))
                    characterComponent.ShootAtPoint(game.groundCursor.position);
            }
        }
    }
}