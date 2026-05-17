using UnityEngine;

public class DisplayItem3D : MonoBehaviour

{
    public Transform rightHandBone;
    public Transform leftHandBone; 
    private Item equippedRightItem;
    private Item equippedLeftItem;
    private GameObject currentRightItem;
    private GameObject currentLeftItem;
    public static DisplayItem3D Instance;
    public enum Hand {
        right, left
    }

    public void DisplayItem(Item item, Hand hand)
    {
        Transform targetBone = hand == Hand.right ? rightHandBone : leftHandBone;

        ref GameObject currentModel = ref (hand == Hand.right ? ref currentRightItem : ref currentLeftItem);
        if (currentModel != null)
        {
            Destroy(currentModel);
            currentModel = null;
        }

        if (hand == Hand.right) equippedRightItem = item;
        else equippedLeftItem = item;
        PlayerStatusTogle(hand);
        if (item == null || item.itemModel == null)
        {
            return;
        }

        currentModel = Instantiate(item.itemModel, targetBone);
        currentModel.transform.localPosition = Vector3.zero;
        currentModel.transform.localRotation = item.itemModel.transform.rotation;
    }

    void Awake()
    {
        Instance = this;
    }
    private void PlayerStatusTogle(Hand currentHand)
    {
   
        bool isNotebookInRight = equippedRightItem != null && equippedRightItem.itemName=="Notebook";
        bool isNotebookInLeft = equippedLeftItem != null && equippedLeftItem.itemName == "Notebook";

        if (isNotebookInRight || isNotebookInLeft)
        {
       
            Hand handWithNotebook = isNotebookInRight ? Hand.right : Hand.left;

            UI_Logs.Log("wywoluje platyerstatusonscreen dla reki:"+handWithNotebook.ToString());
            PlayerStatus_screen.instance.DisplayPlayerStatusOnScreen(handWithNotebook, true);
        }
        else
        {
            PlayerStatus_screen.instance.DisplayPlayerStatusOnScreen(currentHand, false);
        }
    }
}