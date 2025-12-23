using UnityEngine;
using System.Collections.Generic;

public class Build : MonoBehaviour
{
    [Header("DEBUG MODE!!!")]
    public bool debugMode = true;
    public List<GameObject> hits;

    [Header("References")]
    public Construction toPlace;
    public Camera playerCamera;
    public KCC player;

    [Header("Placement")]
    public bool canBuild = true;
    public float placeDistance = 5f;
    public LayerMask buildMask;

    [Header("Snapping")]
    public float snapDistance = 0.4f;

    GameObject ghost;
    Construction ghostConstruction;
    BoxCollider ghostCollider;
    List<GameObject> ghostConnectors;
    Vector3 lastLookPosition;
    int noRaycastLayer;

    void Awake()
    {
        hits = new List<GameObject>();
        ghostConnectors = new List<GameObject>();
        noRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        Vector3 spawnPos = PlayerLook() + Vector3.up * 2f;
        
        ghost = Instantiate(toPlace.Model, spawnPos, Quaternion.identity);
        ghost.name = toPlace.name + " GHOST";
        ghostConstruction = ghost.GetComponent<Construction>();
        ghostCollider = ghost.GetComponent<BoxCollider>();

        if (ghostConstruction != null){
            foreach (GameObject c in ghostConstruction.connectors){
                ghostConnectors.Add(c);
            }
        }
        
        SetLayer(ghost, noRaycastLayer);
    }

    void Update(){
        MoveGhost();

        float interactVal = player.input.PlayerInputMap.InteractInput.ReadValue<float>();

        if (interactVal > 0 && canBuild){
            PlaceConstruction();
            canBuild = false;
        }
        
        if (interactVal <= 0){
            canBuild = true;
        }
    }

    void MoveGhost(){
        if (ghost == null || ghostConstruction == null || ghostCollider == null)
            return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPosition = lastLookPosition;
        bool hasHit = false;
        Vector3 hitNormal = Vector3.up;

        if (Physics.Raycast(ray, out RaycastHit hit, placeDistance, buildMask)){
            lastLookPosition = hit.point;
            targetPosition = hit.point;
            hitNormal = hit.normal;
            hasHit = true;
        }
        else{
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * placeDistance;
        }

        if (hasHit){
            Vector3 extents = ghostCollider.bounds.extents;
            Vector3 absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
            float offsetDist = Vector3.Dot(extents, absNormal);
            targetPosition = lastLookPosition + (hitNormal * offsetDist);
        }

        ghost.transform.position = targetPosition;

        hits.Clear();
        List<GameObject> foundConnectors = new List<GameObject>();

        Collider[] overlaps = Physics.OverlapBox(ghostCollider.bounds.center, ghostCollider.bounds.extents * 1.1f, ghost.transform.rotation, buildMask);
        foreach (Collider c in overlaps){
            Construction hitConstruction = c.transform.root.GetComponent<Construction>();
            if (hitConstruction != null && hitConstruction != ghostConstruction){
                foreach (GameObject connector in hitConstruction.connectors){
                    if (!foundConnectors.Contains(connector)){
                        foundConnectors.Add(connector);
                        hits.Add(connector);
                    }
                }
            }
        }

        GameObject bestGhostConn = null;
        GameObject bestTargetConn = null;
        float bestDist = snapDistance;

        foreach (GameObject gConn in ghostConnectors){
            foreach (GameObject tConn in foundConnectors){
                float d = Vector3.Distance(gConn.transform.position, tConn.transform.position);
                if (d < bestDist){
                    bestDist = d;
                    bestGhostConn = gConn;
                    bestTargetConn = tConn;
                }
            }
        }

        if (bestGhostConn != null && bestTargetConn != null){
            Vector3 offsetFromRoot = bestGhostConn.transform.position - ghost.transform.position;
            ghost.transform.position = bestTargetConn.transform.position - offsetFromRoot;
        }

        if (debugMode){
            DrawConnectorBoxes();
            if (bestTargetConn != null)
                Debug.DrawLine(bestGhostConn.transform.position, bestTargetConn.transform.position, Color.magenta);
        }
    }

    void DrawConnectorBoxes(){
        foreach (GameObject connector in hits){
            if (connector == null) continue;

            float dist = Vector3.Distance(ghost.transform.position, connector.transform.position);
            Color color = (dist <= snapDistance) ? Color.red : Color.green;
            float size = 0.2f;
            Vector3 p = connector.transform.position;
            Vector3 half = Vector3.one * size * 0.5f;

            Vector3[] corners = new Vector3[8];
            corners[0] = p + new Vector3(-half.x, -half.y, -half.z);
            corners[1] = p + new Vector3(half.x, -half.y, -half.z);
            corners[2] = p + new Vector3(half.x, -half.y, half.z);
            corners[3] = p + new Vector3(-half.x, -half.y, half.z);
            corners[4] = p + new Vector3(-half.x, half.y, -half.z);
            corners[5] = p + new Vector3(half.x, half.y, -half.z);
            corners[6] = p + new Vector3(half.x, half.y, half.z);
            corners[7] = p + new Vector3(-half.x, half.y, half.z);

            for (int i = 0; i < 4; i++){
                Debug.DrawLine(corners[i], corners[i + 4], color, 0f, false);
                Debug.DrawLine(corners[i], corners[(i + 1) % 4], color, 0f, false);
                Debug.DrawLine(corners[i + 4], corners[((i + 1) % 4) + 4], color, 0f, false);
            }
        }
    }

    void PlaceConstruction(){
        GameObject placed = Instantiate(toPlace.Model, ghost.transform.position, ghost.transform.rotation);
        placed.name = toPlace.name;
    }

    Vector3 PlayerLook(){
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, placeDistance, buildMask)){
            lastLookPosition = hit.point;
            return hit.point;
        }
        return lastLookPosition;
    }

    void SetLayer(GameObject obj, int layer){
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayer(child.gameObject, layer);
    }
}