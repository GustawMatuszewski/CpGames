using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Combat : MonoBehaviour {
    [System.Serializable]
    public class Limb {
        public string name;
        public Collider limbHitbox;
        public float health = 50;
        public float damageMultiplier = 1f;
        public bool severed = false;
    }

    [Header("Debug Mode!!!!!!")]
    public bool debugMode;

    [Header("References")]
    [SerializeField]public List<Limb> ownerHitboxes;

    public List<string> damageHitboxNameList = new List<string>();
    public string hitboxTag;

    [Header("Attack Templates")]
    public List<AttackTemplate> attackTemplates = new List<AttackTemplate>();
    public AttackTemplate currentAttack;

    public Collider currentCollision;
    public bool canAttack;

    void FixedUpdate() {
        if (canAttack && currentAttack != null)
            ApplyDamage();
    }

    public void ApplyDamage() {
        currentCollision = HitboxDetector();

        if (DamageCollider(currentCollision) != null) {
            EntityStatus status = DetectEntityStatus(currentCollision);
            if (status != null) {
                status.entityHealth = status.CalculateStat(status.entityHealth, -currentAttack.damage, 1.0f, status.entityMaxHealth);
                //NEEDS TO BE MADE SO EVERY LIMB CONTRIBUTES TO THE HEALTH

                if (debugMode)
                    Debug.Log("Combat: Damage applied -> " + currentAttack.damage);
            }
        }

        currentCollision = null;
    }

    public Collider HitboxDetector() {
        foreach (Limb limb in ownerHitboxes) {
            BoxCollider box = limb.limbHitbox as BoxCollider;
            if (box == null)
                continue;

            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            Quaternion rotation = box.transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

            foreach (Collider hit in hits) {
                if (hit == limb.limbHitbox)
                    continue;
                if (ownerHitboxes.Exists(l => l.limbHitbox == hit))
                    continue;
                if (hit.transform.IsChildOf(transform))
                    continue;
                if (!hit.CompareTag(hitboxTag))
                    continue;

                if (debugMode)
                    Debug.Log("Combat: Hit detected -> " + hit.name);

                return hit;
            }
        }

        return null;
    }

    public EntityStatus DetectEntityStatus(Collider hit) {
        if (hit != null)
            return hit.GetComponentInParent<EntityStatus>();

        return null;
    }

    public Collider DamageCollider(Collider damage) {
        if (damage == null)
            return null;

        if (damageHitboxNameList.Contains(damage.name))
            return damage;

        return null;
    }
}
