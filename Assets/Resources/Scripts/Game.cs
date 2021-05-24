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

public static class StringExtensions
{
    public static string FirstCharToUpper(string input)
    {
        return input.First().ToString().ToUpper() + input.Substring(1);
    }
}

public enum InGameUI
{
    Interface,
    Menu,
    Inventory,
    Console
}

public class Game : MonoBehaviour
{
    public class Icons
    {
        Sprite _ammoIcon;
        public Sprite ammoIcon { get { return _ammoIcon; } }
        Sprite _nomagazineIcon;
        public Sprite nomagazineIcon { get { return _nomagazineIcon; } }
        Sprite _ironsightIcon;
        public Sprite ironsightIcon { get { return _ironsightIcon; } }
        Sprite _reflexsightIcon;
        public Sprite reflexsightIcon { get { return _reflexsightIcon; } }
        Sprite _scopeIcon;
        public Sprite scopeIcon { get { return _scopeIcon; } }
        Sprite _pocketIcon;
        public Sprite pocketIcon { get { return _pocketIcon; } }
        Sprite _conditionIcon;
        public Sprite conditionIcon { get { return _conditionIcon; } }
        Sprite _ammoBoxIcon;
        public Sprite ammoBoxIcon { get { return _ammoBoxIcon; } }
        Sprite _ammoTipIcon;
        public Sprite ammoTipIcon { get { return _ammoTipIcon; } }

        public Icons()
        {
            _ammoIcon = Resources.Load<Sprite>("Graphics/icons/ammoicon") as Sprite;
            _nomagazineIcon = Resources.Load<Sprite>("Graphics/icons/nomagazineicon") as Sprite;
            _ironsightIcon = Resources.Load<Sprite>("Graphics/icons/crosshairicon") as Sprite;
            _reflexsightIcon = Resources.Load<Sprite>("Graphics/icons/reflexicon") as Sprite;
            _scopeIcon = Resources.Load<Sprite>("Graphics/icons/scopeicon") as Sprite;
            _pocketIcon = Resources.Load<Sprite>("Graphics/icons/occupiedpocketsicon") as Sprite;
            _conditionIcon = Resources.Load<Sprite>("Graphics/icons/conditionicon") as Sprite;
            _ammoBoxIcon = Resources.Load<Sprite>("Graphics/icons/ammoboxicon") as Sprite;
            _ammoTipIcon = Resources.Load<Sprite>("Graphics/icons/ammotipicon") as Sprite;
        }
    }

    public List<Attribute> attributes = new List<Attribute>();
    public List<Location> locations = new List<Location>();
    public readonly int[] nonWalkableLayers = { 8, 9, 10, 11, 13 };
    public readonly int[] shootableLayers = { 8, 9, 11 };
    public readonly int[] sightObstructionLayers = { 9, 12 };

    public InfoBlock weapons;
    public InfoBlock magazines;
    public InfoBlock slots;
    public InfoBlock LBEitems;
    public InfoBlock calibers;
    public InfoBlock items;
    public InfoBlock characterTemplates;
    public InfoBlock factions;

    public List<Character> activeCharacters = new List<Character>(30);
    Transform characters;
    public GameObject ground;

    public GameObject UI;
    public GameObject menuObject;
    public GameObject gameInterfaceObject;
    public GameObject consoleObject;
    private InGameUI uiMode;

    public InventoryManager inventoryManager;
    public CharacterController characterController;
    public Character observedTarget;

    public Log log;
    public TargetInfo targetInfo;
    public Console console;

    public Transform groundCursor;

    public Font defaultFont;
    public Icons icons;

    public readonly Vector2 cellSize = new Vector2(0.40f, 0.56f);
    private readonly Vector2 minDistance = new Vector2(0.01f, 0.01f);
    public readonly float accuracyModifier = 10;
    public float SoundVolume { get { return PlayerPrefs.GetFloat("SoundVolume", 1f); } }
    public float MusicVolume { get { return PlayerPrefs.GetFloat("MusicVolume", 1f); } }
    public float UIscale { set { PlayerPrefs.SetFloat("UIScale", value); } get { return PlayerPrefs.GetFloat("UIScale", 0.5f); } }
    public int turnsPassed = 0;

