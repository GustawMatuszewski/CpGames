using UnityEngine;
using System.Collections.Generic;

public class Combat : MonoBehaviour {
    [System.Serializable]
    public class Limb {
        public enum DamageType {
            None,
            Beat,
            Fractured,
            Scratched,
            DeepWound,
            HeavyBleeding,
            Infected,
            Bleeding,
            Splinted,
            Bandaged,
            Suttered,
            Bit,
            Fucked
        }

        public string name;
        public Collider limbHitbox;
        public float health = 50f;
        public float damageMultiplier = 1f;
        public float severeMultiplier = 1f;
        public float fractureMultiplier = 1f;
        public List<DamageType> limbDamageList = new List<DamageType>();
        public bool severed;
        public int beatStacks;
    }

    [Header("Debug Mode!!!")]
    public bool debugMode;

    [Header("References")]
    [SerializeField] public List<Limb> ownerHitboxes = new List<Limb>();
    public List<string> damageHitboxNameList = new List<string>();
    public string hitboxTag;
    public List<AttackTemplate> attackTemplates = new List<AttackTemplate>();

    [Header("Combat ios")]
    public bool combatActive;
    public bool canAttack;
    public AttackTemplate currentAttack;
    public Collider currentCollision;

    private bool attackInProgress;
    private float attackTimer;
    private float cooldownTimer;

    void FixedUpdate() {
        if (combatActive == false)
            return; 
            
        if (cooldownTimer > 0f) {
            cooldownTimer -= Time.fixedDeltaTime;
            return;
        }

        if (!attackInProgress && canAttack && currentAttack != null) {
            attackInProgress = true;
            attackTimer = currentAttack.timeToAttack;
        }

        if (attackInProgress) {
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer <= 0f) {
                ApplyDamage(currentAttack);
                attackInProgress = false;
                cooldownTimer = currentAttack.cooldown;
                // canAttack=false;
            }
        }
    }

    public void ApplyDamage(AttackTemplate attackToApply) {
        currentCollision = HitboxDetector();
        if (currentCollision == null)
            return;

        Combat targetCombat = currentCollision.GetComponentInParent<Combat>();
        if (targetCombat == null)
            return;

        Limb hitLimb = targetCombat.ownerHitboxes.Find(l => l.limbHitbox == currentCollision);
        if (hitLimb == null)
            return;

        switch (attackToApply.attackType) {
            case AttackTemplate.AttackType.Fast:
                FastAttackDamageCalc(attackToApply, hitLimb);
                break;
            case AttackTemplate.AttackType.Normal:
                NormalAttackDamageCalc(attackToApply, hitLimb);
                break;
            case AttackTemplate.AttackType.Heavy:
                HeavyAttackDamageCalc(attackToApply, hitLimb);
                break;
        }

        currentCollision = null;
    }

    void AddBeat(Limb limb, int amount) {
        limb.beatStacks += amount;

        if (!limb.limbDamageList.Contains(Limb.DamageType.Beat))
            limb.limbDamageList.Add(Limb.DamageType.Beat);

        if (limb.beatStacks >= 3) {
            limb.limbDamageList.Remove(Limb.DamageType.Beat);
            if (!limb.limbDamageList.Contains(Limb.DamageType.Fractured))
                limb.limbDamageList.Add(Limb.DamageType.Fractured);
        }
    }

    void AddBleeding(Limb limb) {
        if (!limb.limbDamageList.Contains(Limb.DamageType.Bleeding))
            limb.limbDamageList.Add(Limb.DamageType.Bleeding);
    }

    void AddDeepShot(Limb limb) {
        if (!limb.limbDamageList.Contains(Limb.DamageType.DeepWound))
            limb.limbDamageList.Add(Limb.DamageType.DeepWound);
        if (!limb.limbDamageList.Contains(Limb.DamageType.HeavyBleeding))
            limb.limbDamageList.Add(Limb.DamageType.HeavyBleeding);
    }

    public void FastAttackDamageCalc(AttackTemplate attack, Limb limb) {
        float damage = attack.damage * limb.damageMultiplier * 0.8f;
        limb.health -= damage;

        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects) {
            if (effect == AttackTemplate.AttackEffect.Slash) {
                AddBleeding(limb);
                TrySever(limb, 0.05f);
            }

            if (effect == AttackTemplate.AttackEffect.Blunt) {
                AddBeat(limb, 1);
            }

            if (effect == AttackTemplate.AttackEffect.Shot) {
                AddDeepShot(limb);
            }
        }

        if (debugMode)
            Debug.Log("Fast attack -> " + limb.name + " dmg: " + damage);
    }

    public void NormalAttackDamageCalc(AttackTemplate attack, Limb limb) {
        float damage = attack.damage * limb.damageMultiplier;
        limb.health -= damage;

        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects) {
            if (effect == AttackTemplate.AttackEffect.Slash) {
                AddBleeding(limb);
                TrySever(limb, 0.25f);
            }

            if (effect == AttackTemplate.AttackEffect.Blunt) {
                AddBeat(limb, 1);
            }

            if (effect == AttackTemplate.AttackEffect.Shot) {
                AddDeepShot(limb);
            }
        }

        if (debugMode)
            Debug.Log("Normal attack -> " + limb.name + " dmg: " + damage);
    }

    public void HeavyAttackDamageCalc(AttackTemplate attack, Limb limb) {
        float damage = attack.damage * limb.damageMultiplier * 1.3f;
        limb.health -= damage;

        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects) {
            if (effect == AttackTemplate.AttackEffect.Slash) {
                AddBleeding(limb);
                TrySever(limb, 0.6f);
            }

            if (effect == AttackTemplate.AttackEffect.Blunt) {
                AddBeat(limb, 2);
            }

            if (effect == AttackTemplate.AttackEffect.Shot) {
                AddDeepShot(limb);
            }
        }

        if (debugMode)
            Debug.Log("Heavy attack -> " + limb.name + " dmg: " + damage);
    }

    private void TrySever(Limb limb, float chance) {
        if (limb.severed)
            return;

        if (Random.value < chance) {
            limb.severed = true;
            limb.health = 0f;
            if (debugMode)
                Debug.Log("LIMB SEVERED -> " + limb.name);
        }
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
                    Debug.Log("Combat: Hit detected ----> " + hit.name);

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
