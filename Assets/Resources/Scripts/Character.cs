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
    Eyes,
    ThighRig
}

public enum ActionTypes
{
    Attack,
    Move
}

public class Character : MonoBehaviour
{
    private class ShootInfo
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
    public List<string> hostileTowards = new List<string>();
    public List<string> friendlyTowards = new List<string>();
    public List<GameObject> movingBullets = new List<GameObject>();
    public List<Direction> pathDirections = new List<Direction>();
    public bool turnFinished = true;
    public bool performingAction = false;
    public bool invulnerable = false;
    public float actionPoints = 18;
    public string faction = "";
    public string displayName = "";
    public int freeCharacterPoints = 0;
    public int sightDistance = 20;
    public int cellsLeftToTraverse = 0;
    public int waitingTurns = 0;
    int experience = 0;
    public Game game;
    public AI brain;
    public PathFinder pathFinder;
    public Character attackTarget = null;
    public GameObject inventory = null;
    public Vector2 targetPosition;
    public Vector2 moveDelta;
    public Vector2 startPosition;
    public Text characterNameOnGUI;
    //public bool usingKit = false;

    public Attribute GetAttribute(string attributeName) { return GetComponent<ObjectAttributes>().GetAttribute(attributeName); }
    public Attribute.ChangeResult Heal(float amount = 0f) { return GetComponent<ObjectAttributes>().ModAttribute("Health", amount); }
    public Attribute.ChangeResult UseStamina(float amount) { return GetAttribute("Stamina").Modify(-amount + (GetAttribute("Endurance").GetValue() - 14f) / 25); }
    public Attribute.ChangeResult RestoreStamina(float amount) { return GetAttribute("Stamina").Modify(amount); }
    public float Health { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Health"); } }
    public float Stamina { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Stamina"); } }
    public float MaxEncumbrance { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Strength") * 1.5f; } }
    public float MoveCost { get { return 3f; } }
    public bool IsPlayer() { return game.characterController.ControlledCharacter == gameObject; }
    public bool IsMoving() { return moveDelta != Vector2.zero; }
    public int Level { get { return (int)GetComponent<ObjectAttributes>().GetAttributeValue("Level"); } }
    public int GetExperience() { return experience; }

    public void EquipItemAsWeapon(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Weapon); }
    public void EquipVest(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Vest); }
    public void EquipBelt(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Belt); }
    public void EquipBackpack(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Backpack); }
    public void EquipWeaponOnBack(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Backweapon); }
    public void EquipHelmet(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Helmet); }
    public void EquipMask(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Mask); }
    public void EquipArmor(GameObject item) { PlaceItemInSlot(item, BuiltinCharacterSlots.Armor); }

    public GameObject weapon { get { return GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon); } }
    public GameObject GetSlot(BuiltinCharacterSlots slot) { return inventory.transform.GetChild((int)slot).gameObject; }

    public Location GetCurrentLocation()
    {
        foreach (Location location in game.locations)
            if (game.DistanceFromToInCells(location.transform.position, transform.position) <= location.cellRadius)
                return location;
        return null;
    }

    public void UnloadWeapon()
    {
        Firearm firearmComponent = weapon.GetComponent<Firearm>();
        if (firearmComponent)
        {
            GameObject magazine = firearmComponent.UnloadMagazine();
            if (magazine != null)
                TryPlaceItemInInventory(magazine);
        }
    }

    public void ReloadWeapon()
    {
        if (weapon != null)
        {
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
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
                            game.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " is already fully loaded");
                        return;
                    }
                }
                GameObject magazineToLoad = null;
                GameObject vest = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Vest);
                //GameObject belt = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Belt);
                if (vest != null)
                {
                    List<GameObject> items = vest.GetComponent<LBEgear>().GetAllItems();
                    foreach (GameObject item in items)
                    {
                        Magazine magazineComponent = item.GetComponent<Magazine>();
                        if (magazineComponent && !magazineIsIntegral)
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
                        AmmoBox ammoBoxComponent = item.GetComponent<AmmoBox>();
                        if (ammoBoxComponent && magazineIsIntegral)
                        {
                            if (ammoBoxComponent.bulletType.caliber == firearmComponent.magazine.GetComponent<Magazine>().caliber)
                            {
                                if (UseActionPoints(item.GetComponent<Item>().actionPointsToMove))
                                {
                                    currentMagazine.GetComponent<Magazine>().LoadFromAmmoBox(item);
                                    if (ammoBoxComponent.amount <= 0)
                                        Destroy(item);
                                }
                                else if (IsPlayer())
                                    game.UpdateLog("Not enough action points to reload " + firearmComponent.GetComponent<Item>().displayName);
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
                            game.UpdateLog("Reloading " + firearmComponent.GetComponent<Item>().displayName);
                        else if (game.PointIsOnScreen(transform.position))
                            game.UpdateLog(displayName + " reloads");
                    }
                    else if (IsPlayer())
                        game.UpdateLog("Not enough action points to reload " + firearmComponent.GetComponent<Item>().displayName);
                }
                else if (IsPlayer())
                    game.UpdateLog("No magazine found for " + firearmComponent.GetComponent<Item>().displayName);
            }
            else if (IsPlayer())
                game.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " can't be reloaded");
        }
        else if (IsPlayer())
            game.UpdateLog("You don't have a weapon");
    }

    public int GetAttackDistance()
    {
        GameObject equippedItem = weapon;
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
            if (game.shootableLayers.Contains(objectHit.gameObject.layer))
                return false;
        }
        //RaycastHit2D[] raycastHit2DSpreadOne = Physics2D.RaycastAll(from, position - from + spread);
        //for (int i = 0; i < raycastHit2DSpreadOne.Length; i++)
        //{
        //    Transform objectHit = raycastHit2DSpreadOne[i].transform;
        //    if ((Vector2)objectHit.position == position)
        //        continue;
        //    if (game.shootableLayers.Contains(objectHit.gameObject.layer))
        //        return false;
        //}
        //RaycastHit2D[] raycastHit2DSpreadTwo = Physics2D.RaycastAll(from, position - from - spread);
        //for (int i = 0; i < raycastHit2DSpreadTwo.Length; i++)
        //{
        //    Transform objectHit = raycastHit2DSpreadTwo[i].transform;
        //    if ((Vector2)objectHit.position == position)
        //        continue;
        //    if (game.shootableLayers.Contains(objectHit.gameObject.layer))
        //        return false;
        //}
        return objectCanBeHit;
    }

    public float GetMaxActionPoints()
    {
        //return Mathf.Clamp(GetAttribute("Dexterity").GetValue() * Health / GetAttribute("Health").maxValue, 5, 25);
        return Mathf.Clamp(GetAttribute("Dexterity").GetValue(), 5, 25);
    }

    [ContextMenu("Print attributes")]
    public void ListAttirbutes()
    {
        ObjectAttributes attributes = GetComponent<ObjectAttributes>();
        foreach (Attribute a in attributes.attributes)
            Debug.Log(displayName + ": " + a.name + "/" + a.minValue + "/" + a.value + "/" + a.maxValue);
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
        game.GetComponent<InventoryManager>().OverrideItemViewText("Not enough AP!");
        return false;
    }

    public bool IsInCombat()
    {
        if (attackTarget != null)
            return true;
        foreach (Character character in game.activeCharacters)
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
            PlaySound(Resources.Load<AudioClip>("Sounds/bullet_impact"), 1f, 0.1f);
        if (!invulnerable)
            damage.Apply(gameObject);
        if (Health > 0)
        {
            if (damage.source != null)
            {
                Character attacker = damage.source.GetComponent<Character>();
                if (attacker != null)
                {
                    //if (attackTarget == null)                    
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
                    int levelDif = (int)GetAttribute("Level").value + 1 - (int)attacker.GetAttribute("Level").value;
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
        game.activeCharacters.Remove(this);
        DropAllItems();
        game.UpdateLog(displayName + " dies!" + killedBy);
        PlaySound(Resources.Load<AudioClip>("Sounds/death"));
        if (characterNameOnGUI != null)
            Destroy(characterNameOnGUI.gameObject);
        Destroy(gameObject);
    }

    public int GetXPToNextLevel()
    {
        int level = (int)GetAttribute("Level").value;
        int nextLevel = level + 1;
        int nextLevelXpFormula = (nextLevel - level) * 50;
        return nextLevelXpFormula;
    }

    public void AddExperience(int xp)
    {
        if (xp > 0)
        {
            if (experience + xp > GetXPToNextLevel())
            {
                GetAttribute("Level").Modify(1);
                game.UpdateLog(displayName + " gains a level");
            }
            experience += xp;
            if (IsPlayer())
            {
                game.UpdateLog("Recieved " + xp.ToString() + " experience points");
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

    public void PlaceItemInSlot(GameObject item, BuiltinCharacterSlots slot)
    {
        GameObject slotObject = GetSlot(slot);
        Item itemComponent = item.GetComponent<Item>();
        if (itemComponent)
        {
            if (itemComponent.ItemFitsInSlot(slotObject.GetComponent<ItemSlot>().slotType) > 0)
            {
                UnequipItemFromSlot(slotObject);
                slotObject.GetComponent<ItemSlot>().PlaceItem(item);
                //item.transform.parent = slotObject.transform;
            }
        }
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
                //Game.UpdateLog(item.name + ": " + item.GetComponent<Item>().GetWeight().ToString());
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
            ItemExtension extension = item.GetComponent<ItemExtension>();
            if (!extension)
                game.DropItemToCell(item, transform.localPosition);
            else
            {
                LBEgear gear = item.GetComponent<LBEgear>();
                if (gear)
                {
                    List<GameObject> items = gear.GetAllItems();
                    game.DropItemsToCell(items, transform.localPosition);
                }
            }
        }
    }

    public bool PlaceItemAnywhere(GameObject item)
    {
        bool itemPlaced = false;
        if (item.GetComponent<Item>().GetWeight() + CalculateEncumbrance() > MaxEncumbrance)
        {
            if (IsPlayer())
                game.UpdateLog(item.GetComponent<Item>().displayName + " is too heavy.");
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
            Vector2 cell = pathFinder.DirectionToNumbers(d) * game.cellSize + (Vector2)transform.position;
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

    public bool ShootAtPoint(Vector2 position)
    {
        Vector2 characterPosition = transform.position;
        GameObject weapon = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
        if (weapon != null)
        {
            if (Stamina / GetAttribute("Stamina").maxValue <= 0.1f)
            {
                if (IsPlayer())
                    game.UpdateLog("Not enough stamina to shoot");
                return false;
            }
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
            if (firearmComponent)
            {
                if (firearmComponent.GetAmmoCount() == 0 && IsPlayer())
                {
                    game.UpdateLog(firearmComponent.GetComponent<Item>().displayName + " has no ammo!");
                    return false;
                }
                if (!UseActionPoints(firearmComponent.GetFireMode().actionPoints) && IsPlayer())
                {
                    game.UpdateLog("You don't have enough ap to fire " + firearmComponent.GetComponent<Item>().displayName);
                    return false;
                }
                //game.UpdateLog(displayName + " fires " + weapon.GetComponent<Item>().displayName);
                Magazine magazineComponent = firearmComponent.magazine.GetComponent<Magazine>();
                performingAction = true;
                int recoilDirection = UnityEngine.Random.Range(0, 2);
                if (recoilDirection == 0)
                    recoilDirection = -1;
                float finalAccuracy = GetAccuracyMaxSpread();
                Vector2 startingSpread = new Vector2(UnityEngine.Random.Range(Mathf.Clamp(25 - finalAccuracy, 0, 25), Mathf.Clamp(finalAccuracy - 25, 0, -25)),
                                                     UnityEngine.Random.Range(Mathf.Clamp(25 - finalAccuracy, 0, 25), Mathf.Clamp(finalAccuracy - 25, 0, -25)));
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
                if (shootInfo.shotsRemaining > 0)
                {
                    StartCoroutine(Shoot(shootInfo));
                    return true;
                }
            }
        }
        return false;
    }

    public float GetAccuracyMaxSpread()
    {
        GameObject weapon = GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
        if (weapon != null)
        {
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
            if (firearmComponent)
            {
                float weaponAccuracy = firearmComponent.Accuracy;
                //Empty weight is used
                float weightPenalty = Mathf.Clamp(firearmComponent.Weight - 6f - (GetAttribute("Strength").value - 16f), 0, 50);
                float handleModifier = Mathf.Clamp(GetAttribute("Level").value - 5 - firearmComponent.handleDifficulty, -5, 20);
                float skillModifier = GetAttribute("Marksmanship").value / 1.25f;
                float dexterityModifier = GetAttribute("Dexterity").value / 4;
                float perceptionModifier = GetAttribute("Perception").value / 1.25f;
                float finalAccuracy = weaponAccuracy - weightPenalty + handleModifier + skillModifier + dexterityModifier + perceptionModifier;
                return finalAccuracy;
            }
        }
        return 0f;
    }

    public void PlaySound(AudioClip sound, float spatialBlend = 1.0f, float volumeMod = 1.0f)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = game.SoundVolume * volumeMod;
        audioSource.clip = sound;
        audioSource.Play();
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
                if (game.characterController.ControlledCharacter == gameObject)
                    game.UpdateLog(info.firearmComponent.GetComponent<Item>().displayName + " is jammed!");
            }
        }
        if (info.firearmComponent.GetFireMode().shotsPerAttack == 1)
            delay = 0.05f;
        yield return new WaitForSeconds(delay);
        info.firearmComponent.SpendAmmo();
        if (Mathf.Clamp(GetAttribute("Level").value * 2 - info.firearmComponent.handleDifficulty, 5, 20) + UnityEngine.Random.Range(0, 6) < 10)
            info.recoil += new Vector2(info.firearmComponent.GetFireMode().recoil * info.recoilDirection, info.firearmComponent.GetFireMode().recoil * info.recoilDirection);
        else
            info.recoil = Vector2.zero;
        Vector2 randomSpread = Vector2.zero;
        PlaySound(info.firearmComponent.shootSound, Mathf.Clamp(1f - info.magazineComponent.currentCaliber.damage / 100, 0f, 0.9f));
        //PlaySound(Resources.Load<AudioClip>("Sounds/echo" + UnityEngine.Random.Range(1, 5).ToString()), 0.9f);
        for (int i = 0; i < info.magazineComponent.currentCaliber.bulletsPerShot; i++)
        {
            if (i > 0)
                randomSpread = new Vector2(UnityEngine.Random.Range(Mathf.Clamp(25 - info.firearmComponent.Accuracy * 2, 0, 25), Mathf.Clamp(info.firearmComponent.Accuracy * 2 - 25, 0, 25)),
                                           UnityEngine.Random.Range(Mathf.Clamp(25 - info.firearmComponent.Accuracy * 2, 0, 25), Mathf.Clamp(info.firearmComponent.Accuracy * 2 - 25, 0, 25)));
            randomSpread /= 4;
            Vector2 spread = info.spread + info.recoil + randomSpread;
            GameObject bullet = new GameObject("Bullet") { layer = 14 };
            Transform bulletTransform = bullet.transform;
            bulletTransform.SetParent(game.transform);
            bulletTransform.localScale = Vector3.one * 0.3f;
            Vector3 newDirection = info.attackPoint - info.characterPosition;
            float distance = Vector2.Distance(info.attackPoint, info.characterPosition);
            bulletTransform.up = newDirection + (Vector3)randomSpread * distance / game.accuracyModifier / 10 / (GetAttribute("Stamina").maxValue / Stamina / 4) + (Vector3)spread * distance / game.accuracyModifier * (GetAttribute("Stamina").maxValue / Stamina / 4) * (info.firearmComponent.Condition / info.firearmComponent.GetComponent<Item>().GetMaxCondition() / 2);
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
            UseStamina(UnityEngine.Random.Range(0.3f, 0.4f));
            FiredBullet firedBulletComponent = bullet.AddComponent<FiredBullet>();
            //firedBulletComponent.startedFromCameraView = Game.PointIsOnScreen(transform.position);
            firedBulletComponent.game = game;
            firedBulletComponent.bulletDamage = new BulletDamage(info.magazineComponent.currentCaliber, info.firearmComponent.damageModifier);
            firedBulletComponent.bulletDamage.source = gameObject;
            firedBulletComponent.shooter = gameObject;
            firedBulletComponent.startPoint = bulletTransform.position;
            float distanceUnadjusted = info.magazineComponent.currentCaliber.distance + info.firearmComponent.distanceModifier;
            firedBulletComponent.maxDistance = distanceUnadjusted;

            movingBullets.Add(bullet);
        }
        info.shotsRemaining--;
        info.firearmComponent.GetComponent<ObjectAttributes>().GetAttribute("Item condition").Modify(UnityEngine.Random.Range(-0.5f, -0.75f));
        if (info.shotsRemaining > 0 && info.firearmComponent.GetAmmoCount() > 0)
            StartCoroutine(Shoot(info));
        else
            CalculateEncumbrance();
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
                currentPosition += pathFinder.DirectionToNumbers(d) * game.cellSize;
                Debug.DrawRay(prevPosition, currentPosition - prevPosition, Color.red, 10);
                prevPosition = currentPosition;
            }
        }
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

    public bool TryMove(Direction direction)
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
                //MoveInDirection(direction);
                UseStamina(UnityEngine.Random.Range(0.1f, 0.16f));
                Vector2 directionOffset = pathFinder.DirectionToNumbers(direction);
                startPosition = transform.position;
                targetPosition = startPosition + directionOffset * game.cellSize;
                if (!game.PointIsOnScreen(targetPosition))
                {
                    transform.position = new Vector2((float)Math.Round(targetPosition.x, 2), (float)Math.Round(targetPosition.y, 2));//targetPosition;
                    OnMoveEnd();
                    return true;
                }
                moveDelta = 0.01f * directionOffset * game.cellSize;
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

    public void StopMovingOnPath()
    {
        cellsLeftToTraverse = 0;
        pathDirections.Clear();
        //Debug.Log(displayName + " moving no longer");
    }

    public bool EnoughActionPointsToPerformAction(ActionTypes action)
    {
        switch (action)
        {
            case ActionTypes.Attack:
                if (weapon != null)
                    if (weapon.GetComponent<Firearm>() != null)
                        if (actionPoints >= weapon.GetComponent<Firearm>().GetFireMode().actionPoints)
                            return true;
                break;
            case ActionTypes.Move:
                return actionPoints >= MoveCost;
        }
        return false;
    }

    public void LoadFaction(string factionName)
    {
        faction = factionName;
        friendlyTowards.Clear();
        hostileTowards.Clear();
        InfoBlock factionBlock = game.factions.GetBlock(factionName);
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
        InfoBlock template = null;
        foreach (InfoBlock characterTemplate in game.characterTemplates.subBlocks)
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
            //string[] attributeNames = { "Level", "Health", "Stamina", "Encumbrance", "Strength", "Dexterity", "Endurance", "Perception", "Social", "Marksmanship", "Medical", "Mechanical" };
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
                    case "sprite": GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/Characters/" + kvp.Value); break;
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
                    case "sprite": GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Graphics/Characters/" + subBlock.GetRandomValue()); break;
                    case "attributes":
                        foreach (InfoBlock attributeSubBlock in subBlock.subBlocks)
                        {
                            if (objectAttributes.GetAttribute(attributeSubBlock.name) != null)//(attributeNames.Contains(attributeSubBlock.name))
                            {
                                foreach (KeyValuePair<string, string> kvp in attributeSubBlock.namesValues)
                                {
                                    if (kvp.Key == "start")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).value = float.Parse(kvp.Value);
                                    if (kvp.Key == "max")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).maxValue = float.Parse(kvp.Value);
                                }
                                foreach (InfoBlock attributeRandom in attributeSubBlock.subBlocks)
                                {
                                    if (attributeRandom.name == "start")
                                        objectAttributes.GetAttribute(attributeSubBlock.name).value = attributeRandom.GetFloatRange();
                                }
                            }
                        }
                        break;
                    case "equipped":
                        foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            itemsToEquip[(int)Enum.Parse(typeof(BuiltinCharacterSlots), StringExtensions.FirstCharToUpper(kvp.Key))] = kvp.Value;
                        break;
                    case "items":
                        foreach (KeyValuePair<string, string> kvp in subBlock.namesValues)
                            for (int i = 0; i < int.Parse(kvp.Value); i++)
                                createdItems.Add(game.CreateItem(kvp.Key));
                        foreach (InfoBlock itemSubBlock in subBlock.subBlocks)
                        {
                            switch (itemSubBlock.name)
                            {
                                case "weapons":
                                    foreach (InfoBlock weaponSubBlock in itemSubBlock.subBlocks)
                                    {
                                        GameObject createdWeapon = game.CreateItem(weaponSubBlock.name);
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
                                                                    GameObject tempAmmo = game.CreateItem(magazine.caliber);
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
                                        GameObject magazine = game.CreateItem(magazineSubBlock.name);
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
                                        GameObject tempAmmo = game.CreateItem(type);
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
        if (game.PointIsOnScreen(transform.position))
            game.UpdateLog(displayName + " says: " + '"' + s + '"');
    }

    public void StartTurn()
    {
        characterNameOnGUI.color = Color.white;
        enabled = true;
        RestoreActionPoints();
        turnFinished = false;
        if (brain.enabled)
        {
            brain.Think();
            if (turnFinished && game.player != null)
                game.GiveTurnToNextCharacter(this);
        }
    }

    public void EndTurn()
    {
        if (!turnFinished)
        {
            RestoreStamina((GetAttribute("Endurance").value - 12f) / 100 + 0.4f);
            Heal((GetAttribute("Endurance").value - 12f) / 100 + 0.4f);
            turnFinished = true;
            characterNameOnGUI.color = Color.yellow;
            if (brain.enabled)
                brain.OnEndTurn();
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

    void Update()
    {
        if (IsMoving() || performingAction)
        {
            waitingTurns = 0;
            return;
        }
        if (turnFinished)
        {
            game.GiveTurnToNextCharacter(this);
            return;
        }
        if (waitingTurns > 0)
        {
            if (IsPlayer())
                game.UpdateLog("Waiting for " + (waitingTurns - 1).ToString() + " turns");
            waitingTurns--;
            RestoreStamina(0.32f);
            EndTurn();
            game.GiveTurnToNextCharacter(this);
        }
        if (actionPoints < 1f)
        {
            if (IsInCombat())
            {
                StopMovingOnPath();
                EndTurn();
                return;
            }
        }
        if (cellsLeftToTraverse > 0)
        {
            int directionNumber = pathDirections.Count - cellsLeftToTraverse;
            if (directionNumber >= 0)
            {
                if (TryMove(pathDirections[directionNumber]))
                {
                    cellsLeftToTraverse--;
                    //Debug.Log(cellsLeftToTraverse);
                    if (!IsInCombat())
                        EndTurn();
                }
            }
            else
            {
                StopMovingOnPath();
            }
        }
        else if (brain.enabled)
            brain.Think();
    }
}