    public GameObject player { get { return characterController.ControlledCharacter; } }
    public InGameUI CurrentUiMode { get { return uiMode; } }
    public GroundCursorMode GroundCursorMode { get { return groundCursor.GetComponent<GroundCursor>().GroundCursorMode; } set { groundCursor.GetComponent<GroundCursor>().SwitchCursorMode(value); } }

    public void Awake()
    {
        Physics2D.IgnoreLayerCollision(10, 14, true);
        Physics2D.IgnoreLayerCollision(12, 14, true);
        Physics2D.IgnoreLayerCollision(13, 14, true);
        Physics2D.IgnoreLayerCollision(14, 14, true);
        icons = new Icons();
        console.Start();
        inventoryManager = GetComponent<InventoryManager>();
        characterController = GetComponent<CharacterController>();
        LoadAttributes();
        LoadWeapons();
        magazines = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/magazines").text);
        slots = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/slots").text);
        LBEitems = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/LBE").text);
        LoadCalibers();
        items = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/items").text);
        characterTemplates = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/characters").text);
        factions = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/factions").text);
        ReloadDerivativeFactions();
        characters = transform.Find("Characters");
        Transform spawners = transform.Find("Spawners");
        for (int i = 0; i < spawners.childCount; i++)
        {
            ObjectSpawner spawnerComponent = spawners.GetChild(i).GetComponent<ObjectSpawner>();
            if (spawnerComponent && spawners.GetChild(i).gameObject.activeSelf)
            {
                switch (spawnerComponent.objectType)
                {
                    case SpawnerObjectTypes.Item:
                        foreach (string s in spawnerComponent.templates)
                            DropItemToCell(CreateItem(s), spawnerComponent.transform.localPosition);
                        break;
                    case SpawnerObjectTypes.Character: CreateCharacter(spawnerComponent.templates[0], spawnerComponent.transform.localPosition); break;
                    case SpawnerObjectTypes.Decoration: break;
                }
            }
        }
        Destroy(spawners.gameObject);
        Transform locationsTransform = transform.Find("Locations");
        for (int i = 0; i < locationsTransform.childCount; i++)
            if (locationsTransform.GetChild(i).GetComponent<Location>())
                locations.Add(locationsTransform.GetChild(i).GetComponent<Location>());
        characterController.game = this;
        characterController.SetCharacterControl(characters.Find("player").gameObject);
        //character.GetComponent<Character>().GetAttribute("Health").Modify(-40f);
        //foreach (InfoBlock weapon in weapons.subBlocks)
        //    CreateItem(weapon.name, true);
        //foreach (InfoBlock LBE in LBEitems.subBlocks)
        //    CreateItem(LBE.name, true);
        //foreach (InfoBlock caliber in calibers.subBlocks)
        //    for (int i = 0; i < 4; i++)
        //        CreateItem(caliber.name, true);
        //CreateItem("medkit", true);
        //CreateItem("medkit", true);
        //CreateItem("repairkit", true);
        //CreateItem("repairkit", true);
    }

    private void Start()
    {
        GiveTurnToNextCharacter();
    }

    public void SwitchUIMode(InGameUI gameUI)
    {
        menuObject.GetComponent<InGameMainMenu>().SwitchToMenu();
        menuObject.SetActive(false);
        gameInterfaceObject.SetActive(false);
        console.Close();
        inventoryManager.CloseInventory();
        groundCursor.gameObject.SetActive(false);
        Cursor.visible = false;
        uiMode = gameUI;
        switch (gameUI)
        {
            case InGameUI.Console:
                console.Open();
                break;
            case InGameUI.Interface:
                gameInterfaceObject.SetActive(true);
                groundCursor.gameObject.SetActive(true);
                break;
            case InGameUI.Inventory:
                inventoryManager.OpenInventory();
                break;
            case InGameUI.Menu:
                menuObject.SetActive(true);
                Cursor.visible = true;
                break;
        }
    }

