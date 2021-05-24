using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    public Bullet bulletType;
    public int amount;

    public void TransferAmmoFromAnotherBox(GameObject ammoBox)
    {
        AmmoBox anotherAmmoBox = ammoBox.GetComponent<AmmoBox>();
        if (anotherAmmoBox)
        {
            if (anotherAmmoBox.bulletType.caliber == bulletType.caliber)
            {
                int transferredAmmo = Mathf.Min(anotherAmmoBox.amount, bulletType.box - amount);
                anotherAmmoBox.amount -= transferredAmmo;
                amount += transferredAmmo;
            }
            else
                Debug.Log(anotherAmmoBox + " has different ammo type than " + gameObject);
        }
    }

    public float GetAmmoWeight()
    {
        float finalWeight = 0f;
        if (bulletType != null)
            finalWeight += amount * bulletType.weight;
        return finalWeight;
    }
}