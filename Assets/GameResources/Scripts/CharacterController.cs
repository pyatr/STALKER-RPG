using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    private GameObject _controlledCharacter = null;
    public GameObject ControlledCharacter { get { return _controlledCharacter; } }

    private World world;
    public string dialogueName = "none";
    public Character talkingTo = null;
    public int temporaryLockTime = 0;

    private void Start()
    {
        world = World.GetInstance();
    }

    public void SetCharacterControl(GameObject character)
    {
        world = World.GetInstance();
        if (character == null)
            return;
        if (_controlledCharacter != null)
            _controlledCharacter.GetComponent<AI>().enabled = true;
        _controlledCharacter = character;
        character.GetComponent<AI>().enabled = false;
        world.groundCursor.gameObject.SetActive(true);
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
            world.GiveTurnToNextCharacter(ControlledCharacter.GetComponent<Character>());
        }
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (world.CurrentUiMode == InGameUI.Menu)
                world.SwitchUIMode(InGameUI.Interface);
            else if (world.CurrentUiMode == InGameUI.Interface)
                world.SwitchUIMode(InGameUI.Menu);
            return;
        }
        if (Input.GetKeyUp(KeyCode.BackQuote))
        {
            if (world.CurrentUiMode == InGameUI.Console)
                world.SwitchUIMode(InGameUI.Interface);
            else if (world.CurrentUiMode == InGameUI.Interface)
                world.SwitchUIMode(InGameUI.Console);
            return;
        }
        if (ControlledCharacter == null)
            return;
        if (temporaryLockTime > 0)
        {
            temporaryLockTime--;
            return;
        }
        Character characterComponent = ControlledCharacter.GetComponent<Character>();
        if (!characterComponent)
            return;
        if (!characterComponent.enabled)
            return;
        if ((Input.GetKeyUp(KeyCode.LeftControl) || characterComponent.IsInCombat()) && characterComponent.waitingTurns > 0)
        {
            characterComponent.waitingTurns = 0;
            world.UpdateLog("Stopped waiting.");
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
        if (Input.GetKeyUp(KeyCode.Tab) && world.CurrentUiMode == InGameUI.Inventory)
        {
            world.SwitchUIMode(InGameUI.Interface);
            return;
        }
        if (Input.GetKeyUp(KeyCode.C) && world.CurrentUiMode == InGameUI.CharacterMenu)
        {
            world.SwitchUIMode(InGameUI.Interface);
            return;
        }
        if (world.CurrentUiMode == InGameUI.Dialogue)
        {
            Dialogue dialogueMenu = world.userInterfaceElements[InGameUI.Dialogue].GetComponent<Dialogue>();
            if (dialogueMenu && world.userInterfaceElements[InGameUI.Dialogue].activeSelf)
            {
                Dialogue.DialoguePage dialoguePage = dialogueMenu.currentPage;
                if (dialoguePage != null && dialoguePage.responses != null)
                    for (int i = 0; i < dialoguePage.responses.Count && i < 9; i++)
                        if (Input.GetKeyUp((KeyCode)(i + 49)))
                            dialogueMenu.ChooseResponseNumber(i); //(dialoguePage.responses[i]);
            }
            return;
        }
        if (world.CurrentUiMode == InGameUI.Interface)
        {
            Cursor.visible = false;
            if (Input.GetKeyUp(KeyCode.F2))
            {
                world.userInterfaceElements[InGameUI.Interface].SetActive(!world.userInterfaceElements[InGameUI.Interface].activeSelf);
                world.groundCursor.gameObject.SetActive(world.userInterfaceElements[InGameUI.Interface].activeSelf);
                return;
            }
            if (Input.GetKeyUp(KeyCode.Tab))
            {
                world.SwitchUIMode(InGameUI.Inventory);
                characterComponent.StopMovingOnPath();
                return;
            }
            if (Input.GetKeyUp(KeyCode.C))
            {
                world.SwitchUIMode(InGameUI.CharacterMenu);
                return;
            }
            if (Input.GetKeyUp(KeyCode.Space))
            {
                world.UpdateLog("Finished turn");
                characterComponent.EndTurn();
                //world.GiveTurnToNextCharacter(characterComponent);
                return;
            }
            if (Input.GetKeyUp(KeyCode.B))
            {
                if (firearmComponent)
                    firearmComponent.SwitchFireMod();
                return;
            }
            if (Input.GetKeyUp(KeyCode.R))
            {
                characterComponent.ReloadWeapon();
                return;
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                if (equippedItem != null)
                {
                    if (firearmComponent)
                    {
                        if (firearmComponent.jammed)
                        {
                            if (characterComponent.UseActionPoints(2))
                            {
                                float condition = firearmComponent.Condition / firearmComponent.GetComponent<Item>().GetMaxCondition() * 25;
                                float unjamChance = characterComponent.GetAttribute("Mechanical").Value;
                                if (UnityEngine.Random.Range(condition - firearmComponent.reliability, condition) < unjamChance)
                                {
                                    firearmComponent.jammed = false;
                                    world.UpdateLog("Your gun is no longer jammed.");
                                }
                                else
                                    world.UpdateLog("Your fail to unjam your gun.");
                            }
                            else
                                world.UpdateLog("Not enough AP to unjam the gun (2).");
                        }
                        else
                            world.UpdateLog("Your gun doesn't need to be unjammed.");
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
                            world.UpdateLog("Can not use repair kits in combat");
                        //}
                        //else
                        //    world.UpdateLog("Not enough AP to use repair kit (4).");
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
                            world.UpdateLog("Not enough action points to use medkit (4).");
                    }
                }
            }
            if (Input.GetKeyUp(KeyCode.Q))
                world.UpdateLog(Game.Instance.DistanceFromToInCells(characterComponent.transform.position, world.groundCursor.position).ToString() + " cells");
            if (Input.GetKeyUp(KeyCode.Keypad0) && !inCombat)
                TryWait(10);
            if (Input.GetKeyUp(KeyCode.KeypadPeriod) && !inCombat)
                TryWait(20);
            if (Input.GetKeyUp(KeyCode.KeypadEnter) && !inCombat)
                TryWait(50);
            if (Input.GetKeyUp(KeyCode.F5))
                world.GetComponent<Save>().SaveGame(world.currentSlot);
            if (Input.GetKeyUp(KeyCode.D))
            {
                GameObject weaponOnBack = characterComponent.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backweapon);
                if (weaponOnBack != null)
                {
                    int APtoMove = Mathf.Max(weaponOnBack.GetComponent<Item>().actionPointsToMove - 2, 1);
                    if (inCombat && !characterComponent.CanUseActionPoints(APtoMove))
                        return;
                    if (equippedItem == null && weaponOnBack != null && characterComponent.UseActionPoints(APtoMove))
                        characterComponent.EquipItemAsWeapon(weaponOnBack);
                }
                else
                {
                    if (equippedItem != null)
                        characterComponent.EquipWeaponOnBack(equippedItem);
                    else
                        world.UpdateLog("No weapon to put on back or equip.");
                }
                return;
            }
            Cursor.visible = false;
            if (equippedItem == null)
                world.GroundCursorMode = GroundCursorMode.Move;
            if (world.GroundCursorMode == GroundCursorMode.Move)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    Vector2 cursorPosition = world.groundCursor.position;
                    if (!inCombat)
                    {
                        Vector2 playerPosition = ControlledCharacter.transform.position;
                        foreach (Character targetCharacter in world.activeCharacters)
                        {
                            if (targetCharacter != characterComponent && !targetCharacter.hostileTowards.Contains(characterComponent.faction))
                            {
                                Vector2 targetCharacterPosition = targetCharacter.transform.position;
                                int xDistanceToCell = (int)Math.Round((targetCharacterPosition.x - playerPosition.x) / Game.Instance.cellSize.x, MidpointRounding.AwayFromZero);
                                int yDistanceToCell = (int)Math.Round((targetCharacterPosition.y - playerPosition.y) / Game.Instance.cellSize.y, MidpointRounding.AwayFromZero);
                                if (xDistanceToCell <= 1 && xDistanceToCell >= -1 && yDistanceToCell <= 1 && yDistanceToCell >= -1)
                                {
                                    Direction cellDirection = characterComponent.pathFinder.NumbersToDirection(new Vector2(xDistanceToCell, yDistanceToCell));
                                    if (Game.Instance.VectorsAreEqual(targetCharacterPosition, cursorPosition) && characterComponent.CanReachCharacterInDirection(cellDirection))
                                    {
                                        if (targetCharacter.dialoguePackageName != "none")
                                        {
                                            talkingTo = targetCharacter;
                                            dialogueName = targetCharacter.dialoguePackageName;
                                            world.SwitchUIMode(InGameUI.Dialogue);
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    characterComponent.MoveOnPathTo(cursorPosition);
                }
                //Direction direction;
                //if (Input.GetKey(KeyCode.LeftControl))
                //    direction = GetDirectionFromNumPad(false);
                //else
                //direction = GetDirectionFromNumPad();
                //if (Input.GetKey(KeyCode.LeftAlt))
                //    if (Input.GetKey(KeyCode.Keypad5))
                //        Camera.main.GetComponent<FocusOnGameObject>().ResetOffset();
                //    else if (world.PointIsOnScreen((Vector2)controlledCharacter.transform.position - Game.Instance.cellSize * characterComponent.DirectionToNumbers(direction)))
                //        Camera.main.GetComponent<FocusOnGameObject>().ModOffset(characterComponent.DirectionToNumbers(direction) / 4);
                //if (direction != Direction.C)
                //    characterComponent.TryMove(direction);
                if (Input.GetMouseButtonUp(1) && !Input.GetKey(KeyCode.LeftAlt))
                {
                    MeleeWeapon meleeWeaponComponent = equippedItem?.GetComponent<MeleeWeapon>();
                    if (meleeWeaponComponent)
                    {
                        world.GroundCursorMode = GroundCursorMode.MeleeAttack;
                        world.observedTarget = null;
                        return;
                    }
                    if (firearmComponent)
                    {
                        if (!firearmComponent.jammed)
                        {
                            world.observedTarget = null;
                            world.GroundCursorMode = GroundCursorMode.Attack;
                        }
                        else
                        {
                            world.UpdateLog("Your gun is jammed!");
                        }
                    }
                }
                return;
            }
            if (world.GroundCursorMode == GroundCursorMode.Attack)
            {
                world.observedTarget = null;
                Vector2Int cursorTilePosition = Game.Instance.ObjectPositionToCell(world.groundCursor);
                foreach (Character character in world.activeCharacters)
                {
                    if (character.gameObject != ControlledCharacter)
                    {
                        if (cursorTilePosition == Game.Instance.ObjectPositionToCell(character.transform))
                        {
                            world.observedTarget = character;
                            break;
                        }
                    }
                }
                if (characterComponent.performingAction)
                    return;
                if (Input.GetMouseButtonUp(1))
                    world.GroundCursorMode = GroundCursorMode.Move;
                if (Input.GetMouseButtonUp(0))
                    characterComponent.ShootAtPoint(world.groundCursor.position);
            }
            if (world.GroundCursorMode == GroundCursorMode.MeleeAttack)
            {
                world.observedTarget = null;
                Vector2Int cursorTilePosition = Game.Instance.ObjectPositionToCell(world.groundCursor);
                foreach (Character character in world.activeCharacters)
                {
                    if (character.gameObject != ControlledCharacter)
                    {
                        if (cursorTilePosition == Game.Instance.ObjectPositionToCell(character.transform))
                        {
                            world.observedTarget = character;
                            break;
                        }
                    }
                }
                if (characterComponent.performingAction)
                    return;
                if (Input.GetMouseButtonUp(1))
                    world.GroundCursorMode = GroundCursorMode.Move;
                if (Input.GetMouseButtonUp(0))
                {
                    Vector2 cursorPosition = Game.Instance.ObjectPositionToCell(world.groundCursor);
                    int xDistanceToCell = (int)Math.Round((cursorPosition.x - transform.position.x) / Game.Instance.cellSize.x, MidpointRounding.AwayFromZero);
                    int yDistanceToCell = (int)Math.Round((cursorPosition.y - transform.position.y) / Game.Instance.cellSize.y, MidpointRounding.AwayFromZero);
                    if (xDistanceToCell <= 1 && xDistanceToCell >= -1 && yDistanceToCell <= 1 && yDistanceToCell >= -1)
                    {
                        Direction cellDirection = characterComponent.pathFinder.NumbersToDirection(new Vector2(xDistanceToCell, yDistanceToCell));
                        if (Game.Instance.VectorsAreEqual(cursorPosition, cursorPosition) && characterComponent.CanReachCharacterInDirection(cellDirection))
                        {
                            characterComponent.MeleeAttackInDirection(cellDirection);
                            return;
                        }
                    }
                }
            }
        }
    }
}