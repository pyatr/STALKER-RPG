using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInfo : MonoBehaviour
{    
    private Transform weaponImage;
    private Transform ammoCounter;
    private Text weaponName;
    private Text ammoCounterText;
    private Text weaponFireModeText;
    private World world;

    private void Start()
    {
        weaponImage = transform.Find("Image");
        ammoCounter = transform.Find("AmmoCounter");
        weaponName = transform.Find("WeaponName").GetComponent<Text>();
        ammoCounterText = ammoCounter.GetChild(0).GetComponent<Text>();
        weaponFireModeText = transform.Find("FireMode").GetComponent<Text>();
        world = World.GetInstance();
    }

    private void Update()
    {
        if (world.Player != null)
        {
            Character characterComponent = world.Player.GetComponent<Character>();
            GameObject equippedItem = characterComponent.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Weapon);
            if (equippedItem != null)
            {
                weaponImage.gameObject.SetActive(true);
                ammoCounter.gameObject.SetActive(true);
                weaponName.gameObject.SetActive(true);
                ammoCounterText.gameObject.SetActive(true);
                weaponFireModeText.gameObject.SetActive(true);
                Firearm playerWeaponFirearmComponent = equippedItem.GetComponent<Firearm>();
                weaponName.text = equippedItem.GetComponent<Item>().displayName;
                Image slotImage = weaponImage.GetComponent<Image>();
                slotImage.sprite = equippedItem.GetComponent<Item>().Sprite;
                RectTransform rectTransform = GetComponent<RectTransform>();
                float scale = 1.0f;
                scale = Mathf.Min(rectTransform.sizeDelta.x / slotImage.sprite.texture.width, rectTransform.sizeDelta.y / slotImage.sprite.texture.height);
                scale *= 0.8f;
                weaponImage.GetComponent<RectTransform>().sizeDelta = new Vector2(slotImage.sprite.texture.width * scale, slotImage.sprite.texture.height * scale);
                Vector2 sizeDelta = weaponImage.GetComponent<RectTransform>().sizeDelta;
                weaponImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(-sizeDelta.x / 2, sizeDelta.y / 2);
                ammoCounter.GetComponent<Image>().enabled = false;
                ammoCounterText.text = "";
                if (playerWeaponFirearmComponent)
                {
                    ammoCounter.GetComponent<Image>().enabled = true;
                    if (playerWeaponFirearmComponent.magazine != null)
                    {
                        ammoCounter.GetComponent<Image>().sprite = Game.Instance.Icons.ammoIcon;
                        ammoCounterText.text = playerWeaponFirearmComponent.magazine.GetComponent<Magazine>().ammo.ToString();
                        if (playerWeaponFirearmComponent.magazine.GetComponent<Magazine>().currentCaliber != null)
                            ammoCounterText.color = playerWeaponFirearmComponent.magazine.GetComponent<Magazine>().currentCaliber.textColor;
                        string[] fireModes = { "MANUAL", "SEMI", "BURST", "AUTO" };
                        weaponFireModeText.text = fireModes[(int)playerWeaponFirearmComponent.GetFireMode().fireMode];
                    }
                    else
                    {
                        ammoCounter.GetComponent<Image>().sprite = Game.Instance.Icons.nomagazineIcon;
                        ammoCounterText.text = "";
                        weaponFireModeText.text = "";
                    }
                }
                else
                    weaponFireModeText.text = "";
                return;
            }
        }
        weaponImage.gameObject.SetActive(false);
        ammoCounter.gameObject.SetActive(false);
        weaponName.gameObject.SetActive(false);
        ammoCounterText.gameObject.SetActive(false);
        weaponFireModeText.gameObject.SetActive(false);
    }
}