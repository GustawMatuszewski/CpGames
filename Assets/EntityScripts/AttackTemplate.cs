using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Attack", menuName = "AI/Attack")]
public class AttackTemplate : ScriptableObject
{
    [Header("Settings")]
    public float damage;
    public float attackSpeed;
    public float timeToAttack;

    //Add Animation Here for attack
    public GameObject attackPrefab;

    //public float attackFromOriginPointOffset;
    //public float attackFromOriginTimer;

    public float cooldown;
    //public float range;
    //public float radius;

    public enum AttackType
    {
        None,
        Fast,
        Heavy,
        Normal
    }
    public AttackType attackType;

    public enum AttackEffect
    {
        None,
        Poison,
        Bleed,
        Fracture,
        Bit,
        Slash,
        Blunt,
        Shot
    }
    public List<AttackEffect> attackEffects;
}
