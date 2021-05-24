using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnerObjectTypes
{
    Character,
    Item,
    Decoration
}

public class ObjectSpawner : MonoBehaviour
{
    public List<string> templates;
    public SpawnerObjectTypes objectType;
}