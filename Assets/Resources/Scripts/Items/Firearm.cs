using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponFireMode
{
    Nonautomatic,
    Semiautomatic,
    ControlledBurst,
    Automatic
}

public class Firearm : MonoBehaviour
{
    public class FireMode
    {
        public int actionPoints = 3;
        public WeaponFireMode fireMode = WeaponFireMode.Semiautomatic;
        public int shotsPerAttack = 1;
        public int recoil = 0;

        public FireMode(int actionPoints, WeaponFireMode fireMode, int shotsPerAttack, int recoil = 0)
        {
            this.actionPoints = actionPoints;
            this.fireMode = fireMode;
            this.shotsPerAttack = shotsPerAttack;
            this.recoil = recoil;
        }
    }

    public int damageModifier = 0;
    public int distanceModifier = 0;
    public int reliability = 10;
    public int handleDifficulty = 8;
    public string magazineType;
    public string standartMagazine;
    public GameObject magazine = null;
    public List<FireMode> fireModes = new List<FireMode>();
    public string baseWeapon = "";
    public bool jammed = false;
    public AudioClip shootSound;
    int fireMode = 0;

    public string DisplayName { get { return GetComponent<Item>().displayName; } }
    public float Accuracy { get { return GetComponent<ObjectAttributes>().GetAttributeValue("Accuracy"); } }
    public float Weight { get { return GetComponent<Item>().Weight; } }
    public float Condition { get { return GetComponent<Item>().Condition; } }

    public bool HasFireMode(WeaponFireMode weaponFireMode)
    {
        foreach (FireMode fm in fireModes)
            if (fm.fireMode == weaponFireMode)
                return true;
        return false;
    }
    
    public FireMode SwitchFireMod()
    {
        fireMode++;
        if (fireMode >= fireModes.Count)
            fireMode = 0;
        return GetFireMode();
    }

    public FireMode SwitchFireModTo(WeaponFireMode weaponFireMode)
    {
        for (int i = 0; i < fireModes.Count; i++)
        {
            if (fireModes[i].fireMode == weaponFireMode)
            {
                fireMode = i;
                break;
            }
        }
        return GetFireMode();
    }

    public FireMode GetFireMode()
    {
        return fireModes[fireMode];
    }

    public GameObject UnloadMagazine()
    {
        GameObject unloadedMagazine = magazine;
        magazine = null;
        return unloadedMagazine;
    }

    public bool SpendAmmo()
    {
        if (magazine != null)
        {
            if (magazine.GetComponent<Magazine>().ammo > 0)
            {
                magazine.GetComponent<Magazine>().ammo--;
                
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public int GetAmmoCount()
    {
        if (magazine != null)
        {
            if (magazine.GetComponent<Magazine>())
            {
                return magazine.GetComponent<Magazine>().ammo;
            }
            else
            {
                Debug.Log(magazine.name + " has no magazine component");
                return 0;
            }
        }
        return 0;
    }

    public bool LoadMagazine(GameObject magazine)
    {
        if (this.magazine == null && magazine.GetComponent<Magazine>())
        {
            if (magazine.GetComponent<Magazine>().category == magazineType)
            {
                Character owner = GetComponent<Item>()?.GetOwner()?.GetComponent<Character>();
                if (owner != null)
                    owner.PlaySound(Resources.Load<AudioClip>("Sounds/rifle_reload"));
                magazine.transform.parent = transform;
                this.magazine = magazine;
                return true;
            }
        }
        return false;
    }
}