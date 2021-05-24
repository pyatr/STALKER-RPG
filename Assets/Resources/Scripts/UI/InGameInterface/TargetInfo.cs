using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetInfo : MonoBehaviour
{
    Transform targetSprite;
    Transform targetName;
    Transform targetWeapon;
    Transform targetHealth;
    Transform targetFaction;
    Image backgroundImage;

    public void Start()
    {
        targetSprite = transform.Find("TargetSprite");
        targetName = targetSprite.GetChild(0);
        targetWeapon = transform.Find("TargetWeapon");
        targetHealth = transform.Find("TargetHealth");
        targetFaction = transform.Find("TargetFaction");
        backgroundImage = GetComponent<Image>();
    }

    public void SetTargetDisplay(Character character)
    {
        targetWeapon.GetComponent<RectTransform>().sizeDelta = new Vector2(68, 34);
        if (character)
        {
            targetSprite.gameObject.SetActive(true);
            targetName.gameObject.SetActive(true);
            targetWeapon.gameObject.SetActive(true);
            targetHealth.gameObject.SetActive(true);
            targetFaction.gameObject.SetActive(true);
            backgroundImage.enabled = true;
            GameObject targetWeaponObject = character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
            if (targetWeaponObject != null)
            {
                targetWeapon.GetChild(0).GetComponent<Text>().text = targetWeaponObject.GetComponent<Item>().displayName;
                Image slotImage = targetWeapon.GetComponent<Image>();
                slotImage.sprite = targetWeaponObject.GetComponent<Item>().sprite;
                RectTransform rectTransform = targetWeapon.GetComponent<RectTransform>();
                float scale = 1.0f;
                scale = Mathf.Min(rectTransform.sizeDelta.x / slotImage.sprite.texture.width, rectTransform.sizeDelta.y / slotImage.sprite.texture.height);
                targetWeapon.GetComponent<RectTransform>().sizeDelta = new Vector2(slotImage.sprite.texture.width * scale, slotImage.sprite.texture.height * scale);
                Vector2 sizeDelta = targetWeapon.GetComponent<RectTransform>().sizeDelta;
                targetWeapon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-sizeDelta.x / 2, sizeDelta.y / 2);
            }
            targetName.GetComponent<Text>().text = character.displayName;
            character.GetComponent<ObjectAttributes>().GetAttribute("Health").WriteToText(targetHealth.GetComponent<Text>());
            targetFaction.GetComponent<Text>().text = character.faction;
            targetSprite.GetComponent<Image>().sprite = character.GetComponent<SpriteRenderer>().sprite;
        }
        else
        {
            targetSprite.gameObject.SetActive(false);
            targetName.gameObject.SetActive(false);
            targetWeapon.gameObject.SetActive(false);
            targetHealth.gameObject.SetActive(false);
            targetFaction.gameObject.SetActive(false);
            backgroundImage.enabled = false;
        }
    }
}