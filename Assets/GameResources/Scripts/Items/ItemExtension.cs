using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemExtension : MonoBehaviour
{
    public string extensionOf;
    public Item itemComponent;

    public BuiltinCharacterSlots GetParentSlotType()
    {
        return (BuiltinCharacterSlots)Enum.Parse(typeof(BuiltinCharacterSlots), extensionOf);
    }

    public GameObject GetParentObject()
    {
        //Character owner = itemComponent.GetOwner()?.GetComponent<Character>();
        //if (owner)
        //{
        //
        //}
        return null;
    }
}