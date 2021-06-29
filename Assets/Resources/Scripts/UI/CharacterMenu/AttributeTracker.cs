using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AttributeTracker : MonoBehaviour
{
    private CharacterMenu characterMenu;
    private Text attributeNameDisplay;
    private Text attributeText;
    private GameObject attributeIncreaseButtonObject;
    private Button attributeIncreaseButton;
    private GameObject attributeDecreaseButtonObject;
    private Button attributeDecreaseButton;

    public string trackedAttributeName;
    public int pointsInvested = 0;
    public int multiplier = 1;
    public bool increaseWithMax = false;
    public bool limitedByLevel = false;

    private void Start()
    {
        characterMenu = transform?.parent?.parent?.GetComponent<CharacterMenu>();
        attributeNameDisplay = transform.GetChild(0).GetComponent<Text>();
        attributeText = transform.GetChild(1).GetChild(0).GetComponent<Text>();
        attributeIncreaseButtonObject = transform.GetChild(3).gameObject;
        attributeDecreaseButtonObject = transform.GetChild(2).gameObject;
        attributeIncreaseButton = attributeIncreaseButtonObject.GetComponent<Button>();
        attributeDecreaseButton = attributeDecreaseButtonObject.GetComponent<Button>();
        attributeIncreaseButtonObject.AddComponent<IncreaseAttributeButton>().parentAttribute = this;
        attributeDecreaseButtonObject.AddComponent<DecreaseAttributeButton>().parentAttribute = this;
        attributeIncreaseButton.GetComponent<RectTransform>().Rotate(new Vector3(0, -180, 0));
        attributeIncreaseButton.GetComponent<RectTransform>().localScale = new Vector3(-0.8f, 0.8f, 1);
        //attributeIncreaseButton.onClick.AddListener(delegate { IncreaseAttribute(); });
        //attributeDecreaseButton.onClick.AddListener(delegate { DecreaseAttribute(); });
        trackedAttributeName = gameObject.name;
        attributeNameDisplay.text = trackedAttributeName;
        attributeIncreaseButtonObject.SetActive(false);
        attributeDecreaseButtonObject.SetActive(false);
    }

    public void IncreaseAttribute()
    {
        Attribute characterAttribute = characterMenu.player.GetAttribute(trackedAttributeName);
        if (!increaseWithMax)
            if (characterAttribute.Value + pointsInvested * multiplier >= characterAttribute.MaxValue)
                return;
        if (limitedByLevel)
            if (characterAttribute.Value + pointsInvested * multiplier >= characterMenu.player.Level + 16 )
                return;
        if (characterMenu.TakeCharacterPoint())
        {
            if (pointsInvested == 0)
                characterMenu.changedAttributes.Add(this);
            pointsInvested++;
        }
    }

    public void DecreaseAttribute()
    {
        if (characterMenu.ReturnCharacterPoint())
        {
            if (pointsInvested > 0)
                pointsInvested--;
            if (pointsInvested <= 0)
                characterMenu.changedAttributes.Remove(this);
        }
    }

    private void Update()
    {
        attributeText.text = "";
        if (characterMenu.AttributeChangeEnabled)
        {
            if (characterMenu.player.freeCharacterPoints > 0)
                attributeIncreaseButtonObject.SetActive(true);
            attributeDecreaseButtonObject.SetActive(pointsInvested > 0);
        }
        else
        {
            attributeIncreaseButtonObject.SetActive(false);
            attributeDecreaseButtonObject.SetActive(pointsInvested > 0);
        }
        if (characterMenu.player != null)
        {
            Attribute characterAttribute = characterMenu.player.GetAttribute(trackedAttributeName);
            attributeText.text += ((int)characterAttribute.Value).ToString();
            if (pointsInvested > 0)
                attributeText.text += "+" + (pointsInvested * multiplier).ToString();
            if (!increaseWithMax)
                attributeText.text += "/" + ((int)characterAttribute.MaxValue).ToString();
            else
                attributeText.text += "/" + ((int)characterAttribute.MaxValue + pointsInvested * multiplier).ToString();
        }
    }
}