using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class BaseEntity : MonoBehaviour
{
    public KCC player;
    public NavMeshAgent agent;
    public NavMeshAgent smallAgentPrefab;
    public List<GameObject> entities = new List<GameObject>();
    public LayerMask groundMask;
    public LayerMask entityMask;
    public bool debugMode = false;
    public float followDistance = 1f;
    private GameObject currentTarget;

    public EntityStatus status;

    //ADD DEFAULT SPHERE AREA FOR NON MIGRATIONAL AND A SPHERE ARE FOR MIGRATIONAL THAT WILL SPAWN ON PLAYER FOR SHORT PEIROD

    public enum MentalState
    {
        None,
        Neutral,    //Doesnt care bout u 
        Courious,   //Check u out from distance doesnt get near
        Interested, //Comes closer still keeps distance
        Scared,     //Runs away anytime u get near
        Terrified,  //U will never see it close ever again
        Aggresive,  //It will start attacking
        SuperAggresive, //U will be killed instanly fast attacks
        Friendly,   //Doesnt care bout u 
        Hurt    //Trys to escape maybe still fight
    }

    public enum EntityState
    {
        None,
        Walk,   //Walks casualy
        Sprint, //Sprint to target (can be escape target or entity target)
        Jump,   //Jumps thru stuff 
        Dash,   //Dashes to target for a few sec
        Attack, //Attacks the target combatscript connection here
        Hit,    //Entity Status script connection here  
        Patrol, //Walk around without any care
        Search, //Look for somethhig an entity or object
        Crawl,  //Trys to get to the point anyway crawl underneath anything it can fit to
        Prone   //Stands up after crawing needs to be checked wheter it can if there is space for him to stand
    }

    void FixedUpdate()
    {
        DetectEntitiesInSphere(transform.position, 20f, entityMask, groundMask, entities); //Detection sphere can be insta create and it will add entities to entitiy list u choose

        if (entities.Count > 0)
        {
            if (currentTarget != entities[0])
                currentTarget = entities[0];

            FollowTarget(currentTarget);    //Follows set target
        }
        else
        {
            currentTarget = null;
            if (agent != null && agent.hasPath)
                agent.ResetPath();
        }
    }

    void DetectEntitiesInSphere(Vector3 origin, float radius, LayerMask entityMask, LayerMask groundMask, List<GameObject> entitiesList)
    {
        Collider[] hits = Physics.OverlapSphere(origin, radius, entityMask);
        List<GameObject> currentEntities = new List<GameObject>();

        foreach (Collider col in hits)
        {
            GameObject topParent = GetTopParent(col.gameObject);    //Gets the object highest parent from ehere u can get kcc or entity status of the object
            currentEntities.Add(topParent);

            if (!entitiesList.Contains(topParent))
                entitiesList.Add(topParent);
        }

        for (int i = entitiesList.Count - 1; i >= 0; i--)
        {
            if (!currentEntities.Contains(entitiesList[i]))
                entitiesList.RemoveAt(i);
        }
    }

    GameObject GetTopParent(GameObject obj)
    {
        Transform current = obj.transform;
        while (current.parent != null)
            current = current.parent;
        return current.gameObject;
    }

    public bool TrySetDestination(Vector3 target, bool useSmallerCollider = true, bool moveToNearest = true) //check wheter it need smaller hitbox or normal one can fit Crawl: state
    {
        if (agent == null)
            return false;

        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(target, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            agent.SetDestination(target);
            return true;
        }

        if (useSmallerCollider && smallAgentPrefab != null)
        {
            NavMeshAgent temp = Instantiate(smallAgentPrefab, agent.transform.position, agent.transform.rotation);
            temp.enabled = false;
            temp.radius = smallAgentPrefab.radius;
            temp.height = smallAgentPrefab.height;

            if (NavMesh.CalculatePath(temp.transform.position, target, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.radius = temp.radius;
                agent.height = temp.height;
                agent.SetDestination(target);
                Destroy(temp.gameObject);
                return true;
            }

            Destroy(temp.gameObject);
        }

        if (moveToNearest)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(target, out hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return true;
            }
        }

        return false;
    }

    public void FollowTarget(GameObject target) //Folllows set target
    {
        if (target == null || agent == null)
            return;

        Vector3 targetPos = target.transform.position;
        Vector3 dir = (transform.position - targetPos).normalized;
        targetPos += dir * followDistance;

        TrySetDestination(targetPos);

        if (debugMode)
            Debug.Log("Following: " + target.name + " | Target Position: " + targetPos);
    }
}

//Than when u create a new script as this is a base u instead of monobeaviut give BaseEntity than u will be able to use things like state
// and all the void u create here. This is more modular and all the logic is not held in the base entity but all the instrucitons that 
//Any entity no matter what can use . Make it so it detect all things on entity layer and if u want to target player check if
//Game object contains kcc if not its just not the player velocity of the e
