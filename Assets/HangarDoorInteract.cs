using UnityEngine;

public class HangarDoorInteract : MonoBehaviour {
    public GameObject door;
    public QuickTimeEvent interaction;
    public float maxMovementHeight = 2f;

    private Vector3 doorStartPos;
    private Vector3 doorEndPos;

    void Awake(){
        doorStartPos = door.transform.position;
        doorEndPos = doorStartPos + new Vector3(0, maxMovementHeight, 0);
    }

    void FixedUpdate(){
        float progress = (float)interaction.currentProcent / (float)interaction.completeProcent;
        
        if (interaction.eventSucces){
            door.transform.position = doorEndPos;
        }
        else {
            door.transform.position = Vector3.Lerp(doorStartPos, doorEndPos, progress);
        }
    }
}