using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public Dictionary<string, int> slots = new Dictionary<string, int>();
    public List<string> secondarySlots = new List<string>();
    public List<GameObject> extensions = new List<GameObject>();
    public string primarySlot = "";

    Sprite _sprite;
    public int basePrice = 3000;
    public int actionPointsToMove = 3;
    public string displayName;
    public Game game;
    public bool canBeRepaired = false;

    public Sprite sprite { get { return _sprite; } set { _sprite = value; } }
    public float Weight { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Weight"); } }
    public float Condition { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Item condition"); } }
    public float GetMaxCondition() { return GetComponent<ObjectAttributes>().GetAttribute("Item condition").maxValue; }

    public int price
    {
        get
        {
            if (Condition == 0)
                return basePrice;
            int amount = 1;
            if (GetComponent<AmmoBox>())
                amount = GetComponent<AmmoBox>().amount;
            int finalPrice = basePrice * amount * (int)Condition / (int)GetMaxCondition();
            return finalPrice;
        }
    }

    public ItemSlot GetSlot()
    {
        ItemSlot itemSlot = null;
        if (transform.parent != null)
            itemSlot = transform.parent.GetComponent<ItemSlot>();
        return itemSlot;
    }

    public GameObject GetOwner()
    {
        Transform currentTransform = transform;
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            if (currentTransform.GetComponent<Character>())            
                return currentTransform.gameObject;            
        }
        return null;
    }

    public int ItemFitsInSlot(string slotName)
    {
        if (slots.ContainsKey(slotName))
            return slots[slotName];
        //Debug.Log(gameObject.name + " can not fit in " + slotName + " slot type");
        return 0;
    }

    public float GetWeight()
    {
        float finalWeight = Weight;
        //Debug.Log("base weight of " + displayName + " is " + finalWeight);
        Firearm firearmComponent = GetComponent<Firearm>();
        if (firearmComponent)
        {
            if (firearmComponent.magazine != null)
            {
                finalWeight += firearmComponent.magazine.GetComponent<Magazine>().GetAmmoWeight() + firearmComponent.magazine.GetComponent<Item>().Weight;
                return finalWeight;
            }
        }
        Magazine magazineComponent = GetComponent<Magazine>();
        if (magazineComponent)
        {
            finalWeight += magazineComponent.GetAmmoWeight();
            return finalWeight;
        }
        AmmoBox ammoBoxComponent = GetComponent<AmmoBox>();
        if (ammoBoxComponent)
        {
            finalWeight += ammoBoxComponent.GetAmmoWeight();
            return finalWeight;
        }
        LBEgear LBEcomponent = GetComponent<LBEgear>();
        if (LBEcomponent)
        {
            List<GameObject> pockets = LBEcomponent.GetAllSlots();
            foreach (GameObject pocket in pockets)
            {
                for (int i = 0; i < pocket.transform.childCount; i++)
                    finalWeight += pocket.transform.GetChild(i).GetComponent<Item>().GetWeight();
            }
            return finalWeight;
        }
        return finalWeight;
    }
}