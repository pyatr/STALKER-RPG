using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public Character characterComponent;
    public PathFinder pathFinder;
    public Game game;
    public Dictionary<string, int> factionsToForgive = new Dictionary<string, int>();
    public Transform occupationTarget = null;

    Character AttackTarget { get { return characterComponent.attackTarget; } set { characterComponent.attackTarget = value; } }

    public int turnsSinceLastAttack = 0;
    public int turnsToWaitAfterAttack = 14;
    private int thoughts = 0;
    private int maxThoughts = 8;

    public void FindTargetToAttack(bool hasToBeShootable = false)
    {
        foreach (Character currentCharacter in game.activeCharacters)
        {
            if (characterComponent.hostileTowards.Contains(currentCharacter.faction))
            {
                float distanceX = transform.position.x - currentCharacter.transform.position.x;
                if (Math.Abs(distanceX) > characterComponent.sightDistance * game.cellSize.x)
                    continue;
                float distanceY = transform.position.y - currentCharacter.transform.position.y;
                if (Math.Abs(distanceY) > characterComponent.sightDistance * game.cellSize.y)
                    continue;
                if (hasToBeShootable)
                    if (!CanShoot(currentCharacter))
                        continue;
                if (!currentCharacter.IsInCombat())
                {
                    characterComponent.StopMovingOnPath();
                    currentCharacter.StopMovingOnPath();
                    if (currentCharacter.gameObject == game.characterController.ControlledCharacter)
                        game.UpdateLog(characterComponent.displayName + " attacks you!");
                }
                currentCharacter.waitingTurns = 0;
                CallForHelp(currentCharacter);
                AttackTarget = currentCharacter;
                return;
            }
        }
    }

    public void CallForHelp(Character attackedBy)
    {
        occupationTarget = null;
        foreach (Character possibleAlly in game.activeCharacters)
        {
            if (possibleAlly.attackTarget == null && attackedBy.faction != characterComponent.faction)
            {
                if (possibleAlly != characterComponent && attackedBy.faction != possibleAlly.faction && (possibleAlly.faction == characterComponent.faction || possibleAlly.friendlyTowards.Contains(characterComponent.faction) || possibleAlly.hostileTowards.Contains(attackedBy.faction)))
                {
                    if (characterComponent.attackTarget == null)
                        characterComponent.attackTarget = attackedBy;
                    if (game.DistanceFromToInCells(transform.position, possibleAlly.transform.position) < 30)
                    {
                        possibleAlly.attackTarget = attackedBy;
                        if (attackedBy.faction == "playerfaction")
                        {
                            MakeHostileTemporarily(characterComponent, attackedBy.faction);
                            MakeHostileTemporarily(possibleAlly, attackedBy.faction);
                        }
                    }
                }
            }
        }
    }

    public bool ShootSomeoneImmediately()
    {
        foreach (Character targetCharacterComponent in game.activeCharacters)
        {
            if (characterComponent.hostileTowards.Contains(targetCharacterComponent.faction))
            {
                //float distanceX = transform.position.x - targetCharacterComponent.transform.position.x;
                //if (Math.Abs(distanceX) > characterComponent.sightDistance * game.cellSize.x)
                //    continue;
                //float distanceY = transform.position.y - targetCharacterComponent.transform.position.y;
                //if (Math.Abs(distanceY) > characterComponent.sightDistance * game.cellSize.y)
                //    continue;
                if (!TargetWithinAttackRange(targetCharacterComponent))
                    continue;
                if (!CanShoot(targetCharacterComponent))
                    continue;
                if (!targetCharacterComponent.IsInCombat())
                {
                    characterComponent.StopMovingOnPath();
                    targetCharacterComponent.StopMovingOnPath();
                    if (targetCharacterComponent.gameObject == game.characterController.ControlledCharacter)
                        game.UpdateLog(characterComponent.displayName + " attacks you!");
                }
                targetCharacterComponent.waitingTurns = 0;
                AttackTarget = targetCharacterComponent;
                Attack(targetCharacterComponent);
                return true;
            }
        }
        return false;
    }

    public void MakeHostileTemporarily(Character character, string faction)
    {
        if (!character.hostileTowards.Contains(faction) && !character.brain.factionsToForgive.Keys.Contains(faction))
        {
            character.hostileTowards.Add(faction);
            character.brain.factionsToForgive.Add(faction, 200);
        }
    }

    public void MoveRandomly()
    {
        List<Vector2> positions = characterComponent.GetNearbyAccessibleCells();
        if (positions.Count > 0)
        {
            characterComponent.MoveOnPathTo(positions.ElementAt(UnityEngine.Random.Range(0, positions.Count - 1))); // (new List<Direction> { (Direction)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Direction)).Length) });
        }
    }

    public bool Attack(Character character)
    {
        if (character != null)
        {
            //if (!characterComponent.hostileTowards.Contains(character.faction))
            //    return false;
            turnsSinceLastAttack = 0;
            GameObject equippedItem = characterComponent.weapon;
            if (equippedItem != null)
            {
                Firearm equippedFirearm = equippedItem.GetComponent<Firearm>();
                if (equippedFirearm)
                {
                    if (!characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Attack))
                        return false;
                    if (equippedFirearm.GetAmmoCount() == 0)
                        characterComponent.ReloadWeapon();
                    if (equippedFirearm.GetAmmoCount() == 0)
                    {
                        //Hide
                        return false;
                    }
                    return characterComponent.ShootAtPoint(character.transform.position);
                }
            }
        }
        return false;
    }

    public bool CanShoot(Character character)
    {
        if (character != null)
            //if (TargetWithinAttackRange(character))
            return characterComponent.CanShootFromTo(transform.position, character.transform.position);
        return false;
    }

    public void Wander()
    {
        characterComponent.MoveOnPathTo((Vector2)transform.position + Vector2.up * UnityEngine.Random.Range(-8, 9) * game.cellSize.y + Vector2.right * UnityEngine.Random.Range(-8, 9) * game.cellSize.x);
    }

    public void AbandonAttackTargetIfLowOnHealth()
    {
        if (characterComponent.Health < characterComponent.GetAttribute("Health").maxValue / 3)
        {
            if (game.DistanceFromToInCells(transform.position, AttackTarget.transform.position) > characterComponent.sightDistance)
            {
                TravelToChosenLocation();
                return;
            }
        }
    }

    public void TravelToChosenLocation()
    {
        AttackTarget = null;
        characterComponent.StopMovingOnPath();
        if (occupationTarget != null)
            characterComponent.MoveOnPathTo(occupationTarget.position);
    }

    public void FindPositionToOccupy()
    {
        Location currentLocation = characterComponent.GetCurrentLocation();
        if (currentLocation != null)
        {
            if (!characterComponent.hostileTowards.Contains(currentLocation.ownerFaction) || characterComponent.faction == currentLocation.ownerFaction || currentLocation.ownerFaction == "None")
            {
                occupationTarget = currentLocation.GetFreeGuardSpace();
                if (occupationTarget == null)
                    occupationTarget = currentLocation.GetFreeRestingSpace();
                if (occupationTarget == null)
                    occupationTarget = currentLocation.GetFreeInnerGuardSpace();
                if (occupationTarget != null)
                {
                    currentLocation.OccupyPoint(occupationTarget, characterComponent);
                    characterComponent.MoveOnPathTo(occupationTarget.position);
                }
            }
        }
    }

    void TryPursuit()
    {
        if (AttackTarget != null)
        {
            if (characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Move))
            {
                List<Vector2> adjacentCells = AttackTarget.GetNearbyAccessibleCells();
                List<Direction> shortestPath = characterComponent.GetShortestPath(adjacentCells);
                if (shortestPath.Count > 0)
                {
                    characterComponent.MoveOnPath(shortestPath);
                    return;
                }
            }
        }
        characterComponent.EndTurn();
    }

    public bool TargetWithinAttackRange(Character target)
    {
        return game.DistanceFromToInCells(transform.position, target.transform.position) <= characterComponent.GetAttackDistance();//Mathf.Min(characterComponent.GetAttackDistance(), characterComponent.sightDistance);
    }

    public void AbandonOccupationPoint()
    {
        if (occupationTarget != null)
        {
            Location currentLocation = characterComponent.GetCurrentLocation();
            if (currentLocation != null)
                currentLocation.FreePoint(occupationTarget);
        }
    }

    public Character IsInImmediateDanger()
    {
        foreach (Character possibleAttacker in game.activeCharacters)
            if (possibleAttacker.hostileTowards.Contains(characterComponent.faction) || possibleAttacker.attackTarget == characterComponent && possibleAttacker.CanShootFromTo(possibleAttacker.transform.position, transform.position))
                return possibleAttacker;
        return null;
    }

    public bool StandingOnOccupationPoint()
    {
        if (occupationTarget != null)
            return game.VectorsAreEqual(transform.position, occupationTarget.position);
        return false;
    }

    public void Think()
    {
        //if (thoughts > 0)
        //    game.UpdateLog(characterComponent.displayName + " thinks " + thoughts + " times");
        thoughts++;
        if (thoughts >= maxThoughts)
        {
            thoughts = 0;
            characterComponent.EndTurn();
            return;
        }
        if (AttackTarget == null)
        {
            FindTargetToAttack(true);
            if (AttackTarget == null)
                FindTargetToAttack();
            if (AttackTarget == null)
            {
                if (occupationTarget != null && characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Move))
                {
                    if (!StandingOnOccupationPoint())
                    {
                        List<Direction> pathToPoint = pathFinder.FindPath(occupationTarget.transform.position);
                        if (pathToPoint.Count > 0)
                        {
                            characterComponent.MoveOnPath(pathToPoint);
                            return;
                        }
                    }
                }
                characterComponent.EndTurn();
                return;
            }
        }
        if (AttackTarget != null)
        {
            AbandonOccupationPoint();
            if (characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Attack))
            {
                if (TargetWithinAttackRange(AttackTarget))
                {
                    if (CanShoot(AttackTarget))
                        if (Attack(AttackTarget))
                            return;
                }
                //if (ShootSomeoneImmediately())
                //    return;
            }
            TryPursuit();
            return;
        }
        if (AttackTarget != null)
        {
            if (game.DistanceFromToInCells(transform.position, AttackTarget.transform.position) > characterComponent.sightDistance)
            {
                AttackTarget = null;
                characterComponent.EndTurn();
                return;
            }
        }
        if (AttackTarget != null)
            if (!characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Attack))
                characterComponent.EndTurn();
        if (!characterComponent.EnoughActionPointsToPerformAction(ActionTypes.Move))
            characterComponent.EndTurn();
    }

    public void OnEndTurn()
    {
        //Debug.Log(characterComponent.displayName + "");
        //AttackTarget = null;
        if (characterComponent.IsInCombat())
            turnsSinceLastAttack = 0;
        else
            turnsSinceLastAttack++;
        for (int i = 0; i < factionsToForgive.Count; i++)
        {
            string factionName = factionsToForgive.Keys.ElementAt(i);
            factionsToForgive[factionName] -= 1;
            if (factionsToForgive[factionName] <= 0)
            {
                characterComponent.hostileTowards.Remove(factionName);
                factionsToForgive.Remove(factionName);
                i--;
            }
        }
        if (turnsSinceLastAttack >= turnsToWaitAfterAttack)
            if (occupationTarget == null)
                FindPositionToOccupy();
    }

    public void OnMoveStart()
    {
        if (AttackTarget != null)
            if (occupationTarget != null)
                AbandonOccupationPoint();
    }

    public void OnMoveEnd()
    {
        bool inCombat = characterComponent.IsInCombat();
        List<Direction> currentPath = new List<Direction>();
        if (inCombat)
        {
            currentPath = characterComponent.pathDirections;
            characterComponent.StopMovingOnPath();
        }
        if (AttackTarget == null)
        {
            if (inCombat)
            {
                if (!ShootSomeoneImmediately())
                {
                    characterComponent.MoveOnPath(currentPath);
                    return;
                }
            }
            if (occupationTarget != null)
            {
                if (!StandingOnOccupationPoint())
                {
                    characterComponent.MoveOnPathTo(occupationTarget.position);
                    characterComponent.cellsLeftToTraverse++;
                }
                else
                {
                    //Debug.Log(characterComponent.displayName + " reached destination at " + occupationTarget.position);
                    transform.position = occupationTarget.position;
                    characterComponent.EndTurn();
                    game.GiveTurnToNextCharacter(characterComponent);
                }
            }
        }
        else
        {
            if (CanShoot(AttackTarget))
            {
                if (TargetWithinAttackRange(AttackTarget))
                {
                    Attack(AttackTarget);
                    return;
                }
            }
            if (!ShootSomeoneImmediately())
            {
                characterComponent.MoveOnPath(currentPath);
            }
        }
    }
}