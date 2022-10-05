using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class ItemSlot : MonoBehaviour
{
    public string slotType;
    public UIslot slotInInventory;

    public bool IsOccupied()
    {
        if (transform.childCount > 0)
            return true;
        else
            return false;
    }

    public bool IsOccupiedWithSameItemType(GameObject item) //Or is empty
    {
        if (IsOccupied())
        {
            if (item.GetComponent<Item>().displayName == transform.GetChild(0).GetComponent<Item>().displayName)
                return true;
            else
                return false;
        }
        return true;
    }

    public bool ItemAlreadyInSlot(GameObject item)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject == item)
            {
                Debug.Log(item.name + " is already stored in " + gameObject.name);
                return false;
            }
        }
        return true;
    }

    public bool SlotIsBuiltIn()
    {
        if (transform.parent != null)
            return transform.parent.name == "Inventory";
        return false;
    }

    public bool ItemFits(GameObject item)
    {
        Item itemComponent = item.GetComponent<Item>();
        if (itemComponent)
        {
            if (itemComponent.secondarySlots.Count > 0 && transform.parent.name == "Inventory")
            {
                string[] builtinSlots = Enum.GetNames(typeof(BuiltinCharacterSlots));
                if (builtinSlots.Contains(gameObject.name))
                {
                    for (int i = 0; i < transform.parent.childCount; i++)
                    {
                        string slotObjectName = transform.parent.GetChild(i).name.ToLower();
                        //slotObjectName = slotObjectName.First().ToString().ToUpper() + slotObjectName.Substring(1);                        
                        if (itemComponent.secondarySlots.Contains(slotObjectName))
                        {
                            if (transform.parent.GetChild(i).childCount > 0)
                            {
                                //Debug.Log("can't place " + itemComponent.displayName + " in " + gameObject.name + ": " + slotObjectName + " is occupied");
                                return false;
                            }
                        }
                    }
                }
            }
            if (!IsOccupiedWithSameItemType(item))
                return false;
            if (!ItemAlreadyInSlot(item))
                return false;
            if (item.GetComponent<LBEgear>())
            {
                string[] exceptionSlots = { "ground", "backpack", "thighrig", "belt", "vest", "armor" };
                if (gameObject.name != "Weapon" && !exceptionSlots.Contains(slotType) && item.GetComponent<LBEgear>().GetItemCount() > 0)
                    return false;
            }
            int maxStackSize = itemComponent.ItemFitsInSlot(slotType);
            if (maxStackSize > 0 && transform.childCount < maxStackSize)
                return true;
        }
        else
            Debug.Log(item.name + " does not have item component");
        return false;
    }

    public Character GetOwner()
    {
        Transform currentTransform = transform;
        while (currentTransform.parent != null && !currentTransform.GetComponent<Character>())
            currentTransform = currentTransform.parent;
        return currentTransform.GetComponent<Character>();
    }

    public bool PlaceItem(GameObject item)
    {
        if (ItemFits(item))
        {
            Item itemComponent = item.GetComponent<Item>();
            Character owner = GetOwner();
            if (owner != null)
            {
                if (itemComponent.GetWeight() + owner.CalculateEncumbrance() > owner.MaxEncumbrance)
                {
                    itemComponent.world.UpdateLog(itemComponent.displayName + " is too heavy.");
                    itemComponent.world.GetComponent<InventoryManager>().OverrideItemViewText(itemComponent.displayName + " is too heavy.");
                    return false;
                }
            }
            item.transform.SetParent(gameObject.transform);
            item.transform.localPosition = gameObject.transform.localPosition;
            if (itemComponent.primarySlot == slotType && SlotIsBuiltIn())
            {
                for (int i = 0; i < itemComponent.secondarySlots.Count; i++)
                {
                    GameObject itemExtension = Instantiate(item);
                    itemExtension.name = item.name + "Extension";
                    Item itemExtensionItemComponent = itemExtension.GetComponent<Item>();
                    itemExtensionItemComponent.basePrice = 0;
                    //Color32[] pixels = itemComponent.sprite.texture.GetPixels32();
                    //for (int j = 0; j < pixels.Length; j++)
                    //{
                    //    pixels[j].r = (byte)Mathf.Clamp(pixels[j].r - 56, 0, pixels[j].r);
                    //    pixels[j].g = (byte)Mathf.Clamp(pixels[j].g - 56, 0, pixels[j].g);
                    //    pixels[j].b = (byte)Mathf.Clamp(pixels[j].b - 56, 0, pixels[j].b);                        
                    //}
                    //Texture2D newTexture = new Texture2D(itemComponent.sprite.texture.width, itemComponent.sprite.texture.height);
                    //newTexture.SetPixels32(pixels);
                    //newTexture.Apply();
                    itemExtensionItemComponent.Sprite = itemComponent.Sprite;//Sprite.Create(newTexture, itemComponent.sprite.rect, itemComponent.sprite.pivot);
                    itemExtensionItemComponent.secondarySlots.Clear();
                    itemExtensionItemComponent.primarySlot = itemComponent.secondarySlots[i];
                    itemExtensionItemComponent.slots.Clear();
                    itemExtensionItemComponent.slots.Add(itemComponent.secondarySlots[i], 1);
                    for (int j = 0; j < itemExtension.transform.childCount; j++)
                        for (int k = 0; k < itemExtension.transform.GetChild(j).childCount; k++)
                            Destroy(itemExtension.transform.GetChild(j).GetChild(k).gameObject);

                    for (int j = 0; j < item.transform.childCount; j++)
                    {
                        Transform currentSlot = item.transform.GetChild(j);
                        List<GameObject> itemsToTransfer = new List<GameObject>();
                        for (int k = 0; k < currentSlot.childCount; k++)
                            itemsToTransfer.Add(currentSlot.GetChild(k).gameObject);
                        foreach (GameObject itemToTransfer in itemsToTransfer)
                            itemToTransfer.transform.parent = itemExtension.transform.GetChild(j);
                    }
                    ItemExtension itemExtensionComponent = itemExtension.AddComponent<ItemExtension>();
                    itemExtensionComponent.itemComponent = itemExtensionItemComponent;
                    itemExtensionComponent.extensionOf = TextTools.FirstCharToUpper(itemComponent.primarySlot);
                    itemComponent.extensions.Add(itemExtensionComponent);
                    string slotTypeFormatted = TextTools.FirstCharToUpper(itemComponent.secondarySlots[i]);
                    transform.parent.parent.GetComponent<Character>().PlaceItemInSlot(itemExtension, (BuiltinCharacterSlots)Enum.Parse(typeof(BuiltinCharacterSlots), slotTypeFormatted));
                }
            }
            itemComponent.lastOwner = owner;
            return true;
        }
        //Debug.Log("Could not place " + item.name + " to " + gameObject.name);
        return false;
    }

    public GameObject GetItem()
    {
        if (transform.childCount > 0)
        {
            //Debug.Log(transform.GetChild(0).gameObject.name + " stored in " + gameObject.name);
            return transform.GetChild(0).gameObject;
        }
        return null;
    }
}