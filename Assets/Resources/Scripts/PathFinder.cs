using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    class PathFindingNode
    {
        public PathFindingNode parentNode;
        public int x, y;
        public int g, h;
        public int f
        {
            get
            {
                return g + h;
            }
        }
    }

    public Game game;

    public bool CanMoveFromPositionInDirection(Vector2 position, Direction direction)
    {
        Vector2 targetPosition = position + DirectionToNumbers(direction) * game.cellSize;
        RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(position, targetPosition - position, Vector2.Distance(position, targetPosition));
        //Debug.DrawRay(position, raycastDirection, Color.red, 10);
        for (int i = 0; i < raycastHit2D.Length; i++)
        {
            GameObject objectHit = raycastHit2D[i].collider.gameObject;
            if (game.nonWalkableLayers.Contains(objectHit.layer))
                return false;
        }
        return true;
    }

    public Vector2 DirectionToNumbers(Direction direction)
    {
        return new Vector2((int)direction % 3 - 1, (int)direction / 3 - 1);
    }

    public Direction NumbersToDirection(Vector2 numbers)
    {
        return (Direction)((int)numbers.x + 1 + ((int)numbers.y + 1) * 3);
    }

    Direction PathFindingNodeToDirection(PathFindingNode node)
    {
        if (node.parentNode != null)
        {
            int relativeDirectionX = node.x - node.parentNode.x;
            int relativeDirectionY = node.y - node.parentNode.y;
            //Debug.Log(currentNode.x + "; " + currentNode.y + "/" + relativeDirectionX + "; " + relativeDirectionY);
            return NumbersToDirection(new Vector2(relativeDirectionX, relativeDirectionY));
        }
        Debug.Log("Pathfinding node does not have parent: " + node.x + "; " + node.y);
        return Direction.C;
    }

    public List<Direction> FindPath(Vector2 targetPosition)
    {
        List<Direction> nodes = new List<Direction>();// { Direction.C };
        Vector2 startPosition = transform.position;
        //if (game.VectorsAreEqual(startPosition, targetPosition))
        //    return nodes;
        int pathFindingDistance = 77;
        if (pathFindingDistance % 2 == 0)
            pathFindingDistance++;
        int nodeCount = pathFindingDistance * pathFindingDistance;
        int startCoordinates = pathFindingDistance / 2;
        foreach (Character currentCharacter in game.activeCharacters)
        {
            if ((Vector2)currentCharacter.transform.position == targetPosition && currentCharacter != GetComponent<Character>())
            {
                //List<Vector2> adjacentCells = currentCharacter.GetNearbyAccessibleCells();
                //if (adjacentCells.Count == 0)
                //    return nodes;
                return nodes;
                //foreach (Vector2 cell in adjacentCells)
                //    if (Vector2.Distance(cell, transform.position) < 0.01f)
                //        return nodes;                
                //return GetComponent<Character>().GetShortestPath(adjacentCells);
            }
        }
        int xDistanceToTarget = (int)Math.Round((targetPosition.x - startPosition.x) / game.cellSize.x, MidpointRounding.AwayFromZero);
        int yDistanceToTarget = (int)Math.Round((targetPosition.y - startPosition.y) / game.cellSize.y, MidpointRounding.AwayFromZero);
        if (xDistanceToTarget == 0 && yDistanceToTarget == 0)
            return nodes;
        if (Mathf.Abs(xDistanceToTarget) > startCoordinates || Mathf.Abs(yDistanceToTarget) > startCoordinates)
            return nodes;

        List<PathFindingNode> openNodes = new List<PathFindingNode>(nodeCount);
        HashSet<PathFindingNode> closedNodes = new HashSet<PathFindingNode>();
        PathFindingNode[,] allNodes = new PathFindingNode[pathFindingDistance, pathFindingDistance];
        for (int i = 0; i < pathFindingDistance; i++)
            for (int j = 0; j < pathFindingDistance; j++)
                allNodes[i, j] = new PathFindingNode { x = i, y = j, g = 0, h = 0 };
        PathFindingNode targetNode = allNodes[startCoordinates + xDistanceToTarget, startCoordinates + yDistanceToTarget];
        PathFindingNode startingNode = allNodes[startCoordinates, startCoordinates];
        int attempts = 0, attemptsMax = 400;
        openNodes.Add(startingNode);
        while (openNodes.Count > 0 && attempts <= attemptsMax)
        {
            attempts++;
            PathFindingNode currentNode = openNodes[0];
            for (int i = 1; i < openNodes.Count; i++)
                if (openNodes[i].f <= currentNode.f)
                    if (openNodes[i].h < currentNode.h)
                        currentNode = openNodes[i];
            openNodes.Remove(currentNode);
            closedNodes.Add(currentNode);
            if (currentNode == targetNode)
            {
                currentNode = targetNode;
                int i = 0;
                while (currentNode != startingNode && i < 500)
                {
                    nodes.Add(PathFindingNodeToDirection(currentNode));
                    currentNode = currentNode.parentNode;
                    i++;
                }
                if (i >= 500)
                    Debug.Log("Path building failed for " + gameObject.name);
                //if (targetPositionIsCharacter)
                //    nodes.RemoveAt(0);
                nodes.Reverse();
                return nodes;
            }
            List<Direction> directions = Enum.GetValues(typeof(Direction)).Cast<Direction>().ToList();
            directions.Remove(Direction.C);
            foreach (Direction direction in directions)
            {
                int currentNeighbourPositionX = currentNode.x + (int)DirectionToNumbers(direction).x;
                int currentNeighbourPositionY = currentNode.y + (int)DirectionToNumbers(direction).y;
                float x = (currentNode.x - startCoordinates) * game.cellSize.x + startPosition.x;
                float y = (currentNode.y - startCoordinates) * game.cellSize.y + startPosition.y;
                Vector2 currentPosition = new Vector2(x, y);
                bool targetPositionNearby = Mathf.Abs(targetPosition.x - currentPosition.x) <= game.cellSize.x && Mathf.Abs(targetPosition.y - currentPosition.y) <= game.cellSize.y;
                if (CanMoveFromPositionInDirection(currentPosition, direction))
                {
                    if (currentNeighbourPositionX > 0 && currentNeighbourPositionX < pathFindingDistance &&
                        currentNeighbourPositionY > 0 && currentNeighbourPositionY < pathFindingDistance)
                    {
                        PathFindingNode neighbour = allNodes[currentNeighbourPositionX, currentNeighbourPositionY];
                        bool nodeClosed = false;
                        foreach (PathFindingNode node in closedNodes)
                            if (closedNodes.Contains(neighbour))
                                nodeClosed = true;
                        if (!nodeClosed)
                        {
                            int newCostToNeighbour = currentNode.g + GetDistance(currentNode, neighbour);
                            if (newCostToNeighbour < neighbour.g || !openNodes.Contains(neighbour))
                            {
                                neighbour.g = newCostToNeighbour;
                                neighbour.h = GetDistance(neighbour, targetNode);
                                neighbour.parentNode = currentNode;
                                if (!openNodes.Contains(neighbour))
                                    openNodes.Add(neighbour);
                            }
                        }
                    }
                }
            }
        }
        //if (attempts >= attemptsMax && game.characterController.ControlledCharacter == gameObject)
        //game.UpdateLog("Can't reach that cell");
        //Debug.Log("Could not find path from " + startPosition + " to " + targetPosition);
        return nodes;
    }

    int GetDistance(PathFindingNode pfnS, PathFindingNode pfnE)
    {
        int distanceX = Mathf.Abs(pfnS.x - pfnE.x);
        int distanceY = Mathf.Abs(pfnS.y - pfnE.y);

        if (distanceX > distanceY)
            return 14 * distanceY + 10 * (distanceX - distanceY);
        return 14 * distanceX + 10 * (distanceY - distanceX);
    }
}