using UnityEngine;

[CreateAssetMenu(fileName = "Connector", menuName = "Building/Connector")]
public class Connector : ScriptableObject
{
    public enum Type {
        None,
        TopLeft,
        TopMiddle,
        TopRight,
        MiddleLeft,
        Middle,
        MiddleRight,
        BottomLeft,
        BottomMiddle,
        BottomRight
    }

    [Header("Connector Settings")]
    public Type connectorType;
    public Vector3 localPosition;
    public Vector3 facingDirection = Vector3.forward;
    
    public bool CanConnectTo(Connector other){
        if (other == null) return false;

        bool typesMatch = this.connectorType == other.connectorType;
        bool positionsMatch = this.localPosition == other.localPosition;

        return typesMatch && positionsMatch;
    }
}