using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    private int thoughts = 0;
    private int maxThoughts = 8;

    public Character AttackTarget { get { return characterComponent.attackTarget; } set { characterComponent.attackTarget = value; } }

    public World world;
    public Character characterComponent;
    public PathFinder pathFinder;
    public Dictionary<string, int> factionsToForgive = new Dictionary<string, int>();
    public Dictionary<string, int> temporaryPeace = new Dictionary<string, int>();
    public Transform occupationTarget = null;

    public int turnsSinceLastAttack = 0;
    public int turnsToWaitAfterAttack = 14;
    public bool followPlayer = false;

    public void FindTargetToAttack(bool hasToBeShootable = false)
    {
        foreach (Character currentCharacter in world.activeCharacters)
        {
            if (characterComponent.hostileTowards.Contains(currentCharacter.faction))
            {
                if (currentCharacter.immobile && currentCharacter.invulnerable)
                    continue;
                float distanceX = transform.position.x - currentCharacter.transform.position.x;
                if (Math.Abs(distanceX) > characterComponent.sightDistance * Game.Instance.cellSize.x)
                    continue;
                float distanceY = transform.position.y - currentCharacter.transform.position.y;
                if (Math.Abs(distanceY) > characterComponent.sightDistance * Game.Instance.cellSize.y)
                    continue;
                if (hasToBeShootable)
                    if (!CanShoot(currentCharacter))
                        continue;
                if (!currentCharacter.IsInCombat())
                {
                    characterComponent.StopMovingOnPath();
                    currentCharacter.StopMovingOnPath();
                    if (currentCharacter.gameObject == world.characterController.ControlledCharacter)
                        world.UpdateLog(characterComponent.displayName + " attacks you!");
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
        foreach (Character possibleAlly in world.activeCharacters)
        {
            if (possibleAlly.attackTarget == null && attackedBy.faction != characterComponent.faction)
            {
                if (possibleAlly != characterComponent && attackedBy.faction != possibleAlly.faction && (possibleAlly.faction == characterComponent.faction || possibleAlly.friendlyTowards.Contains(characterComponent.faction) || possibleAlly.hostileTowards.Contains(attackedBy.faction)))
                {
                    if (characterComponent.attackTarget == null)
                        characterComponent.attackTarget = attackedBy;
                    if (Game.Instance.DistanceFromToInCells(transform.position, possibleAlly.transform.position) < 30)
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
        foreach (Character targetCharacterComponent in world.activeCharacters)
        {
            if (characterComponent.hostileTowards.Contains(targetCharacterComponent.faction))
            {
                //float distanceX = transform.position.x - targetCharacterComponent.transform.position.x;
                //if (Math.Abs(distanceX) > characterComponent.sightDistance * Game.Instance.cellSize.x)
                //    continue;
                //float distanceY = transform.position.y - targetCharacterComponent.transform.position.y;
                //if (Math.Abs(distanceY) > characterComponent.sightDistance * Game.Instance.cellSize.y)
                //    continue;
                if (targetCharacterComponent.immobile && targetCharacterComponent.invulnerable)                
                    continue;                
                if (!TargetWithinAttackRange(targetCharacterComponent))
                    continue;
                if (!CanShoot(targetCharacterComponent))
                    continue;
                if (!targetCharacterComponent.IsInCombat())
                {
                    characterComponent.StopMovingOnPath();
                    targetCharacterComponent.StopMovingOnPath();
                    if (targetCharacterComponent.gameObject == world.characterController.ControlledCharacter)
                        world.UpdateLog(characterComponent.displayName + " attacks you!");
                }
                targetCharacterComponent.waitingTurns = 0;
                AttackTarget = targetCharacterComponent;
                Attack(targetCharacterComponent);
                return true;
            }
        }
        return false;
    }

    public void MakeHostileTemporarily(Character character, string faction, int turns = 300)
    {
        if (!character.hostileTowards.Contains(faction) && !character.brain.factionsToForgive.Keys.Contains(faction))
        {
            character.hostileTowards.Add(faction);
            character.brain.factionsToForgive.Add(faction, turns);
        }
    }

    public void MakeNeutralTemporarily(Character character, string faction, int turns = 200)
    {
        if (character.hostileTowards.Contains(faction) && !character.brain.temporaryPeace.Keys.Contains(faction))
        {
            character.hostileTowards.Remove(faction);
            character.brain.temporaryPeace.Add(faction, turns);
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
            if (character.immobile && character.invulnerable)
            {
                if (AttackTarget == character)
                {
                    AttackTarget = null;
                    return false;
                }
            }
            turnsSinceLastAttack = 0;
            GameObject equippedItem = characterComponent.Weapon;
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
        characterComponent.MoveOnPathTo((Vector2)transform.position + Vector2.up * UnityEngine.Random.Range(-8, 9) * Game.Instance.cellSize.y + Vector2.right * UnityEngine.Random.Range(-8, 9) * Game.Instance.cellSize.x);
    }

    public void AbandonAttackTargetIfLowOnHealth()
    {
        if (characterComponent.Health < characterComponent.GetAttribute("Health").MaxValue / 3)
        {
            if (Game.Instance.DistanceFromToInCells(transform.position, AttackTarget.transform.position) > characterComponent.sightDistance)
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
                Location currentLocation = characterComponent.GetCurrentLocation();
                Location targetLocation = AttackTarget.GetCurrentLocation();
                if (currentLocation == targetLocation || currentLocation == null && targetLocation == null) 
                {
                    List<Direction> shortestPath = characterComponent.GetShortestPath(adjacentCells);
                    if (shortestPath.Count > 0)
                    {
                        characterComponent.MoveOnPath(shortestPath);
                        return;
                    }
                }
                else if (!CanShoot(AttackTarget) || Game.Instance.DistanceFromToInCells(transform.position, AttackTarget.transform.position) > characterComponent.sightDistance)
                {
                    //Debug.Log(AttackTarget.displayName + " is too far, forget him");
                    AttackTarget = null;
                }
            }
        }
        characterComponent.EndTurn();
    }

    public bool TargetWithinAttackRange(Character target)
    {
        return Game.Instance.DistanceFromToInCells(transform.position, target.transform.position) <= characterComponent.GetAttackDistance();//Mathf.Min(characterComponent.GetAttackDistance(), characterComponent.sightDistance);
    }

    public void AbandonOccupationPoint()
    {
        if (occupationTarget != null)
        {
            occupationTarget = null;
            Location currentLocation = characterComponent.GetCurrentLocation();
            if (currentLocation != null)
                currentLocation.FreePoint(occupationTarget);
        }
    }

    public Character IsInImmediateDanger()
    {
        foreach (Character possibleAttacker in world.activeCharacters)
            if (possibleAttacker.hostileTowards.Contains(characterComponent.faction) || possibleAttacker.attackTarget == characterComponent && possibleAttacker.CanShootFromTo(possibleAttacker.transform.position, transform.position))
                return possibleAttacker;
        return null;
    }

    public bool StandingOnOccupationPoint()
    {
        if (occupationTarget != null)
            return Game.Instance.VectorsAreEqual(transform.position, occupationTarget.position);
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
                if (!characterComponent.immobile)
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
            if (Game.Instance.DistanceFromToInCells(transform.position, AttackTarget.transform.position) > characterComponent.sightDistance)
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
        bool inCombat = characterComponent.IsInCombat();
        if (inCombat)
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
        for (int i = 0; i < temporaryPeace.Count; i++)
        {
            string factionName = temporaryPeace.Keys.ElementAt(i);
            temporaryPeace[factionName] -= 1;
            if (temporaryPeace[factionName] <= 0)
            {
                characterComponent.hostileTowards.Add(factionName);
                temporaryPeace.Remove(factionName);
                i--;
            }
        }
        if (followPlayer && world.Player != null)
        {
            characterComponent.MoveOnPathTo(world.Player.GetComponent<Character>().GetNearbyAccessibleCells()[0]);
            return;
        }
        if (turnsSinceLastAttack >= turnsToWaitAfterAttack)
        {
            if (!characterComponent.immobile)
            {
                Location currentLocation = characterComponent.GetCurrentLocation();
                if (occupationTarget == null)
                {
                    FindPositionToOccupy();
                }
                else if (currentLocation != null)
                {
                    if (currentLocation.PointIsOccupiedByAnyCharacter(occupationTarget, characterComponent))
                    {
                        AbandonOccupationPoint();
                        FindPositionToOccupy();
                    }
                }
            }
        }
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
                    world.GiveTurnToNextCharacter(characterComponent);
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