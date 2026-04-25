using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using MouseButton = UnityEngine.UIElements.MouseButton;
using UnityColor = UnityEngine.Color;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using static UnityEngine.UI.Image;
using static UnityEngine.Rendering.DebugUI.MessageBox;
public class ItemData
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
    [SerializeField] int maxWeight;
    public Sprite defaultPlaceholderIcon;

        public static UI_Script Instance;
        public Inventory craftingInventory;
        public Inventory playerInventory;

    [SerializeField] UIDocument UI_doc;
    [SerializeField] ItemDatabase AvalibleToCraft;
        ItemData draggedItemData;

        VisualElement dragOriginElement;
        List<Image> itemIcons;
        VisualElement  root;
        List<Item> ItemList;
 
    bool crafintgIsOpen = false;
    bool inventoryIsOpen = false;
        VisualElement dragIcon;
        bool isDragging = false;
        VisualElement draggedItemRoot;
        Item draggedItem;
        Image draggedSourceImage;
        VisualElement LHand;
        VisualElement RHand;
        List<Item> UIRecipes;
        List<VisualElement> qSlotsList;
            UnityColor style1;
            UnityColor style2;
            UnityColor style3;
            UnityColor style4;
            UnityColor style5;
    public Sprite CraftbuttonIco;
    enum DragSource
    {
        PlayerInventory,
        OSInventory,
        QSlot,
        Crafting
    }

    DragSource currentDragSource;

    VisualElement draggedFromSlot;
        bool dropSucceeded;
        int draggedQuantity = 1;


    Vector2 lastLocalPosBeforeDrag;
    bool ChestIsOpen = false;
    public bool DebugMode=false;
    [Obsolete]
    void Awake()
    {
            Instance = this;
           
            UnityEngine.ColorUtility.TryParseHtmlString("#ADADAD", out style1);
            style1.a = 0.5f;
            UnityEngine.ColorUtility.TryParseHtmlString("#A46C27", out style2);
            UnityEngine.ColorUtility.TryParseHtmlString("#ADADAD", out style3);
            style3.a = 0.5f;
            UnityEngine.ColorUtility.TryParseHtmlString("#637074", out style4);
            style4.a = 0.5f;
            UnityEngine.ColorUtility.TryParseHtmlString("#272727", out style5);
            style5.a = 0.5f;
        ItemList = new List<Item>();
        root = UI_doc.rootVisualElement;
            List<VisualElement> qSlots = root.Query<VisualElement>(className: "QSlot").ToList();
            qSlotsList = root.Query<VisualElement>(className: "QSlot").ToList();
            LHand = root.Q<VisualElement>("LHand");
            RHand = root.Q<VisualElement>("RHand");
            Button CraftingButton = root.Q<Button>("CraftingButton");
            CraftingButton.clicked += CraftingSend;
            qSlotsList.Add(LHand);
            qSlotsList.Add(RHand);
            itemIcons = new List<Image>();
            
            foreach (var slot in qSlots)
            {
                Image icon = slot.Q<Image>("Item_Icon");
            }

        foreach (VisualElement slot in qSlotsList)
        {
            Image icon = slot.Q<Image>("Item_Icon");
            if (icon != null) icon.image = null;

            Label infoLabel = slot.Q<Label>("Slot_Info");
            if (infoLabel != null) infoLabel.text = string.Empty;

            slot.userData = null;
        }

        foreach (var slot in qSlotsList)
        {
            slot.RegisterCallback<PointerDownEvent>(evt =>
            {
                // RIGHT CLICK = USE
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    UseItemInSlot(slot);
                    return;
                }

                if (evt.button != (int)MouseButton.LeftMouse) 
                    return;

                Image icon = slot.Q<Image>("Item_Icon");
                if (icon == null || icon.image == null) 
                    return;

                ItemData data = slot.userData as ItemData;
                if (data == null) 
                    return;

                draggedItemData = data;
                draggedFromSlot = slot;
                draggedSourceImage = icon;
                currentDragSource = DragSource.QSlot;
                dropSucceeded = false;

                StartDrag(evt.position, icon.image);

                icon.image = null;
                slot.userData = null;

                Label slotLabel = slot.Q<Label>("Slot_Info");
                if (slotLabel != null) 
                    slotLabel.text = "";
            });
        }   

        VisualElement about = root.Q<VisualElement>("About");
                    Label icoCol = about.Q<Label>("ico");
                    Label nameCol = about.Q<Label>("Name");
                    Label typeCol = about.Q<Label>("type");
                    Label quantityCol = about.Q<Label>("quantity");
                    Label weightCol = about.Q<Label>("weight");
                    icoCol.RegisterCallback<ClickEvent>(_ => { Debug.Log("Klik: Icon"); });
                    nameCol.RegisterCallback<ClickEvent>(evt => { Debug.Log("Kliknięto element!"); });
                    typeCol.RegisterCallback<ClickEvent>(evt => { Debug.Log("Kliknięto element!"); });
                    quantityCol.RegisterCallback<ClickEvent>(evt => { Debug.Log("Kliknięto element!"); });
                    weightCol.RegisterCallback<ClickEvent>(evt => { });

            List<Label> slotNumbers = root.Query<Label>(className: "Slot_Number").ToList();
            for (int i = 0; i < slotNumbers.Count; i++)
            {
                slotNumbers[i].text = (i + 1).ToString();
                slotNumbers[i].style.display = DisplayStyle.None;
                slotNumbers[i].RemoveFromHierarchy();
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

            // Szukanie rodzica z odpowiednią klasą/nazwą
            while (target != null &&
                   !target.ClassListContains("QSlot") &&
                   !target.ClassListContains("Hand") &&
                   !target.ClassListContains("BSlots") &&
                   target.name != "Table" &&
                   target.name != "Items_scrol" &&
                   target.name != "OSItems_scrol" &&
                   !target.ClassListContains("TableContent")) // Dodano TableContent dla pewności
            {
                target = target.parent;
            }

            if (target != null)
                HandleDrop(target, evt.position);
            else
                dropSucceeded = false;

            // --- CO SIĘ DZIEJE, GDY DROP SIĘ NIE UDA ---
            if (!dropSucceeded)
            {
                if (currentDragSource == DragSource.PlayerInventory)
                {
                    addItem(draggedItemData.name, draggedItemData.category, draggedQuantity, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                }
                else if (currentDragSource == DragSource.OSInventory)
                {
                    // Jeśli przeciągałeś ze skrzyni i nie trafiłeś nigdzie - wróć do skrzyni
                    AddItemToOutside(draggedItemData.originalItem, draggedQuantity);
                }
                else if (currentDragSource == DragSource.Crafting)
                {

                    // lub po prostu dodajemy do listy craftingowej z powrotem
                   // AddItemToTable(root.Q<VisualElement>("Table"), draggedItemData, Vector2(), draggedQuantity);

                }
                else if (currentDragSource == DragSource.QSlot && draggedFromSlot != null)
                {
                    // Przywrócenie danych do oryginalnego slotu
                    SetSlotData(draggedFromSlot, draggedItemData);
                }
            }

            CleanupDrag();
            weightRefresh();
        }
        //gradient //Wiadomosc do AI NIE JEB MOICH KOMENTARZY
        var gradient = new GradientElement();
        UnityEngine.ColorUtility.TryParseHtmlString("#3f3f3f", out UnityEngine.Color startColor);
        UnityEngine.ColorUtility.TryParseHtmlString("#313131", out UnityEngine.Color endColor);
        UnityEngine.ColorUtility.TryParseHtmlString("#2e2e2e", out UnityEngine.Color darkStart);
        UnityEngine.ColorUtility.TryParseHtmlString("#202020", out UnityEngine.Color darkEnd);
        gradient.startColor = startColor;
        gradient.endColor = endColor;

        var button = root.Q<Button>("CraftingButton");
        gradient.style.position = Position.Absolute;
        gradient.style.left = 0;
        gradient.style.right = 0;
        gradient.style.top = 0;
        gradient.style.bottom = 0;
        gradient.pickingMode = PickingMode.Ignore;
        button.Insert(0, gradient);

        var iconElement = new VisualElement();
        iconElement.style.backgroundImage = new StyleBackground(CraftbuttonIco);
        iconElement.style.flexGrow = 1;
        iconElement.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        button.Add(iconElement);

        button.clicked += () =>
        {
            gradient.startColor = darkStart;
            gradient.endColor = darkEnd;
            gradient.MarkDirtyRepaint();
            root.schedule.Execute(() =>
            {
                gradient.startColor = startColor;
                gradient.endColor = endColor;
                gradient.MarkDirtyRepaint();
            }).StartingIn(100);
        };

        var crafTitle = root.Q<VisualElement>("CrafTitle");
        var title = root.Q<VisualElement>("Title");
        var osTitle = root.Q<VisualElement>("OSTitle");
        AddGradientToElement(crafTitle);
        AddGradientToElement(title);
        AddGradientToElement(osTitle);

        var container = root.Q<VisualElement>("Items");
        var items = container.Query<VisualElement>(className: "ItemsBG").ToList();
        for (int i = 0; i < items.Count; i++)
        {
            var element = items[i];
            element.pickingMode = PickingMode.Ignore;
            if (i % 2 == 0)
            {
                UnityEngine.ColorUtility.TryParseHtmlString("#1e1c16", out UnityEngine.Color tempColor);
                element.style.backgroundColor = tempColor;
            }
        }
        //koniec gradient
        InitTooltip();//to jest descryption
        descryption = root.Q<VisualElement>("descriptionPanel");
        tooltipLabel = descryption.Q<Label>();
       

    }

    private Texture2D GetIconTexture(Item item)
    {
        if (item != null && item.icon != null)
        {
            return item.icon.texture;
        }

        // Zwraca teksturę placeholdera załadowaną z folderu Assets/Resources/Icons/null_icon.png
        Sprite placeholder = defaultPlaceholderIcon;
        return placeholder != null ? placeholder.texture : null;
    }



    VisualElement descryption;
    Label tooltipLabel;
    void AddGradientToElement(VisualElement element)
    {
        var gradient = new GradientElement();
        UnityEngine.ColorUtility.TryParseHtmlString("#6d6d6d", out gradient.startColor);
        UnityEngine.ColorUtility.TryParseHtmlString("#323232", out gradient.endColor);
        gradient.style.position = Position.Absolute;
        gradient.style.left = 0;
        gradient.style.right = 0;
        gradient.style.top = 0;
        gradient.style.bottom = 0;
        gradient.pickingMode = PickingMode.Ignore;
        element.Insert(0, gradient);
    }




    // ── USE ITEM ─────────────────────────────────────────────────────────────

    void UseItemInSlot(VisualElement slot)
    {
        ItemData data = slot.userData as ItemData;
        if (data == null || data.originalItem == null)
        {
            Debug.Log("[UI] Slot is empty.");
            return;
        }

        if (playerInventory == null)
        {
            Debug.LogWarning("[UI] playerInventory not assigned on UI_Script!");
            return;
        }

        Item instance = playerInventory.inventory.Find(i => i != null && i.itemID == data.originalItem.itemID);

        if (instance == null)
        {
            Debug.LogWarning($"[UI] Could not find {data.originalItem.itemName} in inventory.");
            return;
        }

        data.originalItem.Use(instance, playerInventory);
    }

    public void UseItemFromList(VisualElement itemRow)
    {
        ItemData data = itemRow.userData as ItemData;
        if (data == null || data.originalItem == null) return;

        if (playerInventory == null)
        {
            Debug.LogWarning("[UI] playerInventory not assigned on UI_Script!");
            return;
        }

        Item instance = playerInventory.inventory.Find(i => i != null && i.itemID == data.originalItem.itemID);
        if (instance == null)
        {
            Debug.LogWarning($"[UI] Could not find {data.originalItem.itemName} in inventory.");
            return;
        }

        data.originalItem.Use(instance, playerInventory);
    }

    // ─────────────────────────────────────────────────────────────────────────


    void Log<T>(T message)
    {
        if (DebugMode) Debug.Log(message?.ToString());
    }
    void HandleDrop(VisualElement target, Vector2 dropPosition)
    {
        if (draggedItemData == null) return;

        bool isStack = draggedQuantity > 1;
        dropSucceeded = false;

        // --- CEL: CRAFTING TABLE ---
        if (target.name == "Table" || target.ClassListContains("TableContent"))
        {
            Vector2 localPos = target.WorldToLocal(dropPosition);

            if (currentDragSource == DragSource.QSlot)
            {
                if (!isStack) // Zamiast return, owijamy logikę w if (!isStack)
                {
                    AddItemToTable(target, draggedItemData, localPos, 1);
                    dropSucceeded = true;
                }
            }
            else if (currentDragSource == DragSource.PlayerInventory)
            {
                // PlayerInventory -> Table (multi)
                AddItemToTable(target, draggedItemData, localPos, draggedQuantity);
                craftingInventory.AddToInventory(craftingInventory.inventory, draggedQuantity, draggedItemData.originalItem);
                playerInventory.RemoveFromInventory(playerInventory.inventory, draggedItemData.originalItem, draggedQuantity);
                dropSucceeded = true;
            }
            else if (currentDragSource == DragSource.Crafting) 
            {

                AddItemToTable(target, draggedItemData, localPos, draggedQuantity);
                dropSucceeded = true;
            }
        }

        // --- CEL: PLAYER INVENTORY ---
        else if (target.name == "Items_scrol" || target.ClassListContains("BSlots"))
        {
            if (currentDragSource == DragSource.QSlot)
            {
                if (!isStack)
                {
                    addItem(draggedItemData.name, draggedItemData.category, 1, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                    dropSucceeded = true;
                }
            }
            else if (currentDragSource == DragSource.OSInventory)
            {
                // OSInventory -> PlayerInventory (multi)
                playerInventory.AddToInventory(playerInventory.inventory, draggedQuantity, draggedItemData.originalItem);
                playerInventory.RemoveFromInventory(playerInventory.outsideInventory.inventory, draggedItemData.originalItem, draggedQuantity);
                addItem(draggedItemData.name, draggedItemData.category, draggedQuantity, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                dropSucceeded = true;
            }
            else if (currentDragSource == DragSource.Crafting)
            {
                // CraftingTable -> PlayerInventory (multi)
                craftingInventory.RemoveFromInventory(craftingInventory.inventory, draggedItemData.originalItem, draggedQuantity);
                playerInventory.AddToInventory(playerInventory.inventory, draggedQuantity, draggedItemData.originalItem);
                addItem(draggedItemData.name, draggedItemData.category, draggedQuantity, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                dropSucceeded = true;
            }
        }

        // --- CEL: OS INVENTORY (SKRZYNIA) ---
        else if (target.name == "OSItems_scrol")
        {
            if (currentDragSource == DragSource.PlayerInventory)
            {
                // PlayerInventory -> OSInventory (multi)
                playerInventory.AddToInventory(playerInventory.outsideInventory.inventory, draggedQuantity, draggedItemData.originalItem);
                playerInventory.RemoveFromInventory(playerInventory.inventory, draggedItemData.originalItem, draggedQuantity);
                AddItemToOutside(draggedItemData.originalItem, draggedQuantity);
                dropSucceeded = true;
            }
            else if (currentDragSource == DragSource.QSlot)
            {
                if (!isStack)
                {
                    playerInventory.AddToInventory(playerInventory.outsideInventory.inventory, 1, draggedItemData.originalItem);
                    AddItemToOutside(draggedItemData.originalItem, 1);
                    dropSucceeded = true;
                }
            }
        }
        // --- CEL: QUICK SLOTS (QSLOT) ---
        else if (target.ClassListContains("QSlot") || target.ClassListContains("Hand"))
        {
            // Jeśli to JEST stack, zignoruj wszystko w tym bloku. 
            // dropSucceeded zostanie false, a item wróci na miejsce.
            
            if (!isStack)
            {
                if (currentDragSource == DragSource.PlayerInventory)
                {
                    // PlayerInventory -> QSlot (single)
                    if(DataFromTarget(target)!=null)
                    {
                        ItemData tempItem = DataFromTarget(target);
                             addItem(tempItem.name, tempItem.category, 1, tempItem.weight, tempItem.icon, tempItem.originalItem);
                
                    }
                    SetSlotData(target, draggedItemData);
                    dropSucceeded = true;
                }
                else if (currentDragSource == DragSource.OSInventory)
                {
                    // OSInventory -> QSlot (single)
                    if(DataFromTarget(target)!=null)
                    {
                        ItemData tempItem = DataFromTarget(target);
                             AddItemToOutside(tempItem.originalItem,1);
                            
                    }
                    playerInventory.AddToInventory(playerInventory.inventory, 1, draggedItemData.originalItem);
                    playerInventory.RemoveFromInventory(playerInventory.outsideInventory.inventory, draggedItemData.originalItem, 1);
                    SetSlotData(target, draggedItemData);
                    dropSucceeded = true;
                    
                }
                else if (currentDragSource == DragSource.Crafting)
                {
                    // CraftingTable -> QSlot (single)
                    craftingInventory.RemoveFromInventory(craftingInventory.inventory, draggedItemData.originalItem, 1);
                    playerInventory.AddToInventory(playerInventory.inventory, 1, draggedItemData.originalItem);
                    SetSlotData(target, draggedItemData);
                    dropSucceeded = true;
                }
                else if (currentDragSource == DragSource.QSlot)
                {
                    // QSlot -> QSlot (single / zamiana)
                    if (target != draggedFromSlot)
                    {
                        ItemData targetData = target.userData as ItemData;
                        SetSlotData(target, draggedItemData);

                        if (targetData != null)
                        {
                            SetSlotData(draggedFromSlot, targetData);
                        }


                        dropSucceeded = true; 
                    }
                }
            }
        }
    }
    public ItemData DataFromTarget(VisualElement target)
{
    // Sprawdzamy, czy target nie jest nullem i czy posiada przypisane userData
    if (target != null && target.userData is ItemData data)
    {
        return data;
    }


    return null;
}

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
        if (crafintgIsOpen == false)
        {
            LHand.AddToClassList("Hand-Active");
            RHand.AddToClassList("Hand-Active");
        }
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
        weightRefresh();
    }

    void setQSlot(int i, Sprite mySprite)
    {
        itemIcons[i].sprite = mySprite;
    }

    void InitTooltip()
    {
        VisualElement descriptionPanel = new VisualElement();
        descriptionPanel.name = "descriptionPanel";
        descriptionPanel.style.position = Position.Absolute;
        descriptionPanel.style.backgroundColor = new UnityColor(0.1f, 0.1f, 0.1f, 0.95f);
        {
            descriptionPanel.style.paddingRight = 10;
            descriptionPanel.style.paddingLeft = 10;
            descriptionPanel.style.paddingTop = 10;
            descriptionPanel.style.paddingBottom = 10;
        }
        descriptionPanel.style.display = DisplayStyle.None; // Ukryty
        descriptionPanel.pickingMode = PickingMode.Ignore; // Mysz go ignoruje!
        {
            ColorUtility.TryParseHtmlString("#212018", out UnityColor borderColor);
            
            descriptionPanel.style.borderLeftColor = borderColor;
            descriptionPanel.style.borderRightColor = borderColor;
            descriptionPanel.style.borderTopColor = borderColor;
            descriptionPanel.style.borderBottomColor = borderColor;
            

            descriptionPanel.style.borderLeftWidth = 1;
            descriptionPanel.style.borderRightWidth = 1;
            descriptionPanel.style.borderTopWidth = 1;
            descriptionPanel.style.borderBottomWidth = 1;
        }

        Label tooltipLabel = new Label();
        tooltipLabel.style.color = UnityColor.white;
        tooltipLabel.style.whiteSpace = WhiteSpace.Normal;
        descriptionPanel.Add(tooltipLabel);

        root.Add(descriptionPanel); // DODAJEMY DO ROOT, nie do listy!
    }
    public void addItem(string name, string category, int quantity, float weight, Sprite icon, Item original)
    {
        ItemList.Add(original);
        ScrollView scroll = root.Q<ScrollView>("Items_scrol");
        VisualElement existing = scroll.contentContainer.Q(name);

        if (existing == null)
        {
            VisualElement itemRoot = new VisualElement();
            itemRoot.style.height = 40;
            itemRoot.style.paddingRight = 5;
            itemRoot.style.paddingLeft = 5;
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

            // LEFT CLICK = drag, RIGHT CLICK = use
            itemRoot.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == (int)MouseButton.RightMouse)
                {
                    UseItemFromList(itemRoot);
                    return;
                }

                if (evt.button != (int)MouseButton.LeftMouse) return;

                Label qtyLabel = itemRoot.Q<Label>("ItemQty");
                if (qtyLabel == null) return;
                int totalAvailable = int.Parse(qtyLabel.text);
                draggedQuantity = evt.shiftKey ? totalAvailable : 1;
                draggedItemData = itemRoot.userData as ItemData;
                draggedItemRoot = itemRoot;
                currentDragSource = DragSource.PlayerInventory;

                draggedFromSlot = null;
                dropSucceeded = false;

                Image sourceImg = itemRoot.Q<Image>();
                if (sourceImg == null) return;

                StartDrag(evt.position, sourceImg.image);

                if (draggedQuantity >= totalAvailable)
                {
                    itemRoot.RemoveFromHierarchy();
                    RefreshItemsStyles();
                }
                else
                {
                    int newQty = totalAvailable - draggedQuantity;
                    qtyLabel.text = newQty.ToString();
                    Label weightLabel = itemRoot.Q<Label>("ItemWeight");
                    if (weightLabel != null)
                        weightLabel.text = (draggedItemData.weight * newQty).ToString("0.##");
                }
            });

            VisualElement imgContainer = new VisualElement();
            imgContainer.style.width = Length.Percent(10);
            imgContainer.style.justifyContent = Justify.Center;
            imgContainer.style.alignItems = Align.Center;
            imgContainer.style.flexShrink = 0;

            Image imagediv = new Image();
            imagediv.image = icon.texture;
            imagediv.style.height = 32;
            imagediv.scaleMode = ScaleMode.ScaleToFit;
            imgContainer.Add(imagediv);

            Label nameLabel = new Label(name);
            nameLabel.name = "ItemName";
            nameLabel.style.width = Length.Percent(35);
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            Label typeLabel = new Label(category);
            typeLabel.style.width = Length.Percent(25);
            typeLabel.name = "Type";
            typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            Label qtyLabelNew = new Label(quantity.ToString());
            qtyLabelNew.name = "ItemQty";
            qtyLabelNew.style.width = Length.Percent(20);
            qtyLabelNew.style.unityTextAlign = TextAnchor.MiddleCenter;

            Label weightLabel2 = new Label((weight * quantity).ToString());
            weightLabel2.name = "ItemWeight";
            weightLabel2.style.width = Length.Percent(10);
            weightLabel2.style.unityTextAlign = TextAnchor.MiddleCenter;

            itemRoot.Add(imgContainer);
            itemRoot.Add(nameLabel);
            itemRoot.Add(typeLabel);
            itemRoot.Add(qtyLabelNew);
            itemRoot.Add(weightLabel2);

            int hoverVersion = 0;
            

            itemRoot.RegisterCallback<PointerEnterEvent>(async evt =>
            {
                int currentId = ++hoverVersion; // Unikalne ID dla tego konkretnego najechania


                await Task.Delay(250);
                

                if (currentId == hoverVersion && !isDragging)
                {
                    descryption.style.display = DisplayStyle.Flex;
                    tooltipLabel.text = original.description;

                }
            });
            itemRoot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                hoverVersion++; // Zmieniamy ID, więc oczekujący Task z PointerEnter nic nie wyświetli
          
                 
                descryption.style.display = DisplayStyle.None;
               
            });
            itemRoot.RegisterCallback<PointerMoveEvent>(evt => {

                // 1. Definiujemy margines od myszki i krawędzi ekranu
                float offset = 20f;
                float screenPadding = 10f;

                float mouseX = evt.position.x;
                float mouseY = evt.position.y;
                float rootWidth = root.resolvedStyle.width;

                // 2. Obliczamy dostępną szerokość po prawej stronie myszki
                // Dostępne miejsce = Szerokość ekranu - Pozycja myszy - Margines od myszy - Margines od krawędzi
                float availableWidth = rootWidth - mouseX - offset - screenPadding;

                // 3. Ograniczamy szerokość elementu (z zachowaniem sensownego minimum, np. 100px)
                float minWidth = 150f;
                float finalWidth = Mathf.Max(minWidth, availableWidth);

                descryption.style.maxWidth = finalWidth;

                // 4. Ustawiamy pozycję
                descryption.style.left = mouseX + offset;
                descryption.style.top = mouseY + offset;

                // 5. Opcjonalnie: Jeśli nawet przy minimalnej szerokości wystaje za ekran, 
                // przerzuć go na lewą stronę myszy
                if (availableWidth < minWidth)
                {
                    // Tutaj tooltip "odskoczy" na lewo, jeśli po prawej jest ekstremalnie mało miejsca
                    descryption.style.left = StyleKeyword.Null; // Czyścimy lewo
                    descryption.style.right = (rootWidth - mouseX) + offset;
                    descryption.style.maxWidth = 300; // Resetujemy max width do standardu
                }
                else
                {
                    descryption.style.right = StyleKeyword.Null;
                }

            });













           
            scroll.contentContainer.Add(itemRoot);











        }
        else
        {
            Label qtyLabel = existing.Q<Label>("ItemQty");
            Label weightLabel = existing.Q<Label>("ItemWeight");
            if (qtyLabel == null) return;
            int oldQty = int.Parse(qtyLabel.text);
            int newQty = oldQty + quantity;
            qtyLabel.text = newQty.ToString();
            weightLabel.text = (newQty * weight).ToString();
        }

        RefreshItemsStyles();
    }

