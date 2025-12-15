using UnityEngine;

public class EntityStatus : MonoBehaviour
{
    public enum EntityType
    {
        none,
        Player,
        Enemy,
        Neutral
    }
    public EntityType entityType;
    public float entityHealth = 100f;
    private Attack attackScript;
    
    public void ApplyDamage(float damage){
        entityHealth -= damage;
        //Effects list to be  added later here!!!
    }

}
