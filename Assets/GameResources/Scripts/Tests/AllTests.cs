using System.Collections;
using System.Collections.Generic;
//using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/*
namespace Tests
{
    public class AllTests
    {
        private GameObject world;
        private GameObject UI;
        private Game gameComponent;

        [SetUp]
        public void Setup()
        {
            UI = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UItest"));
            world = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Worldtest"));
            gameComponent = world.GetComponent<Game>();
            gameComponent.Start();
        }

        [TearDown]
        public void Teardown()
        {
            GameObject.Destroy(UI);
            GameObject.Destroy(world);
        }

        [Test]
        public void CharacterCreationTest()
        {
            string templateName = "stalker_newbie";
            GameObject character = gameComponent.CreateCharacter(templateName, Vector2.zero);
            Assert.IsNotNull(character, templateName + " could not be created");
        }

        [Test]
        public void MagazineLoadTest()
        {
            string testWeaponName = "PM";
            string fittingMagazine = "PM8Rounds";
            //string unfittingMagazine = "AKmag";
            string testMagazineName = fittingMagazine;
            GameObject weapon = gameComponent.CreateItem(testWeaponName);
            GameObject magazine = gameComponent.CreateItem(testMagazineName);
            Firearm firearmComponent = weapon.GetComponent<Firearm>();
            firearmComponent.UnloadMagazine();
            Assert.IsTrue(firearmComponent.magazine == null, weapon + " already has magazine");
            firearmComponent.LoadMagazine(magazine);
            Assert.IsTrue(firearmComponent.magazine != null, weapon + " does not have magazine");
            Assert.AreSame(magazine, firearmComponent.magazine, magazine + " can not fit in " + weapon);
        }

        [Test]
        public void AmmoLoadTest()
        {
            string testAmmoName = "5,45x39";
            string fittingMagazine = "AK74mag";
            //string unfittingMagazine = "AKmag";
            string testMagazineName = fittingMagazine;
            GameObject ammo = gameComponent.CreateItem(testAmmoName);
            GameObject magazine = gameComponent.CreateItem(testMagazineName);
            Magazine magazineComponent = magazine.GetComponent<Magazine>();
            AmmoBox ammoBoxComponent = ammo.GetComponent<AmmoBox>();
            magazineComponent.LoadFromAmmoBox(ammo);
            Assert.IsTrue(magazineComponent.currentCaliber.caliber == ammoBoxComponent.bulletType.caliber, "Tried to load wrong ammo type to " + magazine + "; base caliber should be " + magazineComponent.caliber + " instead of " + ammoBoxComponent.bulletType.caliber);
        }

        [Test]
        public void HealPlayerTest()
        {
            Character playerCharacterComponent = gameComponent.characterController.controlledCharacter.GetComponent<Character>();
            float maxPlayerHealth = playerCharacterComponent.GetComponent<ObjectAttributes>().GetAttribute("Health").maxValue;
            playerCharacterComponent.Heal(10000000000000f);
            Assert.LessOrEqual(playerCharacterComponent.Health, maxPlayerHealth, "Player health is larger than max health: " + playerCharacterComponent.Health + ">" + maxPlayerHealth);
        }

        [Test]
        public void EquipToDefaultSlotTest()
        {
            string templateName = "stalker_newbie";
            string fittingItem = "BeltA";
            GameObject character = gameComponent.CreateCharacter(templateName, Vector2.zero);
            GameObject vest = gameComponent.CreateItem(fittingItem);
            Character characterComponent = character.GetComponent<Character>();
            characterComponent.RemoveItemFromBuiltinSlot(BuiltinCharacterSlots.Vest);
            characterComponent.EquipVest(vest);
            Assert.NotNull(vest.transform.parent.parent.parent, vest + " is not equipped anywhere");
            Assert.IsTrue(vest.transform.parent.parent.parent == characterComponent.transform, vest + " is not equipped on " + characterComponent.gameObject);
        }
    }
}*/