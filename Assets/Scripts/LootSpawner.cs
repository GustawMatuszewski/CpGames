using UnityEngine;
using System.Collections.Generic;

public class LootSpawner : MonoBehaviour
{
    [Header("Ustawienia Lootu")]
    [Tooltip("Lista przedmiotów, które mogą się wylosować")]
    public List<Item> possibleItems; 
    
    [Header("Ilość (różnych) przedmiotów w skrzynce")]
    public int minItems = 1;
    public int maxItems = 4;

    [Header("Max sztuk danego przedmiotu")]
    public int maxQuantityPerItem = 3;

    void Start()
    {
        if (possibleItems == null || possibleItems.Count == 0)
        {
            Debug.LogWarning("LootSpawner: Brak przedmiotów w liście possibleItems!");
            return;
        }

        // Znajdź wszystkie ekwipunki na scenie
        Inventory[] allInventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);

        foreach (Inventory inv in allInventories)
        {
            // Pomijamy ekwipunek gracza (sprawdzamy po komponencie KCC oraz po typie)
            if (inv.GetComponent<KCC>() != null || inv.type == InventoryType.Player)
                continue;

            // Losujemy, ile różnych rodzajów przedmiotów ma dostać ta skrzynia
            int itemsToGenerate = Random.Range(minItems, maxItems + 1);

            for (int i = 0; i < itemsToGenerate; i++)
            {
                // Losujemy przedmiot z puli
                Item randomItem = possibleItems[Random.Range(0, possibleItems.Count)];
                
                // Losujemy ilość tego konkretnego przedmiotu (np. 1 do 3 sztuk)
                int quantity = Random.Range(1, maxQuantityPerItem + 1);

                // Dodajemy do znalezionego ekwipunku
                inv.Add(randomItem, quantity);
            }
        }
    }
}