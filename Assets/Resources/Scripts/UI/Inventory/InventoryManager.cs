using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;
using System;

public class InventoryManager : MonoBehaviour
{
    public enum InventoryCellSize
    {
        Small,
        Medium,
        Large
    }

    public GameObject itemHeld = null;
    public GameObject lastSlot = null;
    public GameObject itemViewed = null;
    public bool open = false;

    public static readonly int itemPageWidth = 4;
    public static readonly int itemPageHeight = 8;

    public List<GameObject> panels = new List<GameObject>();
    public Dictionary<string, Text> textPanels = new Dictionary<string, Text>();

    Game game;
    ItemPile pileUnderCharacter;
    int currentItemPage = 1;

    GameObject character { get { return game.characterController.ControlledCharacter; } }

    public static int ItemPageSlotCount { get { return itemPageWidth * itemPageHeight; } }

    GameObject CreateItemSlot(Transform parent, Vector2 position, string slotName, string slotType, GameObject pocket, InventoryCellSize size = InventoryCellSize.Small)
    {
        GameObject slot = new GameObject(slotName) { layer = 5 };
        slot.transform.SetParent(parent);
        UIslot slotComponent = slot.AddComponent<UIslot>();
        slotComponent.pocket = pocket.GetComponent<ItemSlot>();
        pocket.GetComponent<ItemSlot>().slotInInventory = slot.GetComponent<UIslot>();
        RectTransform rectTransform = slot.AddComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = new Vector2(64 + 32 * (int)size, 64);
        rectTransform.anchoredPosition = new Vector2(position.x * (rectTransform.sizeDelta.x + 4), -position.y * (64 + 4));
        Image slotBG = slot.AddComponent<Image>();
        slotBG.sprite = Resources.Load<Sprite>("Graphics/Items/Slots/inventory_cell");
        slotBG.type = Image.Type.Tiled;
        GameObject slotItemImage = new GameObject("ContentsDisplay") { layer = 5 };
        slotItemImage.transform.SetParent(slot.transform);
        RectTransform slotItemImageTransform = slotItemImage.AddComponent<RectTransform>();
        slotItemImageTransform.pivot = new Vector2(0.5f, 0.5f);
        slotItemImageTransform.anchoredPosition = new Vector2(0, 0);
        slotItemImageTransform.localScale = Vector3.one;
        slotItemImage.AddComponent<Image>();
        slotItemImage.SetActive(false);
        slotComponent.Create(this, game);
        slotComponent.UpdateSlot();
        return slot;
    }

    GameObject CreateBuiltinSlot(Transform parent, Vector2 position, string slotName, BuiltinCharacterSlots slotType)
    {
        string slotTypeName = Enum.GetName(typeof(BuiltinCharacterSlots), slotType);
        GameObject slot = CreateItemSlot(parent, position, slotName, slotTypeName, character.GetComponent<Character>().GetSlot(slotType), InventoryCellSize.Large);
        GameObject itemInSlot = character.GetComponent<Character>().GetItemFromBuiltinSlot(slotType);
        if (itemInSlot != null)
            LoadSlotsFromLBE(slot);
        return slot;
    }

    GameObject CreateSlotWithNamePanel(Transform parent, Vector2 position, string slotName, string slotType, GameObject pocket, InventoryCellSize size = InventoryCellSize.Small)
    {
        GameObject slot = CreateItemSlot(parent, position, slotName, slotType, pocket, size);
        AddTextPanelToSlot(slot);
        return slot;
    }

    void AddTextPanelToSlot(GameObject slot)
    {
        RectTransform slotRectTransform = slot.GetComponent<RectTransform>();
        float yPos = slotRectTransform.anchoredPosition.y / slotRectTransform.sizeDelta.y;
        slotRectTransform.anchoredPosition = new Vector2(slotRectTransform.anchoredPosition.x, slotRectTransform.anchoredPosition.y + yPos * 16);
        GameObject namePanel = new GameObject("Text") { layer = 5 };
        AddTextToObject(namePanel);
        namePanel.transform.SetParent(slot.transform);
        RectTransform nameTransform = namePanel.GetComponent<RectTransform>();
        nameTransform.sizeDelta = new Vector2(128, 16);
        nameTransform.pivot = new Vector2(0.5f, 0.5f);
        nameTransform.anchoredPosition = new Vector2(8, -40);
        nameTransform.localScale = Vector3.one;
    }

    void AddTextPanelToCategory(GameObject category, string text)
    {
        GameObject namePanel = new GameObject("Text") { layer = 5 };
        AddTextToObject(namePanel).text = text;
        namePanel.transform.SetParent(category.transform);
        RectTransform nameTransform = namePanel.GetComponent<RectTransform>();
        nameTransform.sizeDelta = new Vector2(128, 16);
        nameTransform.pivot = new Vector2(0f, 0.5f);
        nameTransform.anchoredPosition = new Vector2(8, 8);
        nameTransform.localScale = Vector3.one;
    }

