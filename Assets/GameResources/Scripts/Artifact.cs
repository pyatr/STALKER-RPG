using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Artifact : MonoBehaviour
{
    public Dictionary<string, int> attributeModifiers = new Dictionary<string, int>();
    public Dictionary<DamageTypes, int> damageModifiers = new Dictionary<DamageTypes, int>();
    public float regenerationModifier = 0.0f;
    public float radiationGain = 0.0f;

    public void OnPlaceInSlot()
    {
        ItemSlot currentSlot = transform.parent.GetComponent<ItemSlot>();
        if (currentSlot)
        {
            Character slotOwner = currentSlot.GetOwner();
            if(slotOwner)
            {

            }
        }
    }

    public void OnRemoveFromSlot()
    {

    }
}