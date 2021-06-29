using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BuiltinCharacterSlots
{
    Weapon,
    Vest,
    Belt,
    Backpack,
    Backweapon,
    Helmet,
    Mask,
    Armor,
    Eyes
    //ThighRig
}

public enum ActionTypes
{
    Attack,
    Move
}

public class Character : MonoBehaviour
{
    class ShootInfo
    {
        public int shotsRemaining;
        public Vector2 attackPoint;
        public Vector2 characterPosition;
        public Firearm firearmComponent;
        public Magazine magazineComponent;
        public Vector2 recoil;
        public float recoilDirection;
        public Vector2 spread;
    }

    readonly float moveSpeed = 10f;

    public List<GameObject> equipmentSlots = new List<GameObject>();
    public List<GameObject> movingBullets = new List<GameObject>();
    public List<Direction> pathDirections = new List<Direction>();
    public List<string> hostileTowards = new List<string>();
    public List<string> friendlyTowards = new List<string>();
    public List<string> tags = new List<string>();
    public World world;
    public AI brain;
    public PathFinder pathFinder;
    public Character attackTarget = null;
    public GameObject inventory = null;
    public MerchantStock merchantStock;
    public Vector2 targetPosition;
    public Vector2 moveDelta;
    public Vector2 startPosition;
    public Text characterNameOnGUI;
    public bool turnFinished = true;
    public bool performingAction = false;
    public bool invulnerable = false;
    public bool immobile = false;
    public float actionPoints = 18;
    public float regenerationModifier = 0f;
    public string faction = "";
    public string displayName = "";
    public string dialoguePackageName = "none";
    public int freeCharacterPoints = 4;
    public int money = 0;
    public int sightDistance = 20;
    public int cellsLeftToTraverse = 0;
    public int waitingTurns = 0;
    public int experience = 0;
    //public bool usingKit = false;

