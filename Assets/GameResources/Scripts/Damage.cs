using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageTypes
{
    Unknown,
    Bullet,
    Blunt,
    Tear,
    Chemical,
    Fire,
    Psychic,
    Radiation,
    Electricity,
    Explosion
}

public class Damage
{
    public float amount;
    public float modifier = 1f;
    public GameObject source;

    public virtual float Amount()
    {
        return amount;
    }

    public virtual DamageTypes GetDamageType()
    {
        return DamageTypes.Unknown;
    }

    public virtual void Apply(GameObject gameObject)
    {

    }
}

public class BulletDamage : Damage
{
    public Bullet bulletStats;
    public float gunDamageModifier = 0f;

    public BulletDamage(Bullet bulletStats, float gunDamageModifier)
    {
        this.bulletStats = bulletStats.ShallowCopy();
        this.bulletStats.damage -= (int)gunDamageModifier;
        //this.gunDamageModifier = gunDamageModifier;
    }

    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Bullet;
    }

    public override void Apply(GameObject objectHit)
    {
        if (objectHit.GetComponent<ObjectAttributes>())
        {
            objectHit.GetComponent<ObjectAttributes>().ModAttribute("Health", -Amount());
            Character characterComponent = objectHit.GetComponent<Character>();
            if (characterComponent)
            {
                characterComponent.brain.turnsSinceLastAttack = 0;
                string sourceName = "";
                if (source != null)
                    if (source.GetComponent<Character>())
                        sourceName = " from " + source.GetComponent<Character>().displayName;
                float amount = Mathf.Max(0, Amount());
                if (Game.Instance.PointIsOnScreen(characterComponent.transform.position))
                    characterComponent.world.UpdateLog(objectHit.GetComponent<Character>().displayName + " takes " + amount + " damage" + sourceName);
            }
            base.Apply(objectHit);
        }
    }

    public override float Amount()
    {
        if (bulletStats != null)
            return (bulletStats.damage + gunDamageModifier) * bulletStats.tumble * modifier;
        else
            return 0f;
    }
}

public class BluntDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Blunt;
    }
}

public class TearDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Tear;
    }
}

public class ChemicalDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Chemical;
    }
}

public class FireDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Fire;
    }
}

public class PsychicDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Psychic;
    }
}

public class RadiationDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Radiation;
    }
}

public class ElectricDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Electricity;
    }
}

public class ExplosionDamage : Damage
{
    public override DamageTypes GetDamageType()
    {
        return DamageTypes.Explosion;
    }
}