    public void SetGroundCursorSprite(Sprite sprite)
    {
        if (sprite != null)
        {
            groundCursor.GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    public void ReloadDerivativeFactions()
    {
        foreach (InfoBlock faction in factions.subBlocks)
        {
            if (faction.namesValues.ContainsKey("basedon"))
            {
                foreach (InfoBlock factionToBaseOn in factions.subBlocks)
                {
                    string originalName = faction.namesValues["name"];
                    string basedOn = faction.namesValues["basedon"];
                    if (factionToBaseOn.name == basedOn)
                    {
                        faction.subBlocks.Clear();
                        faction.Merge(factionToBaseOn);
                        foreach (InfoBlock factionToAdjustRelationsWith in factions.subBlocks)
                        {
                            InfoBlock friendlyBlock = factionToAdjustRelationsWith.GetBlock("friendlytowards");
                            if (friendlyBlock != null)
                                if (friendlyBlock.values.Contains(factionToBaseOn.name))
                                    friendlyBlock.values.Add(faction.name);
                            InfoBlock hostileBlock = factionToAdjustRelationsWith.GetBlock("hostiletowards");
                            if (hostileBlock != null)
                                if (hostileBlock.values.Contains(factionToBaseOn.name))
                                    hostileBlock.values.Add(faction.name);
                        }
                    }
                }
            }
        }
    }

    public GameObject CreateCharacter(string template, Vector2 position, bool load = true)
    {
        GameObject character = null;
        foreach (InfoBlock charTemplate in characterTemplates.subBlocks)
        {
            if (charTemplate.name == template)
            {
                character = new GameObject(template);
                character.AddComponent<SpriteRenderer>().sortingLayerName = "Characters";
                character.AddComponent<BoxCollider2D>().size = new Vector2(cellSize.x - 0.08f, cellSize.y - 0.08f);
                character.transform.SetParent(characters);
                character.transform.localPosition = position;
                character.layer = 8;
                AudioSource audioSource = character.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0;
                Character characterComponent = character.AddComponent<Character>();
                characterComponent.game = this;
                GameObject characterGUIname = new GameObject(template + "NameUI");
                characterGUIname.transform.SetParent(gameInterfaceObject.transform.Find("CharacterNames"));
                Text nameText = characterGUIname.AddComponent<Text>();
                nameText.fontSize = 8;
                nameText.font = defaultFont;
                //nameText.transform.position = Camera.main.WorldToScreenPoint((Vector2)character.transform.position + new Vector2(0, cellSize.y / 2f + 0.04f));
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.yellow;
                characterGUIname.GetComponent<RectTransform>().sizeDelta = new Vector2(48, 16);
                characterComponent.characterNameOnGUI = nameText;
                characterGUIname.AddComponent<TextFollower>().objectToFollow = character.transform;
                characterGUIname.GetComponent<TextFollower>().offset = new Vector2(0, cellSize.y / 2 + 0.04f);
                activeCharacters.Add(characterComponent);
                PathFinder pathFinder = character.AddComponent<PathFinder>();
                pathFinder.game = this;
                characterComponent.pathFinder = pathFinder;
                AI brain = character.AddComponent<AI>();
                brain.game = this;
                brain.characterComponent = characterComponent;
                characterComponent.brain = brain;
                brain.pathFinder = pathFinder;
                string[] equipmentSlotNames = Enum.GetNames(typeof(BuiltinCharacterSlots));
                int builtInSlotsCount = equipmentSlotNames.Length;
                characterComponent.inventory = new GameObject("Inventory");
                characterComponent.inventory.transform.SetParent(characterComponent.transform, false);
                for (int i = 0; i < builtInSlotsCount; i++)
                {
                    GameObject newSlot = new GameObject(equipmentSlotNames[i]);
                    newSlot.transform.SetParent(characterComponent.inventory.transform);
                    ItemSlot itemSlot = newSlot.AddComponent<ItemSlot>();
                    itemSlot.slotType = equipmentSlotNames[i].ToLower();
                    characterComponent.equipmentSlots.Add(newSlot);
                }
                string[] attributeNames = { "Level", "Health", "Stamina", "Encumbrance", "Strength", "Dexterity", "Endurance", "Perception", "Social", "Marksmanship", "Medical", "Mechanical" };
                character.AddComponent<ObjectAttributes>().LoadAttributes(GetAttributeList(attributeNames));
                if (load)
                    characterComponent.LoadCharacter(template);
                characterComponent.enabled = false;
                return character;
            }
        }
        return character;
    }

    GameObject CreateGenericItem(InfoBlock itemInfo, string spriteFolder, bool dropUnderPlayer)
    {
        GameObject item = new GameObject(itemInfo.name);
        ObjectAttributes objectAttributes = item.AddComponent<ObjectAttributes>();
        string[] names = { "Weight", "Item condition" };
        objectAttributes.LoadAttributes(GetAttributeList(names));
        Item itemComponent = item.AddComponent<Item>();
        itemComponent.displayName = itemInfo.name;
        itemComponent.game = this;
        foreach (KeyValuePair<string, string> kvp in itemInfo.namesValues)
        {
            string name = kvp.Key;
            string value = kvp.Value;
            switch (name)
            {
                case "name": itemComponent.displayName = value.Replace('_', ' '); break;
                case "sprite": itemComponent.sprite = Resources.Load<Sprite>("Graphics/Items/" + spriteFolder + "/" + value); break;
                case "price": itemComponent.basePrice = int.Parse(value); break;
                case "equippedon": itemComponent.slots.Add(value, 1); break;
                case "max_condition": objectAttributes.GetAttribute("Item condition").maxValue = float.Parse(value); break;
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
        foreach (InfoBlock item in items.subBlocks)
        {
            if (item.name == template)
            {
                GameObject newItem = CreateGenericItem(item, "Items", dropUnderPlayer);
                if (item.values.Contains("repairkit"))
                    newItem.AddComponent<Repairkit>();
                if (item.values.Contains("medkit"))
                    newItem.AddComponent<Medkit>();
                return newItem;
            }
        }
        foreach (InfoBlock weapon in weapons.subBlocks)
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
                            weaponItem.GetComponent<ObjectAttributes>().LoadAttributes(GetAttributeList(accuracy));
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
        foreach (InfoBlock LBE in LBEitems.subBlocks)
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
        foreach (InfoBlock magazine in magazines.subBlocks)
        {
            if (magazine.name == template)
            {
                GameObject magazineObject = CreateGenericItem(magazine, "Magazines", dropUnderPlayer);
                Magazine magazineComponent = magazineObject.AddComponent<Magazine>();
                if (magazine.values.Contains("integral"))
                    magazineComponent.builtin = true;
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
        foreach (InfoBlock caliber in calibers.subBlocks)
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
        UpdateLog("Object template " + template + " not found");
        return null;// new GameObject("Nonexistent or forgot to return");
    }

    void LoadWeapons()
    {
        weapons = new InfoBlock();
        InfoBlock weaponsUnfiltered = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/weapons").text);
        foreach (InfoBlock weapon in weaponsUnfiltered.subBlocks)
        {
            weapon.namesValues.Add("baseWeaponType", weapon.name);
            if (weapon.HasBlock("variations"))
            {
                InfoBlock variationsBlock = weapon.GetBlock("variations");
                foreach (InfoBlock variation in variationsBlock.subBlocks)
                {
                    InfoBlock newBlock = new InfoBlock();
                    newBlock.name = variation.name;
                    newBlock.Copy(weapon);
                    newBlock.Merge(variation);
                    foreach (InfoBlock subBlock in newBlock.subBlocks)
                    {
                        if (subBlock.name == "variations") newBlock.subBlocks.Remove(subBlock); break;
                    }
                    weapons.subBlocks.Add(newBlock);
                }
                foreach (InfoBlock subBlock in weapon.subBlocks)
                {
                    if (subBlock.name == "variations") weapon.subBlocks.Remove(subBlock); break;
                }
            }
            weapons.subBlocks.Add(weapon);
        }
        //Debug.Log("Weapons loaded: " + weapons.subBlocks.Count);
    }

    void LoadCalibers()
    {
        calibers = new InfoBlock();
        InfoBlock calibersUnfiltered = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/calibers").text);
        foreach (InfoBlock caliber in calibersUnfiltered.subBlocks)
        {
            if (caliber.HasBlock("types"))
            {
                InfoBlock variationsBlock = caliber.GetBlock("types");
                foreach (InfoBlock variation in variationsBlock.subBlocks)
                {
                    InfoBlock newBlock = new InfoBlock();
                    newBlock.name = caliber.name + variation.name;
                    newBlock.Copy(caliber);
                    foreach (InfoBlock block in newBlock.subBlocks)
                    {
                        if (block.name == "color")
                        {
                            newBlock.subBlocks.Remove(block);
                            break;
                        }
                    }
                    newBlock.Merge(variation);
                    foreach (InfoBlock subBlock in newBlock.subBlocks)
                    {
                        if (subBlock.name == "types") newBlock.subBlocks.Remove(subBlock); break;
                    }
                    calibers.subBlocks.Add(newBlock);
                }
                foreach (InfoBlock subBlock in caliber.subBlocks)
                {
                    if (subBlock.name == "types") caliber.subBlocks.Remove(subBlock); break;
                }
            }
            calibers.subBlocks.Add(caliber);
        }
    }

    void LoadAttributes()
    {
        InfoBlock attributesInfo = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/attributes").text);
        foreach (InfoBlock subBlock in attributesInfo.subBlocks)
        {
            float value = 100, minValue = 0, maxValue = 100;
            foreach (KeyValuePair<string, string> definition in subBlock.namesValues)
            {
                switch (definition.Key)
                {
                    case "min": minValue = float.Parse(definition.Value); break;
                    case "max": maxValue = float.Parse(definition.Value); break;
                    case "default": value = float.Parse(definition.Value); break;
                    default: break;
                }
            }
            Attribute attr = new Attribute(subBlock.name, value, minValue, maxValue);

            if (subBlock.HasBlock("grades"))
            {
                InfoBlock grades = subBlock.GetBlock("grades");
                Attribute.AttributeGrade newGrade = new Attribute.AttributeGrade();
                newGrade.name = attr.name;
                newGrade.gradesAndNames = new Dictionary<float, string>();
                foreach (KeyValuePair<string, string> definition in grades.namesValues)
                {
                    switch (definition.Key)
                    {
                        case "precise": newGrade.showPrecise = bool.Parse(definition.Value); break;
                        default: break;
                    }
                }
                InfoBlock levels = grades.GetBlock("levels");
                InfoBlock names = grades.GetBlock("names");
                for (int i = 0; i < levels.values.Count; i++)
                    newGrade.gradesAndNames.Add(int.Parse(levels.values[i]), names.values[i].Replace('_', ' '));
                if (grades.HasBlock("colors"))
                {
                    InfoBlock colors = grades.GetBlock("colors");
                    newGrade.LoadGradeColors(colors.values.ToArray());
                }
                attr.SetGrade(newGrade);
            }
            attributes.Add(attr);
        }
        //Debug.Log("Attributes loaded: " + attributes.Count);
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
        pile.transform.SetParent(ground.transform, false);
        ItemPile pileComponent = pile.AddComponent<ItemPile>();
        pileComponent.MakeSlotsIfNecessary();
        SpriteRenderer sprite = pile.AddComponent<SpriteRenderer>();
        sprite.sortingLayerName = "Ground objects";
        return pile;
    }

    public GameObject GetItemPile(Vector2 position)
    {
        for (int i = 0; i < ground.transform.childCount; i++)
        {
            Transform currentObject = ground.transform.GetChild(i);
            if (currentObject.GetComponent<ItemPile>() && (Vector2)currentObject.localPosition == position)
                return currentObject.gameObject;
        }
        //Debug.Log("no item pile found at " + position);
        return null;
    }

    public List<Attribute> GetAttributeList(string[] names)
    {
        List<Attribute> newList = new List<Attribute>();
        foreach (string an in names)
            foreach (Attribute a in attributes)
                if (an == a.name)
                    newList.Add(a);
        return newList;
    }

    public void UpdateLog(string s) { log.UpdateLog(s); }
    public float DistanceFromToInCells(Vector2 from, Vector2 to) { return (float)Math.Round(Vector2.Distance(Vector2.zero, (to - from) / cellSize), 2); }

    public bool PointIsOnScreen(Vector2 point)
    {
        Vector3 screenPoint = Camera.main.WorldToViewportPoint(point);
        return screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
    }

    public bool VectorsAreEqual(Vector2 first, Vector2 second)
    {
        if (Mathf.Abs(first.x - second.x) < minDistance.x && Mathf.Abs(first.y - second.y) < minDistance.y) 
            return true;
        return false;
    }

    public Vector2Int ObjectPositionToCell(Transform localTransform)
    {
        Vector2 tempPosition = localTransform.position / cellSize;
        Vector2Int newPosition = new Vector2Int((int)tempPosition.x, (int)tempPosition.y);
        return newPosition;
    }

    public void ListObjectsInRaycast(RaycastHit2D[] raycast)
    {
        string objects = "";
        for (int i = 0; i < raycast.Length; i++)
            objects += raycast[i].collider.gameObject.name + ", ";
        if (objects.Length > 2)
            UpdateLog(raycast.Length.ToString() + " objects in raycast where first hit object was at " + raycast[0].transform.position + ": " + objects.Substring(0, objects.Length - 2));
    }

    public Vector2 GetPlayerPosition(bool local = true)
    {
        if (characterController != null)
            if (!local)
                return player.transform.position;
            else
                return player.transform.localPosition;
        return Vector2.zero;
    }

    public void GiveTurnToNextCharacter(Character finishedTurn = null)
    {
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
        {
            GiveTurnToNextCharacter();
            return;
        }
        else
        {
            //if (currentActiveCharacter.waitingTurns > 1)
            //{
            //    currentActiveCharacter.RestoreActionPoints();
            //    if (currentActiveCharacter.gameObject == player)
            //        UpdateLog("Waiting for " + (currentActiveCharacter.waitingTurns - 1).ToString() + " turns");
            //    currentActiveCharacter.waitingTurns--;
            //    currentActiveCharacter.RestoreStamina(0.32f);
            //    currentActiveCharacter.EndTurn();
            //}
            //else
            //{
            //currentActiveCharacter.waitingTurns = 0;
            currentActiveCharacter.StartTurn();
            //}
        }
    }

    bool introPhraseSaid = false;

    void Update()
    {
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
            UIscale = (float)Math.Round(Mathf.Clamp(UIscale + 0.1f, 0, 2f), 1);
        if (Input.GetKeyUp(KeyCode.KeypadDivide))
            UIscale = (float)Math.Round(Mathf.Clamp(UIscale - 0.1f, 0, 2f), 1);
        for (int i = 0; i < gameInterfaceObject.transform.childCount; i++)
            if (gameInterfaceObject.transform.GetChild(i).tag != "UI_noscaling")
                gameInterfaceObject.transform.GetChild(i).localScale = new Vector3(UIscale, UIscale, UIscale);
        for (int i = 0; i < UI.transform.childCount; i++)
            if (UI.transform.GetChild(i).tag != "UI_noscaling")
                UI.transform.GetChild(i).localScale = new Vector3(UIscale, UIscale, UIscale);
        if (player != null)
        {
            if (targetInfo != null)
                targetInfo.SetTargetDisplay(observedTarget);
        }
        else
        {
            Cursor.visible = true;
        }
    }
}