using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Combat : MonoBehaviour {
    private PlayerAnimationsController animCtrl;
    
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
    public Animator animator;
    [SerializeField] public List<Limb> ownerHitboxes = new List<Limb>();
    public List<string> damageHitboxNameList = new List<string>();
    public string hitboxTag;
    public List<AttackTemplate> attackTemplates = new List<AttackTemplate>();

    [Header("Combat ios")]
    public bool combatActive;
    public bool canAttack;
    // Jezeli true: po jednym ataku combatActive auto-resetuje sie do false (tryb gracza).
    // Jezeli false: atakuje w petli dopoki combatActive == true (tryb wroga).
    public bool singleShot = false;

    [Header("Player Attack Input")]
    // Przypisz akcje ataku z Input Action Asset (tylko dla gracza, zostaw puste dla wroga)
    public InputActionReference attackInput;
    public AttackTemplate currentAttack;
    public Collider currentCollision;

    [Header("Damage Received")]
    public UnityEvent onDamageReceived;

    public bool attackInProgress;
    private float attackTimer;
    private float cooldownTimer;

    void OnEnable()
    {
        if (attackInput != null)
            attackInput.action.Enable();
    }

    void OnDisable()
    {
        if (attackInput != null)
            attackInput.action.Disable();
    }

    void Update()
    {
        // Obsluga inputu gracza — tylko gdy singleShot i attackInput przypisany
        if (singleShot && attackInput != null && attackInput.action.WasPressedThisFrame())
        {
            if (currentAttack == null && attackTemplates.Count > 0)
                currentAttack = attackTemplates[0];

            combatActive = true;
        }
    }

    void FixedUpdate() {
        // Cooldown odliczamy zawsze
        if (cooldownTimer > 0f) {
            cooldownTimer -= Time.fixedDeltaTime;
        }

        // Trwający atak zawsze musi się dokończyć (nawet jeśli combatActive wyłączone w trakcie)
        if (attackInProgress) {
            attackTimer -= Time.fixedDeltaTime;
            if (attackTimer <= 0f) {
                ApplyDamage(currentAttack);
                attackInProgress = false;
                cooldownTimer = currentAttack.cooldown;
                // Tryb gracza: jeden klik = jeden atak, nie petla
                if (singleShot) combatActive = false;
            }
            return;
        }

        if (!combatActive || currentAttack == null)
            return;

        // Automatycznie atakuj gdy gotowi (brak ataku, cooldown minął)
        if (cooldownTimer <= 0f) {
            TriggerAttack();
        }
    }

    // Wywołuje atak — używane przez FixedUpdate oraz opcjonalnie z zewnątrz
    public void TriggerAttack() {
        if (attackInProgress || cooldownTimer > 0f || currentAttack == null) return;

        attackInProgress = true;
        attackTimer = currentAttack.timeToAttack;

        if (debugMode)
            Debug.Log("<color=orange>[Combat] TriggerAttack: </color>" + currentAttack.HittingAnimation);

        // Gracz ma PlayerAnimationsController
        PlayerAnimationsController combatAnim = GetComponent<PlayerAnimationsController>();
        if (combatAnim != null) {
            combatAnim.PlayCombatAnimation(currentAttack.HittingAnimation);
        }
        // Wróg — bezpośrednio przez Animator
        else if (animator != null) {
            animator.CrossFade(currentAttack.HittingAnimation, 0.1f);
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

        if (debugMode)
            Debug.Log("[DAMAGE RECEIVED] " + targetCombat.transform.root.name + " | Limb: " + hitLimb.name + " | HP left: " + hitLimb.health);

        targetCombat.onDamageReceived.Invoke();

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
                    Debug.Log("Attacker: "+transform.root.name+" Combat: Hit detected ----> " + hit.name);

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