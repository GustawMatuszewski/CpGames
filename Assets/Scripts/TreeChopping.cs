using UnityEngine;

public class TreeChopping : MonoBehaviour
{
    public GameObject cutter;
    public GameObject cuttableActor;
    public GameObject tree;
    public bool debugMode;

    private Vector3[] cutterCorners = new Vector3[8];
    private Vector3[] actorCorners = new Vector3[8];
    private Vector3[] treeCorners = new Vector3[8];

    private void Update(){
        if (cutter != null && cuttableActor != null && tree != null){
            CalculateBounds(cutter, cutterCorners);
            CalculateBounds(tree, treeCorners);

            if (AreBoundsOverlapping(cutter, tree)){
                GameObject instance = Instantiate(cuttableActor);
                instance.transform.position = tree.transform.position;
                Vector3 p = instance.transform.position;
                p.y = cutter.transform.position.y;
                instance.transform.position = p;
            }
        }
    }

    private void OnDrawGizmos(){
        if (!debugMode) return;

        if (cutter != null){
            BoxCollider col = cutter.GetComponent<BoxCollider>();
            if (col != null){
                Gizmos.color = Color.red;
                Gizmos.matrix = cutter.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }

        if (tree != null){
            BoxCollider col = tree.GetComponent<BoxCollider>();
            if (col != null){
                Gizmos.color = Color.green;
                Gizmos.matrix = tree.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(col.center, col.size);
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    public void CalculateBounds(GameObject obj, Vector3[] corners){
        BoxCollider col = obj.GetComponent<BoxCollider>();
        if (col == null) return;

        Vector3 center = col.center;
        Vector3 size = col.size * 0.5f;

        corners[0] = center + new Vector3(-size.x, -size.y, -size.z);
        corners[1] = center + new Vector3(size.x, -size.y, -size.z);
        corners[2] = center + new Vector3(size.x, -size.y, size.z);
        corners[3] = center + new Vector3(-size.x, -size.y, size.z);
        corners[4] = center + new Vector3(-size.x, size.y, -size.z);
        corners[5] = center + new Vector3(size.x, size.y, -size.z);
        corners[6] = center + new Vector3(size.x, size.y, size.z);
        corners[7] = center + new Vector3(-size.x, size.y, size.z);

        for (int i = 0; i < 8; i++){
            corners[i] = obj.transform.TransformPoint(corners[i]);
        }
    }

    public bool AreBoundsOverlapping(GameObject a, GameObject b){
        BoxCollider colA = a.GetComponent<BoxCollider>();
        BoxCollider colB = b.GetComponent<BoxCollider>();

        if (colA == null || colB == null) return false;

        return colA.bounds.Intersects(colB.bounds);
    }
}
