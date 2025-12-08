using UnityEngine;

[CreateAssetMenu]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite icon;
    [TextArea]
    public string description;
}