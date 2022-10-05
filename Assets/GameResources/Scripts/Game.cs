using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game
{
    public class IconsStorage
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

        public IconsStorage()
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

    public readonly int[] nonWalkableLayers = { 8, 9, 10, 11, 13, 15 };
    public readonly int[] shootableLayers = { 8, 9, 11 };
    public readonly int[] sightObstructionLayers = { 9, 12 };

    private static Game instance;

    public readonly Vector2 cellSize = new Vector2(0.40f, 0.56f);
    private readonly Vector2 minDistance = new Vector2(0.01f, 0.01f);
    public readonly int maxGameSlots = 6;

    public float SoundVolume { get { return PlayerPrefs.GetFloat("SoundVolume", 1f); } }
    public float MusicVolume { get { return PlayerPrefs.GetFloat("MusicVolume", 1f); } }
    public float UIscale { set { PlayerPrefs.SetFloat("UIScale", value); } get { return PlayerPrefs.GetFloat("UIScale", 0.5f); } }

    private InfoBlock weapons;
    public InfoBlock Weapons { get { return weapons; } }
    private InfoBlock meleeWeapons;
    public InfoBlock MeleeWeapons { get { return meleeWeapons; } }
    private InfoBlock magazines;
    public InfoBlock Magazines { get { return magazines; } }
    private InfoBlock slots;
    public InfoBlock Slots { get { return slots; } }
    private InfoBlock _LBEitems;
    public InfoBlock LBEitems { get { return _LBEitems; } }
    private InfoBlock calibers;
    public InfoBlock Calibers { get { return calibers; } }
    private InfoBlock items;
    public InfoBlock Items { get { return items; } }
    private InfoBlock characterTemplates;
    public InfoBlock CharacterTemplates { get { return characterTemplates; } }
    private InfoBlock factions;
    public InfoBlock Factions { get { return factions; } }
    private InfoBlock dialogues;
    public InfoBlock Dialogues { get { return dialogues; } }
    private InfoBlock merchantStocks;
    public InfoBlock MerchantStocks { get { return merchantStocks; } }

    private List<Attribute> _attributes = new List<Attribute>();
    public List<Attribute> Attributes { get { return _attributes; } }

    private IconsStorage _icons = new IconsStorage();
    public IconsStorage Icons { get { return _icons; } }
        
    private Font defaultFont;
    public Font DefaultFont { get { return defaultFont; } }

    public int loadGameOnStart = -1;
    public bool AIenabled = true;

    private Game()
    {
        Physics2D.IgnoreLayerCollision(10, 14, true);
        Physics2D.IgnoreLayerCollision(12, 14, true);
        Physics2D.IgnoreLayerCollision(13, 14, true);
        Physics2D.IgnoreLayerCollision(14, 14, true);
        Physics2D.IgnoreLayerCollision(14, 15, true);
        LoadAttributes();
        LoadWeapons();
        defaultFont = Resources.Load<Font>("Fonts/FiraSansCondensed-Regular");
        magazines = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/magazines").text);
        slots = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/slots").text);
        _LBEitems = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/LBE").text);
        LoadCalibers();
        items = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/items").text);
        characterTemplates = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/characters").text);
        factions = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/factions").text);
        dialogues = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/dialogues").text);
        merchantStocks = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/merchant_stocks").text);
        meleeWeapons = InfoBlockReader.SplitTextIntoInfoBlocks(Resources.Load<TextAsset>("Definitions/melee_weapons").text);
        ReloadDerivativeFactions();
    }

    private void ReloadDerivativeFactions()
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

    private void LoadWeapons()
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

    private void LoadCalibers()
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

    private void LoadAttributes()
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
                Attribute.AttributeGrade newGrade = new Attribute.AttributeGrade
                {
                    name = attr.Name,
                    gradesAndNames = new Dictionary<float, string>()
                };
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
            Attributes.Add(attr);
        }
        //Debug.Log("Attributes loaded: " + Attributes.Count);
    }

    public List<Attribute> GetAttributeList(string[] names)
    {
        List<Attribute> newList = new List<Attribute>();
        foreach (string an in names)
            foreach (Attribute a in Attributes)
                if (an == a.Name)
                    newList.Add(a);
        //Debug.Log(newList.Count);
        return newList;
    }

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

    public float DistanceFromToInCells(Vector2 from, Vector2 to)
    {
        return (float)Math.Round(Vector2.Distance(Vector2.zero, (to - from) / Instance.cellSize), 2);
    }
    
    public Vector2Int ObjectPositionToCell(Transform localTransform)
    {
        Vector2 tempPosition = localTransform.position / Instance.cellSize;
        Vector2Int newPosition = new Vector2Int((int)tempPosition.x, (int)tempPosition.y);
        return newPosition;
    }

    public void ListObjectsInRaycast(RaycastHit2D[] raycast)
    {
        string objects = "";
        for (int i = 0; i < raycast.Length; i++)
            objects += raycast[i].collider.gameObject.name + ", ";
        if (objects.Length > 2)
            Debug.Log(raycast.Length.ToString() + " objects in raycast where first hit object was at " + raycast[0].transform.position + ": " + objects.Substring(0, objects.Length - 2));
    }

    public bool GameShouldBeLoaded()
    {
        return Instance.loadGameOnStart >= 1 && Instance.loadGameOnStart <= maxGameSlots;
    }

    public static Game Instance
    {
        get
        {
            if (instance == null)
                instance = new Game();
            return instance;
        }
    }
}