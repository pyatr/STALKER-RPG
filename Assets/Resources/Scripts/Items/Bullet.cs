using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet
{
    public string caliber;
    public int box = 20;
    public int distance = 20;
    public float penetration = 8;
    public float tumble = 1;
    public float damage = 20;
    public int bulletsPerShot = 1;
    public Color32 textColor = new Color32(255, 255, 255, 255);
    public bool tracer = false;
    public float weight = 0;

    public Bullet ShallowCopy()
    {
        return (Bullet)MemberwiseClone();
    }
}