using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
    public string ownerFaction = "None";
    public string locationName = "Unknown";
    public Transform guardPoints;
    public Transform innerGuardPoints;
    public Transform restingPoints;
    public int cellRadius = 20;
    Dictionary<Transform, Character> occupiedPoints = new Dictionary<Transform, Character>();
    Game game;

    public void Start()
    {
        game = GameObject.Find("World").GetComponent<Game>();
        guardPoints = transform.Find("AIPoints").Find("Guard");
        innerGuardPoints = transform.Find("AIPoints").Find("InnerGuard");
        restingPoints = transform.Find("AIPoints").Find("Resting");
        for (int i = 0; i < guardPoints.childCount; i++)
            occupiedPoints.Add(guardPoints.GetChild(i), null);
        for (int i = 0; i < innerGuardPoints.childCount; i++)
            occupiedPoints.Add(innerGuardPoints.GetChild(i), null);
        for (int i = 0; i < restingPoints.childCount; i++)
            occupiedPoints.Add(restingPoints.GetChild(i), null);
    }

    public Character PointOccupiedBy(Transform point)
    {
        if (PointIsOccupiedByAnyCharacter(point))
            return null;
        if (point != null)
            if (occupiedPoints.ContainsKey(point))
                return occupiedPoints[point];
        return null;
    }

    public void OccupyPoint(Transform point, Character occupier)
    {
        if (point == null)
            return;
        if (occupiedPoints.ContainsKey(point))
        {
            //if (locationName == "Farm")
            //    Debug.Log(point.position + " occupied by " + occupier.name);
            occupiedPoints[point] = occupier;
        }
    }

    public void FreePoint(Transform point)
    {
        if (point == null)
            return;
        if (occupiedPoints.ContainsKey(point))
            occupiedPoints[point] = null;
    }

    public bool PointIsOccupiedByAnyCharacter(Transform point)
    {
        foreach (Character character in game.activeCharacters)
        {
            Vector2 charPosition = new Vector2(character.transform.position.x, character.transform.position.y);
            if (game.VectorsAreEqual(charPosition, point.position))
                return true;
        }
        return false;
    }

    int GetOccupiedPointCount(Transform points)
    {
        int count = 0;
        if (points == null)
            return 0;
        for (int i = 0; i < points.childCount; i++)
            if (PointOccupiedBy(points.GetChild(i)) != null)
                count++;
        return count;
    }

    public int GetOccupiedGuardPointCount()
    {
        return GetOccupiedPointCount(guardPoints);
    }

    public int GetOccupiedInnerGuardPointCount()
    {
        return GetOccupiedPointCount(innerGuardPoints);
    }

    public int GetOccupiedRestingSpacesPointCount()
    {
        return GetOccupiedPointCount(restingPoints);
    }

    List<Transform> GetAllFreePoints(Transform points)
    {
        List<Transform> freePoints = new List<Transform>();
        for (int j = 0; j < points.childCount; j++)
            if (PointOccupiedBy(points.GetChild(j)) == null)
                freePoints.Add(points.GetChild(j));
        return freePoints;
    }

    Transform GetRandomFreePoint(Transform points)
    {
        List<Transform> freePoints = GetAllFreePoints(points);
        if (freePoints.Count > 0)
            return freePoints.ElementAt(Random.Range(0, freePoints.Count - 1));
        return null;
    }

    public Transform GetFreeGuardSpace()
    {
        return GetRandomFreePoint(guardPoints);
    }

    public Transform GetFreeInnerGuardSpace()
    {
        return GetRandomFreePoint(innerGuardPoints);
    }

    public Transform GetFreeRestingSpace()
    {
        return GetRandomFreePoint(restingPoints);
    }

    //public void Update()
    //{
    //    if (locationName == "Farm")
    //    {
    //        Debug.Log("occupied guard points: " + GetOccupiedGuardPointCount());
    //        Debug.Log("occupied inner guard points: " + GetOccupiedInnerGuardPointCount());
    //        Debug.Log("occupied rest points: " + GetOccupiedRestingSpacesPointCount());
    //    }
    //}
}