using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIslot : MonoBehaviour
{
    enum SlotCounter
    {
        LowerLeft,
        LowerRight,
        UpperLeft,
        UpperRight
    }

    public ItemSlot pocket;
    public InventoryManager inventoryManager;
    public Game game;

    float counterScale = 0.7f;
    Dictionary<SlotCounter, GameObject> counters = new Dictionary<SlotCounter, GameObject>();

    public void Create(InventoryManager inventoryManager, Game game)
    {
        this.inventoryManager = inventoryManager;
        this.game = game;
        AddCounterToSlot("CounterBottomRight", SlotCounter.LowerRight);
        AddCounterToSlot("CounterBottomLeft", SlotCounter.LowerLeft);
        AddCounterToSlot("CounterTopRight", SlotCounter.UpperRight);
        AddCounterToSlot("CounterTopLeft", SlotCounter.UpperLeft);
    }

    void SetSlotImage(Image slotImage, Sprite sprite)
    {
        GameObject slotImageObject = slotImage.gameObject;//slot.transform.Find("ContentsDisplay").gameObject;
        slotImageObject.SetActive(true);
        slotImage.sprite = sprite;
        RectTransform rectTransform = slotImageObject.transform.parent.GetComponent<RectTransform>();
        float scale = 1.0f;
        scale = Mathf.Min(rectTransform.sizeDelta.x / slotImage.sprite.texture.width, rectTransform.sizeDelta.y / slotImage.sprite.texture.height);
        slotImageObject.GetComponent<RectTransform>().sizeDelta = new Vector2(slotImage.sprite.texture.width * scale * 0.8f, slotImage.sprite.texture.height * scale * 0.8f);
    }

    void ChangeSlotText(string text)
    {
        Transform t = transform.Find("Text");
        if (t != null)
            t.GetComponent<Text>().text = text;
    }

    public void UpdateSlot()
    {
        GameObject item = pocket.GetItem();
        Image slotImage = transform.Find("ContentsDisplay").gameObject.GetComponent<Image>();
        foreach (KeyValuePair<SlotCounter, GameObject> counter in counters)
            UpdateSlotCounter(counter.Key, counter.Value);
        if (item != null)
        {
            if (item.GetComponent<Item>().sprite != null)
                SetSlotImage(slotImage, item.GetComponent<Item>().sprite);
            ChangeSlotText(item.GetComponent<Item>().displayName);
        }
        else
        {
            Sprite sprite = null;
            foreach (InfoBlock slotTemplate in game.slots.subBlocks)
                if (slotTemplate.name == GetComponent<UIslot>().pocket.slotType)
                    foreach (KeyValuePair<string, string> kvp in slotTemplate.namesValues)
                        switch (kvp.Key)
                        {
                            case "image": sprite = Resources.Load<Sprite>("Graphics/Items/Slots/" + kvp.Value); break;
                        }
            if (sprite != null)
                SetSlotImage(slotImage, sprite);
            else
                slotImage.gameObject.SetActive(false);
            ChangeSlotText("");
        }
    }

    GameObject AddCounterToSlot(string name, SlotCounter counterType, int maxDigits = 3)
    {
        if (transform.Find(name) != null)
        {
            Debug.Log(transform.parent.name + " already has " + name);
            return null;
        }
        GameObject counter = new GameObject(name) { layer = 5 };
        counter.transform.localScale = new Vector3(counterScale, counterScale, counterScale);
        counter.transform.SetParent(transform);
        Text counterText = counter.AddComponent<Text>();
        counterText.text = "";
        counterText.font = game.defaultFont;//(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        counterText.color = new Color32(222, 222, 222, 255);
        counterText.text = "0";
        counterText.alignment = TextAnchor.MiddleRight;
        int fontSize = 11;
        counterText.fontSize = fontSize;
        counterText.resizeTextMaxSize = fontSize;
        counterText.verticalOverflow = VerticalWrapMode.Overflow;
        Vector2 anchor = new Vector2((int)counterType % 2, (int)counterType / 2);
        RectTransform rectTransform = counter.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(8 * maxDigits, 16);
        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = anchor;
        rectTransform.anchoredPosition = new Vector2(0 - 16 * anchor.x, 0);
        rectTransform.localScale = Vector3.one;
        GameObject counterIcon = new GameObject("Icon") { layer = 5 };
        counterIcon.transform.SetParent(rectTransform.transform);
        Image counterIconImage = counterIcon.AddComponent<Image>();
        RectTransform iconTransform = counterIcon.GetComponent<RectTransform>();
        iconTransform.anchorMin = new Vector2(anchor.x, 0);
        iconTransform.anchorMax = new Vector2(anchor.x, 0);
        iconTransform.pivot = new Vector2(0, 0);
        iconTransform.sizeDelta = new Vector2(16, 16);
        iconTransform.anchoredPosition = new Vector2(24 - 24 * anchor.x, 0/*2 - 4 * anchor.y*/);
        iconTransform.localScale = Vector3.one;
        counterIconImage.raycastTarget = false;
        counterText.enabled = false;
        counterIconImage.enabled = false;
        counters.Add(counterType, counter);
        return counter;
    }

    Image GetCounterIcon(GameObject counter)
    {
        return counter.transform.GetChild(0).GetComponent<Image>();
    }

    Text GetCounterText(GameObject counter)
    {
        return counter.GetComponent<Text>();
    }

    void DisableCounter(GameObject counter)
    {
        GetCounterIcon(counter).enabled = false;
        GetCounterText(counter).enabled = false;
    }

    void EnableCounterIcon(GameObject counter)
    {
        GetCounterIcon(counter).enabled = true;
    }

    void EnableCounterText(GameObject counter)
    {
        GetCounterText(counter).enabled = true;
        GetCounterText(counter).color = new Color32(222, 222, 222, 255);
    }

    void UpdateSlotCounter(SlotCounter counterType, GameObject counter)
    {
        ItemSlot currentPocket = GetComponent<UIslot>().pocket;
        GameObject objectInSlot = currentPocket.GetItem();
        if (counter != null && objectInSlot != null)
        {
            Firearm firearmComponent = objectInSlot.GetComponent<Firearm>();
            Magazine magazineComponent = objectInSlot.GetComponent<Magazine>();
            AmmoBox ammoBoxComponent = objectInSlot.GetComponent<AmmoBox>();
            LBEgear LBEcomponent = objectInSlot.GetComponent<LBEgear>();
            switch (counterType)
            {
                case SlotCounter.LowerRight:
                    EnableCounterText(counter);
                    EnableCounterIcon(counter);
                    if (firearmComponent)
                    {
                        if (firearmComponent.magazine != null)
                        {
                            GetCounterText(counter).text = firearmComponent.magazine.GetComponent<Magazine>().ammo.ToString();
                            if (firearmComponent.magazine.GetComponent<Magazine>().currentCaliber != null)
                                GetCounterText(counter).color = firearmComponent.magazine.GetComponent<Magazine>().currentCaliber.textColor;
                            GetCounterIcon(counter).sprite = game.icons.ammoIcon;
                            return;
                        }
                        else
                        {
                            GetCounterIcon(counter).sprite = game.icons.nomagazineIcon;
                            GetCounterText(counter).text = "0";
                            GetCounterText(counter).enabled = false;
                            return;
                        }
                    }
                    if (magazineComponent)
                    {
                        GetCounterText(counter).text = magazineComponent.ammo.ToString();
                        if (magazineComponent.currentCaliber != null)
                            GetCounterText(counter).color = magazineComponent.currentCaliber.textColor;
                        GetCounterIcon(counter).sprite = game.icons.ammoIcon;
                        return;
                    }
                    if (ammoBoxComponent)
                    {
                        GetCounterText(counter).text = ammoBoxComponent.amount.ToString();
                        GetCounterText(counter).color = ammoBoxComponent.bulletType.textColor;
                        GetCounterIcon(counter).sprite = game.icons.ammoBoxIcon;
                        return;
                    }
                    if (LBEcomponent && (name == "Vest" || name == "Backpack" || name == "Belt" || name.Contains("ThighRig") || name.Contains("OutsideSlot"))) 
                    {
                        int occupiedPockets = LBEcomponent.GetItemCount();
                        GetCounterText(counter).text = occupiedPockets.ToString();
                        GetCounterIcon(counter).sprite = game.icons.pocketIcon;
                        return;
                    }
                    break;
                case SlotCounter.LowerLeft:
                    string slotType = currentPocket.slotType;
                    if (currentPocket.GetItem().GetComponent<Item>().slots.ContainsKey(slotType))
                    {
                        int maxFit = currentPocket.GetItem().GetComponent<Item>().slots[slotType];
                        if (maxFit > 1)
                        {
                            EnableCounterText(counter);
                            GetCounterText(counter).text = currentPocket.transform.childCount.ToString() + "/" + maxFit;
                        }
                    }
                    return;
                case SlotCounter.UpperRight:
                    if (firearmComponent)
                    {
                        string[] fireModes = { "MANUAL", "SEMI", "BURST", "AUTO" };
                        EnableCounterText(counter);
                        GetCounterText(counter).text = fireModes[(int)firearmComponent.GetFireMode().fireMode];
                        GetCounterText(counter).color = Color.red;
                        return;
                    }
                    //Scopes
                    break;
                case SlotCounter.UpperLeft:
                    if (LBEcomponent && name == "Vest" || name == "Backpack" || name == "Belt" || name.Contains("ThighRig"))
                    {
                        if (LBEcomponent.GetComponent<ItemExtension>())
                        {
                            EnableCounterText(counter);
                            GetCounterText(counter).text = "E";
                            GetCounterText(counter).color = Color.green;
                        }
                        return;
                    }
                    break;
            }
        }
        DisableCounter(counter);
    }

    void Update()
    {
        UpdateSlot();
    }
}