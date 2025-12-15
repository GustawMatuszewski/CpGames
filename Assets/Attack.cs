using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Attack : MonoBehaviour
{
    public bool debugMode = true;
    public List<AttackTemplate> attackList;
    public EntityStatus owner;
    public string playerTag = "Player";
    public string enemyTag = "Enemy";
    public bool targetPlayer = true;
    public bool targetEnemy = false;

    AttackTemplate currentAttack;
    bool isAttacking;
    bool canAttack;

    private GameObject attackHitbox;
    private float damageCooldown;

    public void SetAttack(AttackTemplate attack){
        if (isAttacking) return;
        currentAttack = attack;
        if (debugMode)
            Debug.Log("Attack set to: " + currentAttack.name);
    }

    public void TryAttack(){
        if (isAttacking || currentAttack == null || canAttack != true) return;
        if (debugMode)
            Debug.Log("Attempting attack: " + currentAttack.name);

        StartCoroutine(AttackRoutine());
    }

    public GameObject GetDamageHitbox(){
        foreach (Transform child in GetComponentsInChildren<Transform>(true)){
            if (child.CompareTag("AttackHitbox")){
                if (debugMode)
                    Debug.Log("Attack hitbox found: " + child.name);
                return child.gameObject;
            }
        }
        if (debugMode)
            Debug.Log("No attack hitbox found");
        return null;
    }

    public void CheckHit(GameObject target){
        if ((targetPlayer && target.CompareTag(playerTag)) || (targetEnemy && target.CompareTag(enemyTag))){
            if (debugMode)
                Debug.Log("Attack hit: " + target.name);
        }
    }

    IEnumerator AttackRoutine(){
        isAttacking = true;
        canAttack = false;
        if (debugMode)
            Debug.Log("Attack started: " + currentAttack.name);

        yield return new WaitForSeconds(currentAttack.timeToAttack);

        isAttacking = false;
        if (debugMode)
            Debug.Log("Attack finished: " + currentAttack.name);
        StartCoroutine(AttackCoolDown());
        canAttack = true;
    }

    IEnumerator AttackCoolDown(){
        if (debugMode)
            Debug.Log("Attack cooldown started: " + currentAttack.name);
        yield return new WaitForSeconds(currentAttack.cooldown);    
        if (debugMode)
            Debug.Log("Attack cooldown finished: " + currentAttack.name);
    }
}
