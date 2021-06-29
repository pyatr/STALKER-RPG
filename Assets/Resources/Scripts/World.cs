using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Direction
{
    SW, S, SE, W, C, E, NW, N, NE
}

public enum InGameUI
{
    Interface,
    Menu,
    Inventory,
    Console,
    Dialogue,
    CharacterMenu
}

public class World : MonoBehaviour
{
    private InGameUI uiMode;
    public InGameUI CurrentUiMode { get { return uiMode; } }
    private Transform characters;
    public Transform Characters { get { return characters; } }

    private InventoryManager inventoryManager;

    public List<Location> locations = new List<Location>();
    public List<Character> activeCharacters = new List<Character>(30);
    public Transform ground;

    public Transform UI;
    public Dictionary<InGameUI, GameObject> userInterfaceElements = new Dictionary<InGameUI, GameObject>();
    public Transform groundCursor;
    public GroundCursorMode GroundCursorMode { get { return groundCursor.GetComponent<GroundCursor>().GroundCursorMode; } set { groundCursor.GetComponent<GroundCursor>().SwitchCursorMode(value); } }

    public CharacterController characterController;
    public Log log;
    public TargetInfo targetInfo;
    public Character observedTarget;

    public int turnsPassed = 0;
    public int currentSlot = 1;

    public GameObject Player { get { return characterController.ControlledCharacter; } }

    public List<(string, Character)> tags = new List<(string, Character)>();
    public Dictionary<string, bool> worldStates = new Dictionary<string, bool>();

    private void Awake()
    {
        UI = GameObject.Find("UI").transform;
        userInterfaceElements.Add(InGameUI.Interface, UI.Find("InGameInterface").gameObject);
        userInterfaceElements.Add(InGameUI.Menu, UI.Find("InGameMainMenu").gameObject);
        userInterfaceElements.Add(InGameUI.Console, UI.Find("Console").gameObject);
        userInterfaceElements.Add(InGameUI.CharacterMenu, UI.Find("CharacterMenu").gameObject);
        userInterfaceElements.Add(InGameUI.Dialogue, UI.Find("DialogueMenu").gameObject);
        foreach (KeyValuePair<InGameUI, GameObject> UIobject in userInterfaceElements)
            if (UIobject.Value.activeSelf)
                uiMode = UIobject.Key;
        inventoryManager = GetComponent<InventoryManager>();
        characterController = GetComponent<CharacterController>();
        log = GameObject.Find("Log").GetComponent<Log>();
        targetInfo = GameObject.Find("TargetInfo").GetComponent<TargetInfo>();
        ground = transform.Find("Ground");
        groundCursor = transform.Find("GroundCursor");
        characters = transform.Find("Characters");
        InterfaceScaleChanged();
        Transform locationsTransform = transform.Find("Locations");
        for (int i = 0; i < locationsTransform.childCount; i++)
            if (locationsTransform.GetChild(i).GetComponent<Location>())
                locations.Add(locationsTransform.GetChild(i).GetComponent<Location>());
    }

    private void Start()
    {
        if (Game.Instance.GameShouldBeLoaded())
        {
            GetComponent<Save>().LoadGame(Game.Instance.loadGameOnStart);
            currentSlot = Game.Instance.loadGameOnStart;
        }
    }

    public static World GetInstance()
    {
        return GameObject.Find("World").GetComponent<World>();
    }

    public void SwitchUIMode(InGameUI gameUI)
    {
        List<InGameUI> exceptions = new List<InGameUI> { InGameUI.Inventory };
        foreach (KeyValuePair<InGameUI, GameObject> UIobject in userInterfaceElements)
            if (!exceptions.Contains(UIobject.Key))
                UIobject.Value.SetActive(false);
        groundCursor.gameObject.SetActive(false);
        inventoryManager.CloseInventory();
        Cursor.visible = false;
        uiMode = gameUI;
        if (userInterfaceElements.Keys.Contains(uiMode))
            userInterfaceElements[uiMode].SetActive(true);
        switch (gameUI)
        {
            case InGameUI.Interface:
                groundCursor.gameObject.SetActive(true);
                break;
            case InGameUI.Inventory:
                inventoryManager.OpenInventory();
                break;
            case InGameUI.Menu:
                Cursor.visible = true;
                break;
            case InGameUI.CharacterMenu:
                Cursor.visible = true;
                break;
            case InGameUI.Dialogue:
                userInterfaceElements[uiMode].GetComponent<Dialogue>().LoadFromInfoBlock(Game.Instance.Dialogues.GetBlock(characterController.dialogueName), characterController.talkingTo);
                break;
        }
        InterfaceScaleChanged();
    }