    Text AddTextToObject(GameObject UIobject)
    {
        Text text = UIobject.AddComponent<Text>();
        text.text = "";
        text.font = game.defaultFont;//(Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
        text.color = new Color32(222, 222, 222, 255);
        text.fontSize = 10;
        //text.resizeTextForBestFit = true;
        return text;
    }

    GameObject AddSlotCategoryToPanel(Transform parent, Vector2 position, string name)
    {
        GameObject slotCategory = new GameObject(name) { layer = 5 };
        slotCategory.transform.SetParent(parent);
        RectTransform slotCategoryTransform = slotCategory.AddComponent<RectTransform>();
        //Image categoryBorders = slotCategory.AddComponent<Image>();
        //categoryBorders.sprite = Resources.Load<Sprite>("Graphics/Items/Slots/inventory_cell");
        //categoryBorders.type = Image.Type.Tiled;
        //categoryBorders.color = new Color(16, 12, 43);
        //categoryBorders.fillCenter = false;
        slotCategoryTransform.pivot = new Vector2(0, 1);
        slotCategoryTransform.anchorMin = new Vector2(0, 1);
        slotCategoryTransform.anchorMax = new Vector2(0, 1);
        slotCategoryTransform.offsetMin = new Vector2(8 + position.x * 64, -1 * (8 + position.y * 64));
        slotCategoryTransform.offsetMax = slotCategoryTransform.offsetMin;
        slotCategoryTransform.localScale = new Vector3(game.UIscale, game.UIscale, game.UIscale);
        return slotCategory;
    }

    bool RemoveItemFromSlot(GameObject slot)
    {
        if (slot.GetComponent<UIslot>().pocket.transform.childCount == 0)
            return true;
        GameObject item = slot.GetComponent<UIslot>().pocket.transform.GetChild(0).gameObject;
        Item itemComponent = item.GetComponent<Item>();
        int extensionsCount = itemComponent.extensions.Count;
        for (int i = 0; i < extensionsCount; i++)
        {
            if (itemComponent.extensions[i] != null)
            {
                GameObject extension = itemComponent.extensions[i].GetComponent<Item>().GetSlot().slotInInventory.gameObject;
                LBEgear extensionLBEcomponent = itemComponent.extensions[i].GetComponent<Item>().GetComponent<LBEgear>();
                if (extensionLBEcomponent)
                {
                    List<GameObject> itemsToTransfer = extensionLBEcomponent.GetAllItems();
                    List<GameObject> primarySlots = itemComponent.extensions[i].GetComponent<ItemExtension>().extensionOf.GetComponent<LBEgear>().GetAllSlots();
                    foreach (GameObject slotInLBE in primarySlots)
                        foreach (GameObject itemToTransfer in itemsToTransfer)
                            if (slotInLBE.name == itemToTransfer.GetComponent<Item>().GetSlot().gameObject.name)
                                slotInLBE.GetComponent<ItemSlot>().PlaceItem(itemToTransfer);
                }
                RemoveItemFromSlot(extension);
            }
        }
        for (int i = 0; i < extensionsCount; i++)
            Destroy(itemComponent.extensions[i]);

        itemComponent.extensions.Clear();
        //itemComponent.extensions.RemoveAll(itemObject => itemObject == null);
        item.transform.SetParent(gameObject.transform);
        switch (slot.name)
        {
            case "Vest":
                for (int i = 2; i < slot.transform.parent.childCount; i++)
                    Destroy(slot.transform.parent.GetChild(i).gameObject);
                break;
            case "Belt":
                for (int i = 2; i < slot.transform.parent.childCount; i++)
                    Destroy(slot.transform.parent.GetChild(i).gameObject);
                break;
            case "Backpack":
                for (int i = 2; i < slot.transform.parent.childCount; i++)
                    Destroy(slot.transform.parent.GetChild(i).gameObject);
                break;
        }
        return true;
    }

    bool PlaceItemInSlot(GameObject slot, GameObject item)
    {
        bool placedSuccessfully = false;
        if (item != null)
        {
            placedSuccessfully = slot.GetComponent<UIslot>().pocket.PlaceItem(item);
            if (placedSuccessfully)
            {
                if (item.GetComponent<LBEgear>())
                    LoadSlotsFromLBE(slot);
                for (int i = 0; i < item.GetComponent<Item>().extensions.Count; i++)
                {
                    GameObject itemExtension = item.GetComponent<Item>().extensions[i];
                    string extensionSlotName = itemExtension.GetComponent<Item>().GetSlot().slotInInventory.name;
                    if (itemExtension.GetComponent<LBEgear>())
                        if (extensionSlotName == "Vest" || extensionSlotName == "Backpack" || extensionSlotName == "Belt" || extensionSlotName.Contains("ThighRig"))
                            if (itemExtension.GetComponent<Item>().GetSlot().slotInInventory.transform.parent.childCount == 2)
                                LoadSlotsFromLBE(itemExtension.GetComponent<Item>().GetSlot().slotInInventory.gameObject);
                }
            }
        }
        else
            Debug.Log("Could not place item to " + slot.name + ": item is null");
        return placedSuccessfully;
    }

