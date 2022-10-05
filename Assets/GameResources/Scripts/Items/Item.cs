using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    Sprite _sprite;
    public Sprite Sprite { get { return _sprite; } set { _sprite = value; } }
    public float Weight { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Weight"); } }
    public float Condition { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Item condition"); } }
    public float GetMaxCondition() { return GetComponent<ObjectAttributes>().GetAttribute("Item condition").MaxValue; }

    public int Price
    {
        get
        {
            if (Condition == 0)
                return 0;// basePrice;
            int amount = 1;
            if (GetComponent<AmmoBox>())
                amount = GetComponent<AmmoBox>().amount;
            //if (GetComponent<Magazine>())
            //    amount = Mathf.Max(GetComponent<Magazine>().ammo, 1);
            int finalPrice = basePrice * amount * (int)Condition / (int)GetMaxCondition();
            Magazine magazineComponent = GetComponent<Magazine>();
            if (magazineComponent)
                if (magazineComponent.currentCaliber != null)
                    finalPrice += magazineComponent.currentCaliber.price * magazineComponent.ammo;
            
            Firearm firearmComponent = GetComponent<Firearm>();
            if (firearmComponent)
                if (firearmComponent.magazine != null)
                    if (firearmComponent.magazine.GetComponent<Magazine>().currentCaliber != null)
                        finalPrice += firearmComponent.magazine.GetComponent<Magazine>().currentCaliber.price * firearmComponent.magazine.GetComponent<Magazine>().ammo + firearmComponent.magazine.GetComponent<Item>().basePrice;
            LBEgear gearComponent = GetComponent<LBEgear>();
            if (gearComponent)
            {
                List<GameObject> items = gearComponent.GetAllItems();
                foreach (GameObject item in items)
                    finalPrice += item.GetComponent<Item>().Price;
            }
            return finalPrice;
        }
    }

    public World world;
    public Character lastOwner = null;

    public Dictionary<string, int> slots = new Dictionary<string, int>();
    public List<string> secondarySlots = new List<string>();
    public List<ItemExtension> extensions = new List<ItemExtension>();
    public string primarySlot = "";
    public int basePrice = 3000;
    public int actionPointsToMove = 3;
    public string displayName;    
    public bool canBeRepaired = false;

    private void Start()
    {
        world = World.GetInstance();
    }

    public ItemSlot GetSlot()
    {
        ItemSlot itemSlot = null;
        if (transform.parent != null)
            itemSlot = transform.parent.GetComponent<ItemSlot>();
        return itemSlot;
    }

    public Character GetOwner()
    {
        Transform currentTransform = transform;
        while (currentTransform.parent != null)
        {
            currentTransform = currentTransform.parent;
            Character ownerCharacterComponent = currentTransform.GetComponent<Character>();
            if (ownerCharacterComponent)            
                return ownerCharacterComponent;            
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