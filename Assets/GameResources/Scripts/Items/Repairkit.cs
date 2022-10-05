using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repairkit : Kit
{
    public void OnUse(bool inCombat = false)
    {
        Character owner = ItemComponent.GetOwner();
        if (owner != null)
        {
            inCombat = owner.IsInCombat();
            GameObject itemOnBack = owner.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backweapon);
            bool itemRepaired = false;
            if (itemOnBack != null)
            {
                itemRepaired = RepairItem(owner, itemOnBack, inCombat);
            }
            else
            {
                List<GameObject> items = GetItems(owner);
                foreach (GameObject item in items)
                {
                    if (!item.GetComponent<ItemExtension>() && item.GetComponent<Item>().canBeRepaired)
                    {
                        itemRepaired = RepairItem(owner, item, inCombat);
                        break;
                    }
                }
            }
            if (!itemRepaired)
            {
                if (owner.IsPlayer())
                    owner.world.UpdateLog("No items left to repair");
                //owner.waitingTurns = 0;
                //owner.usingKit = false;
            }
        }
    }

    private List<GameObject> GetItems(Character character)
    {
        List<GameObject> items = new List<GameObject>();
        if (character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Armor) != null)
            items.Add(character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Armor));
        if (character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Helmet) != null)
            items.Add(character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Helmet));
        if (character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest) != null)
            items.AddRange(character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest).GetComponent<LBEgear>().GetAllItems());
        if (character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt) != null)
            items.AddRange(character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt).GetComponent<LBEgear>().GetAllItems());
        if (character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backpack) != null && !character.IsInCombat())
            items.AddRange(character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backpack).GetComponent<LBEgear>().GetAllItems());
        return items;
    }

    private bool RepairItem(Character owner, GameObject item, bool ownerIsInCombat)
    {
        Attribute condition = item.GetComponent<ObjectAttributes>().GetAttribute("Item condition");
        if (condition.Value < condition.MaxValue)
        {
            float previousCondition = condition.Value;
            float bonus = 1;
            if (!ownerIsInCombat)
                bonus = 4;
            Attribute.ChangeResult result = condition.Modify(owner.GetAttribute("Mechanical").Value / 4 * bonus);
            if (owner.IsPlayer())
                owner.world.UpdateLog(item.GetComponent<Item>().displayName + " condition improved from " + ((int)previousCondition).ToString() + " to " + ((int)condition.Value).ToString());
            if (result == Attribute.ChangeResult.AboveMax)
            {
                if (owner.IsPlayer())
                    owner.world.UpdateLog(item.GetComponent<Item>().displayName + " fully repaired.");
                DecreaseCondition(bonus * 25 / owner.GetAttribute("Mechanical").Value);
                //owner.waitingTurns = 0;
            }
            return true;
        }
        else if (owner.IsPlayer()) 
            owner.world.UpdateLog(item.GetComponent<Item>().displayName + " does not need repair.");
        return false;
    }
}