    public void LoadSlotsFromLBE(GameObject slot)
    {
        GameObject item = slot.GetComponent<UIslot>().pocket.GetItem();
        if (item != null && slot != null)
        {
            if (item.GetComponent<LBEgear>())
            {
                List<GameObject> LBEslots = item.GetComponent<LBEgear>().GetAllSlots();
                if (slot.name == "Vest")
                {
                    Vector2 offset = new Vector2(-1.04f, -0.8f);
                    foreach (GameObject pocket in LBEslots)
                    {
                        GameObject pocketUI = null;
                        switch (pocket.name)
                        {
                            case "webbing_left": pocketUI = CreateItemSlot(slot.transform.parent, Vector2.zero + offset * Vector2.right, "WebbingLeft", pocket.GetComponent<ItemSlot>().slotType, pocket, InventoryCellSize.Small); break;
                            case "webbing_right": pocketUI = CreateItemSlot(slot.transform.parent, new Vector2(3, 0) + offset * Vector2.right, "WebbingRight", pocket.GetComponent<ItemSlot>().slotType, pocket, InventoryCellSize.Small); break;
                            case "pouch_left": pocketUI = CreateItemSlot(slot.transform.parent, new Vector2(2.06f, 5) + offset, "LeftPouch", pocket.GetComponent<ItemSlot>().slotType, pocket, InventoryCellSize.Medium); break;
                            case "pouch_right": pocketUI = CreateItemSlot(slot.transform.parent, new Vector2(0.52f, 5) + offset, "RightPouch", pocket.GetComponent<ItemSlot>().slotType, pocket, InventoryCellSize.Medium); break;
                            default:
                                float x = 0, y = 0;
                                string[] coordinates = pocket.name.Split('_');
                                if (coordinates.Length == 2)
                                {
                                    if (coordinates[0] == "1" || coordinates[0] == "2" || coordinates[0] == "3" || coordinates[0] == "4")
                                        x = float.Parse(coordinates[0]);
                                    if (coordinates[1] == "1" || coordinates[1] == "2" || coordinates[1] == "3")
                                        y = float.Parse(coordinates[1]);
                                    pocketUI = CreateItemSlot(slot.transform.parent, new Vector2(y, x + 1) + offset, "Slot" + coordinates[0] + "_" + coordinates[1], pocket.GetComponent<ItemSlot>().slotType, pocket, InventoryCellSize.Small);
                                }
                                break;
                        }
                    }
                }
                if (slot.name == "Backpack")
                {
                    foreach (GameObject pocket in LBEslots)
                    {
                        GameObject pocketUI = null;
                        string[] pocketName = pocket.name.Split('_');
                        InventoryCellSize pocketSize = InventoryCellSize.Small;
                        float x = 0;
                        switch (pocketName[0])
                        {
                            case "middle": x = 0.5f; pocketSize = InventoryCellSize.Large; break;
                            case "leftpocket": break;
                            case "rightpocket": x = 3 - 0.12f; break;
                        }
                        float y = float.Parse(pocketName[1]);
                        pocketUI = CreateItemSlot(slot.transform.parent, new Vector2(x, y + 0.3f), "Slot" + ((int)x).ToString() + "_" + ((int)y).ToString(), pocket.GetComponent<ItemSlot>().slotType, pocket, pocketSize);
                    }
                }
            }
        }
    }

