using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kit : MonoBehaviour
{
    public Item ItemComponent { get { return GetComponent<Item>(); } }
    public Attribute Condition { get { return GetComponent<ObjectAttributes>().GetAttribute("Item condition"); } }

    public void DecreaseCondition(float amount = 1)
    {
        Attribute.ChangeResult result = Condition.Modify(-amount);
        if (result == Attribute.ChangeResult.BelowMin)
        {
            if (ItemComponent.game.characterController.ControlledCharacter == ItemComponent.GetOwner())
                ItemComponent.game.UpdateLog(ItemComponent.displayName + " used up.");
            Destroy(gameObject);
            //ItemComponent.GetOwner().GetComponent<Character>().usingKit = false;
            //ItemComponent.GetOwner().GetComponent<Character>().waitingTurns = 0;
        }
    }
}