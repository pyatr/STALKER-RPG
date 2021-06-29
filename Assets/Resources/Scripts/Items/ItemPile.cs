using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPile : MonoBehaviour
{
    public void SetImageToItemCount()
    {
        string[] itemPileNames = { "small", "medium", "big" };
        int pileSize = -1;
        int itemsCount = GetItemAmount();
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        if (itemsCount == 0)
        {
            spriteRenderer.sprite = null;
            return;
        }
        if (itemsCount == 1)
        {
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).childCount > 0)
                {
                    spriteRenderer.sprite = transform.GetChild(i).GetChild(0).GetComponent<Item>().Sprite;
                    transform.localScale = Vector3.one / 4;
                    return;
                }
        }
        transform.localScale = Vector3.one;
        pileSize = Mathf.Clamp(itemsCount / 4, 0, 2);
        if (pileSize >= 0)
            gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/item_pile_" + itemPileNames[pileSize]);
    }

    public int GetItemAmount()
    {
        int itemsCount = 0;
        for (int i = 0; i < transform.childCount; i++)
            itemsCount += transform.GetChild(i).childCount;
        return itemsCount;
    }

    public int GetOccupiedSlotAmount()
    {
        int occupiedSlots = 0;
        for (int i = 0; i < transform.childCount; i++)
            if (transform.GetChild(i).childCount > 0)
                occupiedSlots++;
        return occupiedSlots;
    }

    public bool MakeSlotsIfNecessary()
    {
        int slotsCount = transform.childCount;
        if (!LastPageIsEmpty() || transform.childCount == 0) 
        {
            for (int i = slotsCount; i < InventoryManager.ItemPageSlotCount + slotsCount; i++)
            {
                GameObject ground = new GameObject("Ground" + i.ToString());
                ground.AddComponent<ItemSlot>().slotType = "ground";
                ground.transform.SetParent(transform, false);
            }
            return true;
        }
        return false;
    }

    public int GetPageCount()
    {
        return transform.childCount / InventoryManager.ItemPageSlotCount;
    }

    public int GetSlotCount()
    {
        return transform.childCount;
    }

    public ItemSlot GetSlot(int num)
    {
        return transform.GetChild(num).gameObject.GetComponent<ItemSlot>();
    }

    public bool LastPageIsEmpty()
    {
        int startSlot = Mathf.Max(0, transform.childCount - 1);
        int slotCount = Mathf.Max(0, transform.childCount - InventoryManager.ItemPageSlotCount);
        for (int i = startSlot; i > slotCount; i--)
            if (transform.GetChild(i).childCount > 0)
                return false;
        return true;
    }

    public void DeleteEmptyPages()
    {
        if (LastPageIsEmpty())
            for (int i = transform.childCount - 1; i >= InventoryManager.ItemPageSlotCount; i--)
                Destroy(transform.GetChild(i).gameObject);
        Debug.Log("deleted a page");
    }
}