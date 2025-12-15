using UnityEngine;
using System.Collections.Generic;

public class Combat : MonoBehaviour
{
    [Header("References")]
    public List<Collider> ownerHitboxes = new List<Collider>();
    public List<string> damageHitboxNameList = new List<string>();
    public string hitboxTag;
    AttackTemplate currentAttack;

    public bool canAttack;

    void FixedUpdate() {
        ApplyDamage();
    }
    
    
    public void ApplyDamage(){
        if(DamageCollider(HitboxDetector()) != null){
            EntityStatus status = DetectEntityStatus();
            status.ApplyDamage(20f);
            Debug.Log(HitboxDetector());
        }
    }

public Collider HitboxDetector() {
    foreach (Collider hitbox in ownerHitboxes) {
        Collider[] hits = Physics.OverlapBox(hitbox.bounds.center, hitbox.bounds.extents, hitbox.transform.rotation);
        foreach (Collider hit in hits) {
            if (hit.transform.root == transform.root) continue;
            if (hit.CompareTag(hitboxTag))
                return hit;
        }
    }
    return null;
}



    public EntityStatus DetectEntityStatus(){
        Collider hit = HitboxDetector();
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
