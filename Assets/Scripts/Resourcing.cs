using UnityEngine;
using System.Collections.Generic;

public class Resourcing : MonoBehaviour {
    public enum Type { Tree }
    public List<GameObject> interactions;

    public List<Vector3> cutPositions = new List<Vector3>();
    public float portionSize = 2f;

    public BoxCollider col;

    void Start() {
        col = GetComponentInChildren<BoxCollider>();
        if (col != null) {
            GenerateCutPositions(col);
        }
    }


    public void GenerateShitPieceSize(){

    }
    
    
    public void GenerateCutPositions(BoxCollider col)
    {
        cutPositions.Clear();

        float worldHeight = col.size.y * col.transform.lossyScale.y;

        Vector3 bottomCenter = col.transform.TransformPoint(col.center + new Vector3(0, -col.size.y * 0.5f, 0));
        Vector3 upDirection = col.transform.up;

        float currentDist = portionSize;

        while (currentDist < worldHeight - 0.1f)
        {
            Vector3 cutPoint = bottomCenter + upDirection * currentDist;
            cutPositions.Add(cutPoint);
            currentDist += portionSize;
        }
    }

    private void OnDrawGizmosSelected() {
        if (cutPositions == null) return;
        Gizmos.color = Color.red;
        foreach (Vector3 pos in cutPositions) {
            Gizmos.DrawSphere(pos, 0.2f);
        }
    }
}