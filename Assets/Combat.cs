using UnityEngine;
using System.Collections.Generic;

public class Combat : MonoBehaviour
{
    [Header("Debug Mode!!!!!!")]
    public bool debugMode;

    [Header("References")]
    public List<Collider> ownerHitboxes = new List<Collider>();
    public List<string> damageHitboxNameList = new List<string>(); //Zapytac Pawla czy woli jak to bedzie obiekt nie nazwa
    public string hitboxTag;
    AttackTemplate currentAttack;
    public Collider currentCollision;
    public bool canAttack;

    void FixedUpdate() {
        if(canAttack)
            ApplyDamage();
    }
    
    
    public void ApplyDamage(){
        currentCollision = HitboxDetector();
        if(DamageCollider(currentCollision) != null){
            EntityStatus status = DetectEntityStatus(currentCollision);
            status.ApplyDamage(20f);
        }
        currentCollision = null;
    }
    public Collider HitboxDetector(){
        foreach (Collider col in ownerHitboxes){
            BoxCollider box = col as BoxCollider;
            if (box == null)
                continue;

            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            Quaternion rotation = box.transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

            foreach (Collider hit in hits){
                if (hit == col)
                    continue;
                if (ownerHitboxes.Contains(hit))
                    continue;
                if (hit.transform.IsChildOf(transform))
                    continue;
                if (!hit.CompareTag(hitboxTag))
                    continue;
                if(debugMode)
                    Debug.Log("Hit: " + hit.name);
                return hit;
            }
        }

        return null;
    }




    public EntityStatus DetectEntityStatus(Collider hit){
        if (hit != null){
            EntityStatus status = hit.GetComponentInParent<EntityStatus>();
            if (status != null)
                return status;
        }
        return null;
    }
    
    public Collider DamageCollider(Collider damage){
        if (damage == null)
            return null;
        if (damageHitboxNameList.Contains(damage.name))
            return damage;
        return null;    
    }
}