public void SendItemList(List<Item> items)
    {
        Debug.Log("UI dostało listę itemów:");
        ItemList = new List<Item>(items);
        foreach (Item item in items)
        {
            if (item == null) { Debug.LogWarning("Znaleziono pusty element!"); continue; }
            addItem(item.itemName, item.itemType.ToString(), 1, item.weight, item.icon != null ? item.icon : defaultPlaceholderIcon, item);
        }
    }

    private void RefreshItemsStyles()
    {
        ScrollView scroll = root.Q<ScrollView>("Items_scrol");
        var items = scroll.contentContainer.Children().ToList();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].RemoveFromClassList("row-even");
            items[i].RemoveFromClassList("row-odd");
            if (i % 2 == 0) 
                items[i].AddToClassList("row-even");
            else items[i].AddToClassList("row-odd");
        }
    }
    private void RefreshOSItemsStyles()
    {
        ScrollView scroll = root.Q<ScrollView>("OSItems_scrol");
        var items = scroll.contentContainer.Children().ToList();
        for (int i = 0; i < items.Count; i++)
        {
            items[i].RemoveFromClassList("row-even");
            items[i].RemoveFromClassList("row-odd");
            if (i % 2 == 0)
                items[i].AddToClassList("row-even");
            else items[i].AddToClassList("row-odd");
        }
    }
    public void ShowCrafting()
    {
        ShowInventory();
        var Crafting = root.Q<VisualElement>("Crafting");
        Crafting.style.display = DisplayStyle.Flex;
        crafintgIsOpen = true;
    }

    public void toggleCrafting()
    {
        if (inventoryIsOpen == true)
        {
            HideInventory();
           
        }
        else
        {
            ShowCrafting();
            
        }
    }
    public void toggleChest()
    {
        if (ChestIsOpen == true)
        {
            HideInventory();

        }
        else
        {
            ShowChest();

        }
    }
    public void HideCrafing()
    {
        var Crafting = root.Q<VisualElement>("Crafting");
        Crafting.style.display = DisplayStyle.None;
        crafintgIsOpen = false;
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
        List<Label> slot = root.Query<Label>(className: "QSlot").ToList();
        for (int i = 0; i < slot.Count; i++) 
            slot[i].style.opacity = 0.8f;
        var slots = QSlots.parent;
        slots.style.flexDirection = FlexDirection.RowReverse;
        inventoryIsOpen = false;
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
        List<Label> slot = root.Query<Label>(className: "QSlot").ToList();
        for (int i = 0; i < slot.Count; i++) 
            slot[i].style.opacity = 1f;
        var slots = QSlots.parent;
        slots.style.flexDirection = FlexDirection.Row; 
       // foreach (Item recipeItem in UIRecipes) 
           // AddUnique(recipeItem);
        inventoryIsOpen = true;
    }
    public void ShowChest()
    {
        ShowInventory();
        VisualElement OSInv = root.Q<VisualElement>("OSInventory");
        OSInv.style.display = DisplayStyle.Flex;
        ChestIsOpen = true;

    }
    public void HideChest()
    {
        ShowInventory();
        VisualElement OSInv = root.Q<VisualElement>("OSInventory");
        OSInv.style.display = DisplayStyle.None;
        ChestIsOpen = false;

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
                ItemList.RemoveAt(i);
                removedCount++;
                if (removedCount >= amount) break;
            }
        }
        if (newQty <= 0)
            itemRoot.RemoveFromHierarchy();
        else
        {
            qtyLabel.text = newQty.ToString();
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
        if (index < 0 || index >= qSlotsList.Count) { Debug.LogWarning($"Indeks {index} poza zakresem!"); return null; }
        VisualElement slot = qSlotsList[index];
        ItemData data = slot.userData as ItemData;
        if (data == null) { Debug.Log($"Slot {index} jest pusty."); return null; }
        return data;
    }

    public Item GetItemLeftHand() => GetItemFromQSlot(9)?.originalItem;
    public Item GetItemRightHand() => GetItemFromQSlot(10)?.originalItem;


    void AddItemToTable(VisualElement table, ItemData data, Vector2 localPos, int quantity)
    {
        VisualElement itemOnTable = new VisualElement();
        float size = 64;
        itemOnTable.style.width = size;
        itemOnTable.style.height = size;
        itemOnTable.style.marginRight = 5;
        itemOnTable.style.marginBottom = 5;
        itemOnTable.style.position = Position.Absolute;
        itemOnTable.style.left = localPos.x - (size / 2);
        itemOnTable.style.top = localPos.y - (size / 2);
        itemOnTable.style.backgroundImage = new StyleBackground(GetIconTexture(data.originalItem));

        ItemData tableData = new ItemData { 
            name = data.name, 
            category = data.category, 
            weight = data.weight, 
            icon = data.icon, 
            originalItem = data.originalItem };
        itemOnTable.userData = tableData;
        itemOnTable.AddToClassList("TableItem");

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

        itemOnTable.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse) return;
            draggedQuantity = quantity;
            draggedItemData = tableData;
            draggedFromSlot = null;
            currentDragSource = DragSource.Crafting;
            dropSucceeded = false;
            VisualElement tableContainer = root.Q<VisualElement>("Table");//wiem dzienie nazwalem crafting jako table :)
            lastLocalPosBeforeDrag = tableContainer.WorldToLocal(evt.position);
            StartDrag(evt.position, GetIconTexture(data.originalItem));
            itemOnTable.RemoveFromHierarchy();
           
        });

        table.Add(itemOnTable);
        table.style.flexDirection = FlexDirection.Row;
        table.style.flexWrap = Wrap.Wrap;
    }

    public List<Item> GetItemsOnTable()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        List<Item> itemsOnTable = new List<Item>();
        table.Query<VisualElement>(className: "TableItem").ForEach(itemElement =>
        {
            if (itemElement.userData is ItemData data) itemsOnTable.Add(data.originalItem);
        });
        return itemsOnTable;

    }

    private async void CraftingSend()
    {
        if (craftingInventory == null) { Debug.LogError("craftingInventory jest NULL!"); return; }
        if (crafting.Instance == null) { Debug.LogError("crafting.Instance jest NULL!"); return; }
        Debug.Log("Button was clicked!");
        List<Item> CraftingItems = GetItemsOnTable();
        Debug.Log(CraftingItems.Count);
        craftingInventory.inventory = new List<Item>(CraftingItems);
        crafting.Instance.craft = true;
        while (crafting.Instance.craft == true) 
            await Task.Delay(100);
        ClearTable();
        List<Item> CraftingRturn = craftingInventory.inventory;
        SpawnItemsOnTable(CraftingRturn);
           UpdateAvalibleRecipies();//aktualizacja receptur
        //craftingInventory.inventory.Clear();//badziew ktory jest nie potrzebny juz ale egzystuje w razie w gdybym mial wywalone w crafting
    }

    void ClearTable()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null) 
            return;
        List<VisualElement> itemsToRemove = table.Query<VisualElement>(className: "TableItem").ToList();
        foreach (var item in itemsToRemove) 
            item.RemoveFromHierarchy();
        Debug.Log("Stół został wyczyszczony.");
    }

    public void SpawnItemsOnTable(List<Item> itemsToPlace)
    {
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null || itemsToPlace == null) return;
        float slotSize = 64f;
        float padding = 10f;
        int columns = 4;
        for (int i = 0; i < itemsToPlace.Count; i++)
        {
            Item item = itemsToPlace[i];
            ItemData data = new ItemData { 
                name = item.itemName, 
                category = item.itemType.ToString(), 
                weight = item.weight, 
                icon = item.icon != null ? item.icon : defaultPlaceholderIcon, 
                originalItem = item 
            };
            float x = (i % columns) * (slotSize + padding) + (slotSize / 2);
            float y = (i / columns) * (slotSize + padding) + (slotSize / 2);
            AddItemToTable(table, data, new Vector2(x, y), 1);
        }
    }

    public float weightRefresh()
    {
        float totalWeight = 0f;
        var itemsInScroll = root.Q<ScrollView>("Items_scrol").contentContainer.Query<VisualElement>(className: "Item").ToList();
        foreach (var itemRow in itemsInScroll)
        {
            if (itemRow.userData is ItemData data)
            {
                Label qtyLabel = itemRow.Q<Label>("ItemQty");
                int quantity = (qtyLabel != null && int.TryParse(qtyLabel.text, out int q)) ? q : 1;
                totalWeight += data.weight * quantity;
            }
        }
        float QslotWeight = 0;
        foreach (var slot in qSlotsList)
        {
            if (slot != null && slot.userData is ItemData data)
            {
                totalWeight += data.weight;
                QslotWeight += data.weight;
            }
        }
        if (isDragging && draggedItemData != null) totalWeight += draggedItemData.weight * draggedQuantity;

        Label weight = root.Q<Label>("Weight");
        Label weightQslots = root.Q<Label>("WeightQslots");
        weight.text = totalWeight + "kg / " + maxWeight + "kg";
        weightQslots.text = QslotWeight + "kg / " + maxWeight / 2 + "kg";
        weightQslots.style.whiteSpace = WhiteSpace.NoWrap;
        weightQslots.style.paddingLeft = Length.Percent(5);
        weightQslots.style.paddingRight = Length.Percent(5);
        weightQslots.style.unityTextAlign = TextAnchor.MiddleCenter;
        return totalWeight;
    }

    public void SendItemCraftable(List<Item> recipes) { UIRecipes = recipes; }

    public void AddUnique(Item item)
    {
        if (!ItemList.Exists(i => i.itemName == item.itemName))
        {
            Item instance = Instantiate(item);
            ItemList.Add(instance);
        }
    }

    private Vector2 GetAverageItemPosition()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        var items = table.Query<VisualElement>(className: "TableItem").ToList();
        if (items.Count == 0) 
            return new Vector2(table.layout.width / 2, table.layout.height / 2);
        Vector2 sum = Vector2.zero;
        foreach (var item in items) 
        { 
            sum.x += item.layout.x; sum.y += item.layout.y; 
        }
        return new Vector2(sum.x / items.Count, sum.y / items.Count);
    }
    List<Item> OutsideInventoryList = new List<Item>();
    public void SendItemsToChest(List<Item> outsideInventory) //funkcja przyjmujaca liste i wyslajaca polecenia do UI w celu dodani ich na ekran
    {
        ScrollView scroll = root.Q<ScrollView>("OSItems_scrol");
        scroll.Clear();


        foreach (Item item in outsideInventory)
        {
            if (item == null)
            {
                Debug.LogWarning("Znaleziono pusty element na liście przedmiotów!");
                continue;
            }

            AddItemToOutside(item);
        }
    }
    void AddItemToOutside(Item item ,int quantity=1)
    {
        OutsideInventoryList.Add(item);
        float weight = item.weight;
        string name = item.itemName;
        Sprite icon = item.icon != null ? item.icon : defaultPlaceholderIcon;
        string category = item.itemType.ToString();
        ScrollView scroll = root.Q<ScrollView>("OSItems_scrol");

        VisualElement existing = scroll.contentContainer.Q(name);

        VisualElement table = root.Q<VisualElement>("OSAbout");

    







        if (existing == null)
        {
            // 🔹 NOWY ITEM (DIV)
            VisualElement itemRoot = new VisualElement();
            itemRoot.style.height = 40;
            itemRoot.style.paddingRight = 5;
            itemRoot.style.paddingLeft = 5;


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
                originalItem = item
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
                currentDragSource = DragSource.OSInventory; // To musi być tutaj!

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
                    RefreshOSItemsStyles();
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
            // imgContainer.style.backgroundColor = style2;

            Image imagediv = new Image();
            imagediv.image = icon.texture;
            imagediv.style.height = 32;
            imagediv.scaleMode = ScaleMode.ScaleToFit;

            imgContainer.Add(imagediv);

            Label nameLabel = new Label(name);
            nameLabel.name = "ItemName";
            nameLabel.style.width = Length.Percent(45);
            nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            // nameLabel.style.backgroundColor = style1;

            //Label typeLabel = new Label(category);
            //typeLabel.style.width = Length.Percent(25);
            //typeLabel.name = "Type";
            //typeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            ////typeLabel.style.backgroundColor = style5;

            Label qtyLabel = new Label(quantity.ToString());
            qtyLabel.name = "ItemQty";
            qtyLabel.style.width = Length.Percent(25);
            qtyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            // qtyLabel.style.backgroundColor = style1;

            Label weightLabel = new Label((weight * quantity).ToString());
            weightLabel.name = "ItemWeight";
            weightLabel.style.width = Length.Percent(20);
            weightLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            //   weightLabel.style.backgroundColor = style5;



            itemRoot.Add(imgContainer);
            itemRoot.Add(nameLabel);
            //itemRoot.Add(typeLabel);
            itemRoot.Add(qtyLabel);
            itemRoot.Add(weightLabel);





            int hoverVersion = 0;


            itemRoot.RegisterCallback<PointerEnterEvent>(async evt =>
            {
                int currentId = ++hoverVersion; // Unikalne ID dla tego konkretnego najechania


                await Task.Delay(250);


                if (currentId == hoverVersion && !isDragging)
                {
                    descryption.style.display = DisplayStyle.Flex;
                    tooltipLabel.text = item.description;

                }
            });
            itemRoot.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                hoverVersion++; // Zmieniamy ID, więc oczekujący Task z PointerEnter nic nie wyświetli


                descryption.style.display = DisplayStyle.None;

            });
            itemRoot.RegisterCallback<PointerMoveEvent>(evt => {

                // 1. Definiujemy margines od myszki i krawędzi ekranu
                float offset = 20f;
                float screenPadding = 10f;

                float mouseX = evt.position.x;
                float mouseY = evt.position.y;
                float rootWidth = root.resolvedStyle.width;

                // 2. Obliczamy dostępną szerokość po prawej stronie myszki
                // Dostępne miejsce = Szerokość ekranu - Pozycja myszy - Margines od myszy - Margines od krawędzi
                float availableWidth = rootWidth - mouseX - offset - screenPadding;

                // 3. Ograniczamy szerokość elementu (z zachowaniem sensownego minimum, np. 100px)
                float minWidth = 150f;
                float finalWidth = Mathf.Max(minWidth, availableWidth);

                descryption.style.maxWidth = finalWidth;

                // 4. Ustawiamy pozycję
                descryption.style.left = mouseX + offset;
                descryption.style.top = mouseY + offset;

                // 5. Opcjonalnie: Jeśli nawet przy minimalnej szerokości wystaje za ekran, 
                // przerzuć go na lewą stronę myszy
                if (availableWidth < minWidth)
                {
                    // Tutaj tooltip "odskoczy" na lewo, jeśli po prawej jest ekstremalnie mało miejsca
                    descryption.style.left = StyleKeyword.Null; // Czyścimy lewo
                    descryption.style.right = (rootWidth - mouseX) + offset;
                    descryption.style.maxWidth = 300; // Resetujemy max width do standardu
                }
                else
                {
                    descryption.style.right = StyleKeyword.Null;
                }

            });



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
        RefreshOSItemsStyles();

    }
    bool colorAvalibleRecipie;
    VisualElement AvalibleRecipie(Item item) 
    {
        //root element
        VisualElement Recipie = new VisualElement();
        Recipie.style.flexDirection = FlexDirection.Row;
        Recipie.style.width = Length.Percent(100);
        Recipie.style.height = 50;
        if (colorAvalibleRecipie)
            Recipie.style.backgroundColor = (UnityColor)new Color32(35, 32, 29, 255);
        else
            Recipie.style.backgroundColor = (UnityColor)new Color32(45, 43, 40, 255);
        Recipie.style.paddingBottom = 5;
        Recipie.style.paddingTop = 5;
        //ico
        
            VisualElement icon = new VisualElement();
            icon.style.width = Length.Percent(10);
            Image iconImage = new Image();
            iconImage.sprite = item.icon;
            iconImage.scaleMode = ScaleMode.ScaleToFit;
            icon.Add(iconImage);
        //item name
        VisualElement itemName = new VisualElement();
        Label itemTextName = new Label();
        itemTextName.text = item.itemName.ToString();
        itemTextName.style.color =(UnityColor) new Color32(127, 127, 126, 255);
        itemTextName.style.unityFontStyleAndWeight = FontStyle.Bold;
        itemName.style.justifyContent = Justify.Center;
        itemName.style.alignItems = Align.Center;
        itemName.style.width = Length.Percent(50);
        itemName.Add(itemTextName);
        //Crafintg selector buttons
        VisualElement buttons = new VisualElement();
        Label craftQuantity = new Label();//quantity ile craftujesz
        buttons.style.width = Length.Percent(40);
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.style.justifyContent = Justify.FlexEnd;
        buttons.style.alignItems = Align.Center;
        buttons.style.paddingRight = 10;
        Button subButton = new Button();
        subButton.text = "-";
        subButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        subButton.style.fontSize = 30;
        subButton.style.width = Length.Percent(25);
        subButton.style.height = Length.Percent(80);
        subButton.clicked += () => subFromCrafting(craftQuantity , item);
        buttons.Add(subButton);
        //-----------------------------------
       
        craftQuantity.text = "0";
        craftQuantity.style.unityTextAlign = TextAnchor.MiddleCenter;
        craftQuantity.style.fontSize = 30;
        craftQuantity.style.unityFontStyleAndWeight = FontStyle.Bold;
        craftQuantity.style.width = Length.Percent(50);


        
        //-----------------------------------
        Button addButton = new Button();
        addButton.text = "+";
        addButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        addButton.style.fontSize = 30;
        addButton.style.width = Length.Percent(25);
        addButton.style.height = Length.Percent(80);
        addButton.clicked += () => AddToCrafting(craftQuantity, item);

        buttons.Add(craftQuantity);
        buttons.Add(addButton);


        Recipie.Add(icon);
        Recipie.Add(itemName);
        Recipie.Add(buttons);

        colorAvalibleRecipie = !colorAvalibleRecipie;
        return Recipie;
    }
    void subFromCrafting(Label Quantity, Item item)
    {
        int qty = int.Parse(Quantity.text);

  
        if (qty > 0)
        {
      
            qty--;
            Quantity.text = qty.ToString();


            foreach (Item ingredient in item.craftingRecipe.itemsList)
            {
                // A. Logika danych: Zabierz z craftingu, oddaj graczowi
                craftingInventory.RemoveFromInventory(craftingInventory.inventory, ingredient, 1);
                playerInventory.AddToInventory(playerInventory.inventory, 1, ingredient);


                // B. Logika UI Ekwipunku: Dodaj przedmiot z powrotem do listy UI
                // Korzystamy z Twojej metody 'addItem' (z UI_Script), 
                // która sprawdza czy dodać nowy wiersz, czy zwiększyć cyferkę
                addItem(ingredient.itemName, ingredient.itemType.ToString(),1, ingredient.weight, ingredient.icon, ingredient);

                // C. Logika Wizualna: Usuń fizyczny element ze stołu
                RemoveOneItemFromTable(ingredient);
            }

            // 3. Odświeżenie wyglądu listy i wagi
            RefreshItemsStyles();
            weightRefresh();
        }
    }
    void RemoveOneItemFromTable(Item itemToRemove)
    {
        // 1. Znajdź kontener stołu
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null) return;

        // 2. Pobierz wszystkie elementy na stole, które mają klasę "TableItem"
        // (tę klasę nadajesz w funkcji AddItemToTable w linii 1042)
        var itemsOnTable = table.Query<VisualElement>(className: "TableItem").ToList();

        // 3. Przeszukaj elementy, aby znaleźć ten pasujący do zwracanego przedmiotu
        foreach (var itemElement in itemsOnTable)
        {
            if (itemElement.userData is ItemData data)
            {
                // Sprawdzamy, czy to jest ten sam przedmiot (ScriptableObject)
                if (data.originalItem == itemToRemove)
                {
                    // 4. Usuwamy tylko JEDNĄ sztukę z UI i kończymy funkcję (return)
                    itemElement.RemoveFromHierarchy();
                    return;
                }
            }
        }
    }
    int GetCountManual(List<Item> inventory, Item itemToFind)
    {
        int count = 0;
        foreach (Item i in inventory)
        {
            if (i == itemToFind)
            {
                count++;
            }
        }
        return count;
    }
    void AddToCrafting(Label Quantity, Item item)
    {
        bool canAfford = true;

        var recipeGrouped = item.craftingRecipe.itemsList
            .GroupBy(i => i)
            .Select(g => new { Item = g.Key, Required = g.Count() });

        foreach (var requirement in recipeGrouped)
        {
            // Liczymy ile mamy TERAZ w plecaku
            int inPlayerInv = playerInventory.inventory.Count(i => i != null && i.itemID == requirement.Item.itemID);
            Debug.Log($"Szukam: {requirement.Item.itemName}. W plecaku mam: {inPlayerInv} sztuk. Potrzebuję: {requirement.Required}");
            // Sprawdzamy TYLKO czy mamy wystarczająco na JEDNĄ kolejną sztukę
            // Nie mnożymy przez (currentQty + 1), bo poprzednie sztuki już zabraliśmy!
            if (inPlayerInv < requirement.Required)
            {
                canAfford = false;
                break;
            }
        }


        if (canAfford)
            {
                int qty = int.Parse(Quantity.text);
                qty++;
                Quantity.text = qty.ToString();

                VisualElement tableRoot = root.Q<VisualElement>("Table");

                // 3. Logika przenoszenia
                foreach (Item ingredient in item.craftingRecipe.itemsList)
                {
                    // Pozycja na stole
                    Vector2 tablePos = new Vector2(50, 50);

                    // WAŻNE: Musisz przekazać pełne dane ItemData, inaczej AddItemToTable wywali błąd na ikonie
                    ItemData dataForTable = new ItemData
                    {
                        originalItem = ingredient,
                        icon = ingredient.icon != null ? ingredient.icon : defaultPlaceholderIcon,
                        name = ingredient.itemName,
                        weight = ingredient.weight
                    };

                    // Dodaj wizualnie na stół
                    AddItemToTable(tableRoot, dataForTable, tablePos, 1);

                    // Przenieś w danych
                    craftingInventory.AddToInventory(craftingInventory.inventory, 1, ingredient);
                    playerInventory.RemoveFromInventory(playerInventory.inventory, ingredient, 1);

                    // USUWANIE Z UI: 
                    // Używamy Twojej istniejącej metody RemoveItem, która aktualizuje Label qty w ScrollView
                    RemoveItem(ingredient.itemName, 1);
                }

                // WYDAJNOŚĆ: Odświeżamy style (kolory wierszy) TYLKO RAZ po dodaniu wszystkich składników
                RefreshItemsStyles();
                weightRefresh(); // Aktualizujemy wagę całkowitą
            }
            else
            {
                Debug.Log("Brak składników!");
            }
        }
    void UpdateAvalibleRecipies() 
    {

        VisualElement AvalibleRecipies = root.Q<VisualElement>("CraftableList");
        AvalibleRecipies.Clear();
        colorAvalibleRecipie=false;
        foreach (Item item in AvalibleToCraft.allItems)
        {
            VisualElement Recipie = AvalibleRecipie(item);
            AvalibleRecipies.Add(Recipie);
        }



    }
    public void OnSelectSlot(InputAction.CallbackContext context)
    {
      
        if (context.performed)
        {
            
            string keyName = context.control.name;//pobiera nazwe klawisza -- index QSLOTA
            if (int.TryParse(keyName, out int slotIndex))
            {
                swapQSlots(slotIndex - 1);
            }
        }
    }

    void swapQSlots(int index)
    {
        Log("Swapuję na slot: " + index);
        
    
        Item slotItem = GetOriginalItemFromSlot(index);
        Item leftHandItem = GetItemLeftHand();

        // 2. Przygotuj dane dla slotu (idzie tam to, co było w ręce)
        if (leftHandItem != null)
        {
            ItemData handData = new ItemData
            {
                name = leftHandItem.itemName,
                category = leftHandItem.itemType.ToString(),
                weight = leftHandItem.weight,
                icon = leftHandItem.icon != null ? leftHandItem.icon : defaultPlaceholderIcon,
                originalItem = leftHandItem
            };
            SetSlotData(qSlotsList[index], handData);
        }
        else
        {
            // Jeśli ręka była pusta, wyczyść slot
            ClearSlot(qSlotsList[index]);
        }

        // 3. Przygotuj dane dla ręki (idzie tam to, co było w slocie)
        if (slotItem != null)
        {
            ItemData slotData = new ItemData
            {
                name = slotItem.itemName,
                category = slotItem.itemType.ToString(),
                weight = slotItem.weight,
                icon = slotItem.icon != null ? slotItem.icon : defaultPlaceholderIcon,
                originalItem = slotItem
            };
            SetSlotData(LHand, slotData);
        }
        else 
        {
           
            ClearSlot(LHand);
        }

        weightRefresh();
    }
    public void swapHands()
    {
        

        Item rightHandItem = GetItemRightHand();
        Item leftHandItem = GetItemLeftHand();

        // 2. Przygotuj dane dla slotu (idzie tam to, co było w ręce)
        if (leftHandItem != null)
        {
            ItemData leftHandData = new ItemData
            {
                name = leftHandItem.itemName,
                category = leftHandItem.itemType.ToString(),
                weight = leftHandItem.weight,
                icon = leftHandItem.icon != null ? leftHandItem.icon : defaultPlaceholderIcon,
                originalItem = leftHandItem
            };
            SetSlotData(RHand, leftHandData);
        }
        else
        {
            // Jeśli ręka była pusta, wyczyść slot
            ClearSlot(RHand);
        }

        // 3. Przygotuj dane dla ręki (idzie tam to, co było w slocie)
        if (rightHandItem != null)
        {
            ItemData rightHandData = new ItemData
            {
                name = rightHandItem.itemName,
                category = rightHandItem.itemType.ToString(),
                weight = rightHandItem.weight,
                icon = rightHandItem.icon != null ? rightHandItem.icon : defaultPlaceholderIcon,
                originalItem = rightHandItem
            };
            SetSlotData(LHand, rightHandData);
        }
        else
        {
            // Jeśli slot był pusty, wyczyść rękę
            ClearSlot(LHand);
        }
    }

    void ClearSlot(VisualElement slot)
    {
        Image icon = slot.Q<Image>("Item_Icon");
        if (icon != null) icon.image = null;
        Label nameLabel = slot.Q<Label>("Slot_Info");
        if (nameLabel != null) nameLabel.text = string.Empty;
        slot.userData = null;
    }

    private async void  Start()
    {
        await Task.Delay(1000);
        UpdateAvalibleRecipies();
        //Log("Wystartowało UI");


    }

    private void initRightClick()
    {
        VisualElement optionWindow  = new VisualElement();
        optionWindow.style.width = 300;
        optionWindow.style.height = StyleKeyword.Auto;
        optionWindow.style.flexGrow = 0;
        




    }
}