    public void InitiateTrade(MerchantStock merchantStock)
    {
        List<InGameUI> exceptions = new List<InGameUI> { InGameUI.Inventory };
        foreach (KeyValuePair<InGameUI, GameObject> UIobject in userInterfaceElements)
            if (!exceptions.Contains(UIobject.Key))
                UIobject.Value.SetActive(false);
        groundCursor.gameObject.SetActive(false);
        uiMode = InGameUI.Inventory;
        inventoryManager.merchantStock = merchantStock;
        inventoryManager.OpenInventory(true);
        InterfaceScaleChanged();
    }

    public GameObject CreateCharacter(string template, Vector2 position, bool load = true)
    {
        GameObject character = null;
        foreach (InfoBlock charTemplate in Game.Instance.CharacterTemplates.subBlocks)
        {
            if (charTemplate.name == template)
            {
                character = new GameObject(template);
                character.AddComponent<SpriteRenderer>().sortingLayerName = "Characters";
                character.AddComponent<BoxCollider2D>().size = new Vector2(Game.Instance.cellSize.x - 0.08f, Game.Instance.cellSize.y - 0.08f);
                character.transform.SetParent(characters);
                character.transform.localPosition = position;
                character.layer = 8;
                //AudioSource audioSource = character.AddComponent<AudioSource>();
                //audioSource.spatialBlend = 0;
                Character characterComponent = character.AddComponent<Character>();
                characterComponent.world = this;
                GameObject characterGUIname = new GameObject(template + "NameUI");
                characterGUIname.transform.SetParent(userInterfaceElements[InGameUI.Interface].transform.Find("CharacterNames"));
                characterGUIname.transform.localScale = Vector3.one;
                Text nameText = characterGUIname.AddComponent<Text>();
                nameText.fontSize = 8;
                nameText.font = Game.Instance.DefaultFont;
                //nameText.transform.position = Camera.main.WorldToScreenPoint((Vector2)character.transform.position + new Vector2(0, cellSize.y / 2f + 0.04f));
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.yellow;
                characterGUIname.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 16);
                characterComponent.characterNameOnGUI = nameText;
                characterGUIname.AddComponent<TextFollower>().objectToFollow = character.transform;
                characterGUIname.GetComponent<TextFollower>().offset = new Vector2(0, Game.Instance.cellSize.y / 2 + 0.04f);
                activeCharacters.Add(characterComponent);
                PathFinder pathFinder = character.AddComponent<PathFinder>();
                pathFinder.world = this;
                characterComponent.pathFinder = pathFinder;
                AI brain = character.AddComponent<AI>();
                brain.world = this;
                brain.characterComponent = characterComponent;
                brain.turnsToWaitAfterAttack = UnityEngine.Random.Range(6, 20);
                characterComponent.brain = brain;
                brain.pathFinder = pathFinder;
                string[] equipmentSlotNames = Enum.GetNames(typeof(BuiltinCharacterSlots));
                characterComponent.inventory = new GameObject("Inventory");
                characterComponent.inventory.transform.SetParent(characterComponent.transform, false);
                for (int i = 0; i < equipmentSlotNames.Length; i++)
                {
                    GameObject newSlot = new GameObject(equipmentSlotNames[i]);
                    newSlot.transform.SetParent(characterComponent.inventory.transform);
                    ItemSlot itemSlot = newSlot.AddComponent<ItemSlot>();
                    itemSlot.slotType = equipmentSlotNames[i].ToLower();
                    characterComponent.equipmentSlots.Add(newSlot);
                }
                string[] attributeNames = { "Level", "Health", "Stamina", "Encumbrance", "Strength", "Dexterity", "Endurance", "Perception", "Social", "Marksmanship", "Medical", "Mechanical" };
                character.AddComponent<ObjectAttributes>().LoadAttributes(Game.Instance.GetAttributeList(attributeNames));
                if (load)
                    characterComponent.LoadCharacter(template);
                characterComponent.enabled = false;
                return character;
            }
        }
        return character;
    }

    private GameObject CreateGenericItem(InfoBlock itemInfo, string spriteFolder, bool dropUnderPlayer)
    {
        GameObject item = new GameObject(itemInfo.name);
        ObjectAttributes objectAttributes = item.AddComponent<ObjectAttributes>();
        string[] names = { "Weight", "Item condition" };
        objectAttributes.LoadAttributes(Game.Instance.GetAttributeList(names));
        Item itemComponent = item.AddComponent<Item>();
        itemComponent.displayName = itemInfo.name;
        foreach (KeyValuePair<string, string> kvp in itemInfo.namesValues)
        {
            string name = kvp.Key;
            string value = kvp.Value;
            switch (name)
            {
                case "name": itemComponent.displayName = value.Replace('_', ' '); break;
                case "sprite": itemComponent.Sprite = Resources.Load<Sprite>("Graphics/Items/" + spriteFolder + "/" + value); break;
                case "price": itemComponent.basePrice = int.Parse(value); break;
                case "equippedon": itemComponent.slots.Add(value, 1); break;
                case "max_condition": objectAttributes.GetAttribute("Item condition").MaxValue = float.Parse(value); break;
                case "AP_to_move": itemComponent.actionPointsToMove = int.Parse(value); break;
                case "repairable": itemComponent.canBeRepaired = true; break;
                case "weight": objectAttributes.SetAttribute("Weight", float.Parse(value)); break;
                default: break;
            }
        }
        objectAttributes.MaximizeAttribute("Item condition");
        foreach (InfoBlock subBlock in itemInfo.subBlocks)
        {
            switch (subBlock.name)
            {
                case "item_slots":
                    foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                        itemComponent.slots.Add(kvp.Key, int.Parse(kvp.Value));
                    break;
                default: break;
            }
        }
        itemComponent.slots.Add("ground", 4);
        itemComponent.slots.Add("weapon", 1);
        //BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
        //collider.size = new Vector2(cellSizeX, cellSizeY);
        //collider.size *= 0.5f;
        if (dropUnderPlayer)
        {
            //Debug.Log("Dropped " + item.name + " under player");            
            DropItemToCell(item, GetPlayerPosition());
        }
        return item;
    }

    public GameObject CreateItem(string template, bool dropUnderPlayer = false)
    {
        foreach (InfoBlock item in Game.Instance.Items.subBlocks)
        {
            if (item.name == template)
            {
                GameObject newItem = CreateGenericItem(item, "Items", dropUnderPlayer);
                if (item.values.Contains("repairkit"))
                    newItem.AddComponent<Repairkit>();
                if (item.values.Contains("medkit"))
                    newItem.AddComponent<Medkit>();
                InfoBlock artifactBlock = item.GetBlock("artifact");
                if (artifactBlock != null)
                {
                    Artifact artifactComponent = newItem.AddComponent<Artifact>();
                    InfoBlock damageResistances = artifactBlock.GetBlock("damage_resistances");
                    if (damageResistances != null)
                    {
                        string[] damageTypes = Enum.GetNames(typeof(DamageTypes));
                        foreach (KeyValuePair<string, string> kvp in damageResistances.namesValues)
                        {
                            string key = TextTools.FirstCharToUpper(kvp.Key);
                            if (damageTypes.Contains(key))
                                artifactComponent.damageModifiers.Add((DamageTypes)Enum.Parse(typeof(DamageTypes), key), int.Parse(kvp.Value));
                        }
                    }
                    InfoBlock attributeModifiers = artifactBlock.GetBlock("attribute_modifiers");
                    if (attributeModifiers != null)                    
                        foreach (KeyValuePair<string, string> kvp in attributeModifiers.namesValues)
                            artifactComponent.attributeModifiers.Add(kvp.Key, int.Parse(kvp.Value));                    
                    if (artifactBlock.namesValues.ContainsKey("regeneration_modifier"))
                        artifactComponent.regenerationModifier = float.Parse(artifactBlock.namesValues["regeneration_modifier"]);
                    if (artifactBlock.namesValues.ContainsKey("radiation_gain"))
                        artifactComponent.radiationGain = float.Parse(artifactBlock.namesValues["radiation_gain"]);
                }
                return newItem;
            }
        }
        foreach (InfoBlock weapon in Game.Instance.Weapons.subBlocks)
        {
            if (weapon.name == template)
            {
                GameObject weaponItem = CreateGenericItem(weapon, "Weapons", dropUnderPlayer);
                weaponItem.GetComponent<Item>().canBeRepaired = true;
                Firearm firearmComponent = weaponItem.AddComponent<Firearm>();
                //foreach (string value in weapon.values)
                foreach (KeyValuePair<string, string> kvp in weapon.namesValues)
                {
                    string name = kvp.Key;
                    string value = kvp.Value;
                    switch (name)
                    {
                        case "baseWeaponType": firearmComponent.baseWeapon = kvp.Value; break;
                        case "accuracy":
                            string[] accuracy = { "Accuracy" };
                            weaponItem.GetComponent<ObjectAttributes>().LoadAttributes(Game.Instance.GetAttributeList(accuracy));
                            weaponItem.GetComponent<ObjectAttributes>().SetAttribute(accuracy[0], float.Parse(value));
                            break;
                        case "reliability": firearmComponent.reliability = int.Parse(value); break;
                        case "distance_modifier": firearmComponent.distanceModifier = int.Parse(value); break;
                        case "damage_bonus": firearmComponent.damageModifier = int.Parse(value); break;
                        case "magazine": firearmComponent.magazineType = value; break;
                        case "standart_magazine": firearmComponent.standartMagazine = value; break;
                        case "handle_difficulty": firearmComponent.handleDifficulty = int.Parse(value); break;
                        case "shoot_sound": firearmComponent.shootSound = Resources.Load<AudioClip>("Sounds/" + value); break;
                        default: break;
                    }
                }
                foreach (InfoBlock weaponSubBlock in weapon.subBlocks)
                {
                    switch (weaponSubBlock.name)
                    {
                        case "fire_modes":
                            foreach (InfoBlock fireModeSubBlock in weaponSubBlock.subBlocks)
                            {
                                switch (fireModeSubBlock.name)
                                {
                                    case "manual":
                                        firearmComponent.fireModes.Add(new Firearm.FireMode(int.Parse(fireModeSubBlock.namesValues["AP"]), WeaponFireMode.Nonautomatic, 1)); break;
                                    case "semi":
                                        firearmComponent.fireModes.Add(new Firearm.FireMode(int.Parse(fireModeSubBlock.namesValues["AP"]), WeaponFireMode.Semiautomatic, 1)); break;
                                    case "burst":
                                        firearmComponent.fireModes.Add(new Firearm.FireMode(int.Parse(fireModeSubBlock.namesValues["AP"]), WeaponFireMode.ControlledBurst, int.Parse(fireModeSubBlock.namesValues["shots"]), int.Parse(fireModeSubBlock.namesValues["recoil"]))); break;
                                    case "auto":
                                        firearmComponent.fireModes.Add(new Firearm.FireMode(int.Parse(fireModeSubBlock.namesValues["AP"]), WeaponFireMode.Automatic, int.Parse(fireModeSubBlock.namesValues["shots"]), int.Parse(fireModeSubBlock.namesValues["recoil"]))); break;
                                }
                            }
                            if (firearmComponent.fireModes.Count == 0)
                                firearmComponent.fireModes.Add(new Firearm.FireMode(4, WeaponFireMode.Semiautomatic, 1));
                            firearmComponent.SwitchFireModTo(WeaponFireMode.Automatic);
                            break;
                    }
                }
                if (firearmComponent.standartMagazine != "")
                    firearmComponent.GetComponent<Firearm>().LoadMagazine(CreateItem(firearmComponent.standartMagazine));
                //weaponItem.GetComponent<Item>().slots.Add("Backweapon", 1);
                return weaponItem;
            }
        }
        foreach (InfoBlock LBE in Game.Instance.LBEitems.subBlocks)
        {
            if (LBE.name == template)
            {
                GameObject LBEitem = CreateGenericItem(LBE, "LBE", dropUnderPlayer);
                LBEgear gearComponent = LBEitem.AddComponent<LBEgear>();
                foreach (KeyValuePair<string, string> kvp in LBE.namesValues)
                {
                    string name = kvp.Key;
                    string value = kvp.Value;
                    switch (name)
                    {
                        case "equippedon": LBEitem.GetComponent<Item>().primarySlot = value; break;
                        default: break;
                    }
                }
                if (LBEitem.GetComponent<Item>().primarySlot == "armor")// || LBEitem.GetComponent<Item>().primarySlot == "helmet")
                    LBEitem.GetComponent<Item>().canBeRepaired = true;
                foreach (InfoBlock subBlock in LBE.subBlocks)
                {
                    switch (subBlock.name)
                    {
                        case ("protection"):
                            Armor armorComponent = LBEitem.AddComponent<Armor>();
                            foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            {
                                switch (kvp.Key)
                                {
                                    case "burn": armorComponent.resistances.Add(DamageTypes.Fire, int.Parse(kvp.Value)); break;
                                    case "blunt": armorComponent.resistances.Add(DamageTypes.Blunt, int.Parse(kvp.Value)); break;
                                    case "electricity": armorComponent.resistances.Add(DamageTypes.Electricity, int.Parse(kvp.Value)); break;
                                    case "tear": armorComponent.resistances.Add(DamageTypes.Tear, int.Parse(kvp.Value)); break;
                                    case "radiation": armorComponent.resistances.Add(DamageTypes.Radiation, int.Parse(kvp.Value)); break;
                                    case "chemical": armorComponent.resistances.Add(DamageTypes.Chemical, int.Parse(kvp.Value)); break;
                                    case "explosion": armorComponent.resistances.Add(DamageTypes.Explosion, int.Parse(kvp.Value)); break;
                                    case "bullet": armorComponent.resistances.Add(DamageTypes.Bullet, int.Parse(kvp.Value)); break;
                                    case "psychic": armorComponent.resistances.Add(DamageTypes.Psychic, int.Parse(kvp.Value)); break;
                                }
                            }
                            break;
                        case ("vest_slot_provider"):
                            //gearComponent.occupiedSlots.Add("vest");
                            foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            {
                                GameObject newSlot = new GameObject(kvp.Key);
                                ItemSlot itemSlot = newSlot.AddComponent<ItemSlot>();
                                itemSlot.slotType = kvp.Value;
                                newSlot.transform.SetParent(LBEitem.transform);
                            }
                            break;
                        case ("backpack_slot_provider"):
                            //gearComponent.occupiedSlots.Add("backpack");
                            foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            {
                                GameObject newSlot = new GameObject(kvp.Key);
                                ItemSlot itemSlot = newSlot.AddComponent<ItemSlot>();
                                itemSlot.slotType = kvp.Value;
                                newSlot.transform.SetParent(LBEitem.transform);
                            }
                            break;
                        case ("secondary_slots"):
                            LBEitem.GetComponent<Item>().secondarySlots.AddRange(subBlock.values);
                            break;
                    }
                }
                return LBEitem;
            }
        }
        foreach (InfoBlock magazine in Game.Instance.Magazines.subBlocks)
        {
            if (magazine.name == template)
            {
                GameObject magazineObject = CreateGenericItem(magazine, "Magazines", dropUnderPlayer);
                Magazine magazineComponent = magazineObject.AddComponent<Magazine>();
                magazineComponent.builtin = magazine.values.Contains("integral");
                foreach (KeyValuePair<string, string> kvp in magazine.namesValues)
                {
                    string name = kvp.Key;
                    string value = kvp.Value;
                    switch (name)
                    {
                        case "caliber": magazineComponent.caliber = value; break;
                        case "capacity": magazineComponent.maxammo = int.Parse(value); break;
                        case "category": magazineComponent.category = value; break;
                    }
                }
                return magazineObject;
            }
        }
        foreach (InfoBlock meleeWeapon in Game.Instance.MeleeWeapons.subBlocks)
        {
            if (meleeWeapon.name == template)
            {
                GameObject meleeObject = CreateGenericItem(meleeWeapon, "Weapons", dropUnderPlayer);
                MeleeWeapon meleeWeaponComponent = meleeObject.AddComponent<MeleeWeapon>();
                foreach (KeyValuePair<string, string> kvp in meleeWeapon.namesValues)
                {
                    string name = kvp.Key;
                    string value = kvp.Value;
                    switch (name)
                    {
                        case "AP_to_attack": meleeWeaponComponent.actionPointsToAttack = int.Parse(value); break;
                        case "handle_difficulty": meleeWeaponComponent.handleDifficulty = int.Parse(value); break;
                        case "damage": meleeWeaponComponent.damage = int.Parse(value); break;
                    }
                }
                return meleeObject;
            }
        }
        foreach (InfoBlock caliber in Game.Instance.Calibers.subBlocks)
        {
            if (caliber.name == template)
            {
                GameObject ammoBox = CreateGenericItem(caliber, "Bullets", dropUnderPlayer);
                AmmoBox ammoBoxComponent = ammoBox.AddComponent<AmmoBox>();
                ammoBoxComponent.bulletType = new Bullet();
                ammoBox.GetComponent<ObjectAttributes>().SetAttribute("Weight", 0.1f);
                foreach (KeyValuePair<string, string> kvp in caliber.namesValues)
                {
                    string name = kvp.Key;
                    string value = kvp.Value;
                    switch (name)
                    {
                        case "price": ammoBoxComponent.bulletType.price = int.Parse(value); break;
                        case "weight": ammoBoxComponent.bulletType.weight = float.Parse(value); break;
                        case "caliber": ammoBoxComponent.bulletType.caliber = template; break;
                        case "penetration": ammoBoxComponent.bulletType.penetration = float.Parse(value); break;
                        case "tumble": ammoBoxComponent.bulletType.tumble = float.Parse(value); break;
                        case "damage": ammoBoxComponent.bulletType.damage = int.Parse(value); break;
                        case "bullets": ammoBoxComponent.bulletType.bulletsPerShot = int.Parse(value); break;
                        case "distance": ammoBoxComponent.bulletType.distance = int.Parse(value); break;
                        case "box": ammoBoxComponent.bulletType.box = int.Parse(value); ammoBoxComponent.amount = ammoBoxComponent.bulletType.box; break;
                        case "tracer": ammoBoxComponent.bulletType.tracer = bool.Parse(value); break;
                    }
                }
                ammoBoxComponent.bulletType.textColor = caliber.GetColor();
                return ammoBox;
            }
        }
        //UpdateLog("Object template " + template + " not found");
        Debug.Log("Object template " + template + " not found");
        return null;// new GameObject("Nonexistent or forgot to return");
    }

    public void DropItemToCell(GameObject item, Vector2 cellCoordinates)
    {
        if (item.GetComponent<Item>())
        {
            List<GameObject> oneItem = new List<GameObject> { item };
            DropItemsToCell(oneItem, cellCoordinates);
        }
    }

    public void DropItemsToCell(List<GameObject> items, Vector2 cellCoordinates)
    {
        List<GameObject> allItems = new List<GameObject>();
        allItems.AddRange(items);
        GameObject pile = GetItemPile(cellCoordinates);
        if (pile == null)
            pile = CreateItemPile(cellCoordinates);
        ItemPile pileComponent = pile.GetComponent<ItemPile>();
        foreach (GameObject item in allItems)
        {
            pileComponent.MakeSlotsIfNecessary();
            bool itemPlaced = false;
            for (int j = 0; j < pile.transform.childCount && !itemPlaced; j++)
                if (pile.transform.GetChild(j).GetComponent<ItemSlot>().PlaceItem(item))
                    itemPlaced = true;
            if (!itemPlaced)
                Debug.Log("could not place " + item.name + " at " + (Vector2)pile.transform.localPosition);
        }
        pileComponent.SetImageToItemCount();
    }

    public GameObject CreateItemPile(Vector2 position)
    {
        GameObject pile = new GameObject("Item pile");
        pile.transform.localPosition = position;
        pile.transform.SetParent(ground, false);
        ItemPile pileComponent = pile.AddComponent<ItemPile>();
        pileComponent.MakeSlotsIfNecessary();
        SpriteRenderer sprite = pile.AddComponent<SpriteRenderer>();
        sprite.sortingLayerName = "Ground objects";
        return pile;
    }

    public GameObject GetItemPile(Vector2 position)
    {
        for (int i = 0; i < ground.childCount; i++)
        {
            Transform currentObject = ground.GetChild(i);
            if (currentObject.GetComponent<ItemPile>() && (Vector2)currentObject.localPosition == position)
                return currentObject.gameObject;
        }
        //Debug.Log("no item pile found at " + position);
        return null;
    }

    public void UpdateLog(string s) { log.UpdateLog(s); }

    public Vector2 GetPlayerPosition(bool local = true)
    {
        if (characterController != null)
            if (!local)
                return Player.transform.position;
            else
                return Player.transform.localPosition;
        return Vector2.zero;
    }

    public void GiveTurnToNextCharacter(Character finishedTurn = null)
    {
        tags.RemoveAll(item => item.Item2 == null);        
        Character currentActiveCharacter = null;
        if (finishedTurn == null)
        {
            turnsPassed++;
            currentActiveCharacter = activeCharacters.First();
        }
        else
        {
            bool nextCharacterIsActive = false;
            foreach (Character currentCharacter in activeCharacters)
            {
                currentCharacter.enabled = false;
                if (nextCharacterIsActive)
                {
                    currentActiveCharacter = currentCharacter;
                    nextCharacterIsActive = false;
                }
                if (currentCharacter == finishedTurn)
                    nextCharacterIsActive = true;
            }
        }
        if (currentActiveCharacter == null)
            GiveTurnToNextCharacter();
        else
            currentActiveCharacter.StartTurn();
    }

    bool introPhraseSaid = false;

    private void InterfaceScaleChanged()
    {
        for (int i = 0; i < userInterfaceElements[InGameUI.Interface].transform.childCount; i++)
            if (userInterfaceElements[InGameUI.Interface].transform.GetChild(i).tag != "UI_noscaling")
                userInterfaceElements[InGameUI.Interface].transform.GetChild(i).localScale = new Vector3(Game.Instance.UIscale, Game.Instance.UIscale, Game.Instance.UIscale);
        for (int i = 0; i < UI.transform.childCount; i++)
            if (UI.transform.GetChild(i).tag != "UI_noscaling")
                UI.transform.GetChild(i).localScale = new Vector3(Game.Instance.UIscale, Game.Instance.UIscale, Game.Instance.UIscale);
    }

    void Update()
    {
        if (Player == null)
        {
            if (!Game.Instance.GameShouldBeLoaded())
            {
                Transform player = characters.Find("player");
                if (player != null)                
                    characterController.SetCharacterControl(player.gameObject);                
            }
        }
        if (turnsPassed == 1 && !introPhraseSaid)
        {
            foreach (Character character in activeCharacters)
            {
                if (character.displayName == "Volk")
                {
                    character.Say("Готовность 15 минут. Всем проверить снаряжение.");
                    introPhraseSaid = true;
                    break;
                }
            }
        }
        bool activeCharacterPresent = false;
        foreach (Character currentCharacter in activeCharacters)
        {
            if (currentCharacter.enabled)
            {
                activeCharacterPresent = true;
                break;
            }
        }
        if (!activeCharacterPresent)
            if (activeCharacters.Count > 0)
                GiveTurnToNextCharacter();
        if (Input.GetKeyUp(KeyCode.KeypadMultiply))
        {
            Game.Instance.UIscale = (float)Math.Round(Mathf.Clamp(Game.Instance.UIscale + 0.1f, 0, 2f), 1);
            InterfaceScaleChanged();
        }
        if (Input.GetKeyUp(KeyCode.KeypadDivide))
        {
            Game.Instance.UIscale = (float)Math.Round(Mathf.Clamp(Game.Instance.UIscale - 0.1f, 0, 2f), 1);
            InterfaceScaleChanged();
        }
        if (Player != null)
        {
            targetInfo.SetTargetDisplay(observedTarget);
        }
        else
        {
            targetInfo.SetTargetDisplay(null);
            Cursor.visible = true;
            groundCursor.gameObject.SetActive(false);
        }
    }
}