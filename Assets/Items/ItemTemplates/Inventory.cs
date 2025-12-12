using UnityEngine;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    [Header("DEBUG MODE!!!")]
    public bool debugMode = true;

    [Header("References")]
    public Inventory outsideInventory;
    public ItemDatabase itemDatabase;

    [Header("Inventory ITEMS")]
    public List<Item> inventory = new List<Item>();
    public void AddToInventory(List<Item> targetInventory, int quantity, Item item){
        for (int i = 0; i < quantity; i++){
            Item instance = Instantiate(item);
            targetInventory.Add(instance);
            if (debugMode)
                Debug.Log("Added "+ item.itemName);
        }
    }

    public void Add(Item item, int quantity = 1){
        AddToInventory(inventory, quantity, item);
    }

    public void RemoveFromInventory(List<Item> fromInventory, Item item, int quantity){
        int removed = 0;
        for (int i = fromInventory.Count - 1; i >= 0 && removed < quantity; i--){
            if (fromInventory[i] != null){
                if (fromInventory[i] == item){
                    fromInventory.RemoveAt(i);
                    removed++;
                    if (debugMode)
                        Debug.Log("Removed "+ item.itemName);
                }
            }
        }
    }

    public void MoveBetweenInventories(List<Item> fromInventory, List<Item> toInventory, Item item, int quantity){
        int moved = 0;
        for (int i = fromInventory.Count - 1; i >= 0 && moved < quantity; i--){
            if (fromInventory[i] != null){
                if (fromInventory[i] == item){
                    Item instance = fromInventory[i];
                    fromInventory.RemoveAt(i);
                    toInventory.Add(instance);
                    moved++;
                }
            }   
        }
        if (debugMode)
            Debug.Log($"Moved {moved} of {item.itemName}");
    }

    public bool HasItem(Item item){
        return inventory.Exists(i => i == item);
    }
}