    public Attribute GetAttribute(string attributeName) { return GetComponent<ObjectAttributes>().GetAttribute(attributeName); }
    public Attribute.ChangeResult Heal(float amount = 0f) { return GetComponent<ObjectAttributes>().ModAttribute("Health", amount); }
    public Attribute.ChangeResult UseStamina(float amount) { return GetAttribute("Stamina").Modify(Mathf.Min(0, -amount + (GetAttribute("Endurance").Value - 14f) / 25)); }
    public Attribute.ChangeResult RestoreStamina(float amount) { return GetAttribute("Stamina").Modify(Mathf.Max(0, amount)); }
    public float Health { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Health"); } }
    public float Stamina { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Stamina"); } }
    public float MaxEncumbrance { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Strength") * 1.5f; } }
    public float MoveCost { get { return 2f; } }
    public bool IsPlayer() { return world.characterController.ControlledCharacter == gameObject; }
    public bool IsMoving() { return moveDelta != Vector2.zero; }
    public int Level { get { return (int)GetComponent<ObjectAttributes>().GetAttributeValue("Level"); } }

    public bool EquipItemAsWeapon(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Weapon); }
    public bool EquipVest(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Vest); }
    public bool EquipBelt(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Belt); }
    public bool EquipBackpack(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Backpack); }
    public bool EquipWeaponOnBack(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Backweapon); }
    public bool EquipHelmet(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Helmet); }
    public bool EquipMask(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Mask); }
    public bool EquipArmor(GameObject item) { return PlaceItemInSlot(item, BuiltinCharacterSlots.Armor); }

    public GameObject Weapon { get { return GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon); } }
    public GameObject GetSlot(BuiltinCharacterSlots slot) { return inventory.transform.GetChild((int)slot).gameObject; }

    public Location GetCurrentLocation()
    {
        foreach (Location location in world.locations)
            if (Game.Instance.DistanceFromToInCells(location.transform.position, transform.position) <= location.cellRadius)
                return location;
        return null;
    }

    public void UnloadWeapon()
    {
        Firearm firearmComponent = Weapon.GetComponent<Firearm>();
        if (firearmComponent)
        {
            GameObject magazine = firearmComponent.UnloadMagazine();
            if (magazine != null)
                TryPlaceItemInInventory(magazine);
        }
    }

    public void ReloadWeapon()
    {
        if (Weapon != null)
        {
            Firearm firearmComponent = Weapon.GetComponent<Firearm>();
            if (firearmComponent)
            {
                GameObject currentMagazine = null;
                bool magazineIsIntegral = false;
                if (firearmComponent.magazine != null)
                    currentMagazine = firearmComponent.magazine;
                if (currentMagazine != null)
                {
                    magazineIsIntegral = currentMagazine.GetComponent<Magazine>().builtin;
                    if (currentMagazine.GetComponent<Magazine>().ammo == currentMagazine.GetComponent<Magazine>().maxammo)
                    {
                        if (IsPlayer())
                            world.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " is already fully loaded");
                        return;
                    }
                }
                GameObject magazineToLoad = null;
                GameObject vest = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest);
                GameObject backpack = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backpack);
                //GameObject belt = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt);
                List<GameObject> items = new List<GameObject>();
                if (vest != null)
                    items.AddRange(vest.GetComponent<LBEgear>().GetAllItems());
                if (backpack != null && !IsInCombat())
                    items.AddRange(backpack.GetComponent<LBEgear>().GetAllItems());
                foreach (GameObject item in items)
                {
                    if (!magazineIsIntegral)
                    {
                        Magazine magazineComponent = item.GetComponent<Magazine>();
                        if (magazineComponent)
                        {
                            if (magazineComponent.category == firearmComponent.magazineType)
                            {
                                if (currentMagazine != null)
                                {
                                    if (magazineComponent.ammo > currentMagazine.GetComponent<Magazine>().ammo)
                                        currentMagazine = item;
                                }
                                else if (magazineComponent.ammo > 0)
                                    currentMagazine = magazineComponent.gameObject;
                            }
                        }
                    }
                    else
                    {
                        AmmoBox ammoBoxComponent = item.GetComponent<AmmoBox>();
                        if (ammoBoxComponent)
                        {
                            if (ammoBoxComponent.bulletType.caliber.Contains(firearmComponent.magazine.GetComponent<Magazine>().caliber))
                            {
                                if (UseActionPoints(item.GetComponent<Item>().actionPointsToMove))
                                {
                                    currentMagazine.GetComponent<Magazine>().LoadFromAmmoBox(item);
                                    if (ammoBoxComponent.amount <= 0)
                                        Destroy(item);
                                }
                                else if (IsPlayer())
                                    world.UpdateLog("Not enough action points to reload " + firearmComponent.GetComponent<Item>().displayName);
                                return;
                            }
                        }
                    }
                }
                if (currentMagazine != firearmComponent.magazine)
                    magazineToLoad = currentMagazine;
                if (magazineToLoad != null)
                {
                    float APsum = magazineToLoad.GetComponent<Item>().actionPointsToMove + currentMagazine.GetComponent<Item>().actionPointsToMove;
                    if (UseActionPoints(APsum))
                    {
                        UnloadWeapon();
                        firearmComponent.LoadMagazine(currentMagazine);
                        if (IsPlayer())
                            world.UpdateLog("Reloading " + firearmComponent.GetComponent<Item>().displayName);
                        else if (Game.Instance.PointIsOnScreen(transform.position))
                            world.UpdateLog(displayName + " reloads");
                    }
                    else if (IsPlayer())
                        world.UpdateLog("Not enough action points to reload " + firearmComponent.GetComponent<Item>().displayName);
                }
                else if (IsPlayer())
                    world.UpdateLog("No magazine found for " + firearmComponent.GetComponent<Item>().displayName);
            }
            else if (IsPlayer())
                world.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " can't be reloaded");
        }
        else if (IsPlayer())
            world.UpdateLog("You don't have a weapon");
    }

    public int GetAttackDistance()
    {
        GameObject equippedItem = Weapon;
        if (equippedItem != null)
        {
            Firearm equippedFirearm = equippedItem.GetComponent<Firearm>();
            if (equippedFirearm)
                if (equippedFirearm.magazine != null)
                    if (equippedFirearm.magazine.GetComponent<Magazine>().currentCaliber != null)
                    {
                        int distance = equippedFirearm.magazine.GetComponent<Magazine>().currentCaliber.distance + equippedFirearm.distanceModifier;
                        //Debug.Log(distance);
                        return distance;
                    }
        }
        return 0;
    }

    public bool CanShootFromTo(Vector2 from, Vector2 position)
    {
        RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(from, position - from);
        //float accuracy = Mathf.Clamp(GetAccuracyMaxSpread(), 0, 25);
        //Debug.Log(accuracy);
        //Vector2 spread = new Vector2(25 - accuracy, accuracy - 25);
        bool objectCanBeHit = false;
        for (int i = 0; i < raycastHit2D.Length; i++)
        {
            Transform objectHit = raycastHit2D[i].transform;
            if ((Vector2)objectHit.position == position)
                return true;//objectCanBeHit = true;
            //return true;
            if (Game.Instance.shootableLayers.Contains(objectHit.gameObject.layer))
                return false;
        }
        //RaycastHit2D[] raycastHit2DSpreadOne = Physics2D.RaycastAll(from, position - from + spread);
        //for (int i = 0; i < raycastHit2DSpreadOne.Length; i++)
        //{
        //    Transform objectHit = raycastHit2DSpreadOne[i].transform;
        //    if ((Vector2)objectHit.position == position)
        //        continue;
        //    if (world.shootableLayers.Contains(objectHit.gameObject.layer))
        //        return false;
        //}
        //RaycastHit2D[] raycastHit2DSpreadTwo = Physics2D.RaycastAll(from, position - from - spread);
        //for (int i = 0; i < raycastHit2DSpreadTwo.Length; i++)
        //{
        //    Transform objectHit = raycastHit2DSpreadTwo[i].transform;
        //    if ((Vector2)objectHit.position == position)
        //        continue;
        //    if (world.shootableLayers.Contains(objectHit.gameObject.layer))
        //        return false;
        //}
        return objectCanBeHit;
    }

    public float GetMaxActionPoints()
    {
        //return Mathf.Clamp(GetAttribute("Dexterity").Value * Health / GetAttribute("Health").maxValue, 5, 25);
        return Mathf.Clamp(GetAttribute("Dexterity").Value, 5, 25);
    }

    [ContextMenu("Print attributes")]
    public void ListAttributes()
    {
        ObjectAttributes attributes = GetComponent<ObjectAttributes>();
        foreach (Attribute a in attributes.attributes)
            Debug.Log(displayName + ": " + a.Name + "/" + a.MinValue + "/" + a.Value + "/" + a.MaxValue);
    }

    public void RestoreActionPoints() { actionPoints = GetMaxActionPoints(); }

    public bool CanUseActionPoints(float amount)
    {
        if (!IsInCombat())
            return true;
        else if (actionPoints >= amount)
            return true;
        return false;
    }

    public bool UseActionPoints(float amount)
    {
        if (amount <= 0f)
            amount = 0f;
        if (!IsInCombat())
        {
            RestoreActionPoints();
            return true;
        }
        else if (actionPoints >= amount)
        {
            actionPoints -= amount;
            return true;
        }
        world.GetComponent<InventoryManager>().OverrideItemViewText("Not enough AP!");
        return false;
    }

    public bool IsInCombat()
    {
        if (attackTarget != null)
            return true;
        foreach (Character character in world.activeCharacters)
        {
            if (character.attackTarget != null && IsPlayer())
                return true;
            if (character.attackTarget == this)
                return true;
        }
        return false;
    }

    public bool TakeDamage(Damage damage)
    {
        if (damage == null)
            return false;
        waitingTurns = 0;
        if (damage.GetDamageType() == DamageTypes.Bullet && damage.source != null)
        {
            Character attacker = damage.source.GetComponent<Character>();
            int chanceMod = 0;
            int damageMod = 1;
            if (attacker != null)
                chanceMod = (int)Mathf.Max(0, attacker.GetAttribute("Perception").Value - 16f);
            int[] damageChances = { 20, 40, 70, 100 };
            int[] damageModifiers = { 100, 70, 60, 40 };
            int chanceNum = UnityEngine.Random.Range(0, 100) - chanceMod * 3;
            for (int i = 0; i < damageChances.Length; i++)
                if (chanceNum >= damageChances[i])
                    damageMod = damageModifiers[i];
            damage.modifier = damageMod / 100f;            
            //damage.amount = damage.amount * damageMod / 100;
        }
        GameObject equippedArmor = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Armor);
        if (equippedArmor != null)
        {
            if (!equippedArmor.GetComponent<ItemExtension>())
            {
                Armor armorComponent = equippedArmor.GetComponent<Armor>();
                if (armorComponent)
                    armorComponent.AbsorbDamage(damage);
            }
        }
        GameObject equippedHelmet = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Helmet);
        if (equippedHelmet != null)
        {
            if (!equippedArmor.GetComponent<ItemExtension>())
            {
                Armor armorComponent = equippedHelmet.GetComponent<Armor>();
                if (armorComponent)
                    armorComponent.AbsorbDamage(damage);
            }
        }
        if (damage.GetDamageType() == DamageTypes.Bullet || damage.GetDamageType() == DamageTypes.Blunt)        
            PlaySound(Resources.Load<AudioClip>("Sounds/bullet_impact"), 1f, 0.4f);             
        if (!invulnerable)
            damage.Apply(gameObject);
        //List<GameObject> allItems = GetAllItemsInInventory();
        //foreach (GameObject possibleArtifact in allItems)
        //{
        //    Artifact artifactComponent = possibleArtifact.GetComponent<Artifact>();
        //    if (artifactComponent)
        //    {
        //
        //    }
        //}
        if (Health > 0)
        {
            if (damage.source != null)
            {
                Character attacker = damage.source.GetComponent<Character>();
                if (attacker != null)
                {
                    //if (attackTarget == null)    
                    if (!IsPlayer())
                        if (attacker.faction != faction && attacker.hostileTowards.Contains(faction) || hostileTowards.Contains(attacker.faction))
                            attackTarget = attacker;
                    brain.CallForHelp(attacker);
                }
            }
            return false;
        }
        else
        {
            string killedBy = "";
            if (damage.source != null)
            {
                Character attacker = damage.source.GetComponent<Character>();
                if (attacker != null)
                {
                    killedBy = " Killed by " + attacker.displayName + ".";
                    int levelDif = Level + 1 - attacker.Level;
                    if (levelDif < 0)
                        levelDif = 0;
                    int XPgain = 20 * levelDif;
                    attacker.AddExperience(XPgain);
                }
            }
            Die(killedBy);
            return true;
        }
    }

    public void Die(string killedBy = "")
    {
        if (world.activeCharacters.Contains(this))
        {
            DropAllItems();
            if (Game.Instance.PointIsOnScreen(transform.position))
                world.UpdateLog(displayName + " dies!" + killedBy);
            PlaySound(Resources.Load<AudioClip>("Sounds/death"));
            if (characterNameOnGUI != null)
                Destroy(characterNameOnGUI.gameObject);
            Destroy(gameObject);
        }
        world.activeCharacters.Remove(this);
    }

    public int GetXPToNextLevel(int level)
    {
        return (level * (level - 1) / 2) * 200;
    }

    public List<GameObject> GetAllItemsInInventory()
    {
        List<GameObject> items = new List<GameObject>();
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            GameObject equippedItem = GetItemFromBuiltinSlot((BuiltinCharacterSlots)i);
            if (equippedItem != null)
            {
                items.Add(equippedItem);
                LBEgear gear = equippedItem.GetComponent<LBEgear>();
                if (gear)
                    items.AddRange(gear.GetAllItems());
                Firearm firearm = equippedItem.GetComponent<Firearm>();
                if (firearm)
                    if (firearm.magazine != null)
                        items.Add(firearm.magazine);
            }
        }
        return items;
    }

    public void AddExperience(int xp)
    {
        if (xp > 0)
        {
            if (IsPlayer())
                world.UpdateLog("Recieved " + xp.ToString() + " experience points");
            int currentLevel = (int)GetAttribute("Level").Value;
            experience += xp;
            int nextLevel = currentLevel + 1;
            int nextLevelXpFormula = GetXPToNextLevel(nextLevel);
            while (nextLevelXpFormula <= experience)
            {
                nextLevel++;
                nextLevelXpFormula = GetXPToNextLevel(nextLevel);
            }
            int levelsGained = nextLevel - currentLevel - 1;
            while (levelsGained > 0)
            {
                Attribute.ChangeResult result = GetAttribute("Level").Modify(1);
                if (result == Attribute.ChangeResult.None)
                {
                    freeCharacterPoints += 3;
                    if (Game.Instance.PointIsOnScreen(transform.position))
                        world.UpdateLog(displayName + " gains a level");
                }
                levelsGained--;
            }
        }
    }

    public GameObject GetItemFromBuiltinSlot(BuiltinCharacterSlots slot)
    {
        GameObject itemObject = null;
        if (inventory.transform.childCount > (int)slot)
        {
            Transform slotTransform = inventory.transform.GetChild((int)slot);
            if (slotTransform.childCount > 0)
                itemObject = slotTransform.GetChild(0).gameObject;
        }
        return itemObject;
    }

    public void UnequipItemFromSlot(GameObject slot)
    {
        if (slot.transform.childCount > 0)
            for (int i = 0; i < slot.transform.childCount; i++)
                TryPlaceItemInInventory(slot.transform.GetChild(i).gameObject);
    }

    public bool PlaceItemInSlot(GameObject item, BuiltinCharacterSlots slot)
    {
        GameObject slotObject = GetSlot(slot);
        Item itemComponent = item.GetComponent<Item>();
        if (itemComponent)
        {
            if (itemComponent.ItemFitsInSlot(slotObject.GetComponent<ItemSlot>().slotType) > 0)
            {
                UnequipItemFromSlot(slotObject);
                return slotObject.GetComponent<ItemSlot>().PlaceItem(item);                
            }
        }
        return false;
    }

    public void DropAllItems()
    {
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            GameObject itemToDrop = GetItemFromBuiltinSlot((BuiltinCharacterSlots)i);
            DropItem(itemToDrop);
        }
    }

    public float CalculateEncumbrance()
    {
        float encumbrance = 0f;
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            GameObject item = GetItemFromBuiltinSlot((BuiltinCharacterSlots)i);
            if (item != null)
            {
                encumbrance += item.GetComponent<Item>().GetWeight();
            }
        }
        GetComponent<ObjectAttributes>().SetAttribute("Encumbrance", encumbrance);
        return encumbrance;
    }

    public void TryPlaceItemInInventory(GameObject item)
    {
        if (!PlaceItemAnywhere(item))
            DropItem(item);
    }

    public void DropItem(GameObject item)
    {
        if (item != null)
        {
            LBEgear gear = item.GetComponent<LBEgear>();
            if (gear)
            {
                List<GameObject> items = gear.GetAllItems();
                world.DropItemsToCell(items, transform.localPosition);
            }
            world.DropItemToCell(item, transform.localPosition);
        }
    }

    public bool PlaceItemAnywhere(GameObject item)
    {
        bool itemPlaced = false;
        if (item.GetComponent<Item>().GetWeight() + CalculateEncumbrance() > MaxEncumbrance)
        {
            if (IsPlayer())
                world.UpdateLog(item.GetComponent<Item>().displayName + " is too heavy.");
            return false;
        }
        if (item != null)
        {
            if (GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest) != null)
                itemPlaced = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest).GetComponent<LBEgear>().PlaceItemAnywhere(item);
            if (GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt) != null && !itemPlaced)
                itemPlaced = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt).GetComponent<LBEgear>().PlaceItemAnywhere(item);
            if (GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backpack) != null && !itemPlaced && !IsInCombat())
                itemPlaced = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backpack).GetComponent<LBEgear>().PlaceItemAnywhere(item);
            //if (GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backweapon) != null && !itemPlaced)
            //    itemPlaced = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backweapon).GetComponent<LBEgear>().PlaceItemAnywhere(item);
        }
        else
            Debug.Log("Tried to place null item to " + displayName);
        return itemPlaced;
    }

    public bool ItemIsEquipped(GameObject item)
    {
        for (int i = 0; i < inventory.transform.childCount; i++)
            if (inventory.transform.GetChild(i).childCount > 0)
                if (inventory.transform.GetChild(i).GetChild(0).gameObject == item)
                    return true;
        return false;
    }

    public List<Vector2> GetNearbyAccessibleCells()
    {
        List<Vector2> cells = new List<Vector2>();
        List<Direction> directions = Enum.GetValues(typeof(Direction)).Cast<Direction>().ToList();
        directions.Remove(Direction.C);
        foreach (Direction d in directions)
        {
            Vector2 cell = pathFinder.DirectionToNumbers(d) * Game.Instance.cellSize + (Vector2)transform.position;
            List<Direction> path = pathFinder.FindPath(cell);
            if (path.Count == 1)
                cells.Add(cell);
        }
        return cells;
    }

    public List<Direction> GetShortestPath(List<Vector2> cells)
    {
        List<List<Direction>> paths = new List<List<Direction>>();
        List<Direction> shortestPath = new List<Direction>();
        foreach (Vector2 cell in cells)
        {
            List<Direction> currentPath = pathFinder.FindPath(cell);
            if (currentPath.Count > 0)
            {
                if (shortestPath.Count == 0)
                    shortestPath = currentPath;
                else if (shortestPath.Count > currentPath.Count)
                {
                    int shortestPathCost = (int)GetPathMoveCost(shortestPath);
                    int currentPathCost = (int)GetPathMoveCost(currentPath);
                    if (shortestPathCost > currentPathCost)
                        shortestPath = currentPath;
                }
            }
        }
        return shortestPath;
    }

    public void PlaySound(AudioClip sound, float spatialBlend = 1.0f, float volumeMod = 1.0f)
    {
        GameObject soundObject = new GameObject("Sound");
        soundObject.transform.position = transform.position;
        soundObject.AddComponent<OneTimeSoundPlayer>().Play(sound, Game.Instance.SoundVolume, spatialBlend, volumeMod);
    }

    public bool ShootAtPoint(Vector2 position)
    {
        Vector2 characterPosition = transform.position;
        GameObject weapon = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
        if (weapon != null)
        {
            if (Stamina / GetAttribute("Stamina").MaxValue <= 0.1f)
            {
                if (IsPlayer())
                    world.UpdateLog("Not enough stamina to shoot");
                return false;
            }
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
            if (firearmComponent)
            {
                if (firearmComponent.GetAmmoCount() == 0)
                {
                    if (IsPlayer())
                        world.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " has no ammo!");
                    return false;
                }
                if (!UseActionPoints(firearmComponent.GetFireMode().actionPoints))
                {
                    if (IsPlayer())
                        world.UpdateLog("You don't have enough ap to fire " + firearmComponent.GetComponent<Item>().displayName);
                    return false;
                }
                //world.UpdateLog(displayName + " fires " + weapon.GetComponent<Item>().displayName);
                Magazine magazineComponent = firearmComponent.magazine.GetComponent<Magazine>();
                performingAction = true;
                int recoilDirection = UnityEngine.Random.Range(0, 2);
                if (recoilDirection == 0)
                    recoilDirection = -1;
                float finalAccuracy = GetAccuracy();
                float spread = 25 - finalAccuracy;
                Vector2 startingSpread = Vector2.zero;
                if (spread > 0)
                    startingSpread = new Vector2(UnityEngine.Random.Range(spread, -spread), UnityEngine.Random.Range(spread, -spread));
                startingSpread *= Game.Instance.cellSize;
                startingSpread /= 150;
                //Debug.Log(startingSpread);
                ShootInfo shootInfo = new ShootInfo
                {
                    attackPoint = position,
                    characterPosition = characterPosition,
                    firearmComponent = firearmComponent,
                    magazineComponent = magazineComponent,
                    spread = startingSpread,
                    recoil = Vector2.zero,
                    recoilDirection = recoilDirection,
                    shotsRemaining = firearmComponent.GetFireMode().shotsPerAttack
                };
                if (firearmComponent.GetFireMode().fireMode == WeaponFireMode.Automatic && shootInfo.shotsRemaining >= 3)
                {
                    int difference = 1;
                    for (int i = 0; i < shootInfo.shotsRemaining; i++)
                        if (i % 3 == 0)
                            difference++;
                    shootInfo.shotsRemaining += UnityEngine.Random.Range(-difference, difference + 1);
                    shootInfo.shotsRemaining = Mathf.Max(shootInfo.shotsRemaining, 3);
                }
                if (shootInfo.shotsRemaining > 0)
                {
                    StartCoroutine(Shoot(shootInfo));
                    return true;
                }
            }
        }
        return false;
    }

    public float GetAccuracy()
    {
        GameObject weapon = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
        if (weapon != null)
        {
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
            if (firearmComponent)
            {
                float weaponAccuracy = firearmComponent.Accuracy;
                //Empty weight is used
                float weightPenalty = Mathf.Clamp(firearmComponent.Weight - 6f - (GetAttribute("Strength").Value - 16f), 0, 50);
                float handleModifier = Mathf.Clamp(GetAttribute("Level").Value / 2 - 5 - firearmComponent.handleDifficulty, -10, 24);
                float skillModifier = GetAttribute("Marksmanship").Value;
                float dexterityModifier = GetAttribute("Dexterity").Value / 4;
                float perceptionModifier = GetAttribute("Perception").Value / 2;
                float finalAccuracy = weaponAccuracy - weightPenalty + handleModifier + skillModifier + dexterityModifier + perceptionModifier - 16;
                //Debug.Log(finalAccuracy);
                return finalAccuracy;
            }
        }
        return 0f;
    }

    IEnumerator Shoot(ShootInfo info)
    {
        float delay = 0.04f * 10 / info.firearmComponent.GetFireMode().shotsPerAttack;
        float condition = info.firearmComponent.Condition / info.firearmComponent.GetComponent<Item>().GetMaxCondition() * 25;
        if (condition < 25f)
        {
            if (UnityEngine.Random.Range(0, 25f - condition) > info.firearmComponent.reliability)
            {
                info.shotsRemaining = 0;
                info.firearmComponent.jammed = true;
                if (IsPlayer())
                    world.UpdateLog(info.firearmComponent.GetComponent<Item>().displayName + " is jammed!");
            }
        }
        if (!info.firearmComponent.jammed)
        {
            if (info.firearmComponent.GetFireMode().shotsPerAttack == 1)
                delay = 0.05f;
            yield return new WaitForSeconds(delay);
            info.firearmComponent.SpendAmmo();
            if (info.firearmComponent.GetFireMode().shotsPerAttack > 1)
            {
                if (Mathf.Clamp(GetAttribute("Level").Value * 2, 5, 24) + UnityEngine.Random.Range(0, 6) < 15 + info.firearmComponent.handleDifficulty)
                {
                    float recoilModifier = info.firearmComponent.GetFireMode().recoil / 400f + UnityEngine.Random.Range(-0.005f, 0.005f);
                    info.recoil += new Vector2(recoilModifier, recoilModifier);
                    //Debug.Log(info.recoil);
                }
                else
                {
                    info.recoil = Vector2.zero;
                    //if (IsPlayer())
                    //    world.UpdateLog("You take control of recoil.");
                }
            }
            Vector2 randomSpread = Vector2.zero;

            if (Game.Instance.DistanceFromToInCells(transform.position, Camera.main.transform.position) > info.magazineComponent.currentCaliber.distance)
            {
                PlaySound(Resources.Load<AudioClip>("Sounds/echo" + UnityEngine.Random.Range(1, 5).ToString()), 0.9f, 0.7f / info.shotsRemaining);
                PlaySound(info.firearmComponent.shootSound, 0.3f, 0.1f);
            }
            else
            {
                PlaySound(info.firearmComponent.shootSound, Mathf.Clamp(1f - info.magazineComponent.currentCaliber.damage / 100, 0f, 0.9f));
            }
            UseStamina(UnityEngine.Random.Range(0.3f, 0.4f));
            bool shotOutsideCamera = !Game.Instance.PointIsOnScreen(info.attackPoint) && !Game.Instance.PointIsOnScreen(info.characterPosition);
            for (int i = 0; i < info.magazineComponent.currentCaliber.bulletsPerShot; i++)
            {
                if (i > 0)
                {
                    float pelletSpread = 25 - info.firearmComponent.Accuracy * 2;
                    randomSpread = new Vector2(UnityEngine.Random.Range(Mathf.Clamp(pelletSpread, 0, 25), Mathf.Clamp(-pelletSpread, 0, -25)),
                                               UnityEngine.Random.Range(Mathf.Clamp(pelletSpread, 0, 25), Mathf.Clamp(-pelletSpread, 0, -25)));
                }
                Vector2 spread = info.spread + info.recoil * new Vector2(info.spread.x < 0 ? -1 : 1, info.spread.y < 0 ? -1 : 1);
                Vector3 finalDirection = Vector3.zero;
                Vector3 newDirection = info.attackPoint - info.characterPosition;
                float distance = Vector2.Distance(info.attackPoint, info.characterPosition);
                float staminaModifier = 4 / GetAttribute("Stamina").GetPercentage01();
                finalDirection = newDirection;
                finalDirection += (Vector3)spread * distance * staminaModifier * (info.firearmComponent.GetConditionPercentage01() / 2);
                finalDirection += (Vector3)randomSpread * distance / 50 / staminaModifier;
                GameObject bullet = new GameObject("Bullet") { layer = 14 };
                Transform bulletTransform = bullet.transform;
                bulletTransform.SetParent(world.transform);
                bulletTransform.localScale = Vector3.one * 0.5f;
                bulletTransform.up = finalDirection;
                bulletTransform.position = info.characterPosition;
                SpriteRenderer bulletSpriteRenderer = bullet.AddComponent<SpriteRenderer>();
                bulletSpriteRenderer.sprite = Resources.Load<Sprite>("Graphics/bullet");
                bulletSpriteRenderer.sortingLayerName = "Ground objects";
                BoxCollider2D bulletCollider = bullet.AddComponent<BoxCollider2D>();
                bulletCollider.size = new Vector2(0.03f, 0.08f);
                Physics2D.IgnoreCollision(GetComponent<BoxCollider2D>(), bulletCollider);
                Rigidbody2D bulletRigidBody = bullet.AddComponent<Rigidbody2D>();
                bulletRigidBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                bulletRigidBody.velocity = bulletTransform.up * 16;
                bulletRigidBody.freezeRotation = true;
                FiredBullet firedBulletComponent = bullet.AddComponent<FiredBullet>();
                firedBulletComponent.bulletDamage = new BulletDamage(info.magazineComponent.currentCaliber, info.firearmComponent.damageModifier) { source = gameObject };
                firedBulletComponent.shooter = gameObject;
                firedBulletComponent.startPoint = bulletTransform.position;
                firedBulletComponent.targetPoint = info.attackPoint;
                firedBulletComponent.maxDistance = info.magazineComponent.currentCaliber.distance + info.firearmComponent.distanceModifier;
                movingBullets.Add(bullet);
            }
            info.shotsRemaining--;
            info.firearmComponent.GetComponent<ObjectAttributes>().GetAttribute("Item condition").Modify(UnityEngine.Random.Range(-0.5f, -0.75f));
            if (info.shotsRemaining > 0 && info.firearmComponent.GetAmmoCount() > 0)
            {
                StartCoroutine(Shoot(info));
            }
            else
            {
                //if (info.firearmComponent.GetAmmoCount() == 0)
                //    if (info.firearmComponent.magazine != null)
                //        info.firearmComponent.magazine.GetComponent<Magazine>().currentCaliber = null;
                CalculateEncumbrance();
            }
        }
    }

    public bool EnoughActionPointsToPerformAction(ActionTypes action)
    {
        switch (action)
        {
            case ActionTypes.Attack:
                if (Weapon != null)
                    if (Weapon.GetComponent<Firearm>() != null)
                        if (actionPoints >= Weapon.GetComponent<Firearm>().GetFireMode().actionPoints)
                            return true;
                break;
            case ActionTypes.Move:
                return actionPoints >= MoveCost * 1.43f;
        }
        return false;
    }

    public void LoadFaction(string factionName)
    {
        faction = factionName;
        friendlyTowards.Clear();
        hostileTowards.Clear();
        InfoBlock factionBlock = Game.Instance.Factions.GetBlock(factionName);
        if (factionBlock != null)
        {
            InfoBlock friendlyBlock = factionBlock.GetBlock("friendlytowards");
            if (friendlyBlock != null)
                friendlyTowards.AddRange(friendlyBlock.values);
            InfoBlock hostileBlock = factionBlock.GetBlock("hostiletowards");
            if (hostileBlock != null)
                hostileTowards.AddRange(hostileBlock.values);
        }
    }

    public void LoadCharacter(string templateName)
    {
        world = World.GetInstance();
        InfoBlock template = null;
        foreach (InfoBlock characterTemplate in Game.Instance.CharacterTemplates.subBlocks)
        {
            if (characterTemplate.name == templateName)
            {
                template = characterTemplate;
                break;
            }
        }
        if (template != null)
        {
            string[] equipmentSlotNames = Enum.GetNames(typeof(BuiltinCharacterSlots));
            int builtInSlotsCount = equipmentSlotNames.Length;
            invulnerable = template.values.Contains("invulnerable");
            immobile = template.values.Contains("immobile");
            ObjectAttributes objectAttributes = GetComponent<ObjectAttributes>();
            foreach (KeyValuePair<string, string> kvp in template.namesValues)
            {
                switch (kvp.Key)
                {
                    case "faction":
                        LoadFaction(kvp.Value);
                        break;
                    case "name":
                        displayName = kvp.Value.Replace('_', ' ');
                        characterNameOnGUI.text = displayName;
                        break;
                    case "money": money = int.Parse(kvp.Value); break;
                    case "sprite": GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/Characters/" + kvp.Value); break;
                    case "dialogue": dialoguePackageName = kvp.Value; break;
                    case "merchant_stock":
                        if (merchantStock == null)
                        {
                            GameObject merchantStockObject = new GameObject("MerchantStock");
                            merchantStockObject.transform.SetParent(transform);
                            merchantStock = merchantStockObject.AddComponent<MerchantStock>();
                            merchantStock.infoBlockReference = kvp.Value;
                            merchantStock.Initiate();
                        }
                        break;
                    default: break;
                }
            }
            string[] itemsToEquip = new string[builtInSlotsCount];
            for (int i = 0; i < builtInSlotsCount; i++)
                itemsToEquip[i] = "";
            List<GameObject> createdItems = new List<GameObject>();
            bool loadMagazineAfterCreation = false;
            foreach (InfoBlock subBlock in template.subBlocks)
            {
                switch (subBlock.name)
                {
                    case "money": money = subBlock.GetIntRange(); break;
                    case "sprite": GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/Characters/" + subBlock.GetRandomValue()); break;
                    case "attributes":
                        foreach (InfoBlock attributeSubBlock in subBlock.subBlocks)
                        {
                            if (objectAttributes.GetAttribute(attributeSubBlock.name) != null)//(attributeNames.Contains(attributeSubBlock.name))
                            {
                                foreach (KeyValuePair<string, string> kvp in attributeSubBlock.namesValues)
                                {
                                    if (kvp.Key == "start")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).Set(float.Parse(kvp.Value));
                                    if (kvp.Key == "max")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).MaxValue = float.Parse(kvp.Value);
                                }
                                foreach (InfoBlock attributeRandom in attributeSubBlock.subBlocks)
                                {
                                    if (attributeRandom.name == "start")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).Set(attributeRandom.GetIntRange());
                                }
                            }
                        }
                        break;
                    case "tags":
                        tags.AddRange(subBlock.values);
                        foreach (string tag in tags)
                            world.tags.Add((tag, this));
                        break;
                    case "equipped":
                        foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            itemsToEquip[(int)Enum.Parse(typeof(BuiltinCharacterSlots), TextTools.FirstCharToUpper(kvp.Key))] = kvp.Value;
                        break;
                    case "items":
                        foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            for (int i = 0; i < int.Parse(kvp.Value); i++)
                                createdItems.Add(world.CreateItem(kvp.Key));
                        foreach (InfoBlock itemSubBlock in subBlock.subBlocks)
                        {
                            switch (itemSubBlock.name)
                            {
                                case "weapons":
                                    foreach (InfoBlock weaponSubBlock in itemSubBlock.subBlocks)
                                    {
                                        GameObject createdWeapon = world.CreateItem(weaponSubBlock.name);
                                        createdItems.Add(createdWeapon);
                                        foreach (KeyValuePair<string, string> kvp in weaponSubBlock.namesValues)
                                        {
                                            switch (kvp.Key)
                                            {
                                                case "magazine":
                                                    switch (kvp.Value)
                                                    {
                                                        case "find": loadMagazineAfterCreation = true; break;
                                                        case "standart":
                                                            if (createdWeapon.GetComponent<Firearm>().magazine != null)
                                                            {
                                                                Magazine magazine = createdWeapon.GetComponent<Firearm>().magazine.GetComponent<Magazine>();
                                                                if (magazine != null)
                                                                {
                                                                    magazine.ammo = magazine.maxammo;
                                                                    GameObject tempAmmo = world.CreateItem(magazine.caliber);
                                                                    magazine.currentCaliber = tempAmmo.GetComponent<AmmoBox>().bulletType;
                                                                    Destroy(tempAmmo);
                                                                }
                                                            }
                                                            break;
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                case "magazines":
                                    foreach (InfoBlock magazineSubBlock in itemSubBlock.subBlocks)
                                    {
                                        GameObject magazine = world.CreateItem(magazineSubBlock.name);
                                        createdItems.Add(magazine);
                                        Magazine magazineComponent = magazine.GetComponent<Magazine>();
                                        string type = "standart";
                                        int amount = 0;
                                        foreach (KeyValuePair<string, string> kvp in magazineSubBlock.namesValues)
                                        {
                                            switch (kvp.Key)
                                            {
                                                case "type": type = kvp.Value; break;
                                                case "amount": amount = int.Parse(kvp.Value); break;
                                            }
                                        }
                                        foreach (InfoBlock createdMagazineSubBlock in magazineSubBlock.subBlocks)
                                        {
                                            switch (createdMagazineSubBlock.name)
                                            {
                                                case "amount":
                                                    amount = createdMagazineSubBlock.GetIntRange();
                                                    break;
                                            }
                                        }
                                        if (type == "standart")
                                            type = magazineComponent.caliber;
                                        else
                                            type = magazineComponent.caliber + type;
                                        GameObject tempAmmo = world.CreateItem(type);
                                        magazineComponent.currentCaliber = tempAmmo.GetComponent<AmmoBox>().bulletType;
                                        magazineComponent.ammo = Mathf.Clamp(amount, 0, magazineComponent.maxammo);
                                        Destroy(tempAmmo);
                                    }
                                    break;
                            }
                        }
                        break;
                    default: break;
                }
            }
            //string allItemsList = "";
            //foreach (GameObject go in createdItems)        
            //    allItemsList += go.name + ", ";        
            //if (allItemsList.Length > 3)
            //    allItemsList = allItemsList.Substring(0, allItemsList.Length - 2);
            //Debug.Log("Created " + createdItems.Count.ToString() + " items when creating " + template.name + ": " + allItemsList);
            foreach (GameObject item in createdItems)
                for (int j = 0; j < builtInSlotsCount; j++)
                    if (item.name == itemsToEquip[j])
                        PlaceItemInSlot(item, (BuiltinCharacterSlots)j);
            foreach (GameObject remaining in createdItems)
                if (!ItemIsEquipped(remaining))
                    TryPlaceItemInInventory(remaining);
            if (loadMagazineAfterCreation)
            {
                ReloadWeapon();
                RestoreActionPoints();
            }
        }
    }

    public void Say(string s)
    {
        if (Game.Instance.PointIsOnScreen(transform.position))
            world.UpdateLog(displayName + " says: " + '"' + s + '"');
    }

    public void StartTurn()
    {
        characterNameOnGUI.color = Color.white;
        enabled = true;
        RestoreActionPoints();
        turnFinished = false;
        if (!Game.Instance.AIenabled && !IsPlayer() && world.Player != null)
        {
            EndTurn();
            world.GiveTurnToNextCharacter(this);
            return;
        }
        if (brain.enabled)
        {
            brain.Think();
            if (cellsLeftToTraverse > 0 && !IsInCombat())
                MoveOnGivenPath();
            if (turnFinished && world.Player != null)
                world.GiveTurnToNextCharacter(this);
        }
    }

    public void EndTurn()
    {
        if (!turnFinished)
        {
            RestoreStamina((GetAttribute("Endurance").Value - 12f) / 20 + 0.2f);
            Heal((GetAttribute("Endurance").Value - 12f) / 20 + 0.2f + regenerationModifier);
            turnFinished = true;
            characterNameOnGUI.color = Color.yellow;
            if (IsPlayer())
                attackTarget = null;
            if (brain.enabled)
                brain.OnEndTurn();
            if (merchantStock != null)
                merchantStock.EndTurn();
        }
    }

    public void OnMoveStart()
    {
        if (brain.enabled)
            brain.OnMoveStart();
    }

    public void OnMoveEnd()
    {
        if (brain.enabled)
            brain.OnMoveEnd();
    }

    public void MoveOnPathTo(Vector2 position)
    {
        if (actionPoints >= MoveCost)
        {
            List<Direction> path = pathFinder.FindPath(position);
            if (path.Count > 0)
                MoveOnPath(path);
        }
    }

    public void MoveOnPath(List<Direction> path)
    {
        if (immobile)
            return;
        if (actionPoints >= MoveCost && path.Count > 0)
        {
            waitingTurns = 0;
            pathDirections = path;
            //Debug.Log(pathDirections.Count + " cells to traverse from " + (Vector2)transform.position + " to " + targetPosition);
            cellsLeftToTraverse = pathDirections.Count;
            Vector2 currentPosition = transform.position;
            Vector2 prevPosition = transform.position;
            foreach (Direction d in pathDirections)
            {
                currentPosition += pathFinder.DirectionToNumbers(d) * Game.Instance.cellSize;
                Debug.DrawRay(prevPosition, currentPosition - prevPosition, Color.red, 10);
                prevPosition = currentPosition;
            }
        }
    }

    public float GetPathMoveCost(List<Direction> specificPath = null)
    {
        float cost = 0f;
        List<Direction> path = specificPath;
        if (path == null)
            path = pathDirections;
        for (int i = 0; i < path.Count; i++)
        {
            float additionalCost = 1f;
            if (Enum.GetName(typeof(Direction), path[i]).Length > 1)
                additionalCost = 1.43f;
            if (path[i] != Direction.C)
                cost += additionalCost * MoveCost;
        }
        return cost;
    }    

    IEnumerator MoveCharacter()
    {
        yield return new WaitForEndOfFrame();
        if (moveDelta != Vector2.zero)
        {
            performingAction = true;
            transform.position = new Vector3(startPosition.x + moveDelta.x, startPosition.y + moveDelta.y, 0);
            startPosition = transform.position;
            float distance = Vector2.Distance(startPosition, targetPosition);
            if (distance > 0.01f)
            {
                StartCoroutine(MoveCharacter());
            }
            else
            {
                performingAction = false;
                moveDelta = Vector2.zero;
                transform.position = new Vector2((float)Math.Round(targetPosition.x, 2), (float)Math.Round(targetPosition.y, 2));
                OnMoveEnd();
            }
        }
    }

    public bool CanReachCharacterInDirection(Direction direction)
    {
        Vector2 currentPosition = transform.position;
        Vector2 directionOffset = pathFinder.DirectionToNumbers(direction) * Game.Instance.cellSize;
        float castDistance = Vector2.Distance(currentPosition, currentPosition + directionOffset);
        RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(currentPosition, directionOffset, castDistance);
        //Debug.DrawRay(currentPosition, directionOffset, Color.blue, 40);
        for (int i = 0; i < raycastHit2D.Length; i++)
        {
            Transform objectHit = raycastHit2D[i].transform;
            if (objectHit.gameObject == gameObject)
                continue;
            if (objectHit.GetComponent<Character>())
                return true;
            if (Game.Instance.nonWalkableLayers.Contains(objectHit.gameObject.layer))
                return false;
        }
        return true;
    }

    public void MeleeAttackInDirection(Direction direction)
    {
        GameObject weapon = Weapon;
        if (weapon != null)
        {
            MeleeWeapon meleeWeaponComponent = weapon.GetComponent<MeleeWeapon>();
            if (meleeWeaponComponent)
            {
                Vector2 currentPosition = transform.position;
                Vector2 directionOffset = pathFinder.DirectionToNumbers(direction) * Game.Instance.cellSize;
                float castDistance = Vector2.Distance(currentPosition, currentPosition + directionOffset);
                RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(currentPosition, directionOffset, castDistance);
                //Debug.DrawRay(currentPosition, directionOffset, Color.blue, 40);
                for (int i = 0; i < raycastHit2D.Length; i++)
                {
                    Transform objectHit = raycastHit2D[i].transform;
                    if (objectHit.gameObject == gameObject)
                        continue;
                    Character character = objectHit.GetComponent<Character>();
                    if (character && character != this)
                    {
                        Damage damage = new Damage();
                        damage.amount = meleeWeaponComponent.damage;
                        character.TakeDamage(damage);
                        return;
                    }
                }
            }
        }
    }

    public bool TryMove(Direction direction)
    {
        if (!IsMoving())
        {
            if (direction == Direction.C)
                return true;
            Vector2 currentPosition = transform.position;
            Vector2 directionOffset = pathFinder.DirectionToNumbers(direction) * Game.Instance.cellSize;
            float castDistance = Vector2.Distance(currentPosition, currentPosition + directionOffset);
            RaycastHit2D[] raycastHit2D = Physics2D.RaycastAll(currentPosition, directionOffset, castDistance);
            //Debug.DrawRay(currentPosition, directionOffset, Color.blue, 40);
            for (int i = 0; i < raycastHit2D.Length; i++)
            {
                Transform objectHit = raycastHit2D[i].transform;
                if (objectHit.gameObject == gameObject)
                    continue;
                if (Game.Instance.nonWalkableLayers.Contains(objectHit.gameObject.layer))
                {
                    //Debug.Log(objectHit.gameObject.name + " is in the way");
                    if (UnityEngine.Random.Range(0, 100) < 30)
                    {
                        Character guyInTheWay = objectHit.GetComponent<Character>();
                        if (guyInTheWay)
                        {
                            if (guyInTheWay.IsPlayer() && !hostileTowards.Contains(guyInTheWay.faction))
                            {
                                int phraseNum = UnityEngine.Random.Range(0, 4);
                                string[] phrases = { "Bro, get out of my way", "Don't stand in my way, bro", "Can you move aside?", "Out of the way" };
                                Say(phrases[phraseNum]);
                            }
                        }
                    }
                    StopMovingOnPath();
                    return false;
                }
            }
            float additionalCost = 1f;
            if (Enum.GetName(typeof(Direction), direction).Length > 1)
                additionalCost = 1.43f;
            if (UseActionPoints(MoveCost * additionalCost))
            {
                UseStamina(UnityEngine.Random.Range(0.1f, 0.16f));
                startPosition = currentPosition;
                targetPosition = startPosition + directionOffset;
                moveDelta = 0.01f * directionOffset;
                moveDelta *= moveSpeed;
                StartCoroutine(MoveCharacter());
                return true;
            }
            else
            {
                StopMovingOnPath();
                EndTurn();
            }
        }
        return false;
    }

    bool TryMoveInstantly(Direction direction)
    {
        if (!IsMoving())
        {
            if (direction == Direction.C)
                return true;
            float additionalCost = 1f;
            if (Enum.GetName(typeof(Direction), direction).Length > 1)
                additionalCost = 1.43f;
            if (UseActionPoints(MoveCost * additionalCost))
            {
                UseStamina(UnityEngine.Random.Range(0.1f, 0.16f));
                Vector2 directionOffset = pathFinder.DirectionToNumbers(direction);
                targetPosition = (Vector2)transform.position + directionOffset * Game.Instance.cellSize;
                transform.position = new Vector2((float)Math.Round(targetPosition.x, 2), (float)Math.Round(targetPosition.y, 2));//targetPosition;                    
                OnMoveEnd();
                return true;
            }
            else
            {
                StopMovingOnPath();
                EndTurn();
            }
        }
        return false;
    }

    public void StopMovingOnPath()
    {
        cellsLeftToTraverse = 0;
        pathDirections.Clear();
        //Debug.Log(displayName + " moving no longer");
    }

    public void MoveOnGivenPath()
    {
        if (immobile)
        {
            StopMovingOnPath();
            return;
        }
        if (!IsMoving() && cellsLeftToTraverse > 0)
        {
            int directionNumber = pathDirections.Count - cellsLeftToTraverse;
            if (directionNumber >= 0)
            {
                Vector2 directionOffset = pathFinder.DirectionToNumbers(pathDirections[directionNumber]);
                Vector2 tempTargetPosition = (Vector2)transform.position + directionOffset * Game.Instance.cellSize;
                if (!Game.Instance.PointIsOnScreen(transform.position) && !Game.Instance.PointIsOnScreen(tempTargetPosition))
                {
                    if (TryMoveInstantly(pathDirections[directionNumber]))
                    {
                        cellsLeftToTraverse--;
                        if (!IsInCombat())
                            EndTurn();
                        else
                            MoveOnGivenPath();
                    }
                }
                else
                {
                    if (TryMove(pathDirections[directionNumber]))
                    {
                        cellsLeftToTraverse--;
                        if (!IsInCombat())
                            EndTurn();
                    }
                }
            }
            else
            {
                StopMovingOnPath();
            }
        }
    }

    void Update()
    {
        //if (!IsPlayer())
        //    world.UpdateLog(displayName + " moving");
        if (IsMoving() || performingAction)
        {
            waitingTurns = 0;
            return;
        }
        if (turnFinished)
        {
            world.GiveTurnToNextCharacter(this);
            return;
        }
        if (waitingTurns > 0)
        {
            if (IsPlayer())
                world.UpdateLog("Waiting for " + (waitingTurns - 1).ToString() + " turns");
            waitingTurns--;
            RestoreStamina(0.32f);
            EndTurn();
            world.GiveTurnToNextCharacter(this);
            return;
        }
        bool inCombat = IsInCombat();
        if (actionPoints < 1f)
        {
            if (inCombat)
            {
                StopMovingOnPath();
                EndTurn();
                return;
            }
        }
        if (cellsLeftToTraverse > 0)
            MoveOnGivenPath();
        else if (brain.enabled)
            brain.Think();
    }
}