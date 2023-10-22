using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;


public class UIController : MonoBehaviour
{
   public UIDocument doc;
   private VisualElement root;
   public VisualTreeAsset listTemplate;
   public PlayerController playerController;

   private void Start()
   {
      root = doc.rootVisualElement;
      
      root.Q<VisualElement>("mod-menu").visible = false;
      
      UpdateInventory();      
   }
   
   public void ToggleModMenu()
   {
      var modMenu = root.Q<VisualElement>("mod-menu");
      modMenu.visible = !modMenu.visible;
   }

   public void UpdateStats()
   {
      if (playerController.level == null) return;
      
      var statsText = root.Q<Label>("stats");
      
      statsText.text = "Level: " + playerController.level + "\n" + "XP: " + playerController.xp + "\n" + "HP: " + playerController.hp + "/" + playerController.maxHp;
   }
   
   public void UpdateInventory()
   {
      return;
      /*if (playerController.inventory == null) return;

      var inventory = playerController.inventory;

      var inventoryList = root.Q<ScrollView>("inventory-list");

      inventoryList.Clear();

      foreach (var item in inventory)
      {
         var label = new Label();

         var itemData = ConvertObject<Dictionary<string, string>>(item[0]);

         if (itemData["type"] == "tombstone")
         {
            label.text = item[1] as string + " - " + itemData["type"] + " - " + itemData["text"];
         }
         else
         {
            label.text = item[1] as string + " - " + itemData["type"];
         }

         if (label.text.Length > 45)
         {
            label.text = label.text.Substring(0, 45) + "...";
         }

         label.style.color = new StyleColor(new Color32(191, 191, 191 ,255));
         label.style.fontSize = Length.Percent(10f);
         label.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
         string itemIndex = inventory.IndexOf(item).ToString();
         label.userData = itemIndex;

         if (itemIndex == playerController.currentSlot)
         {
            label.style.backgroundColor = new StyleColor(new Color32(255, 255, 255, 50));
         }

         label.RegisterCallback<ClickEvent>(OnInventoryItemClicked);

         inventoryList.Add(label);
      }*/
   }
   private void OnInventoryItemClicked(ClickEvent evt)
   {
      var targetBox = evt.target as VisualElement;
      targetBox.style.backgroundColor = new StyleColor(new Color32(255, 255, 255, 50));      
      
      var inventoryList = root.Q<ScrollView>("inventory-list").Children();

      foreach (var item in inventoryList)
      {
         if (item == targetBox) continue;
         item.style.backgroundColor = new StyleColor(new Color32(0, 0, 0, 0));      
      }
      
      playerController.currentSlot = targetBox.userData as string;
   }
   
   private static TValue ConvertObject<TValue>(object obj)
   {       
      var json = JsonConvert.SerializeObject(obj);
      var res = JsonConvert.DeserializeObject<TValue>(json);   
      return res;
   }
}
