using UnityEngine;
using System.Collections.Generic;

public class Combat : MonoBehaviour
{
    [Header("Debug Mode!!!!!!")]
    public bool debugMode;

    [Header("References")]
    public List<Collider> ownerHitboxes = new List<Collider>();
    public List<string> damageHitboxNameList = new List<string>();
    public string hitboxTag;

    [Header("Attack Templates")]
    public List<AttackTemplate> attackTemplates = new List<AttackTemplate>();
    public AttackTemplate currentAttack;

    public Collider currentCollision;
    public bool canAttack;

    void FixedUpdate()
    {
        if (canAttack && currentAttack != null)
            ApplyDamage();
    }

    public void AddAttackTemplate(AttackTemplate attack)
    {
        if (attack == null)
            return;

        if (!attackTemplates.Contains(attack))
        {
            attackTemplates.Add(attack);

            if (debugMode)
                Debug.Log("Combat: Added attack template -> " + attack.name);
        }
    }

    public void SetCurrentAttack(AttackTemplate attack)
    {
        if (attack == null)
            return;

        if (!attackTemplates.Contains(attack))
        {
            if (debugMode)
                Debug.Log("Combat: Attack not registered -> " + attack.name);
            return;
        }

        currentAttack = attack;

        if (debugMode)
            Debug.Log("Combat: Current attack set -> " + attack.name);
    }

    public void ApplyDamage()
    {
        currentCollision = HitboxDetector();

        if (DamageCollider(currentCollision) != null)
        {
            EntityStatus status = DetectEntityStatus(currentCollision);
            if (status != null)
            {
                status.entityHealth = status.CalculateStat(status.entityHealth, -currentAttack.damage, 1.0f, status.entityMaxHealth);

                if (debugMode)
                    Debug.Log("Combat: Damage applied -> " + currentAttack.damage);
            }
        }

        currentCollision = null;
    }

    public Collider HitboxDetector()
    {
        foreach (Collider col in ownerHitboxes)
        {
            BoxCollider box = col as BoxCollider;
            if (box == null)
                continue;

            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            Quaternion rotation = box.transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);

            foreach (Collider hit in hits)
            {
                if (hit == col)
                    continue;
                if (ownerHitboxes.Contains(hit))
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

    public EntityStatus DetectEntityStatus(Collider hit)
    {
        if (hit != null)
            return hit.GetComponentInParent<EntityStatus>();

        return null;
    }

    public Collider DamageCollider(Collider damage)
    {
        if (damage == null)
            return null;

        if (damageHitboxNameList.Contains(damage.name))
            return damage;

        return null;
    }
}
