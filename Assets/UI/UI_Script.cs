using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Reflection.Emit;
    using TMPro;
    using Unity.VisualScripting;
    using UnityEditor.ShaderGraph;
    using UnityEngine;
    using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR;
    using static UnityEngine.Rendering.DebugUI.MessageBox;
using ColorUtility = Unity.VisualScripting.ColorUtility;
    using Label = UnityEngine.UIElements.Label;
    using MouseButton = UnityEngine.UIElements.MouseButton;
    using UnityColor = UnityEngine.Color;

public  class ItemData
{
    public string name;
    public string category;
    public float weight;
    public Sprite icon;
    public Item originalItem;
}

public class UI_Script : MonoBehaviour
    {
        public static UI_Script Instance;

        [SerializeField] UIDocument UI_doc;
    ItemData draggedItemData; // zamiast Item

    VisualElement dragOriginElement;
        List<Image> itemIcons;
        VisualElement  root;
        List<Item> TempItems;
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




    void Awake()
        {

            Instance = this;
            TempItems = new List<Item>();
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
                if (icon == null || icon.image == null) return;

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
                   !target.ClassListContains("BSlots"))
            {
                target = target.parent;
            }

            if (target != null)
            {
                HandleDrop(target);
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
                    addItem(draggedItemData.name, draggedItemData.category, 1, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
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
    void HandleDrop(VisualElement target)
    {
        if (draggedItemData == null) return;

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
                icon = icon,
                originalItem = original
            };



            itemRoot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse) return;

                // 1. Najpierw zbierz dane
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
                Label qtyLabel = itemRoot.Q<Label>("ItemQty");
                if (qtyLabel != null)
                {
                    int dragOriginQty = int.Parse(qtyLabel.text);
                    if (dragOriginQty <= 1)
                        itemRoot.RemoveFromHierarchy();
                    else
                    {
                        int newQty = dragOriginQty - 1;
                        qtyLabel.text = newQty.ToString();
                        Label weightLabel = itemRoot.Q<Label>("ItemWeight");
                        if (weightLabel != null)
                        {
                            float single = draggedItemData.weight;
                            weightLabel.text = (single * newQty).ToString("0.##");
                        }
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
            TempItems = items;

            foreach (Item item in items)
            {
                string name = item.itemName;
                float weight = item.weight;
                Sprite icon = item.icon;
                 Item.ItemType type = item.itemType;
                  string typeString = type.ToString(); // ✅ This is correct






            addItem(name, typeString, 1, 10.5f,icon, item);
            }
        }


        public void HideInventory()
            {
                var BSlots = root.Q<VisualElement>("BSlots");
                var Title = root.Q<VisualElement>("Title");
                var QSlots = root.Q<VisualElement>("QSlots");



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
        RemoveItem("Coal");

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
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                
                HideInventory();
            RemoveItem("coal");
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {

                ShowInventory();
            }
        }





    }




