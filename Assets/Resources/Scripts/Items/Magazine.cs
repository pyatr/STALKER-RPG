using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magazine : Attachment
{
    public int ammo = 0, maxammo = 5;
    public string caliber;
    public string category;
    public Bullet currentCaliber = null;
    public bool builtin = false;

    public void LoadFromAmmoBox(GameObject ammoBox)
    {
        AmmoBox ammoBoxComponent = ammoBox.GetComponent<AmmoBox>();
        if (ammoBoxComponent)
        {
            bool caliberFits = false;
            if (currentCaliber != null)
                if (currentCaliber.caliber == ammoBoxComponent.bulletType.caliber)
                    caliberFits = true;
            if (ammo == 0 || currentCaliber == null)
                if (ammoBoxComponent.bulletType.caliber.Contains(caliber))
                    caliberFits = true;
            if (caliberFits)
            {
                if (ammoBoxComponent.amount > 0 && ammo < maxammo)
                {
                    int transferredAmmo = Mathf.Min(ammoBoxComponent.amount, maxammo - ammo);
                    ammo += transferredAmmo;
                    currentCaliber = ammoBoxComponent.bulletType;
                    ammoBoxComponent.amount -= transferredAmmo;
                }
                else
                {
                    Debug.Log(gameObject + " is already fully loaded or ammo box has no ammo");
                }
            }
            else
            {
                Debug.Log(ammoBox.name + " has wrong caliber than " + gameObject.name + ": " + ammoBoxComponent.bulletType.caliber + " vs " + caliber);
            }
        }
        else
        {
            Debug.Log(ammoBox.name + " does not have bullet component");
        }
    }

    public GameObject UnloadAmmo()
    {
        if (currentCaliber != null && ammo > 0)
        {
            GameObject ammoBox = GetComponent<Item>().game.CreateItem(currentCaliber.caliber);
            if (ammoBox != null)
            {
                AmmoBox ammoBoxComponent = ammoBox.GetComponent<AmmoBox>();
                ammoBoxComponent.amount = Mathf.Clamp(ammoBoxComponent.bulletType.box, 1, ammo);
                ammo -= ammoBox.GetComponent<AmmoBox>().amount;
                if (ammo <= 0)
                    currentCaliber = null;
            }
            return ammoBox;
        }
        return null;
    }

    public float GetAmmoWeight()
    {
        float finalWeight = 0f;
        if (currentCaliber != null)
            finalWeight += ammo * currentCaliber.weight;
        return finalWeight;
    }
}