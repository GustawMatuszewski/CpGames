using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Combat : MonoBehaviour
{
    private PlayerAnimationsController animCtrl;
    private MovementStateController msc;

    [System.Serializable]
    public class Limb
    {
        public enum DamageType
        {
            None, Beat, Fractured, Scratched, DeepWound,
            HeavyBleeding, Infected, Bleeding, Splinted,
            Bandaged, Suttered, Bit, Fucked
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
    public bool singleShot = false;

    [Header("Player Attack Input")]
    public InputActionReference attackInput;
    public AttackTemplate currentAttack;
    public Collider currentCollision;

    [Header("Damage Received")]
    public UnityEvent onDamageReceived;

    [Header("Hit Reaction")]
    public string hitAnimationName = "GetHurt";
    public float hitStunDuration = 0.5f;

    [HideInInspector] public bool isStunned;
    private float stunTimer;

    public bool attackInProgress;
    private float attackTimer;
    private float cooldownTimer;
    private bool damageAlreadyApplied;

    void Start()
    {
        animCtrl = GetComponent<PlayerAnimationsController>();
        msc      = GetComponent<MovementStateController>();
    }

    void OnEnable()
    {
        if (attackInput != null) attackInput.action.Enable();
    }

    void OnDisable()
    {
        if (attackInput != null) attackInput.action.Disable();
    }

    void Update()
    {
        if (isStunned) return;

        // singleShot: jeden klik = jeden atak (tylko dla gracza)
        if (singleShot && attackInput != null && attackInput.action.WasPressedThisFrame())
        {
            if (attackInProgress || cooldownTimer > 0f) return;

            if (currentAttack == null && attackTemplates.Count > 0)
                currentAttack = attackTemplates[0];

            TriggerAttack();
        }
    }

    void FixedUpdate()
    {
        // ── STAN OGŁUSZENIA (GetHurt) ─────────────────────────────────────
        if (isStunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            attackInProgress     = false;
            attackTimer          = 0f;
            damageAlreadyApplied = true;

            bool hitAnimStillPlaying = (animCtrl != null) && animCtrl.IsAnimationPlaying(hitAnimationName);

            if (stunTimer <= 0f && !hitAnimStillPlaying)
            {
                isStunned = false;
                cooldownTimer = 0.1f;
                if (msc != null) msc.stateRefreshRequired = false;
            }
            return;
        }

        // ── COOLDOWN ──────────────────────────────────────────────────────
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.fixedDeltaTime;

        // ── TRWAJĄCY ATAK ─────────────────────────────────────────────────
        if (attackInProgress)
        {
            if (attackTimer > 0f)
                attackTimer -= Time.fixedDeltaTime;

            // Sprawdzaj hitbox co klatkę po upływie windupa
            if (attackTimer <= 0f && !damageAlreadyApplied)
            {
                currentCollision = HitboxDetector();
                if (currentCollision != null)
                {
                    ApplyDamage(currentAttack);
                    damageAlreadyApplied = true;
                }
            }

            // Zdejmij atak gdy animacja skończyła się w Animatorze
            bool isAttackAnimPlaying = (animCtrl != null && currentAttack != null)
                && animCtrl.IsAnimationPlaying(currentAttack.HittingAnimation);

            if (attackTimer <= 0f && !isAttackAnimPlaying)
            {
                attackInProgress     = false;
                damageAlreadyApplied = false;
                cooldownTimer        = currentAttack != null ? currentAttack.cooldown : 0.5f;

                // WAŻNE: resetuj combatActive tylko dla gracza (singleShot)
                // Enemy ma combatActive ustawiane przez AI — nie resetuj!
                if (singleShot) combatActive = false;
            }
            return;
        }

        // ── NOWY ATAK (enemy continuous lub gracz non-singleShot) ─────────
        if (!combatActive || currentAttack == null) return;

        if (cooldownTimer <= 0f)
            TriggerAttack();
    }

    public void TriggerAttack()
    {
        if (attackInProgress || cooldownTimer > 0f || currentAttack == null) return;

        attackInProgress     = true;
        damageAlreadyApplied = false;
        attackTimer          = currentAttack.timeToAttack;

        if (debugMode)
            Debug.Log("<color=orange>[Combat] TriggerAttack: </color>" + currentAttack.HittingAnimation);

        if (animCtrl != null)
            animCtrl.PlayCombatAnimation(currentAttack.HittingAnimation);
        else if (animator != null)
            animator.Play(currentAttack.HittingAnimation, 0, 0f);
    }

    public void ApplyDamage(AttackTemplate attackToApply)
    {
        if (currentCollision == null) return;

        Combat targetCombat = currentCollision.GetComponentInParent<Combat>();
        if (targetCombat == null) return;

        // Natychmiast przerywamy i odpalamy GetHurt na celu
        targetCombat.InterruptAndTakeHit();

        Limb hitLimb = targetCombat.ownerHitboxes.Find(l => l.limbHitbox == currentCollision);
        if (hitLimb != null)
        {
            switch (attackToApply.attackType)
            {
                case AttackTemplate.AttackType.Fast:   FastAttackDamageCalc(attackToApply, hitLimb);   break;
                case AttackTemplate.AttackType.Normal: NormalAttackDamageCalc(attackToApply, hitLimb); break;
                case AttackTemplate.AttackType.Heavy:  HeavyAttackDamageCalc(attackToApply, hitLimb);  break;
            }

            if (debugMode)
                Debug.Log("[DAMAGE] " + targetCombat.transform.root.name + " | " + hitLimb.name + " | HP: " + hitLimb.health);
        }

        targetCombat.onDamageReceived.Invoke();
        currentCollision = null;
    }

    public void InterruptAndTakeHit()
    {
        attackInProgress     = false;
        attackTimer          = 0f;
        damageAlreadyApplied = true;

        isStunned = true;
        stunTimer = hitStunDuration;

        if (msc != null) msc.stateRefreshRequired = false;

        if (animCtrl != null)
            animCtrl.PlayHitAnimation(hitAnimationName);
        else if (animator != null)
            animator.Play(hitAnimationName, 0, 0f);

        if (debugMode)
            Debug.Log("<color=cyan>[Combat] GetHurt: </color>" + gameObject.name);
    }

    void AddBeat(Limb limb, int amount)
    {
        limb.beatStacks += amount;
        if (!limb.limbDamageList.Contains(Limb.DamageType.Beat))
            limb.limbDamageList.Add(Limb.DamageType.Beat);
        if (limb.beatStacks >= 3)
        {
            limb.limbDamageList.Remove(Limb.DamageType.Beat);
            if (!limb.limbDamageList.Contains(Limb.DamageType.Fractured))
                limb.limbDamageList.Add(Limb.DamageType.Fractured);
        }
    }

    void AddBleeding(Limb limb)
    {
        if (!limb.limbDamageList.Contains(Limb.DamageType.Bleeding))
            limb.limbDamageList.Add(Limb.DamageType.Bleeding);
    }

    void AddDeepShot(Limb limb)
    {
        if (!limb.limbDamageList.Contains(Limb.DamageType.DeepWound))
            limb.limbDamageList.Add(Limb.DamageType.DeepWound);
        if (!limb.limbDamageList.Contains(Limb.DamageType.HeavyBleeding))
            limb.limbDamageList.Add(Limb.DamageType.HeavyBleeding);
    }

    public void FastAttackDamageCalc(AttackTemplate attack, Limb limb)
    {
        float damage = attack.damage * limb.damageMultiplier * 0.8f;
        limb.health -= damage;
        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects)
        {
            if (effect == AttackTemplate.AttackEffect.Slash) { AddBleeding(limb); TrySever(limb, 0.05f); }
            if (effect == AttackTemplate.AttackEffect.Blunt) AddBeat(limb, 1);
            if (effect == AttackTemplate.AttackEffect.Shot)  AddDeepShot(limb);
        }
        if (debugMode) Debug.Log("Fast -> " + limb.name + " dmg: " + damage);
    }

    public void NormalAttackDamageCalc(AttackTemplate attack, Limb limb)
    {
        float damage = attack.damage * limb.damageMultiplier;
        limb.health -= damage;
        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects)
        {
            if (effect == AttackTemplate.AttackEffect.Slash) { AddBleeding(limb); TrySever(limb, 0.25f); }
            if (effect == AttackTemplate.AttackEffect.Blunt) AddBeat(limb, 1);
            if (effect == AttackTemplate.AttackEffect.Shot)  AddDeepShot(limb);
        }
        if (debugMode) Debug.Log("Normal -> " + limb.name + " dmg: " + damage);
    }

    public void HeavyAttackDamageCalc(AttackTemplate attack, Limb limb)
    {
        float damage = attack.damage * limb.damageMultiplier * 1.3f;
        limb.health -= damage;
        foreach (AttackTemplate.AttackEffect effect in attack.attackEffects)
        {
            if (effect == AttackTemplate.AttackEffect.Slash) { AddBleeding(limb); TrySever(limb, 0.6f); }
            if (effect == AttackTemplate.AttackEffect.Blunt) AddBeat(limb, 2);
            if (effect == AttackTemplate.AttackEffect.Shot)  AddDeepShot(limb);
        }
        if (debugMode) Debug.Log("Heavy -> " + limb.name + " dmg: " + damage);
    }

    private void TrySever(Limb limb, float chance)
    {
        if (limb.severed) return;
        if (Random.value < chance)
        {
            limb.severed = true;
            limb.health  = 0f;
            if (debugMode) Debug.Log("SEVERED -> " + limb.name);
        }
    }

    public Collider HitboxDetector()
    {
        foreach (Limb limb in ownerHitboxes)
        {
            BoxCollider box = limb.limbHitbox as BoxCollider;
            if (box == null) continue;

            Vector3    center      = box.transform.TransformPoint(box.center);
            Vector3    halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            Quaternion rotation    = box.transform.rotation;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation);
            foreach (Collider hit in hits)
            {
                if (hit == limb.limbHitbox) continue;
                if (ownerHitboxes.Exists(l => l.limbHitbox == hit)) continue;
                if (hit.transform.IsChildOf(transform)) continue;
                if (!hit.CompareTag(hitboxTag)) continue;
                if (debugMode) Debug.Log("Hit: " + hit.name);
                return hit;
            }
        }
        return null;
    }

    public EntityStatus DetectEntityStatus(Collider hit)
    {
        if (hit != null) return hit.GetComponentInParent<EntityStatus>();
        return null;
    }

    public Collider DamageCollider(Collider damage)
    {
        if (damage == null) return null;
        if (damageHitboxNameList.Contains(damage.name)) return damage;
        return null;
    }
}