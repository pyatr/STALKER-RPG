using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Armor : MonoBehaviour
{
    public Dictionary<DamageTypes, int> resistances = new Dictionary<DamageTypes, int>();

    public void AbsorbDamage(Damage damage)
    {
        switch (damage.GetDamageType())
        {
            case DamageTypes.Bullet:
                BulletDamage bulletDamage = damage as BulletDamage;
                int difference = resistances[DamageTypes.Bullet] - (int)bulletDamage.bulletStats.penetration;
                if (difference > 0)
                {
                    Bullet newBulletDamage = bulletDamage.bulletStats.ShallowCopy();
                    //Debug.Log(resistances[DamageTypes.Bullet] + "/" + newBulletDamage.penetration + "/" + difference + "/" + newBulletDamage.damage);
                    newBulletDamage.damage -= newBulletDamage.damage * difference / 10;
                    bulletDamage.bulletStats = newBulletDamage;
                }
                break;
            default:
                
                break;
        }
    }
}