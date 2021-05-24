using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medkit : Kit
{
    public void OnUse(bool inCombat = false)
    {
        Character owner = ItemComponent.GetOwner().GetComponent<Character>();
        if (owner != null)
        {
            float bonus = 1;
            inCombat = owner.IsInCombat();
            if (!inCombat)
                bonus = 4;
            float healthToRestore = owner.GetAttribute("Medical").GetValue() / 4 * bonus;
            float missingHealth = owner.GetAttribute("Health").maxValue - owner.Health;
            healthToRestore = Mathf.Min(healthToRestore, missingHealth);
            Attribute.ChangeResult result = owner.Heal(healthToRestore);
            if (result == Attribute.ChangeResult.AboveMax)
            {
                if (owner.IsPlayer())
                {
                    owner.game.UpdateLog("You are fully healed.");
                    //owner.waitingTurns = 0;
                    //owner.usingKit = false;
                }
            }
            DecreaseCondition(healthToRestore);
        }
    }
}