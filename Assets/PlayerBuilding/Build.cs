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

    [Header("Settings")]
    public bool requireGroundEvenWhenSnapped = false;
    public float blockCheckScale = 0.45f; 
    
    [Header("Rotation Settings")]
    public bool useContinuousRotation = false;
    public float continuousRotationSpeed = 2f;
    public float snapRotationDegrees = 15f;

    [Header("Placement")]
    public bool canBuild = true;
    public float placeDistance = 5f;
    public LayerMask buildMask;
    public LayerMask groundMask;

    [Header("Snapping")]
    public float snapDistance = 0.5f;
    public bool snapRotation = true;

    GameObject ghost;
    Construction ghostConstruction;
    BoxCollider ghostCollider;
    List<GameObject> ghostConnectors;
    Vector3 lastLookPosition;
    int noRaycastLayer;
    
    bool isBlocked;
    bool isGrounded;
    GameObject currentSnappedObject;

    void Awake() => SpawnGhost();

    void Update(){
        MoveGhost();

        float interactVal = player.input.PlayerInputMap.InteractInput.ReadValue<float>();
        float swapVal = player.input.PlayerInputMap.CrouchInput.ReadValue<float>();

        float rotateVal = player.input.PlayerInputMap.RKey.ReadValue<float>();

        if (interactVal > 0 && canBuild && isGrounded && !isBlocked){
            PlaceConstruction();
            canBuild = false;
        }

        if (swapVal > 0) SpawnGhost();
        
        if (ghost != null){
            if (useContinuousRotation)
            {
                if (rotateVal > 0){
                    ghost.transform.Rotate(0, continuousRotationSpeed, 0); 
                }
            }else{
                if (player.input.PlayerInputMap.RKey.WasPressedThisFrame()){
                    ghost.transform.Rotate(0, snapRotationDegrees, 0);
                }
            }
        }

        if (interactVal <= 0) canBuild = true;
    }

    void MoveGhost(){
        if (ghost == null || ghostConstruction == null || ghostCollider == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Vector3 targetPosition = lastLookPosition;
        bool hasHit = false;
        Vector3 hitNormal = Vector3.up;

        if (Physics.Raycast(ray, out RaycastHit hit, placeDistance, buildMask | groundMask)){
            lastLookPosition = hit.point;
            targetPosition = hit.point;
            hitNormal = hit.normal;
            hasHit = true;
        } else {
            targetPosition = playerCamera.transform.position + playerCamera.transform.forward * placeDistance;
        }

        if (hasHit){
            Vector3 extents = Vector3.Scale(ghostCollider.size, ghost.transform.localScale) * 0.5f;
            Vector3 absNormal = new Vector3(Mathf.Abs(hitNormal.x), Mathf.Abs(hitNormal.y), Mathf.Abs(hitNormal.z));
            float offsetDist = Vector3.Dot(extents, absNormal);
            targetPosition = lastLookPosition + (hitNormal * offsetDist);
        }

        ghost.transform.position = targetPosition;

        hits.Clear();
        List<GameObject> foundConnectors = new List<GameObject>();
        Collider[] overlaps = Physics.OverlapSphere(ghost.transform.position, placeDistance, buildMask);
        
        foreach (Collider c in overlaps){
            if (c.transform.root == ghost.transform.root) continue;
            Construction hitConstruction = c.transform.root.GetComponent<Construction>();
            if (hitConstruction != null){
                foreach (GameObject connector in hitConstruction.connectors){
                    if (connector != null && !foundConnectors.Contains(connector))
                        foundConnectors.Add(connector);
                }
            }
        }

        GameObject bestGhostConn = null;
        GameObject bestTargetConn = null;
        float bestDist = snapDistance;
        currentSnappedObject = null;

        foreach (GameObject gConn in ghostConnectors){
            foreach (GameObject tConn in foundConnectors){
                float d = Vector3.Distance(gConn.transform.position, tConn.transform.position);
                if (!hits.Contains(tConn)) hits.Add(tConn);
                if (d < bestDist){
                    bestDist = d;
                    bestGhostConn = gConn;
                    bestTargetConn = tConn;
                }
            }
        }

        bool isSnapped = false;
        if (bestGhostConn != null && bestTargetConn != null){
            if (snapRotation){
                Quaternion delta =
                    bestTargetConn.transform.rotation *
                    Quaternion.Inverse(bestGhostConn.transform.rotation);

                ghost.transform.rotation = delta * ghost.transform.rotation;

                if (snapRotationDegrees > 0f){
                    Vector3 e = ghost.transform.eulerAngles;
                    e.y = Mathf.Round(e.y / snapRotationDegrees) * snapRotationDegrees;
                    ghost.transform.eulerAngles = e;
                }
            }

            Vector3 offsetFromRoot = bestGhostConn.transform.position - ghost.transform.position;
            ghost.transform.position = bestTargetConn.transform.position - offsetFromRoot;
            currentSnappedObject = bestTargetConn.transform.root.gameObject;
            isSnapped = true;
        }

        Vector3 checkExtents = Vector3.Scale(ghostCollider.size, ghost.transform.localScale) * blockCheckScale;
        Collider[] blockers = Physics.OverlapBox(ghostCollider.bounds.center, checkExtents, ghost.transform.rotation, buildMask | groundMask);
        
        isBlocked = false;
        foreach (var b in blockers) {
            if (b.transform.root == ghost.transform.root) continue;
            if (isSnapped && b.transform.root == currentSnappedObject.transform) continue;
            isBlocked = true;
            break;
        }

        Vector3 rayStart = ghost.transform.TransformPoint(ghostCollider.center);
        float rayLength = (ghostCollider.size.y * ghost.transform.lossyScale.y * 0.5f) + 0.15f;

        if (isSnapped && !requireGroundEvenWhenSnapped)
            isGrounded = true;
        else
            isGrounded = Physics.Raycast(rayStart, Vector3.down, rayLength, groundMask | buildMask);

        if (debugMode){
            DrawConnectorBoxes();
            DrawAllCollisionBoxes();
            DrawBox(ghostCollider.bounds.center, checkExtents, ghost.transform.rotation, isBlocked ? Color.red : Color.yellow);
            Debug.DrawRay(rayStart, Vector3.down * rayLength, isGrounded ? Color.green : Color.red);
        }
    }

    void DrawAllCollisionBoxes(){
        Collider[] nearby = Physics.OverlapSphere(ghost.transform.position, placeDistance, buildMask | groundMask);
        foreach (Collider col in nearby){
            if (col.gameObject == ghost || col.transform.root == ghost.transform) continue;
            if (col is BoxCollider box){
                Vector3 worldCenter = box.transform.TransformPoint(box.center);
                Vector3 worldExtents = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;
                DrawBox(worldCenter, worldExtents, box.transform.rotation, Color.white);
            }
        }
    }

    void DrawBox(Vector3 center, Vector3 extents, Quaternion rot, Color color){
        Vector3 v1 = rot * new Vector3(-extents.x, -extents.y, -extents.z) + center;
        Vector3 v2 = rot * new Vector3(extents.x, -extents.y, -extents.z) + center;
        Vector3 v3 = rot * new Vector3(extents.x, -extents.y, extents.z) + center;
        Vector3 v4 = rot * new Vector3(-extents.x, -extents.y, extents.z) + center;
        Vector3 v5 = rot * new Vector3(-extents.x, extents.y, -extents.z) + center;
        Vector3 v6 = rot * new Vector3(extents.x, extents.y, -extents.z) + center;
        Vector3 v7 = rot * new Vector3(extents.x, extents.y, extents.z) + center;
        Vector3 v8 = rot * new Vector3(-extents.x, extents.y, extents.z) + center;
        Debug.DrawLine(v1, v2, color); Debug.DrawLine(v2, v3, color); Debug.DrawLine(v3, v4, color); Debug.DrawLine(v4, v1, color);
        Debug.DrawLine(v5, v6, color); Debug.DrawLine(v6, v7, color); Debug.DrawLine(v7, v8, color); Debug.DrawLine(v8, v5, color);
        Debug.DrawLine(v1, v5, color); Debug.DrawLine(v2, v6, color); Debug.DrawLine(v3, v7, color); Debug.DrawLine(v4, v8, color);
    }

    void DrawConnectorBoxes(){
        foreach (GameObject connector in hits){
            if (connector == null) continue;
            float minDist = float.MaxValue;
            foreach(var gc in ghostConnectors) {
                float d = Vector3.Distance(gc.transform.position, connector.transform.position);
                if(d < minDist) minDist = d;
            }
            Color color = (minDist <= snapDistance) ? Color.green : Color.red;
            DrawBox(connector.transform.position, Vector3.one * 0.05f, connector.transform.rotation, color);
        }
    }

    void PlaceConstruction() => Instantiate(toPlace.Model, ghost.transform.position, ghost.transform.rotation);

    Vector3 PlayerLook(){
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, placeDistance, buildMask | groundMask)){
            lastLookPosition = hit.point;
            return hit.point;
        }
        return lastLookPosition;
    }

    void SetLayer(GameObject obj, int layer){
        obj.layer = layer;
        foreach (Transform child in obj.transform) SetLayer(child.gameObject, layer);
    }
    
    void SpawnGhost(){
        if (ghost != null) Destroy(ghost);
        hits = new List<GameObject>();
        ghostConnectors = new List<GameObject>();
        noRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        
        ghost = Instantiate(toPlace.Model, PlayerLook() + Vector3.up * 2f, Quaternion.identity);
        ghost.name = toPlace.name + " GHOST";
        ghostConstruction = ghost.GetComponent<Construction>();
        ghostCollider = ghost.GetComponent<BoxCollider>();

        if (ghostConstruction != null){
            foreach (GameObject c in ghostConstruction.connectors) ghostConnectors.Add(c);
        }
        SetLayer(ghost, noRaycastLayer);
    }
}