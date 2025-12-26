using UnityEngine;

public class BuildingPart : MonoBehaviour
{
    public enum Material
    {
        Plaster,
        Brick
    }

    public enum Type
    {
        Wall,
        Floor,
        Roof
    }

    public Material material;
    public Type type;
    public Vector3 sizeInGrid = Vector3.one;
}
