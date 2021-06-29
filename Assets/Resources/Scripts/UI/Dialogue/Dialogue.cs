using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    public class DialogueNode
    {
        public Dictionary<Attribute, int> attributesRequired = new Dictionary<Attribute, int>();
        public Dictionary<string, string> reputationRequired = new Dictionary<string, string>();
        public Dictionary<string, bool> worldStatesRequired = new Dictionary<string, bool>();
        public List<string> charactersWithTagAlive = new List<string>();
        public List<string> charactersWithTagDead = new List<string>();
        public DialoguePage leadsTo = null;
        public DialoguePage leadsToOnFailure = null;
        public int moneyRequired = -1;
        public bool initiateTrade = false;
        public string text;
        public string textIfConditionsNotSatisfied;
        public string name;

        public bool ConditionsSatisfied(Character player, Character talkingTo)
        {
            if (player.money < moneyRequired && moneyRequired > -1)
                return false;
            foreach (string tag in charactersWithTagAlive)
            {
                bool tagFound = false;
                foreach ((string, Character) worldTag in player.world.tags)
                    if (worldTag.Item1 == tag)
                    {
                        tagFound = true;
                        break;
                    }
                if (!tagFound)
                    return false;
            }
            foreach (string tag in charactersWithTagDead)            
                foreach ((string, Character) worldTag in player.world.tags)
                    if (worldTag.Item1 == tag)
                        return false;
            
            foreach (KeyValuePair<string, bool> localWorldState in worldStatesRequired)
                if (player.world.worldStates.ContainsKey(localWorldState.Key))                
                    if (localWorldState.Value != player.world.worldStates[localWorldState.Key])
                        return false;
            foreach (KeyValuePair<string, string> kvp in reputationRequired)
            {
                switch (kvp.Value)
                {
                    case "hostile":
                        foreach (Character character in player.world.activeCharacters)
                            if (character != player && character.faction == kvp.Key)
                                if (!character.hostileTowards.Contains(player.faction))
                                    return false;
                        //if (!player.hostileTowards.Contains(kvp.Key))
                        //    return false;
                        break;
                    case "neutral":
                        foreach (Character character in player.world.activeCharacters)
                            if (character != player && character.faction == kvp.Key)
                                if (!character.brain.temporaryPeace.Keys.Contains(player.faction) && (character.hostileTowards.Contains(player.faction) || character.friendlyTowards.Contains(player.faction)))
                                    return false;
                        break;
                    case "friendly":
                        foreach (Character character in player.world.activeCharacters)
                            if (character != player && character.faction == kvp.Key)
                                if (!character.friendlyTowards.Contains(player.faction))
                                    return false;
                        //if (!player.friendlyTowards.Contains(kvp.Key))
                        //    return false; 
                        break;
                }
            }
            foreach (KeyValuePair<Attribute, int> attributeCheck in attributesRequired)
                if ((int)player.GetAttribute(attributeCheck.Key.Name).Value < attributeCheck.Value)
                    return false;
            return true;
        }
    }

    public class DialoguePage
    {
        public List<DialogueNode> responses = new List<DialogueNode>();
        public Dictionary<string, int> turnHostile = new Dictionary<string, int>();
        public Dictionary<string, int> turnNeutral = new Dictionary<string, int>();
        public Dictionary<string, bool> worldStateChanges = new Dictionary<string, bool>();
        public List<string> itemsToGiveToPlayer = new List<string>();
        public int takeMoney = 0;
        public string text = "";
        public string name = "";
    }

    private World world;
    private DialogueNode nullAnswerNode;
    private Dictionary<Text, Text> responsesAndNumbersTexts = new Dictionary<Text, Text>();
    private List<DialogueNode> currentPageResponses = new List<DialogueNode>();

    public Dictionary<string, DialoguePage> pages = new Dictionary<string, DialoguePage>();
    public DialoguePage currentPage;
    public Character player;
    public Character talkingTo;
    public Text targetCharacterDialogue;

    private void Initiate()
    {
        targetCharacterDialogue = transform.Find("TargetDialogue").Find("Dialogue").GetComponent<Text>();
        Transform playerDialogue = transform.Find("TargetDialogue").Find("PlayerDialogue");
        for (int i = 0; i < 8; i++)
        {
            GameObject newResponse = playerDialogue.GetChild(i).gameObject;
            responsesAndNumbersTexts.Add(newResponse.GetComponent<Text>(), newResponse.transform.GetChild(0).GetComponent<Text>());
        }
        nullAnswerNode = new DialogueNode() { text = "..." };
        world = World.GetInstance();
        player = world.Player.GetComponent<Character>();
    }

    private void LoadPage(DialoguePage page)
    {
        if (page == null)
        {
            EndDialogue();
            return;
        }
        player.money -= page.takeMoney;
        foreach (KeyValuePair<string, bool> worldStateChange in page.worldStateChanges)
        {
            if (world.worldStates.ContainsKey(worldStateChange.Key))
                world.worldStates[worldStateChange.Key] = worldStateChange.Value;
            else
                world.worldStates.Add(worldStateChange.Key, worldStateChange.Value);
        }
        if (page.itemsToGiveToPlayer.Count > 0)
        {
            List<GameObject> allTargetItems = talkingTo.GetAllItemsInInventory();
            List<GameObject> targetItems = new List<GameObject>();
            foreach (string s in page.itemsToGiveToPlayer)
                targetItems.AddRange(allTargetItems.Where(item => item.name == s));
            for (int i = 0; i < targetItems.Count; i++)
            {
                //Debug.Log("Will try to equip " + targetItems[i].name);
                bool equipped = false;
                Firearm firearmComponent = targetItems[i].GetComponent<Firearm>();
                if (firearmComponent)
                {
                    if (player.Weapon == null)
                    {
                        player.EquipItemAsWeapon(targetItems[i]);
                        equipped = true;                        
                    }
                    else if (player.GetItemFromBuiltinSlot(BuiltinCharacterSlots.Backweapon) == null)
                    {
                        player.EquipWeaponOnBack(targetItems[i]);
                        equipped = true;
                    }
                }
                if (!equipped)
                    player.TryPlaceItemInInventory(targetItems[i]);
            }
        }
        foreach (KeyValuePair<string, int> kvp in page.turnHostile)
            foreach (Character character in world.activeCharacters)
                if (!character.IsPlayer() && character.faction == kvp.Key)
                    character.brain.MakeHostileTemporarily(character, player.faction, kvp.Value);
        foreach (KeyValuePair<string, int> kvp in page.turnNeutral)
            foreach (Character character in world.activeCharacters)
                if (!character.IsPlayer() && character.faction == kvp.Key)
                    character.brain.MakeNeutralTemporarily(character, player.faction, kvp.Value);
        targetCharacterDialogue.text = page.text;
        LoadPageResponses(page);
    }

    private void LoadPageResponses(DialoguePage page)
    {
        if (page.responses.Count == 0)
            page.responses.Add(nullAnswerNode);
        int validResponses = 0;
        currentPageResponses.Clear();
        for (int i = 0; i < 8; i++)
        {
            responsesAndNumbersTexts.Keys.ElementAt(i).text = "";
            responsesAndNumbersTexts.Values.ElementAt(i).text = "";
        }
        for (int i = 0; i < page.responses.Count; i++)
        {
            DialogueNode currentResponse = page.responses[i];
            if (currentResponse.ConditionsSatisfied(player, talkingTo))
            {
                responsesAndNumbersTexts.Keys.ElementAt(validResponses).text = currentResponse.text;
                responsesAndNumbersTexts.Values.ElementAt(validResponses).text = (validResponses + 1).ToString();
                currentPageResponses.Add(currentResponse);
                validResponses++;
            }
            else if (currentResponse.leadsToOnFailure != null)
            {
                responsesAndNumbersTexts.Keys.ElementAt(validResponses).text = currentResponse.textIfConditionsNotSatisfied;
                responsesAndNumbersTexts.Values.ElementAt(validResponses).text = (validResponses + 1).ToString();
                currentPageResponses.Add(currentResponse);
                validResponses++;
            }
        }
    }

    public void ChooseResponseNumber(int number)
    {
        if (number >= 0 && number < currentPageResponses.Count)
        {
            DialogueNode response = currentPageResponses[number];
            if (response.leadsTo == null)
            {                
                EndDialogue(response.initiateTrade);
                return;
            }
            if (response.ConditionsSatisfied(player, talkingTo))
                currentPage = response.leadsTo;
            else
                currentPage = response.leadsToOnFailure;
            LoadPage(currentPage);
        }
    }

    private void EndDialogue(bool startTrade = false)
    {
        pages.Clear();
        world.characterController.dialogueName = "none";
        currentPage = null;
        if (!startTrade || talkingTo.merchantStock == null)
            world.SwitchUIMode(InGameUI.Interface);
        else
            world.InitiateTrade(talkingTo.merchantStock);
    }

    private string LoadTextFromArray(List<string> words)
    {
        string text = "";
        foreach (string s in words)
            text += s + " ";
        //Debug.Log(text);
        return text;
    }

    public void LoadFromInfoBlock(InfoBlock dialogueSet, Character talkingTo)
    {
        if (dialogueSet == null || talkingTo == null)
        {
            EndDialogue();
            return;
        }
        if (targetCharacterDialogue == null)
            Initiate();
        this.talkingTo = talkingTo;
        string startingNode = dialogueSet.namesValues["starting_node"];
        InfoBlock nodes = dialogueSet.GetBlock("nodes");
        Dictionary<string, DialogueNode> nodesUnsorted = new Dictionary<string, DialogueNode>();
        Dictionary<DialogueNode, string> nodesAndPages = new Dictionary<DialogueNode, string>();
        Dictionary<DialogueNode, string> nodesAndFailurePages = new Dictionary<DialogueNode, string>();
        foreach (InfoBlock node in nodes.subBlocks)
        {
            DialogueNode newNode = new DialogueNode
            {
                text = LoadTextFromArray(node.GetBlock("text").values),
                initiateTrade = node.values.Contains("trade"),
                name = node.name
            };
            if (node.HasBlock("text_failure"))
                newNode.textIfConditionsNotSatisfied = LoadTextFromArray(node.GetBlock("text_failure").values);
            if (node.namesValues.ContainsKey("required_money"))
                newNode.moneyRequired = int.Parse(node.namesValues["required_money"]);
            InfoBlock requiredRep = node.GetBlock("reputation_required");
            if (requiredRep != null)
                foreach (KeyValuePair<string, string> kvp in requiredRep.namesValues)
                    newNode.reputationRequired.Add(kvp.Key, kvp.Value);
            InfoBlock requiredStates = node.GetBlock("required_states");
            if (requiredStates != null)
                foreach (KeyValuePair<string, string> kvp in requiredStates.namesValues)
                    newNode.worldStatesRequired.Add(kvp.Key, kvp.Value == "true" ? true : false);
            InfoBlock deadCharactersRequired = node.GetBlock("dead_character_tags");
            if (deadCharactersRequired != null)
                newNode.charactersWithTagDead.AddRange(deadCharactersRequired.values);
            nodesUnsorted.Add(node.name, newNode);
            if (node.namesValues.ContainsKey("leads_to"))
                nodesAndPages.Add(newNode, node.namesValues["leads_to"]);
            if (node.namesValues.ContainsKey("leads_to_failure"))
                nodesAndFailurePages.Add(newNode, node.namesValues["leads_to_failure"]);
        }
        InfoBlock pagesBlock = dialogueSet.GetBlock("pages");
        foreach (InfoBlock page in pagesBlock.subBlocks)
        {
            DialoguePage newPage = new DialoguePage
            {
                text = LoadTextFromArray(page.GetBlock("text").values),
                name = page.name
            };
            if (page.namesValues.Keys.Contains("take_money"))
                newPage.takeMoney = int.Parse(page.namesValues["take_money"]);
            if (page.HasBlock("make_peace"))
                foreach (KeyValuePair<string, string> kvp in page.GetBlock("make_peace").namesValues)
                    newPage.turnNeutral.Add(kvp.Key, int.Parse(kvp.Value));
            if (page.HasBlock("become_hostile"))
                foreach (KeyValuePair<string, string> kvp in page.GetBlock("become_hostile").namesValues)
                    newPage.turnHostile.Add(kvp.Key, int.Parse(kvp.Value));
            InfoBlock pageNodes = page.GetBlock("nodes");
            if (pageNodes != null)
                foreach (string node in pageNodes.values)
                    if (nodesUnsorted.ContainsKey(node))
                        newPage.responses.Add(nodesUnsorted[node]);
            InfoBlock worldStateChanges = page.GetBlock("state_changes");
            if (worldStateChanges != null)
                foreach (KeyValuePair<string, string> kvp in worldStateChanges.namesValues)
                    newPage.worldStateChanges.Add(kvp.Key, kvp.Value == "true" ? true : false);
            InfoBlock itemsToTranfser = page.GetBlock("recieve_items");
            if (itemsToTranfser != null)
                newPage.itemsToGiveToPlayer.AddRange(itemsToTranfser.values);
            foreach (KeyValuePair<DialogueNode, string> dialogueNode in nodesAndPages)
                if (dialogueNode.Value == page.name)
                    dialogueNode.Key.leadsTo = newPage;
            foreach (KeyValuePair<DialogueNode, string> dialogueNode in nodesAndFailurePages)
                if (dialogueNode.Value == page.name)
                    dialogueNode.Key.leadsToOnFailure = newPage;
            pages.Add(page.name, newPage);
        }
        currentPage = pages[startingNode];
        LoadPage(currentPage);
    }
}