    GameObject CreateSlotPanel(string name, Vector2 pivot, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name) { tag = "UI_noscaling", layer = 5 };
        panel.transform.SetParent(game.UI.transform);
        RectTransform panelTransform = panel.AddComponent<RectTransform>();
        panelTransform.localScale = Vector3.one;
        panelTransform.pivot = pivot;
        panelTransform.anchorMin = anchorMin;
        panelTransform.anchorMax = anchorMax;
        panelTransform.offsetMin = Vector2.zero;
        panelTransform.offsetMax = Vector2.zero;
        Color32 panelColor = new Color32(123, 86, 43, 255);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.raycastTarget = false;
        panelImage.sprite = Resources.Load<Sprite>("Graphics/Items/Slots/inventory_cell");
        panelImage.type = Image.Type.Tiled;
        panelImage.color = panelColor;
        return panel;
    }

    public void CloseInventory()
    {
        if (!open)
            return;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        character.GetComponent<Character>().CalculateEncumbrance();
        open = false;
        if (itemHeld != null)
        {
            if (lastSlot != null)
                PlaceItemInSlot(lastSlot, itemHeld);
            else
                character.GetComponent<Character>().TryPlaceItemInInventory(itemHeld);
            itemHeld = null;
        }
        for (int i = 0; i < panels.Count; i++)
            Destroy(panels[i]);
        if (pileUnderCharacter != null)
            if (pileUnderCharacter.GetItemAmount() == 0)
                Destroy(pileUnderCharacter.gameObject);
            else
                pileUnderCharacter.SetImageToItemCount();
        itemViewed = null;
        textPanels.Clear();
        panels.Clear();
    }

    void AddCharacterPanel()
    {
        GameObject characterPanel = CreateSlotPanel("CharacterPanel", new Vector2(0, 1), Vector2.zero, new Vector2(0.5f, 1));
        panels.Add(characterPanel);
        CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(0, 0.25f / 2), "Weapon on back", BuiltinCharacterSlots.Backweapon);
        CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(2.25f / 2, 3f), "Vest", BuiltinCharacterSlots.Vest);
        GameObject pwCategory = CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(2.25f, 0.25f / 2), "Primary weapon", BuiltinCharacterSlots.Weapon);
        AddTextBox("ActionPoints", new Vector2(192, 24), new Vector2(60, -80), pwCategory.transform);
        textPanels["ActionPoints"].fontSize = 14;
        AddTextBox("ItemStatNames", new Vector2(192, 384), new Vector2(208, -112), pwCategory.transform);
        textPanels["ItemStatNames"].alignment = TextAnchor.UpperLeft;
        textPanels["ItemStatNames"].fontSize = 16;
        textPanels["ItemStatNames"].horizontalOverflow = HorizontalWrapMode.Overflow;
        AddTextBox("ItemStats", new Vector2(128, 384), new Vector2(334 - 32, -112), pwCategory.transform);
        textPanels["ItemStats"].alignment = TextAnchor.UpperRight;
        textPanels["ItemStats"].fontSize = 16;
        CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(0, 1.75f), "Armor", BuiltinCharacterSlots.Armor);
        CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(2.25f, 1.75f), "Helmet", BuiltinCharacterSlots.Helmet);
        //CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(4.5f, 0.25f / 2 + 1.50f), "Eyes", BuiltinCharacterSlots.Eyes);
        //CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(4.5f, 0.25f / 2 + 2.75f), "Face", BuiltinCharacterSlots.Mask);
        //CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(3.5f, 0.25f / 2 + 6f), "Belt", BuiltinCharacterSlots.Belt);
        CreateBuiltinSlotCategory(characterPanel.transform, new Vector2(4.5f, 0.25f / 2 + 4f), "Backpack", BuiltinCharacterSlots.Backpack);
    }

    GameObject CreateBuiltinSlotCategory(Transform panelTransform, Vector2 categoryPosition, string categoryDisplayName, BuiltinCharacterSlots slotType)
    {
        string slotName = Enum.GetName(typeof(BuiltinCharacterSlots), slotType);
        GameObject category = AddSlotCategoryToPanel(panelTransform, categoryPosition, slotName + "Category");
        AddTextPanelToCategory(category, categoryDisplayName);
        GameObject slot = CreateBuiltinSlot(category.transform, Vector2.zero, slotName, slotType);
        AddTextPanelToSlot(slot);
        return category;
    }

    void AddOutsideItemsPanel()
    {
        GameObject outsideContentsPanel = CreateSlotPanel("ItemsOutside", Vector2.one, new Vector2(0.5f, 0), Vector2.one);
        panels.Add(outsideContentsPanel);
        GameObject scaler = AddSlotCategoryToPanel(outsideContentsPanel.transform, new Vector2(0, 0.25f / 2), "Items");
        AddTextPanelToCategory(scaler, "Items on ground");
        Vector2 playerPosition = character.transform.localPosition;
        GameObject pileUnderCharacter = game.GetItemPile(playerPosition);
        if (pileUnderCharacter == null)
            pileUnderCharacter = game.CreateItemPile(playerPosition);
        this.pileUnderCharacter = pileUnderCharacter.GetComponent<ItemPile>();
        GameObject buttonNext = new GameObject("button_next");
        buttonNext.transform.SetParent(scaler.transform);
        buttonNext.AddComponent<Image>().sprite = Resources.Load<Sprite>("Graphics/button_next");
        buttonNext.GetComponent<RectTransform>().Rotate(new Vector3(0, 0, -180));
        buttonNext.transform.localPosition = new Vector2(256, 4);
        buttonNext.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
        GameObject buttonPrevious = new GameObject("button_previous");
        buttonPrevious.transform.SetParent(scaler.transform);
        buttonPrevious.AddComponent<Image>().sprite = Resources.Load<Sprite>("Graphics/button_next");
        buttonPrevious.transform.localPosition = new Vector2(128, 4);
        buttonPrevious.GetComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
        AddTextBox("PageDisplay", new Vector2(96, 24), new Vector2(192, 4), scaler.transform);
        textPanels["PageDisplay"].fontSize = 12;
        SwitchOutsideItemsToPage(1);
    }

    GameObject AddTextBox(string boxName, Vector2 sizeDelta, Vector2 localPosition, Transform categoryTransform)
    {
        GameObject pageDisplay = new GameObject(boxName);
        Sprite inventoryCell = Resources.Load<Sprite>("Graphics/Items/Slots/inventory_cell");
        pageDisplay.layer = 5;
        pageDisplay.transform.SetParent(categoryTransform);
        RectTransform pageDisplayTransform = pageDisplay.AddComponent<RectTransform>();
        //pageDisplayTransform.localScale = Vector3.one;
        pageDisplayTransform.sizeDelta = sizeDelta;// new Vector2(96, 24);
        pageDisplayTransform.transform.localPosition = localPosition;//new Vector2(192, 8);
        Color32 pageDisplayColor = new Color32(14, 16, 33, 255);
        Image pageDisplayImage = pageDisplay.AddComponent<Image>();
        pageDisplayImage.raycastTarget = false;
        pageDisplayImage.sprite = inventoryCell;
        pageDisplayImage.type = Image.Type.Tiled;
        pageDisplayImage.color = pageDisplayColor;
        GameObject pageDisplayText = new GameObject(boxName + "text");
        pageDisplayText.transform.SetParent(pageDisplay.transform, false);
        pageDisplayText.AddComponent<RectTransform>().sizeDelta = sizeDelta;
        textPanels.Add(boxName, AddTextToObject(pageDisplayText));
        return pageDisplay;
    }

    void NextItemPage()
    {
        currentItemPage++;
        currentItemPage = Mathf.Clamp(currentItemPage, 1, pileUnderCharacter.GetPageCount());
        pileUnderCharacter.MakeSlotsIfNecessary();
        SwitchOutsideItemsToPage(currentItemPage);
    }

    void PreviousItemPage()
    {
        currentItemPage--;
        //pileUnderCharacter.DeleteEmptyPages();
        currentItemPage = Mathf.Clamp(currentItemPage, 1, pileUnderCharacter.GetPageCount());
        SwitchOutsideItemsToPage(currentItemPage);
    }

    void SwitchOutsideItemsToPage(int n)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i].name == "ItemsOutside")
            {
                Transform itemsOutside = panels[i].transform.GetChild(0);
                for (int j = 0; j < itemsOutside.childCount; j++)
                    if (itemsOutside.GetChild(j).GetComponent<UIslot>())
                        Destroy(itemsOutside.GetChild(j).gameObject);
                Text pageDisplayText = textPanels["PageDisplay"]; //itemsOutside.Find("page_display").GetComponentInChildren<Text>();
                pageDisplayText.text = "Page " + n.ToString() + "/" + pileUnderCharacter.GetPageCount();
                pageDisplayText.resizeTextMaxSize = 14;
                pageDisplayText.alignment = TextAnchor.MiddleCenter;
                n--;
                for (int k = 0; k < itemPageHeight; k++)
                {
                    for (int j = 0; j < itemPageWidth; j++)
                    {
                        int itemNumber = k * itemPageWidth + j;
                        GameObject slot = CreateSlotWithNamePanel(itemsOutside, new Vector2(j, k + 0.25f / 2), "OutsideSlot" + j.ToString() + "_" + k.ToString(), "ground", pileUnderCharacter.transform.GetChild(itemNumber + n * ItemPageSlotCount).gameObject, InventoryCellSize.Large);
                    }
                }
            }
        }
    }

    public void OpenInventory()
    {
        open = true;
        game = GetComponent<Game>();
        Cursor.visible = true;
        AddCharacterPanel();
        AddOutsideItemsPanel();
    }

    List<RaycastResult> RaycastMouse()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current) { pointerId = -1, };
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, results);
        return results;
    }

    void PickItem(GameObject item)
    {
        itemHeld = item;
        character.GetComponent<Character>().PlaySound(Resources.Load<AudioClip>("Sounds/pick_item"));
        if (itemHeld.GetComponent<Item>().sprite != null)
        {
            Texture2D cursor = new Texture2D(itemHeld.GetComponent<Item>().sprite.texture.width, itemHeld.GetComponent<Item>().sprite.texture.height);
            cursor.SetPixels32(itemHeld.GetComponent<Item>().sprite.texture.GetPixels32());
            TextureScaler.scale(cursor, (int)(cursor.width * 1.7f * game.UIscale), (int)(cursor.height * 1.7f * game.UIscale), FilterMode.Point);
            Cursor.SetCursor(cursor, new Vector2(cursor.width, cursor.height) / 2, CursorMode.ForceSoftware);
        }
    }

    void PickItemFromSlot(GameObject slot)
    {
        itemHeld = slot.GetComponent<UIslot>().pocket.GetItem();
        if (itemHeld != null)
        {
            if (character.GetComponent<Character>().UseActionPoints(itemHeld.GetComponent<Item>().actionPointsToMove))
            {
                if (RemoveItemFromSlot(slot))
                {
                    lastSlot = slot;
                    PickItem(itemHeld);
                }
            }
            else
            {
                itemHeld = null;
            }
        }
    }

    void RemoveHeldItem()
    {
        character.GetComponent<Character>().PlaySound(Resources.Load<AudioClip>("Sounds/place_item"));
        itemHeld = null;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        lastSlot = null;
    }

    void TryPlaceHeldItem(GameObject slot)
    {
        if (itemHeld != null)
        {
            if (!PlaceItemInSlot(slot, itemHeld))
            {
                if (lastSlot != null)
                    PlaceItemInSlot(lastSlot, itemHeld);
                else
                    character.GetComponent<Character>().TryPlaceItemInInventory(itemHeld);
            }
            RemoveHeldItem();
        }
    }

    public void OverrideItemViewText(string text)
    {
        if (open)
        {
            itemViewed = null;
            textPanels["ItemStatNames"].text = text;
            textPanels["ItemStats"].text = "";
        }
    }

    void Update()
    {
        if (!open)
            return;
        Character characterComponent = character.GetComponent<Character>();
        if (characterComponent.IsInCombat())
        {
            textPanels["ActionPoints"].transform.parent.gameObject.SetActive(true);
            textPanels["ActionPoints"].text = "\tAction points: " + ((int)characterComponent.actionPoints).ToString() + "/" + ((int)characterComponent.GetMaxActionPoints()).ToString();
        }
        else
        {
            textPanels["ActionPoints"].transform.parent.gameObject.SetActive(false);
        }
        if (Input.GetMouseButtonUp(0))
        {
            List<RaycastResult> results = RaycastMouse();
            foreach (RaycastResult rr in results)
            {
                //Debug.Log(rr.gameObject.name);
                if (rr.gameObject.name == "button_next")
                {
                    NextItemPage();
                    continue;
                }
                if (rr.gameObject.name == "button_previous")
                {
                    PreviousItemPage();
                    continue;
                }
                GameObject currentSlot = rr.gameObject;
                if (currentSlot.GetComponent<UIslot>() == null)
                    continue;
                if (currentSlot.GetComponent<UIslot>().pocket.transform.parent.parent.name == "Backpack" && character.GetComponent<Character>().IsInCombat())
                {
                    OverrideItemViewText("Not in combat!");
                    continue;
                }
                GameObject itemInSlot = currentSlot.GetComponent<UIslot>().pocket.GetItem();
                if (itemInSlot != null)
                    if (itemInSlot.GetComponent<ItemExtension>())
                        return;
                if (itemHeld == null && itemInSlot != null)
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (pileUnderCharacter != null)
                        {
                            bool itemOutside = false;
                            for (int i = 0; i < pileUnderCharacter.GetSlotCount(); i++)
                                if (pileUnderCharacter.GetSlot(i).gameObject == currentSlot.GetComponent<UIslot>().pocket.gameObject)
                                    itemOutside = true;
                            if (!itemOutside)
                            {
                                pileUnderCharacter.MakeSlotsIfNecessary();
                                for (int i = 0; i < pileUnderCharacter.GetSlotCount(); i++)
                                    if (pileUnderCharacter.GetSlot(i).ItemFits(itemInSlot))
                                        if (characterComponent.UseActionPoints(itemInSlot.GetComponent<Item>().actionPointsToMove))
                                        {
                                            if (RemoveItemFromSlot(currentSlot))
                                                pileUnderCharacter.GetSlot(i).PlaceItem(itemInSlot);
                                            break;
                                        }
                            }
                            else if (characterComponent.UseActionPoints(itemInSlot.GetComponent<Item>().actionPointsToMove))
                                if (RemoveItemFromSlot(currentSlot))
                                    characterComponent.TryPlaceItemInInventory(itemInSlot);
                        }
                        return;
                    }
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (!characterComponent.IsInCombat())
                        {
                            Firearm firearmComponent = itemInSlot.GetComponent<Firearm>();
                            Magazine magazineComponent = itemInSlot.GetComponent<Magazine>();
                            if (magazineComponent)
                            {
                                GameObject ammoBox = magazineComponent.UnloadAmmo();
                                if (ammoBox != null)
                                    if (characterComponent.UseActionPoints(ammoBox.GetComponent<Item>().actionPointsToMove))
                                        PickItem(ammoBox);
                                return;
                            }
                            if (firearmComponent)
                            {
                                GameObject magazine = firearmComponent.UnloadMagazine();
                                if (magazine != null)
                                    if (!magazine.GetComponent<Magazine>().builtin)
                                        if (characterComponent.UseActionPoints(magazine.GetComponent<Item>().actionPointsToMove))
                                            PickItem(magazine);
                                return;
                            }
                        }
                        else
                        {
                            OverrideItemViewText("Not in combat!");
                            return;
                        }
                    }
                    PickItemFromSlot(currentSlot);
                    return;
                }
                else //Item held
                {
                    if (itemInSlot != null) //Another item is in slot
                    {
                        AmmoBox ammoBoxComponentHeld = itemHeld.GetComponent<AmmoBox>();
                        AmmoBox ammoBoxComponentInSlot = itemInSlot.GetComponent<AmmoBox>();
                        Firearm fireArmComponentHeld = itemHeld.GetComponent<Firearm>();
                        Firearm firearmComponentInSlot = itemInSlot.GetComponent<Firearm>();
                        Magazine magazineComponentHeld = itemHeld.GetComponent<Magazine>();
                        Magazine magazineComponentInSlot = itemInSlot.GetComponent<Magazine>();
                        if (ammoBoxComponentHeld)
                        {
                            if (magazineComponentInSlot)
                                if (magazineComponentInSlot.builtin)
                                    if (firearmComponentInSlot)
                                        if (firearmComponentInSlot.magazine != null)
                                            if (characterComponent.UseActionPoints(ammoBoxComponentHeld.GetComponent<Item>().actionPointsToMove))
                                            {
                                                firearmComponentInSlot.magazine.GetComponent<Magazine>().LoadFromAmmoBox(itemHeld);
                                                return;
                                            }
                            if (!characterComponent.IsInCombat())
                            {
                                if (magazineComponentInSlot)
                                    magazineComponentInSlot.LoadFromAmmoBox(itemHeld);
                                if (firearmComponentInSlot)
                                    if (firearmComponentInSlot.magazine != null)
                                        firearmComponentInSlot.magazine.GetComponent<Magazine>().LoadFromAmmoBox(itemHeld);
                                if (ammoBoxComponentInSlot)
                                    if (ammoBoxComponentInSlot.amount == ammoBoxComponentInSlot.bulletType.box)
                                        TryPlaceHeldItem(currentSlot);
                                    else
                                        ammoBoxComponentInSlot.TransferAmmoFromAnotherBox(itemHeld);
                                if (ammoBoxComponentHeld.amount <= 0)
                                {
                                    Destroy(itemHeld);
                                    RemoveHeldItem();
                                }
                            }
                            else
                            {
                                OverrideItemViewText("Not in combat!");
                            }
                            return;
                        }
                        if (fireArmComponentHeld)
                        {
                            //Nothing
                        }
                        if (magazineComponentHeld && firearmComponentInSlot)
                        {
                            if (firearmComponentInSlot.magazine == null)
                            {
                                if (firearmComponentInSlot.LoadMagazine(itemHeld))
                                    RemoveHeldItem();
                            }
                            return;
                        }
                    }
                    if (itemHeld == null)
                    {
                        //Debug.Log("Held item lost along the way. Oops!");
                        return;
                    }
                    TryPlaceHeldItem(currentSlot);
                }
            }
        }
        if (Input.GetMouseButtonUp(1))
        {
            List<RaycastResult> results = RaycastMouse();
            foreach (RaycastResult rr in results)
            {
                GameObject currentSlot = rr.gameObject;
                if (currentSlot.GetComponent<UIslot>() == null)
                    continue;
                itemViewed = currentSlot.GetComponent<UIslot>().pocket.GetItem();
                if (itemViewed != null)
                    if (itemViewed.GetComponent<ItemExtension>())
                        itemViewed = itemViewed.GetComponent<ItemExtension>().extensionOf;
            }
        }
        if (itemViewed == null)
            return;
        string statNames = "";
        string statValues = "";
        Item itemComponentViewed = itemViewed.GetComponent<Item>();
        AmmoBox ammoBoxComponentViewed = itemViewed.GetComponent<AmmoBox>();
        Firearm firearmComponentViewed = itemViewed.GetComponent<Firearm>();
        Armor armorComponentViewed = itemViewed.GetComponent<Armor>();
        Magazine magazineComponentViewed = itemViewed.GetComponent<Magazine>();
        if (itemComponentViewed)
        {
            //statNames += "Name\n";
            //statValues += itemComponentViewed.displayName + "\n";
            statNames += itemComponentViewed.displayName + "\n";
            statValues += "\n";
            statNames += "Total weight\n";
            statValues += itemComponentViewed.GetWeight().ToString() + "\n";
            statNames += "Price\n";
            statValues += itemComponentViewed.price.ToString() + "\n";
            statNames += "Action points to move\n";
            statValues += itemComponentViewed.actionPointsToMove.ToString() + "\n";
            if (itemComponentViewed.canBeRepaired)
            {
                statNames += "Repairable\n";
                statValues += "\n";
            }
            if (!ammoBoxComponentViewed && !magazineComponentViewed)
            {
                statNames += "Condition\n";
                statValues += itemComponentViewed.Condition.ToString() + " / " + itemComponentViewed.GetMaxCondition().ToString() + "\n";
            }
            if (itemComponentViewed.primarySlot != "")
            {
                statNames += "Equipped on\n";
                statValues += itemComponentViewed.primarySlot + "\n";
                if (itemComponentViewed.secondarySlots.Count > 0)
                {
                    statNames += "Secondary slots\n";
                    for (int i = 0; i < itemComponentViewed.secondarySlots.Count; i++)
                    {
                        statValues += itemComponentViewed.secondarySlots[i];
                        if (i < itemComponentViewed.secondarySlots.Count - 1)
                            statValues += ", ";
                    }
                    statValues += "\n";
                }
            }
        }
        if (armorComponentViewed)
        {
            if (armorComponentViewed.resistances.Count > 0)
            {
                statNames += "Damage resistances\n";
                statValues += '\n';
                foreach (KeyValuePair<DamageTypes, int> resistance in armorComponentViewed.resistances)
                {
                    statNames += resistance.Key.ToString() + '\n';
                    statValues += resistance.Value.ToString() + '\n';
                }
            }
        }
        if (firearmComponentViewed)
        {
            statNames += "Handle difficulty\n";
            statValues += firearmComponentViewed.handleDifficulty.ToString() + "\n";
            statNames += "Accuracy\n";
            statValues += firearmComponentViewed.GetComponent<ObjectAttributes>().GetAttributeValue("Accuracy").ToString() + "\n";
            foreach (Firearm.FireMode fireMode in firearmComponentViewed.fireModes)
            {
                switch (fireMode.fireMode)
                {
                    case WeaponFireMode.Nonautomatic:
                        {
                            statNames += "Non-auto mode shots\n";
                            statValues += fireMode.shotsPerAttack.ToString() + "\n";
                        }
                        break;
                    case WeaponFireMode.Semiautomatic:
                        {
                            statNames += "Semi-auto mode shots\n";
                            statValues += fireMode.shotsPerAttack.ToString() + "\n";
                        }
                        break;
                    case WeaponFireMode.ControlledBurst:
                        {
                            statNames += "Burst mode shots\n";
                            statValues += fireMode.shotsPerAttack.ToString() + "\n";
                            statNames += "Burst mode recoil\n";
                            statValues += fireMode.recoil.ToString() + "\n";
                        }
                        break;
                    case WeaponFireMode.Automatic:
                        {
                            statNames += "Automatic mode shots\n";
                            statValues += fireMode.shotsPerAttack.ToString() + "\n";
                            statNames += "Automatic mode recoil\n";
                            statValues += fireMode.recoil.ToString() + "\n";
                        }
                        break;
                }
            }
            statNames += "Damage modifier\n";
            statValues += firearmComponentViewed.damageModifier.ToString() + "\n";
            statNames += "Distance modifier\n";
            statValues += firearmComponentViewed.distanceModifier.ToString() + "\n";
            if (firearmComponentViewed.magazine != null)
                magazineComponentViewed = firearmComponentViewed.magazine.GetComponent<Magazine>();
        }
        if (magazineComponentViewed)
        {
            statNames += "Ammo\n";
            statValues += magazineComponentViewed.ammo.ToString() + " / " + magazineComponentViewed.maxammo.ToString() + "\n";
            if (magazineComponentViewed.currentCaliber != null)
            {
                statNames += "Caliber\n";
                statValues += magazineComponentViewed.currentCaliber.caliber + "\n";
                statNames += "Distance\n";
                statValues += magazineComponentViewed.currentCaliber.distance.ToString() + "\n";
                statNames += "Damage\n";
                statValues += magazineComponentViewed.currentCaliber.damage.ToString() + "\n";
                statNames += "Penetration\n";
                statValues += magazineComponentViewed.currentCaliber.penetration.ToString() + "\n";
                statNames += "Tumble\n";
                statValues += magazineComponentViewed.currentCaliber.tumble.ToString() + "\n";
                if (magazineComponentViewed.currentCaliber.tracer)
                {
                    statNames += "Tracer ammo\n";
                    statValues += "\n";
                }
                statNames += "Single bullet weight\n";
                statValues += magazineComponentViewed.currentCaliber.weight.ToString() + "\n";
            }
            else
            {
                statNames += "Base caliber\n";
                statValues += magazineComponentViewed.caliber + "\n";
            }
        }
        if (ammoBoxComponentViewed)
        {
            statNames += "Ammo\n";
            statValues += ammoBoxComponentViewed.amount.ToString() + " / " + ammoBoxComponentViewed.bulletType.box.ToString() + "\n";
            statNames += "Caliber\n";
            statValues += ammoBoxComponentViewed.bulletType.caliber + "\n";
            statNames += "Distance\n";
            statValues += ammoBoxComponentViewed.bulletType.distance.ToString() + "\n";
            statNames += "Damage\n";
            statValues += ammoBoxComponentViewed.bulletType.damage.ToString() + "\n";
            statNames += "Penetration\n";
            statValues += ammoBoxComponentViewed.bulletType.penetration.ToString() + "\n";
            statNames += "Tumble\n";
            statValues += ammoBoxComponentViewed.bulletType.tumble.ToString() + "\n";
            if (ammoBoxComponentViewed.bulletType.tracer)
            {
                statNames += "Tracer ammo\n";
                statValues += "\n";
            }
            statNames += "Single bullet weight\n";
            statValues += ammoBoxComponentViewed.bulletType.weight.ToString() + "\n";
        }
        textPanels["ItemStatNames"].text = statNames;
        textPanels["ItemStats"].text = statValues;
        return;
    }
}