using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMenu : MonoBehaviour
{
    private bool _attributeChangeEnabled = false;
    public bool AttributeChangeEnabled { get { return _attributeChangeEnabled; } }

    private int characterPointsInvestedTotal = 0;
    private World world;

    public List<AttributeTracker> changedAttributes = new List<AttributeTracker>();
    public Character player;
    public GameObject confirmButtonObject;
    public GameObject closeButtonObject;

    private void Start()
    {
        world = World.GetInstance();
        player = world?.Player?.GetComponent<Character>();
        if (player != null)
            if (player.freeCharacterPoints > 0)
                _attributeChangeEnabled = true;
    }

    public void OnEnable()
    {
        player = world?.Player?.GetComponent<Character>();
        if (player != null)
            if (player.freeCharacterPoints > 0)
                _attributeChangeEnabled = true;
        gameObject.SetActive(true);
        closeButtonObject = transform.Find("CloseButton").gameObject;
        confirmButtonObject = closeButtonObject.transform.Find("ConfirmButton").gameObject;
        confirmButtonObject.SetActive(false);
    }

    public bool TakeCharacterPoint()
    {
        if (player.freeCharacterPoints > 0)
        {
            player.freeCharacterPoints--;
            characterPointsInvestedTotal++;
            return true;
        }
        return false;
    }

    public bool ReturnCharacterPoint()
    {
        if (characterPointsInvestedTotal > 0)
        {
            player.freeCharacterPoints++;
            characterPointsInvestedTotal--;
            return true;
        }
        return false;
    }

    public void Close()
    {
        //gameObject.SetActive(false);
        world.SwitchUIMode(InGameUI.Interface);
        world.characterController.temporaryLockTime = 4;
        foreach (AttributeTracker attribute in changedAttributes)
        {
            player.freeCharacterPoints += attribute.pointsInvested;
            attribute.pointsInvested = 0;
        }
        changedAttributes.Clear();
    }

    public void ConfirmCharacterPointInvestment()
    {
        foreach (AttributeTracker attribute in changedAttributes)
        {
            if (attribute.increaseWithMax)
                player.GetAttribute(attribute.trackedAttributeName).MaxValue += attribute.pointsInvested * attribute.multiplier;
            player.GetAttribute(attribute.trackedAttributeName).Modify(attribute.pointsInvested * attribute.multiplier);
            attribute.pointsInvested = 0;
        }
    }

    public void Update()
    {
        if (player != null)
            _attributeChangeEnabled = player.freeCharacterPoints > 0;
        confirmButtonObject.SetActive(characterPointsInvestedTotal > 0);
    }
}