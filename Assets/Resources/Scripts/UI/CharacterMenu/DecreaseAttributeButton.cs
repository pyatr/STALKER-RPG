using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DecreaseAttributeButton : MonoBehaviour, IPointerClickHandler
{
    public AttributeTracker parentAttribute;

    public void OnPointerClick(PointerEventData eventData)
    {
        parentAttribute.DecreaseAttribute();
    }
}