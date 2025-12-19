using UnityEngine;
using System.Collections.Generic;

public class HangarDoorInteract : MonoBehaviour {
    public GameObject door;
    public List<QuickTimeEvent> interactions;
    public float maxMovementHeight = 2f;

    private Vector3 doorStartPos;
    private Vector3 doorEndPos;

    void Start(){
        doorStartPos = door.transform.position;
        doorEndPos = doorStartPos + new Vector3(0, maxMovementHeight, 0);
    }

    void FixedUpdate(){
        if (interactions == null || interactions.Count == 0) return;

        float totalCurrent = 0;
        float totalMax = 0;
        bool allSuccessful = true;

        foreach (QuickTimeEvent qte in interactions) {
            totalCurrent += qte.currentProcent;
            totalMax += qte.completeProcent;
            
            if (!qte.eventSucces) {
                allSuccessful = false;
            }
        }

        float progress = totalMax > 0 ? totalCurrent / totalMax : 0;
        
        if (allSuccessful){
            door.transform.position = doorEndPos;
        }
        else {
            door.transform.position = Vector3.Lerp(doorStartPos, doorEndPos, progress);
        }
    }
}