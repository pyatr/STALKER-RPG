using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBEgear : MonoBehaviour
{
    public List<GameObject> GetAllSlots()
    {
        List<GameObject> slots = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            slots.Add(transform.GetChild(i).gameObject);
        return slots;
    }

    public bool HasEmptySlots()
    {
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).childCount == 0)
                return true;
        return false;
    }

    public int GetEmptyPocketsCount()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).childCount == 0)
                count++;
        return count;
    }

    public int GetOccupiedPocketsCount()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).childCount > 0)
                count++;
        return count;
    }

    public int GetItemCount()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
            count += transform.GetChild(i).childCount;
        return count;
    }

    public List<GameObject> GetAllItems()
    {
        List<GameObject> items = new List<GameObject>();
        List<GameObject> slots = GetAllSlots();
        foreach (GameObject slot in slots)
            for (int i = 0; i < slot.transform.childCount; i++)
                items.Add(slot.transform.GetChild(i).gameObject);
        return items;
    }

    public bool PlaceItemAnywhere(GameObject item)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            ItemSlot currentSlot = transform.GetChild(i).gameObject.GetComponent<ItemSlot>();
            if (currentSlot.IsOccupiedWithSameItemType(item))
                if (currentSlot.PlaceItem(item))
                    return true;
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            ItemSlot currentSlot = transform.GetChild(i).gameObject.GetComponent<ItemSlot>();
            if (currentSlot.ItemFits(item))
                if (currentSlot.PlaceItem(item))
                    return true;
        }
        return false;
    }
}