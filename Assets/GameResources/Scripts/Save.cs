using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class Save : MonoBehaviour
{
    private World world;

    private void Awake()
    {
        world = World.GetInstance();
    }

    public void SaveGame(int slotNumber)
    {
        if (slotNumber >= 1 && slotNumber <= Game.Instance.maxGameSlots)
        {
            try
            {
                if (world.Player != null)
                {
                    Character player = world.Player.GetComponent<Character>();
                    if (player.performingAction || player.IsMoving())
                    {
                        world.UpdateLog("You can not save while performing action");
                        return;
                    }
                    if (player.turnFinished)
                    {
                        world.UpdateLog("You can only save during your turn");
                        return;
                    }
                }
                string saveSlot = "Save" + slotNumber.ToString();
                string savePath = Application.persistentDataPath + "/Saves/" + saveSlot;
                if (!Directory.Exists(savePath))
                    Directory.CreateDirectory(savePath);
                InfoBlock save = new InfoBlock { name = "save" };
                save.namesValues.Add("seed", "abcdefgh");
                save.namesValues.Add("turns_passed", world.turnsPassed.ToString());
                InfoBlock worldStates = new InfoBlock { name = "world_states" };
                foreach (KeyValuePair<string, bool> worldState in world.worldStates)
                    worldStates.namesValues.Add(worldState.Key, worldState.Value.ToString());
                save.subBlocks.Add(worldStates);
                save.subBlocks.Add(SaveCharacters());
                save.subBlocks.Add(SaveItemsOnGround());
                save.WriteToFile("Saves/" + saveSlot + "/save.sav");
                ScreenCapture.CaptureScreenshot(savePath + "/screenshot.png");
                world.UpdateLog("Game saved");
                //FileStream dataStream = new FileStream(savePath, FileMode.Create);
                //BinaryFormatter converter = new BinaryFormatter();
                //converter.Serialize(dataStream, saveData);
                //dataStream.Close();
            }
            catch (ArgumentException e)
            {
                Debug.Log("Could not save game " + e.ToString());
            }
        }
    }

    public void LoadGame(int slotNumber)
    {
        if (slotNumber >= 1 && slotNumber <= Game.Instance.maxGameSlots)
        {
            string saveSlot = "Save" + slotNumber.ToString();
            string savePath = Application.persistentDataPath + "/Saves/" + saveSlot + "/";
            string saveFile = savePath + "save.sav";
            string rawSaveData = TextTools.GetTextFromFileAsString(saveFile);
            if (rawSaveData != "")
            {
                //UnloadGame();
                InfoBlock saveFileBlock = InfoBlockReader.SplitTextIntoInfoBlocks(rawSaveData);
                world.turnsPassed = int.Parse(saveFileBlock.namesValues["turns_passed"]) - 1;
                world.characterController.temporaryLockTime = 10;
                InfoBlock worldStates = saveFileBlock.GetBlock("world_states");
                foreach (KeyValuePair<string, string> worldState in worldStates.namesValues)
                    world.worldStates.Add(worldState.Key, bool.Parse(worldState.Value));
                InfoBlock characters = saveFileBlock.GetBlock("characters");
                foreach (InfoBlock character in characters.subBlocks)
                {
                    GameObject newCharacter = LoadCharacter(character);
                    if (character.values.Contains("player"))
                        world.characterController.SetCharacterControl(newCharacter);
                }
                InfoBlock itemPiles = saveFileBlock.GetBlock("piles");
                LoadItemsOnGround(itemPiles);
                world.UpdateLog("Game loaded");
            }
            else
            {
                Debug.Log("Save file at slot " + slotNumber + " not found (" + saveFile + ")");
            }
        }
    }

    private void UnloadGame()
    {
        for (int i = 0; i < world.Characters.childCount; i++)
        {
            Character character = world.Characters.GetChild(i).GetComponent<Character>();
            Destroy(character.characterNameOnGUI.gameObject);
            Destroy(character.gameObject);
        }
        for (int i = 0; i < world.ground.childCount; i++)
        {
            //Debug.Log("Will destroy " + world.ground.GetChild(i).name);
            Destroy(world.ground.GetChild(i).gameObject);
        }
        world.activeCharacters.Clear();
        //Debug.Log("unloaded game");
    }

    private InfoBlock SaveCharacters()
    {
        InfoBlock characters = new InfoBlock { name = "characters" };
        foreach (Character character in world.activeCharacters)
        {
            InfoBlock savedCharacter = new InfoBlock { name = character.name };
            if (character.IsPlayer())
                savedCharacter.values.Add("player");
            savedCharacter.subBlocks.Add(SaveCoordinates(character.gameObject));
            InfoBlock relations = new InfoBlock { name = "relations" };
            InfoBlock friendly = new InfoBlock { name = "friendly" };
            friendly.values.AddRange(character.friendlyTowards);
            InfoBlock hostile = new InfoBlock { name = "hostile" };
            hostile.values.AddRange(character.hostileTowards);
            InfoBlock factionsToForgive = new InfoBlock { name = "factions_to_forgive" };
            foreach (KeyValuePair<string, int> kvp in character.brain.factionsToForgive)
                factionsToForgive.namesValues.Add(kvp.Key, kvp.Value.ToString());
            InfoBlock temporaryPeace = new InfoBlock { name = "temporary_peace" };
            foreach (KeyValuePair<string, int> kvp in character.brain.temporaryPeace)
                temporaryPeace.namesValues.Add(kvp.Key, kvp.Value.ToString());
            savedCharacter.namesValues.Add("turns_since_last_attack", character.brain.turnsSinceLastAttack.ToString());
            if (character.brain.occupationTarget != null)
            {
                savedCharacter.namesValues.Add("occupation_target", character.brain.occupationTarget.name);
                savedCharacter.namesValues.Add("occupation_target_type", character.brain.occupationTarget.parent.name);
            }
            relations.subBlocks.Add(friendly);
            relations.subBlocks.Add(hostile);
            relations.subBlocks.Add(factionsToForgive);
            relations.subBlocks.Add(temporaryPeace);
            savedCharacter.subBlocks.Add(relations);
            savedCharacter.namesValues.Add("sprite", character.GetComponent<SpriteRenderer>().sprite.name);
            savedCharacter.namesValues.Add("finished", character.turnFinished.ToString());
            savedCharacter.namesValues.Add("invulnerable", character.invulnerable.ToString());
            savedCharacter.namesValues.Add("immobile", character.immobile.ToString());
            savedCharacter.namesValues.Add("action_points", character.actionPoints.ToString().Replace(',', '.'));
            savedCharacter.namesValues.Add("faction", character.faction);
            savedCharacter.namesValues.Add("dialogue", character.dialoguePackageName);
            savedCharacter.namesValues.Add("display_name", character.displayName);
            savedCharacter.namesValues.Add("character_points", character.freeCharacterPoints.ToString());
            savedCharacter.namesValues.Add("money", character.money.ToString());
            savedCharacter.namesValues.Add("regeneration_modifier", character.regenerationModifier.ToString());
            savedCharacter.namesValues.Add("sight_distance", character.sightDistance.ToString());
            savedCharacter.namesValues.Add("experience", character.experience.ToString());
            savedCharacter.subBlocks.Add(SaveAttributes(character.GetComponent<ObjectAttributes>().attributes));
            if (character.tags.Count > 0)
            {
                InfoBlock tagsBlock = new InfoBlock { name = "tags" };
                tagsBlock.values.AddRange(character.tags);
                savedCharacter.subBlocks.Add(tagsBlock);
            }
            if (character.merchantStock != null)
            {
                InfoBlock merchantStockBlock = new InfoBlock { name = "merchant_stock" };
                merchantStockBlock.namesValues.Add("refresh_timer", character.merchantStock.refreshTimer.ToString());
                merchantStockBlock.namesValues.Add("settings_name", character.merchantStock.infoBlockReference);
                Transform characterMerchantStockItems = character.merchantStock.transform;
                for (int i = 0; i < characterMerchantStockItems.childCount; i++)
                {
                    Transform itemsInSlot = characterMerchantStockItems.GetChild(i);
                    for (int j = 0; j < itemsInSlot.childCount; j++)
                        merchantStockBlock.subBlocks.Add(SaveItem(itemsInSlot.GetChild(j).gameObject));
                }
                savedCharacter.subBlocks.Add(merchantStockBlock);
            }
            InfoBlock equippedItems = new InfoBlock { name = "items" };
            Transform itemSlots = character.inventory.transform;
            int itemCount = itemSlots.childCount;
            for (int i = 0; i < itemCount; i++)
            {
                Transform currentSlot = itemSlots.GetChild(i);
                for (int j = 0; j < currentSlot.childCount; j++)
                    equippedItems.subBlocks.Add(SaveItem(currentSlot.GetChild(j).gameObject));
            }
            //Debug.Log(character.displayName + " has " + equippedItems.subBlocks.Count + " items equipped");
            savedCharacter.subBlocks.Add(equippedItems);
            characters.subBlocks.Add(savedCharacter);
        }
        return characters;
    }

    private GameObject LoadCharacter(InfoBlock character)
    {
        Vector2 position = Vector2.zero;
        character.GetBlock("position").GetCoordinates(out position);
        GameObject newCharacter = world.CreateCharacter("player", position, false);
        newCharacter.name = character.name;
        Character newCharacterComponent = newCharacter.GetComponent<Character>();
        InfoBlock relationsBlock = character.GetBlock("relations");
        newCharacterComponent.friendlyTowards.AddRange(relationsBlock.GetBlock("friendly").values);
        newCharacterComponent.hostileTowards.AddRange(relationsBlock.GetBlock("hostile").values);
        foreach (KeyValuePair<string, string> kvp in relationsBlock.GetBlock("factions_to_forgive").namesValues)
            newCharacterComponent.brain.factionsToForgive.Add(kvp.Key, int.Parse(kvp.Value));
        foreach (KeyValuePair<string, string> kvp in relationsBlock.GetBlock("temporary_peace").namesValues)
            newCharacterComponent.brain.temporaryPeace.Add(kvp.Key, int.Parse(kvp.Value));
        newCharacterComponent.brain.turnsSinceLastAttack = int.Parse(character.namesValues["turns_since_last_attack"]);
        Location characterLocation = newCharacterComponent.GetCurrentLocation();
        if (characterLocation != null)
        {
            if (character.namesValues.ContainsKey("occupation_target") && character.namesValues.ContainsKey("occupation_target_type"))
            {
                Transform point = characterLocation.GetSpaceByName(character.namesValues["occupation_target"], character.namesValues["occupation_target_type"]);
                if (point != null)
                    newCharacterComponent.brain.occupationTarget = point;
            }
        }
        newCharacterComponent.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/Characters/" + character.namesValues["sprite"]);
        newCharacterComponent.turnFinished = bool.Parse(character.namesValues["finished"]);
        newCharacterComponent.invulnerable = bool.Parse(character.namesValues["invulnerable"]);
        newCharacterComponent.immobile = bool.Parse(character.namesValues["immobile"]);
        newCharacterComponent.dialoguePackageName = character.namesValues["dialogue"];
        newCharacterComponent.actionPoints = float.Parse(character.namesValues["action_points"]);
        newCharacterComponent.faction = character.namesValues["faction"];
        newCharacterComponent.displayName = character.namesValues["display_name"];
        newCharacterComponent.characterNameOnGUI.text = newCharacterComponent.displayName;
        newCharacterComponent.freeCharacterPoints = int.Parse(character.namesValues["character_points"]);
        newCharacterComponent.money = int.Parse(character.namesValues["money"]);
        newCharacterComponent.regenerationModifier = int.Parse(character.namesValues["regeneration_modifier"]);
        newCharacterComponent.sightDistance = int.Parse(character.namesValues["sight_distance"]);
        newCharacterComponent.experience = int.Parse(character.namesValues["experience"]);
        LoadAttributes(newCharacter.GetComponent<ObjectAttributes>().attributes, character.GetBlock("attributes"));
        InfoBlock merchantStockBlock = character.GetBlock("merchant_stock");
        if (merchantStockBlock != null)
        {
            GameObject merchantStockObject = new GameObject("MerchantStock");
            merchantStockObject.transform.SetParent(newCharacter.transform);
            newCharacterComponent.merchantStock = merchantStockObject.AddComponent<MerchantStock>();
            newCharacterComponent.merchantStock.refreshTimer = int.Parse(merchantStockBlock.namesValues["refresh_timer"]);
            newCharacterComponent.merchantStock.infoBlockReference = merchantStockBlock.namesValues["settings_name"];
            newCharacterComponent.merchantStock.LoadSettingsFromInfoBlock(Game.Instance.MerchantStocks.GetBlock(newCharacterComponent.merchantStock.infoBlockReference));
            foreach (InfoBlock item in merchantStockBlock.subBlocks)
                newCharacterComponent.merchantStock.AddItemToStock(LoadItem(item));
        }
        InfoBlock tags = character.GetBlock("tags");
        if (tags != null)
        {
            newCharacterComponent.tags.AddRange(tags.values);
            foreach (string newTag in newCharacterComponent.tags)
                world.tags.Add((newTag, newCharacterComponent));
        }
        InfoBlock inventory = character.GetBlock("items");
        if (inventory != null)
            foreach (InfoBlock equippedItem in inventory.subBlocks)
                newCharacterComponent.PlaceItemInSlot(LoadItem(equippedItem), (BuiltinCharacterSlots)Enum.Parse(typeof(BuiltinCharacterSlots), equippedItem.namesValues["parent_slot"]));
        return newCharacter;
    }

    private InfoBlock SaveItemsOnGround()
    {
        Transform itemPiles = world.ground;
        InfoBlock pilesBlock = new InfoBlock { name = "piles" };
        int pileCount = itemPiles.childCount;
        for (int i = 0; i < pileCount; i++)
        {
            Transform pile = itemPiles.GetChild(i);
            int slotsInPile = pile.childCount;
            InfoBlock itemsInPile = new InfoBlock { name = "items" };
            for (int j = 0; j < slotsInPile; j++)
            {
                Transform slot = pile.GetChild(j);
                int itemsInSlot = slot.childCount;
                for (int k = 0; k < itemsInSlot; k++)
                    itemsInPile.subBlocks.Add(SaveItem(slot.GetChild(k).gameObject));
            }
            InfoBlock pileBlock = new InfoBlock { name = "pile" };
            if (itemsInPile.subBlocks.Count > 0)
            {
                pileBlock.subBlocks.Add(itemsInPile);
                pileBlock.subBlocks.Add(SaveCoordinates(pile.gameObject));
                pilesBlock.subBlocks.Add(pileBlock);
            }
        }
        return pilesBlock;
    }

    private void LoadItemsOnGround(InfoBlock itemsOnGround)
    {
        foreach (InfoBlock itemPile in itemsOnGround.subBlocks)
        {
            if (itemPile.name == "pile")
            {
                Vector2 pilePosition = Vector2.zero;
                itemPile.GetBlock("position").GetCoordinates(out pilePosition);
                InfoBlock itemsBlock = itemPile.GetBlock("items");
                if (itemsBlock != null)
                {
                    List<GameObject> loadedItems = new List<GameObject>();
                    foreach (InfoBlock item in itemsBlock.subBlocks)
                        loadedItems.Add(LoadItem(item));
                    world.DropItemsToCell(loadedItems, pilePosition);
                }
            }
        }
        //Debug.Log("Dropped items in " + itemsOnGround.subBlocks.Count.ToString() + " piles");
    }

    private InfoBlock SaveCoordinates(GameObject GO)
    {
        InfoBlock coordinates = new InfoBlock { name = "position" };
        coordinates.namesValues.Add("x", GO.transform.position.x.ToString().Replace(',', '.'));
        coordinates.namesValues.Add("y", GO.transform.position.y.ToString().Replace(',', '.'));
        return coordinates;
    }

    private InfoBlock SaveAttributes(List<Attribute> attributeList)
    {
        InfoBlock attributes = new InfoBlock { name = "attributes" };
        foreach (Attribute a in attributeList)
        {
            InfoBlock savedAttribute = new InfoBlock { name = a.Name };
            savedAttribute.namesValues.Add("min", a.MinValue.ToString().Replace(',', '.'));
            savedAttribute.namesValues.Add("max", a.MaxValue.ToString().Replace(',', '.'));
            savedAttribute.namesValues.Add("value", a.Value.ToString().Replace(',', '.'));
            attributes.subBlocks.Add(savedAttribute);
        }
        return attributes;
    }

    private void LoadAttributes(List<Attribute> attributeList, InfoBlock attributes)
    {
        foreach (Attribute a in attributeList)
        {
            foreach (InfoBlock attributeBlock in attributes.subBlocks)
            {
                if (attributeBlock.name == a.Name)
                {
                    a.MinValue = float.Parse(attributeBlock.namesValues["min"]);
                    a.MaxValue = float.Parse(attributeBlock.namesValues["max"]);
                    a.Set(float.Parse(attributeBlock.namesValues["value"]));
                }
            }
        }
    }

    private InfoBlock SaveItem(GameObject item)
    {
        Item itemComponent = item.GetComponent<Item>();
        if (itemComponent)
        {
            InfoBlock itemBlock = new InfoBlock { name = item.name.Replace(',', '.') };
            itemBlock.subBlocks.Add(SaveAttributes(item.GetComponent<ObjectAttributes>().attributes));
            itemBlock.namesValues.Add("parent_slot", item.transform.parent.name);
            //itemBlock.namesValues.Add("weight", itemComponent.Weight.ToString());
            //itemBlock.namesValues.Add("condition", itemComponent.Condition.ToString());
            AmmoBox ammoBoxComponent = item.GetComponent<AmmoBox>();
            if (ammoBoxComponent)
            {
                InfoBlock ammoBoxBlock = new InfoBlock { name = "ammobox" };
                ammoBoxBlock.namesValues.Add("ammo", ammoBoxComponent.amount.ToString());
                ammoBoxBlock.namesValues.Add("caliber", ammoBoxComponent.bulletType.caliber.Replace(',', '.'));
                itemBlock.subBlocks.Add(ammoBoxBlock);
                return itemBlock;
            }
            Firearm firearmComponent = item.GetComponent<Firearm>();
            if (firearmComponent)
            {
                InfoBlock firearmBlock = new InfoBlock { name = "firearm" };
                if (firearmComponent.magazine != null)
                    firearmBlock.subBlocks.Add(SaveItem(firearmComponent.magazine));
                itemBlock.subBlocks.Add(firearmBlock);
                return itemBlock;
            }
            Armor armorComponent = item.GetComponent<Armor>();
            if (armorComponent)
            {
                //itemBlock.namesValues.Add("")
                InfoBlock armorBlock = new InfoBlock { name = "armor" };
                itemBlock.subBlocks.Add(armorBlock);
                return itemBlock;
            }
            Magazine magazineComponent = item.GetComponent<Magazine>();
            if (magazineComponent)
            {
                InfoBlock magazineBlock = new InfoBlock { name = "magazine" };
                magazineBlock.namesValues.Add("ammo", magazineComponent.ammo.ToString());
                if (magazineComponent.currentCaliber != null)
                    magazineBlock.namesValues.Add("caliber", magazineComponent.currentCaliber.caliber.Replace(',', '.'));
                itemBlock.subBlocks.Add(magazineBlock);
                return itemBlock;
            }
            LBEgear gearComponent = item.GetComponent<LBEgear>();
            if (gearComponent)
            {
                InfoBlock gearBlock = new InfoBlock { name = "gear" };
                int slotCount = item.transform.childCount;
                for (int i = 0; i < slotCount; i++)
                {
                    for (int j = 0; j < item.transform.GetChild(i).childCount; j++)
                    {
                        gearBlock.subBlocks.Add(SaveItem(item.transform.GetChild(i).GetChild(j).gameObject));
                    }
                }
                itemBlock.subBlocks.Add(gearBlock);
                return itemBlock;
            }
            return itemBlock;
        }
        Debug.Log("Could not save item " + item.name);
        return null;
    }

    public GameObject LoadItem(InfoBlock item)
    {
        GameObject newItem = world.CreateItem(item.name);
        LoadAttributes(newItem.GetComponent<ObjectAttributes>().attributes, item.GetBlock("attributes"));
        if (item.HasBlock("firearm"))
        {
            Destroy(newItem.GetComponent<Firearm>().UnloadMagazine());
            foreach (InfoBlock attachmentBlock in item.GetBlock("firearm").subBlocks)
            {
                if (attachmentBlock.HasBlock("magazine"))
                {
                    GameObject newMagazine = LoadItem(attachmentBlock);
                    newItem.GetComponent<Firearm>().LoadMagazine(newMagazine);
                }
            }
        }
        if (item.HasBlock("ammobox"))
        {
            newItem.GetComponent<AmmoBox>().amount = int.Parse(item.GetBlock("ammobox").namesValues["ammo"]);
        }
        if (item.HasBlock("gear"))
        {
            LBEgear newGearComponent = newItem.GetComponent<LBEgear>();
            InfoBlock gearBlock = item.GetBlock("gear");
            List<GameObject> gearSlots = newGearComponent.GetAllSlots();
            foreach (InfoBlock itemBlock in gearBlock.subBlocks)
            {
                GameObject itemInSlot = LoadItem(itemBlock);
                foreach (GameObject slot in gearSlots)
                {
                    if (slot.name == itemBlock.namesValues["parent_slot"])
                    {
                        itemInSlot.transform.SetParent(slot.transform);
                        break;
                    }
                }
            }
        }
        if (item.HasBlock("magazine"))
        {
            InfoBlock magazineBlock = item.GetBlock("magazine");
            Magazine newMagazineComponent = newItem.GetComponent<Magazine>();
            newMagazineComponent.ammo = int.Parse(magazineBlock.namesValues["ammo"]);
            if (magazineBlock.namesValues.ContainsKey("caliber"))
            {
                GameObject tempAmmo = world.CreateItem(magazineBlock.namesValues["caliber"]);
                if (tempAmmo != null)
                {
                    newMagazineComponent.currentCaliber = tempAmmo.GetComponent<AmmoBox>().bulletType;
                    Destroy(tempAmmo);
                }
                else
                    Debug.Log("huh " + magazineBlock.namesValues["caliber"]);
            }
        }
        return newItem;
    }
}