    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection.Emit;
    using TMPro;
    using Unity.VisualScripting;
    using UnityEditor.ShaderGraph;
    using UnityEditorInternal.VersionControl;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.UIElements;
    using UnityEngine.XR;
    using static UnityEngine.Rendering.DebugUI.MessageBox;
    using ColorUtility = Unity.VisualScripting.ColorUtility;
    using Label = UnityEngine.UIElements.Label;
    using MouseButton = UnityEngine.UIElements.MouseButton;
    using UnityColor = UnityEngine.Color;
using System.Threading.Tasks;//czekam az crafting ogarnie swoje


public  class ItemData
{
    public string name;
    public string category;
    public float weight;
    public Sprite icon;
    public Item originalItem;
}
public class ItemWithPosition
{
    public Item item;
    public Vector2 position;
}

public class UI_Script : MonoBehaviour
    {
    public Sprite defaultPlaceholderIcon;

        public static UI_Script Instance;
        public Inventory craftingInventory;
    
    [SerializeField] UIDocument UI_doc;
        ItemData draggedItemData; // zamiast Item

        VisualElement dragOriginElement;
        List<Image> itemIcons;
        VisualElement  root;
        List<Item> InitItemList;
        List<Item> ItemList;

        VisualElement dragIcon;
        bool isDragging = false;
        VisualElement draggedItemRoot;
        Item draggedItem;
        Image draggedSourceImage;
        VisualElement LHand;
        VisualElement RHand;
        List<VisualElement> qSlotsList;
            UnityColor style1;
            UnityColor style2;
            UnityColor style3;
            UnityColor style4;
            UnityColor style5;
        enum DragSourceType
        {
            List,
            QSlot
        }

        DragSourceType currentDragSource;
        VisualElement draggedFromSlot;
        bool dropSucceeded;
        int draggedQuantity = 1;

    private void Start()
    {
        //  HideInventory();
      //  HideCrafing();
       // ShowInventory();
    }

    void Awake()
        {

            Instance = this;
            InitItemList = new List<Item>();
            UnityEngine.ColorUtility.TryParseHtmlString("#8693AB", out style1);
            UnityEngine.ColorUtility.TryParseHtmlString("#BDD4E7", out style2);
            UnityEngine.ColorUtility.TryParseHtmlString("#212227", out style3);
            UnityEngine.ColorUtility.TryParseHtmlString("#637074", out style4);
            UnityEngine.ColorUtility.TryParseHtmlString("#AAB9CF", out style5);

            root = UI_doc.rootVisualElement;
            List<VisualElement> qSlots = root.Query<VisualElement>(className: "QSlot").ToList();
            // pobieramy listę QSlotów
            qSlotsList = root.Query<VisualElement>(className: "QSlot").ToList();
            LHand = root.Q<VisualElement>("LHand");
            RHand = root.Q<VisualElement>("RHand");
            Button CraftingButton = root.Q<Button>("CraftingButton");
            CraftingButton.clicked += CraftingSend;
        // dodajemy je do listy
        qSlotsList.Add(LHand);
            qSlotsList.Add(RHand);
            itemIcons = new List<Image>();
            
            foreach (var slot in qSlots)
            {
                Image icon = slot.Q<Image>("Item_Icon");
                //itemIcons.Add(icon);
            }


        foreach (VisualElement slot in qSlotsList)
        {
            // 1. Czyścimy ikonę (Image)
            Image icon = slot.Q<Image>("Item_Icon");
            if (icon != null)
            {
                icon.image = null;
                // Jeśli używasz .sprite zamiast .image w innych miejscach:
                // icon.sprite = null; 
            }

            // 2. Czyścimy Label z informacją (np. nazwa przedmiotu)
            Label infoLabel = slot.Q<Label>("Slot_Info");
            if (infoLabel != null)
            {
                infoLabel.text = string.Empty;
            }

            // 3. Czyścimy dane techniczne (UserData), aby system Drag&Drop wiedział, że slot jest pusty
            slot.userData = null;

            // 4. (Opcjonalnie) Jeśli masz tam numerki slotów, których nie chcesz usuwać, 
            // upewnij się, że ich nie czyścisz (u Ciebie mają klasę "Slot_Number").
        }
        foreach (var slot in qSlotsList)
            {
            // Wewnątrz foreach (var slot in qSlotsList) w Awake:
            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;

                Image icon = slot.Q<Image>("Item_Icon");
                if (icon == null || icon.image == null) 
                    return;

                // Pobierz dane zapisane w slocie
                ItemData data = slot.userData as ItemData;
                if (data == null) return;

                // Ustawiamy dane globalne dla całego systemu Drag & Drop
                draggedItemData = data;
                draggedFromSlot = slot;
                draggedSourceImage = icon;
                currentDragSource = DragSourceType.QSlot;
                dropSucceeded = false; // Kluczowe: reset przed startem

                // Startujemy ducha (ghost)
                StartDrag(evt.position, icon.image);

                // Czyścimy slot (przedmiot "wisi" w powietrzu)
                icon.image = null;
                slot.userData = null;

                // Opcjonalnie wyczyść etykietę w slocie
                Label slotLabel = slot.Q<Label>("Slot_Info");
                if (slotLabel != null) slotLabel.text = "";
            });



        }



        VisualElement about = root.Q<VisualElement>("About");
                    Label icoCol = about.Q<Label>("ico");
                    Label nameCol = about.Q<Label>("Name");
                    Label typeCol = about.Q<Label>("type");
                    Label quantityCol = about.Q<Label>("quantity");
                    Label weightCol = about.Q<Label>("weight");
                    icoCol.RegisterCallback<ClickEvent>(_ =>
                    {
                        Debug.Log("Klik: Icon");
                    });
                    nameCol.RegisterCallback<ClickEvent>(evt =>
                    {
                        Debug.Log("Kliknięto element!");
                        // Tutaj można wywołać swoją funkcję
                    });

                    typeCol.RegisterCallback<ClickEvent>(evt =>
                    {
                        Debug.Log("Kliknięto element!");
                        // Tutaj można wywołać swoją funkcję
                    });
                    quantityCol.RegisterCallback<ClickEvent>(evt =>
                    {
                        Debug.Log("Kliknięto element!");
                        // Tutaj można wywołać swoją funkcję
                    });
                    weightCol.RegisterCallback<ClickEvent>(evt =>
                    {


                    });


            List<Label> slotNumbers = root.Query<Label>(className: "Slot_Number").ToList();

            for (int i = 0; i < slotNumbers.Count; i++)
            {
                slotNumbers[i].text = (i + 1).ToString();
                slotNumbers[i].style.display = DisplayStyle.None;
            }
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);
            void OnPointerMove(PointerMoveEvent evt)
            {
                if (!isDragging || dragIcon == null)
                    return;

                dragIcon.style.left = evt.position.x - 32;
                dragIcon.style.top = evt.position.y - 32;
            }
        void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || evt.button != (int)MouseButton.LeftMouse)
                return;

            isDragging = false;
            VisualElement picked = root.panel.Pick(evt.position);
            VisualElement target = picked;

            // Szukaj rodzica z odpowiednią klasą
            while (target != null &&
                   !target.ClassListContains("QSlot") &&
                   !target.ClassListContains("Item") &&
                   !target.ClassListContains("Hand") &&
                   !target.ClassListContains("BSlots")&&
                    target.name != "Table" &&
                     target.name != "Items_scrol" &&
                !target.ClassListContains("CraftSlot"))
            {
                target = target.parent;
            }

            if (target != null)
            {
                HandleDrop(target,evt.position);
            }
            else
            {
                dropSucceeded = false; // Upuszczono w próżnię
            }

            // JEŚLI DROP SIĘ NIE POWIÓDŁ - ZWRÓĆ PRZEDMIOT
            if (!dropSucceeded)
            {
                if (currentDragSource == DragSourceType.List)
                {
                    // Przywróć do listy
                    addItem(draggedItemData.name, draggedItemData.category, draggedQuantity, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                }
                else if (currentDragSource == DragSourceType.QSlot && draggedFromSlot != null)
                {
                    // PRZYWRACANIE DANYCH
                    Image icon = draggedFromSlot.Q<Image>("Item_Icon");
                    if (icon != null) icon.image = draggedItemData.icon.texture;
                    draggedFromSlot.userData = draggedItemData;

                    // PRZYWRACANIE TEKSTU (bo w PointerDown go czyścisz)
                    Label slotLabel = draggedFromSlot.Q<Label>("Slot_Info");
                    if (slotLabel != null) slotLabel.text = draggedItemData.name;
                }
            }


            CleanupDrag();
        }







        


    }
    void HandleDrop(VisualElement target, Vector2 dropPosition)
    {
        if (draggedItemData == null) return;
        bool isStack = draggedQuantity > 1;

        if (target.name == "Table" || target.ClassListContains("TableContent")) // Celujemy w stół
        {
            Vector2 localPos = target.WorldToLocal(dropPosition);
            AddItemToTable(target, draggedItemData, localPos,draggedQuantity);
            dropSucceeded = true;
        }

        if (isStack && !(target.name == "Table" || target.ClassListContains("TableContent")))
        {
            Debug.Log(draggedQuantity);
            dropSucceeded = false;
            return;
        }

        // PRZYPADEK: QSlot -> Hand
        if (currentDragSource == DragSourceType.QSlot && target.ClassListContains("Hand"))
        {
            SetSlotData(target, draggedItemData);
            dropSucceeded = true;
        }
        // PRZYPADEK: QSlot -> QSlot (Swap / Zamiana)
        else if (currentDragSource == DragSourceType.QSlot && target.ClassListContains("QSlot"))
        {
            if (target == draggedFromSlot) // Upuszczenie na ten sam slot
            {
                dropSucceeded = false; // OnPointerUp zajmie się przywróceniem
                return;
            }

            ItemData targetData = target.userData as ItemData;

            // Wstawiamy ciągnięty przedmiot do nowego slotu
            SetSlotData(target, draggedItemData);

            // Jeśli w docelowym slocie coś było, przenieś to do starego slotu (Swap)
            if (targetData != null)
            {
                SetSlotData(draggedFromSlot, targetData);
            }

            dropSucceeded = true;
        }
        // PRZYPADEK: QSlot -> Lista (Powrót do ekwipunku)
        else if (currentDragSource == DragSourceType.QSlot &&
                (target.name == "Items_scrol" || target.ClassListContains("BSlots") || target.ClassListContains("Item")))
        {
            addItem(draggedItemData.name, draggedItemData.category, 1, draggedItemData.weight, draggedItemData.icon,draggedItemData.originalItem);
            dropSucceeded = true;
        }
        // PRZYPADEK: Lista -> Slot
        else if (currentDragSource == DragSourceType.List &&
                (target.ClassListContains("QSlot") || target.ClassListContains("Hand")))
        {
            SetSlotData(target, draggedItemData);
            dropSucceeded = true;
        }


    }

    // Pomocnicza metoda, żeby nie powtarzać kodu:
    void SetSlotData(VisualElement slot, ItemData data)
    {
        if (slot == null || data == null) return;

        Image icon = slot.Q<Image>("Item_Icon");
        if (icon != null) icon.image = data.icon.texture;

        Label nameLabel = slot.Q<Label>("Slot_Info");
        if (nameLabel != null) nameLabel.text = data.name;

        slot.userData = data;
    }
    void SetCraftingSlotData(VisualElement slot, ItemData data)
    {
        if (slot == null || data == null) return;

        Image icon = slot.Q<Image>("Crafting_Item_Ico");
        if (icon != null) icon.image = data.icon.texture;

        //Label nameLabel = slot.Q<Label>("Slot_Info");
        //if (nameLabel != null) nameLabel.text = data.name;

        slot.userData = data;
    }
    void CleanupDrag()
        {
            dragIcon?.RemoveFromHierarchy();
            dragIcon = null;
            draggedSourceImage = null;
            draggedItemRoot = null;
            draggedFromSlot = null;

            LHand.RemoveFromClassList("Hand-Active");
            RHand.RemoveFromClassList("Hand-Active");
        }


        void StartDrag(Vector2 position, Texture texture)
        {
            LHand.AddToClassList("Hand-Active");
            RHand.AddToClassList("Hand-Active");

            dragIcon = new VisualElement();
            dragIcon.style.width = 64;
            dragIcon.style.height = 64;
            dragIcon.style.position = Position.Absolute;
            dragIcon.style.backgroundImage = new StyleBackground((Background)texture);
            dragIcon.style.opacity = 0.8f;
            dragIcon.pickingMode = PickingMode.Ignore;

            root.Add(dragIcon);
            isDragging = true;

            dragIcon.style.left = position.x - 32;
            dragIcon.style.top = position.y - 32;
        }




        void setQSlot(int i, Sprite mySprite) 
        {

            itemIcons[i].sprite = mySprite;



        }
        public void addItem(string name, string category, int quantity, float weight, Sprite icon,Item original)
        {


            ItemList.Add(original);
            ScrollView scroll = root.Q<ScrollView>("Items_scrol");

            VisualElement existing = scroll.contentContainer.Q(name);

            VisualElement table = root.Q<VisualElement>("#About");






            


            if (existing == null)
            {
                // 🔹 NOWY ITEM (DIV)
                VisualElement itemRoot = new VisualElement();
                itemRoot.style.height = 40;

                itemRoot.name = name;
                itemRoot.style.flexDirection = FlexDirection.Row;
                itemRoot.style.color = style3;
                itemRoot.style.width = Length.Percent(100);
                itemRoot.style.unityTextAlign = TextAnchor.MiddleCenter;
                itemRoot.style.fontSize = 20;
                itemRoot.style.borderTopWidth = 2;
                itemRoot.style.borderBottomWidth = 2;
                itemRoot.style.marginTop = 5;
                itemRoot.AddToClassList("Item");

                itemRoot.style.borderTopColor = UnityEngine.Color.black;
                itemRoot.style.borderBottomColor = UnityEngine.Color.black;
                itemRoot.style.borderLeftColor = UnityEngine.Color.black;
                itemRoot.style.borderRightColor = UnityEngine.Color.black;

            itemRoot.userData = new ItemData
            {
                name = name,
                category = category,
                weight = weight,
                icon = icon != null ? icon : defaultPlaceholderIcon,
                originalItem = original
            };



            itemRoot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;

                // 1. Najpierw zbierz dane
                Label qtyLabel = itemRoot.Q<Label>("ItemQty");
                if (qtyLabel == null) return;
                int totalAvailable = int.Parse(qtyLabel.text);
                if (evt.shiftKey)
                {
                    draggedQuantity = totalAvailable; //wszystko
                }
                else
                {
                    draggedQuantity = 1; //jedna sztuka
                }
                draggedItemData = itemRoot.userData as ItemData;
                draggedItemRoot = itemRoot;
                currentDragSource = DragSourceType.List; // To musi być tutaj!
                draggedFromSlot = null;
                dropSucceeded = false; // Reset statusu

                // 2. Pobierz ikonę do "ducha" (ghost icon)
                Image sourceImg = itemRoot.Q<Image>();
                if (sourceImg == null) return;

                // 3. Rozpocznij przeciąganie (Ghost)
                StartDrag(evt.position, sourceImg.image);

                // 4. Dopiero teraz odejmij z listy (skoro już mamy dane w draggedItemData)

                // Aktualizacja UI listy po podniesieniu
                if (draggedQuantity >= totalAvailable)
                {
                    itemRoot.RemoveFromHierarchy();
                }
                else
                {
                    int newQty = totalAvailable - draggedQuantity;
                    qtyLabel.text = newQty.ToString();

                    Label weightLabel = itemRoot.Q<Label>("ItemWeight");
                    if (weightLabel != null)
                    {
                        weightLabel.text = (draggedItemData.weight * newQty).ToString("0.##");
                    }
                }
            });





            VisualElement imgContainer = new VisualElement();
                imgContainer.style.width = Length.Percent(10);
                imgContainer.style.justifyContent = Justify.Center;
                imgContainer.style.alignItems = Align.Center;
            
                imgContainer.style.flexShrink = 0;
                imgContainer.style.backgroundColor = style2;

                Image imagediv = new Image();
                imagediv.image = icon.texture;
                imagediv.style.height = 32;
                imagediv.scaleMode = ScaleMode.ScaleToFit;

                imgContainer.Add(imagediv);

                Label nameLabel = new Label(name);
                nameLabel.name = "ItemName";
                nameLabel.style.width= Length.Percent(40);
                nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                nameLabel.style.backgroundColor = style1;
            
                Label typeLabel = new Label(category);
                typeLabel.style.width = Length.Percent(30);
                typeLabel.name = "Type";  
                typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                typeLabel.style.backgroundColor = style5;

                Label qtyLabel = new Label(quantity.ToString());
                qtyLabel.name = "ItemQty";
                qtyLabel.style.width = Length.Percent(10);
                qtyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
         
                qtyLabel.style.backgroundColor = style1;

                Label weightLabel = new Label((weight * quantity).ToString());
                weightLabel.name = "ItemWeight";
                weightLabel.style.width = Length.Percent(10);
                weightLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
          
                weightLabel.style.backgroundColor = style5;



                itemRoot.Add(imgContainer);
                itemRoot.Add(nameLabel);
                itemRoot.Add(typeLabel);
                itemRoot.Add(qtyLabel);
                itemRoot.Add(weightLabel);

                scroll.contentContainer.Add(itemRoot);




            }
            else
            {
                // 🔹 ITEM ISTNIEJE → UPDATE
                Label qtyLabel = existing.Q<Label>("ItemQty");
                Label weightLabel = existing.Q<Label>("ItemWeight");

                if (qtyLabel == null)
                {
                    return;
                }

                int oldQty = int.Parse(qtyLabel.text);
                int newQty = oldQty + quantity;

                qtyLabel.text = newQty.ToString();
                weightLabel.text = (newQty * weight).ToString();
            }


        }

  

    



        public void SendItemList(List<Item> items)
        {
            Debug.Log("UI dostało listę itemów:");
        InitItemList = new List<Item>(items);
        ItemList = new List<Item>(items);

        foreach (Item item in items)
            {
                string name = item.itemName;
                float weight = item.weight;
                Sprite icon = item.icon != null ? item.icon : defaultPlaceholderIcon;
                 Item.ItemType type = item.itemType;
                  string typeString = type.ToString(); // ✅ This is correct






            addItem(name, typeString, 1, 10.5f,icon, item);
            }
        }

    public void ShowCrafting()
    {
        ShowInventory();
        var Crafting = root.Q<VisualElement>("Crafting");
        Crafting.style.display = DisplayStyle.Flex;

    }
    public void HideCrafing()
    {
        
        var Crafting = root.Q<VisualElement>("Crafting");
        Crafting.style.display = DisplayStyle.None;
    }
    public void HideInventory()
            {

                var BSlots = root.Q<VisualElement>("BSlots");
                var Title = root.Q<VisualElement>("Title");
                var QSlots = root.Q<VisualElement>("QSlots");
                var Crafting = root.Q<VisualElement>("Crafting");
                Crafting.style.display = DisplayStyle.None;
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        LHand.style.display = DisplayStyle.None;
            RHand.style.display = DisplayStyle.None;
            BSlots.style.display = DisplayStyle.None;
                Title.style.display = DisplayStyle.None;
            QSlots.style.backgroundColor = new UnityEngine.Color(0, 0, 0, 0.2f); // przyciemnienie


            List<Label> slot = root.Query<Label>(className: "QSlot").ToList();

                for (int i = 0; i < slot.Count; i++)
                {
                    slot[i].style.opacity = 0.8f;
            
                }

                // ustawiamy direction parenta
                var slots = QSlots.parent; // to powinien być "Slots"
                slots.style.flexDirection = FlexDirection.RowReverse;
      
    }
        public void ShowInventory()
        {
            var BSlots = root.Q<VisualElement>("BSlots");
            var Title = root.Q<VisualElement>("Title");
            var QSlots = root.Q<VisualElement>("QSlots");
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            LHand.style.display = DisplayStyle.Flex;
            RHand.style.display = DisplayStyle.Flex;
            BSlots.style.display = DisplayStyle.Flex;
            Title.style.display = DisplayStyle.Flex;

            QSlots.style.backgroundColor = new UnityEngine.Color(255, 0, 0, 1f);


            List<Label> slot = root.Query<Label>(className: "QSlot").ToList();

            for (int i = 0; i < slot.Count; i++)
            {
                slot[i].style.opacity = 1f;

            }
        

            // ustawiamy direction parenta
            var slots = QSlots.parent; // to powinien być "Slots"
            slots.style.flexDirection = FlexDirection.Row;
        }




    public void RemoveItem(string itemName, int amount = 1)
    {
        ScrollView scroll = root.Q<ScrollView>("Items_scrol");
        if (scroll == null)
            return;

        VisualElement itemRoot = scroll.contentContainer.Q(itemName);
        if (itemRoot == null)
            return;

        Label qtyLabel = itemRoot.Q<Label>("ItemQty");
        Label weightLabel = itemRoot.Q<Label>("ItemWeight");

        if (qtyLabel == null || weightLabel == null)
            return;

        int currentQty = int.Parse(qtyLabel.text);
        int newQty = currentQty - amount;
       
        int removedCount = 0;

        for (int i = ItemList.Count - 1; i >= 0; i--)
        {
            if (ItemList[i].itemName == itemName)
            {
                InitItemList.RemoveAt(i);
                removedCount++;

                if (removedCount >= amount)
                    break;
            }
        }

        if (newQty <= 0)
        {
            // 🔥 usuń cały item z listy
            itemRoot.RemoveFromHierarchy();
        }
        else
        {
            qtyLabel.text = newQty.ToString();

            // oblicz wagę jednostkową
            float totalWeight = float.Parse(weightLabel.text);
            float singleWeight = totalWeight / currentQty;

            weightLabel.text = (singleWeight * newQty).ToString("0.##");
        }
    }

    public Item GetOriginalItemFromSlot(int index)
    {
        ItemData data = GetItemFromQSlot(index);
        return data?.originalItem; 
    }
   ItemData GetItemFromQSlot(int index)
    {
        // 1. Sprawdzamy czy indeks mieści się w liście
        if (index < 0 || index >= qSlotsList.Count)
        {
            Debug.LogWarning($"Indeks {index} poza zakresem slotów!");
            return null;
        }

        // 2. Pobieramy VisualElement slotu
        VisualElement slot = qSlotsList[index];

        // 3. Pobieramy dane z userData i rzutujemy na ItemData
        ItemData data = slot.userData as ItemData;

        if (data == null)
        {
            Debug.Log($"Slot {index} jest pusty.");
            return null;
        }

        return data;
    }
    public Item GetItemLeftHand()
    {
        ItemData data = GetItemFromQSlot(9);
        return data?.originalItem;
    }
    public Item GetItemRighHand()
    {
        ItemData data = GetItemFromQSlot(9);
        return data?.originalItem;
    }
    void Update()
        {
        ShowCrafting();
    }

    void AddItemToTable(VisualElement table, ItemData data,Vector2 localPos, int quantity)
    {
        // Jeśli stół ma w środku jakiś kontener na przedmioty (np. ScrollView lub VisualElement), 
        // upewnij się, że dodajesz do niego. Jeśli nie, dodajemy bezpośrednio do 'table'.

        VisualElement itemOnTable = new VisualElement();
        float size = 64;
        itemOnTable.style.width = size;
        itemOnTable.style.height = size;
        itemOnTable.style.marginRight = 5;
        itemOnTable.style.marginBottom = 5;
        itemOnTable.style.position = Position.Absolute;
        itemOnTable.style.left = localPos.x - (size / 2);
        itemOnTable.style.top = localPos.y - (size / 2);
        itemOnTable.style.backgroundImage = new StyleBackground(data.icon.texture);


        ItemData tableData = new ItemData
        {
            name = data.name,
            category = data.category,
            weight = data.weight,
            icon = data.icon,
            originalItem = data.originalItem
        };
        itemOnTable.userData = tableData;
        itemOnTable.AddToClassList("TableItem"); // Opcjonalna klasa do stylizacji w USS
        if (quantity > 1)
        {
            Label countLabel = new Label($"x{quantity}");
            countLabel.style.position = Position.Absolute;
            countLabel.style.bottom = 0;
            countLabel.style.right = 0;
            countLabel.style.backgroundColor = new UnityColor(0, 0, 0, 0.5f);
            countLabel.style.color = UnityColor.white;
            countLabel.style.fontSize = 12;
            itemOnTable.Add(countLabel);
        }
        // Rejestrujemy podnoszenie przedmiotu ze stołu
        itemOnTable.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse) return;
            draggedQuantity = quantity;
            draggedItemData = tableData;
            draggedFromSlot = null; // To nie jest slot, tylko luźny obiekt
            currentDragSource = DragSourceType.List; // Traktujemy to jak wyciąganie z listy (powrót do eq)
            dropSucceeded = false;

            StartDrag(evt.position, data.icon.texture);

            // Usuwamy go ze stołu, bo "wisi" teraz pod myszką
            itemOnTable.RemoveFromHierarchy();
        });

        table.Add(itemOnTable);

        // Opcjonalnie: jeśli stół ma Layout ustawiony na 'Flex-Row' i 'Flex-Wrap: Wrap', 
        // przedmioty będą się ładnie układać obok siebie.
        table.style.flexDirection = FlexDirection.Row;
        table.style.flexWrap = Wrap.Wrap;
        
    }
    public List<Item> GetItemsOnTable()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        List<Item> itemsOnTable = new List<Item>();

        // Szukamy wszystkich elementów, które mają przypisaną klasę "TableItem"
        table.Query<VisualElement>(className: "TableItem").ForEach(itemElement =>
        {
            if (itemElement.userData is ItemData data)
            {
                itemsOnTable.Add(data.originalItem);
                
            }
        });

        return itemsOnTable;
    }

    private async void CraftingSend()
    {
        Debug.Log("Button was clicked!");
        
        List<Item> CraftingItems = GetItemsOnTable();
        Debug.Log(CraftingItems.Count);
        craftingInventory.inventory = new List<Item>(CraftingItems);
        Debug.Log(GetItemsOnTable());
        crafting.Instance.craft = true;
        while (crafting.Instance.craft == true)
        {

            await Task.Delay(100);
        }
        ClearTable();
        List<Item> CraftingRturn = craftingInventory.inventory;
        SpawnItemsOnTable(CraftingRturn);
        craftingInventory.inventory.Clear();

    }
    void ClearTable()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null) return;

        // Szukamy wszystkich elementów, które dodaliśmy jako przedmioty
        List<VisualElement> itemsToRemove = table.Query<VisualElement>(className: "TableItem").ToList();

        foreach (var item in itemsToRemove)
        {
            item.RemoveFromHierarchy();
        }

        Debug.Log("Stół został wyczyszczony.");
    }
    public void SpawnItemsOnTable(List<Item> itemsToPlace)
    {
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null || itemsToPlace == null) return;


        // Parametry układu (Grid)
        float slotSize = 64f;
        float padding = 10f;
        int columns = 4; // Ile przedmiotów w rzędzie

        for (int i = 0; i < itemsToPlace.Count; i++)
        {
            Item item = itemsToPlace[i];

            // 2. Przygotowujemy dane ItemData (bo tego wymaga Twoja funkcja AddItemToTable)
            ItemData data = new ItemData
            {
                name = item.itemName,
                category = item.itemType.ToString(),
                weight = item.weight,
                icon = item.icon != null ? item.icon : defaultPlaceholderIcon,
                originalItem = item
            };

            // 3. Obliczamy pozycję w siatce (x, y)
            float x = (i % columns) * (slotSize + padding) + (slotSize / 2);
            float y = (i / columns) * (slotSize + padding) + (slotSize / 2);
            Vector2 pos = new Vector2(x, y);
            AddItemToTable(table, data, pos, 1);
        }
    }
    private Vector2 GetAverageItemPosition() //poprostu licze gdzie chce aby przedmiot poajiwl sie na stole
    {
        VisualElement table = root.Q<VisualElement>("Table");
        // Pobieramy wszystkie elementy, które są "przedmiotami" na stole
        var items = table.Query<VisualElement>(className: "TableItem").ToList();

        if (items.Count == 0)
        {
            // Jeśli stół jest pusty, zwróć środek stołu jako fallback
            return new Vector2(table.layout.width / 2, table.layout.height / 2);
        }

        Vector2 sum = Vector2.zero;

        foreach (var item in items)
        {
            sum.x += item.layout.x;
            sum.y += item.layout.y;
        }

        
        return new Vector2(sum.x / items.Count, sum.y / items.Count);
    }
}




