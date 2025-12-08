using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public bool debugMode = true;

    public List<Item> inventory = new List<Item>();
    public Inventory outsideInventory;

    public List<Item> allItems = new List<Item>();


    public void AddToInventory(List<Item> inventoryToAddTo, int itemQuantity, int itemID){
        Item itemToAdd = allItems[itemID];

        for (int i = 0; i < itemQuantity; i++){
            inventoryToAddTo.Add(itemToAdd);

            if(debugMode)
                Debug.Log($"Added {itemQuantity} items with ID {itemID}");
        }
    }

    public void RemoveFromInventory(List<Item> inventoryToRemoveFrom, int itemID, int itemQuantity){
        for (int i = 0; i < itemQuantity; i++){
            Item itemToRemove = inventoryToRemoveFrom.Find(item => item.itemID == itemID);
            if(itemToRemove != null)
                inventoryToRemoveFrom.Remove(itemToRemove);

            if(debugMode)
                Debug.Log($"Removed {itemQuantity} items with ID {itemID}");
        }
    }
    
    public void MoveBetweenInventories(List<Item> fromInventory, List<Item> toInventory, int itemID, int itemQuantity){
        List<Item> itemsToMove = fromInventory.FindAll(item => item.itemID == itemID);

        if (itemsToMove.Count == 0){
            if(debugMode)
                Debug.Log("No items with this ID in source inventory!");
            return;
        }

        int moveCount = Mathf.Min(itemQuantity, itemsToMove.Count);
        for (int i = 0; i < moveCount; i++){
            Item item = itemsToMove[i];
            fromInventory.Remove(item);
            toInventory.Add(item);
        }

        if(debugMode)
            Debug.Log($"Moved {moveCount} items with ID {itemID}");
    }

}
