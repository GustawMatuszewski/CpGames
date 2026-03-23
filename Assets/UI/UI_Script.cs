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
using System.Threading.Tasks;


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
    }

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

                if (evt.button != (int)MouseButton.LeftMouse) return;

                Image icon = slot.Q<Image>("Item_Icon");
                if (icon == null || icon.image == null) return;

                ItemData data = slot.userData as ItemData;
                if (data == null) return;

                draggedItemData = data;
                draggedFromSlot = slot;
                draggedSourceImage = icon;
                currentDragSource = DragSourceType.QSlot;
                dropSucceeded = false;

                StartDrag(evt.position, icon.image);

                icon.image = null;
                slot.userData = null;

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
            }

            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);

            void OnPointerMove(PointerMoveEvent evt)
            {
                if (!isDragging || dragIcon == null) return;
                dragIcon.style.left = evt.position.x - 32;
                dragIcon.style.top = evt.position.y - 32;
            }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!isDragging || evt.button != (int)MouseButton.LeftMouse) return;

            isDragging = false;
            VisualElement picked = root.panel.Pick(evt.position);
            VisualElement target = picked;

            while (target != null &&
                   !target.ClassListContains("QSlot") &&
                   !target.ClassListContains("Item") &&
                   !target.ClassListContains("Hand") &&
                   !target.ClassListContains("BSlots") &&
                    target.name != "Table" &&
                    target.name != "Items_scrol" &&
                   !target.ClassListContains("CraftSlot"))
            {
                target = target.parent;
            }

            if (target != null)
                HandleDrop(target, evt.position);
            else
                dropSucceeded = false;

            if (!dropSucceeded)
            {
                if (currentDragSource == DragSourceType.List)
                    addItem(draggedItemData.name, draggedItemData.category, draggedQuantity, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
                else if (currentDragSource == DragSourceType.QSlot && draggedFromSlot != null)
                {
                    Image icon = draggedFromSlot.Q<Image>("Item_Icon");
                    if (icon != null) icon.image = draggedItemData.icon.texture;
                    draggedFromSlot.userData = draggedItemData;
                    Label slotLabel = draggedFromSlot.Q<Label>("Slot_Info");
                    if (slotLabel != null) slotLabel.text = draggedItemData.name;
                }
            }

            CleanupDrag();
            weightRefresh();
        }

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
        AddGradientToElement(crafTitle);
        AddGradientToElement(title);

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

    void HandleDrop(VisualElement target, Vector2 dropPosition)
    {
        if (draggedItemData == null) return;
       
        bool isStack = draggedQuantity > 1;

        if (target.name == "Table" || target.ClassListContains("TableContent"))
        {
            Vector2 localPos = target.WorldToLocal(dropPosition);
            AddItemToTable(target, draggedItemData, localPos, draggedQuantity);
            dropSucceeded = true;
        }

        if (isStack && !(target.name == "Table" || target.ClassListContains("TableContent")))
        {
            Debug.Log(draggedQuantity);
            dropSucceeded = false;
            return;
        }

        if (currentDragSource == DragSourceType.QSlot && target.ClassListContains("Hand"))
        {
            SetSlotData(target, draggedItemData);
            dropSucceeded = true;
        }
        else if (currentDragSource == DragSourceType.QSlot && target.ClassListContains("QSlot"))
        {
            if (target == draggedFromSlot)
            {
                dropSucceeded = false;
                return;
            }
            ItemData targetData = target.userData as ItemData;
            SetSlotData(target, draggedItemData);
            if (targetData != null) SetSlotData(draggedFromSlot, targetData);
            dropSucceeded = true;
        }
        else if (currentDragSource == DragSourceType.QSlot &&
                (target.name == "Items_scrol" || target.ClassListContains("BSlots") || target.ClassListContains("Item")))
        {
            addItem(draggedItemData.name, draggedItemData.category, 1, draggedItemData.weight, draggedItemData.icon, draggedItemData.originalItem);
            dropSucceeded = true;
        }
        else if (currentDragSource == DragSourceType.List &&
                (target.ClassListContains("QSlot") || target.ClassListContains("Hand")))
        {
            SetSlotData(target, draggedItemData);
            dropSucceeded = true;
        }
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
                currentDragSource = DragSourceType.List;
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
            if (i % 2 == 0) items[i].AddToClassList("row-even");
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
        if (inventoryIsOpen == true) HideInventory();
        else ShowCrafting();
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
        for (int i = 0; i < slot.Count; i++) slot[i].style.opacity = 0.8f;
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
        for (int i = 0; i < slot.Count; i++) slot[i].style.opacity = 1f;
        var slots = QSlots.parent;
        slots.style.flexDirection = FlexDirection.Row;
        foreach (Item recipeItem in UIRecipes) AddUnique(recipeItem);
        inventoryIsOpen = true;
    }

    public void RemoveItem(string itemName, int amount = 1)
    {
        ScrollView scroll = root.Q<ScrollView>("Items_scrol");
        if (scroll == null) return;
        VisualElement itemRoot = scroll.contentContainer.Q(itemName);
        if (itemRoot == null) return;
        Label qtyLabel = itemRoot.Q<Label>("ItemQty");
        Label weightLabel = itemRoot.Q<Label>("ItemWeight");
        if (qtyLabel == null || weightLabel == null) return;
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
    public Item GetItemRighHand() => GetItemFromQSlot(10)?.originalItem;

    void Update() { }

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
        itemOnTable.style.backgroundImage = new StyleBackground(data.icon.texture);

        ItemData tableData = new ItemData { name = data.name, category = data.category, weight = data.weight, icon = data.icon, originalItem = data.originalItem };
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
            currentDragSource = DragSourceType.List;
            dropSucceeded = false;
            StartDrag(evt.position, data.icon.texture);
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
        Debug.Log("Button was clicked!");
        List<Item> CraftingItems = GetItemsOnTable();
        Debug.Log(CraftingItems.Count);
        craftingInventory.inventory = new List<Item>(CraftingItems);
        crafting.Instance.craft = true;
        while (crafting.Instance.craft == true) await Task.Delay(100);
        ClearTable();
        List<Item> CraftingRturn = craftingInventory.inventory;
        SpawnItemsOnTable(CraftingRturn);
        craftingInventory.inventory.Clear();
    }

    void ClearTable()
    {
        VisualElement table = root.Q<VisualElement>("Table");
        if (table == null) return;
        List<VisualElement> itemsToRemove = table.Query<VisualElement>(className: "TableItem").ToList();
        foreach (var item in itemsToRemove) item.RemoveFromHierarchy();
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
            ItemData data = new ItemData { name = item.itemName, category = item.itemType.ToString(), weight = item.weight, icon = item.icon != null ? item.icon : defaultPlaceholderIcon, originalItem = item };
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
        if (items.Count == 0) return new Vector2(table.layout.width / 2, table.layout.height / 2);
        Vector2 sum = Vector2.zero;
        foreach (var item in items) { sum.x += item.layout.x; sum.y += item.layout.y; }
        return new Vector2(sum.x / items.Count, sum.y / items.Count);
    }
}