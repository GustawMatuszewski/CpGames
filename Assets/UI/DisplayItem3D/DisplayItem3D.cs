using UnityEngine;

public class DisplayItem3D : MonoBehaviour

{
    public Transform rightHandBone;
    public Transform leftHandBone; 

     private GameObject currentRightItem;
    private GameObject currentLeftItem;
    public static DisplayItem3D Instance;
    public enum Hand {
        right, left
    }
        public void DisplayItem(Item item, Hand hand)
    {
        Transform targetBone = hand == Hand.right ? rightHandBone : leftHandBone;

        // Usuń poprzedni item z tej ręki
        ref GameObject current = ref (hand == Hand.right ? ref currentRightItem : ref currentLeftItem);
        if (current != null)
            Destroy(current);

        if (item is WeaponItem weapon)
        {
            
            if (weapon == null || weapon.itemModel == null)
                return;

            current = Instantiate(weapon.itemModel, targetBone);
            current.transform.localPosition = Vector3.zero;
            current.transform.localRotation = Quaternion.identity;
        }   
    }

        void Awake()
    {
        Instance = this;
    }

}