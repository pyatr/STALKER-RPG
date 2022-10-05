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
    public List<string> tagsOnSpawn;
    public SpawnerObjectTypes objectType;

    private void Start()
    {
        if (!Game.Instance.GameShouldBeLoaded())
        {
            if (gameObject.activeSelf)
            {
                World world = World.GetInstance();
                switch (objectType)
                {
                    case SpawnerObjectTypes.Item:
                        foreach (string s in templates)
                            world.DropItemToCell(world.CreateItem(s), transform.localPosition);
                        break;
                    case SpawnerObjectTypes.Character:
                        GameObject newCharacter = world.CreateCharacter(templates[0], transform.localPosition);
                        newCharacter.GetComponent<Character>().tags.AddRange(tagsOnSpawn);
                        foreach (string s in tagsOnSpawn)
                            world.tags.Add((s, newCharacter.GetComponent<Character>()));
                        break;
                    case SpawnerObjectTypes.Decoration: break;
                }
            }
        }
        Destroy(gameObject);
    }
}