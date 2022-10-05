using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MerchantStock : MonoBehaviour
{
    public int refreshFrequency = 500;
    public int moneyGainMin = 500;
    public int moneyGainMax = 5000;
    public int itemRefreshAmountMin = 10;
    public int itemRefreshAmountMax = 15;
    public int maxMoney = 60000;
    public int maxOccupiedSlots = 64;
    public int refreshTimer = 0;
    public string infoBlockReference = "";

    public void Initiate()
    {
        if (infoBlockReference != "")
        {
            MakeSlotsIfNecessary();
            InfoBlock merchantStockBlock = Game.Instance.MerchantStocks.GetBlock(infoBlockReference);
            if (merchantStockBlock != null)
            {
                LoadSettingsFromInfoBlock(merchantStockBlock);
                for (int i = 0; i < 4; i++)
                    LoadItems(merchantStockBlock.GetBlock("items"));
            }
        }
    }

    public void LoadSettingsFromInfoBlock(InfoBlock stockTemplate)
    {
        if (stockTemplate != null)
        {
            foreach (KeyValuePair<string, string> kvp in stockTemplate.namesValues)
            {
                switch (kvp.Key)
                {
                    case "refresh_frequency": refreshFrequency = int.Parse(kvp.Value); break;
                    case "max_money": maxMoney = int.Parse(kvp.Value); break;
                    case "max_occupied_slots": maxOccupiedSlots = int.Parse(kvp.Value); break;
                    case "money_gain_amount": moneyGainMin = int.Parse(kvp.Value); moneyGainMax = moneyGainMin; break;
                    case "item_refresh_amount": itemRefreshAmountMin = int.Parse(kvp.Value); itemRefreshAmountMax = itemRefreshAmountMin; break;
                }
            }
            foreach (InfoBlock subBlock in stockTemplate.subBlocks)
            {
                switch (subBlock.name)
                {
                    case "money_gain_amount": moneyGainMin = int.Parse(subBlock.values[0]); moneyGainMax = int.Parse(subBlock.values[1]); break;
                    case "item_refresh_amount": itemRefreshAmountMin = int.Parse(subBlock.values[0]); itemRefreshAmountMax = int.Parse(subBlock.values[1]); break;
                }
            }
        }
        else
            Debug.Log("Tried to load null merchant stock settings");
    }

    public void LoadItems(InfoBlock itemList)
    {
        if (itemList != null && GetOccupiedSlotAmount() <= maxOccupiedSlots)
        {
            int itemsToCreate = itemRefreshAmountMin;
            if (itemRefreshAmountMax > itemRefreshAmountMin)
                itemsToCreate = Random.Range(itemRefreshAmountMin, itemRefreshAmountMax);
            for (int j = 0; j < itemsToCreate; j++)
            {
                int desiredEntryNum = Random.Range(0, itemList.namesValues.Count + itemList.subBlocks.Count);
                int entryNum = 0;
                foreach (KeyValuePair<string, string> kvp in itemList.namesValues)
                {
                    entryNum++;
                    if (entryNum >= desiredEntryNum)
                    {
                        entryNum = 0;
                        int amountToCreate = int.Parse(kvp.Value);
                        for (int i = 0; i < amountToCreate && j < itemsToCreate; i++)
                        {
                            AddItemToStock(kvp.Key);
                            j++;
                        }
                        break;
                    }
                }
                foreach (InfoBlock subBlock in itemList.subBlocks)
                {
                    entryNum++;
                    if (entryNum >= desiredEntryNum)
                    {
                        entryNum = 0;
                        int minAmount = 1;
                        int maxAmount = 1;
                        minAmount = int.Parse(subBlock.values[0]);
                        maxAmount = int.Parse(subBlock.values[1]);
                        int amountToCreate = 1;
                        if (maxAmount > minAmount)
                            amountToCreate = Random.Range(minAmount, maxAmount);
                        for (int i = 0; i < amountToCreate && j < itemsToCreate; i++)
                        {
                            AddItemToStock(subBlock.name);
                            j++;
                        }
                        break;
                    }
                }
            }
        }
    }

    public void AddItemToStock(string templateName)
    {
        World world = World.GetInstance();
        GameObject newItem = world.CreateItem(templateName);
        AddItemToStock(newItem);
    }

    public void AddItemToStock(GameObject item)
    {
        if (item != null)
        {
            MakeSlotsIfNecessary();
            bool itemPlaced = false;
            for (int j = 0; j < transform.childCount && !itemPlaced; j++)
                if (transform.GetChild(j).GetComponent<ItemSlot>().PlaceItem(item))
                    itemPlaced = true;
        }
    }

    private void TakeItems()
    {
        if (GetItemAmount() > 0)
        {
            int amount = itemRefreshAmountMin;
            if (itemRefreshAmountMax > itemRefreshAmountMin)
                amount = Random.Range(itemRefreshAmountMin, itemRefreshAmountMax);
            int slotCount = transform.childCount;
            int itemNum = Random.Range(0, GetItemAmount());
            int currentItemNumber = 0;
            bool startOver = false;
            while (amount > 0 && GetItemAmount() > 0)
            {
                startOver = false;
                for (int j = 0; j < slotCount && !startOver; j++)
                {
                    Transform currentSlot = transform.GetChild(j);
                    for (int i = 0; i < currentSlot.childCount; i++)
                    {
                        currentItemNumber++;
                        if (currentItemNumber >= itemNum)
                        {
                            //Debug.Log("destroying " + currentSlot.GetChild(i).gameObject.name + " - " + currentItemNumber + "/" + amount);
                            currentItemNumber = 0;
                            Destroy(currentSlot.GetChild(i).gameObject);
                            amount--;
                            startOver = true;
                            break;
                        }
                    }
                }
            }
        }
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
                GameObject shopSlot = new GameObject("Stock" + i.ToString());
                shopSlot.AddComponent<ItemSlot>().slotType = "ground";
                shopSlot.transform.SetParent(transform, false);
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
    }

    public void EndTurn()
    {
        refreshTimer++;
        if (refreshTimer >= refreshFrequency)
        {
            refreshTimer = 0;
            TakeItems();
            LoadItems(Game.Instance.MerchantStocks.GetBlock(infoBlockReference)?.GetBlock("items"));
            //World.GetInstance().UpdateLog("stock updated for " + transform.parent.name);
            Debug.Log("stock updated for " + transform.parent.name);
            if (transform.parent.GetComponent<Character>().money < maxMoney)
            {
                int moneyToAdd = moneyGainMin;
                if (moneyGainMax > moneyGainMin)
                    moneyToAdd = Random.Range(moneyGainMin, moneyGainMax);
                Debug.Log("Added " + moneyToAdd + " to " + transform.parent.name);
                transform.parent.GetComponent<Character>().money += moneyToAdd;
            }
        }
    }
}