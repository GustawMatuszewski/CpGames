using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class testShit : MonoBehaviour
{
    public Attack attackTest;
    public AttackTemplate currentAttack;
    public string playerTag = "Player";
    public string enemyTag = "Enemy";
    public bool targetPlayer = true;
    public bool targetEnemy = false;

    void Awake(){
        if(currentAttack == null && attackTest.attackList.Count > 0)
            currentAttack = attackTest.attackList[0];

        attackTest.SetAttack(currentAttack);
        attackTest.playerTag = playerTag;
        attackTest.enemyTag = enemyTag;
        attackTest.targetPlayer = targetPlayer;
        attackTest.targetEnemy = targetEnemy;
        attackTest.GetDamageHitbox();
    }

    void FixedUpdate(){
        attackTest.TryAttack();
        DetectHit();
    }

    void DetectHit(){
        GameObject hitbox = attackTest.GetDamageHitbox();
        if(hitbox != null){
            Collider[] hits = Physics.OverlapBox(hitbox.transform.position, hitbox.transform.localScale / 2, hitbox.transform.rotation);
            foreach(Collider hit in hits){
                attackTest.CheckHit(hit.gameObject);
            }
        }
    }
}
