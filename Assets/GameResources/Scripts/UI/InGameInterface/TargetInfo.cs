using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetInfo : MonoBehaviour
{
    private Transform targetSprite;
    private Transform targetName;
    private Transform targetWeapon;
    private Transform targetHealth;
    private Transform targetFaction;
    private Image backgroundImage;
    private World world;

    public void Start()
    {
        targetSprite = transform.Find("TargetSprite");
        targetName = targetSprite.GetChild(0);
        targetWeapon = transform.Find("TargetWeapon");
        targetHealth = transform.Find("TargetHealth");
        targetFaction = transform.Find("TargetFaction");
        backgroundImage = GetComponent<Image>();
        world = World.GetInstance();
    }

    public void SetTargetDisplay(Character character)
    {
        if (targetWeapon == null)
            return;
        targetWeapon.GetComponent<RectTransform>().sizeDelta = new Vector2(68, 34);
        if (character)
        {
            Character player = world?.Player.GetComponent<Character>();
            targetSprite.gameObject.SetActive(true);
            targetName.gameObject.SetActive(true);
            targetHealth.gameObject.SetActive(true);
            targetFaction.gameObject.SetActive(true);
            backgroundImage.enabled = true;
            GameObject targetWeaponObject = character.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
            if (targetWeaponObject != null && targetWeaponObject.GetComponent<Item>().Sprite)
            {
                targetWeapon.gameObject.SetActive(true);
                targetWeapon.GetChild(0).GetComponent<Text>().text = targetWeaponObject.GetComponent<Item>().displayName;
                Image slotImage = targetWeapon.GetComponent<Image>();
                slotImage.sprite = targetWeaponObject.GetComponent<Item>().Sprite;
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
            targetFaction.GetComponent<Text>().color = Color.yellow;
            if (player != null)
            {
                if (player.hostileTowards.Contains(character.faction) || character.hostileTowards.Contains(player.faction))
                    targetFaction.GetComponent<Text>().color = Color.red;
                if (player.friendlyTowards.Contains(character.faction) || character.friendlyTowards.Contains(player.faction))
                    targetFaction.GetComponent<Text>().color = Color.green;
            }
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