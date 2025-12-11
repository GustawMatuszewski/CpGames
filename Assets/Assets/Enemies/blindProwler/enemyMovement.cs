using UnityEngine;
using UnityEngine.AI;

public class enemyMovement : MonoBehaviour
{
    [Header("DEBUG MODE!")]
    public bool debugMode;
    [Header("Definitions")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Agent Settings")]
    public float speed;
    private Vector3 velocity;

    [Header("Actions' settings")]
    public Vector3 walkPoint;
    bool walkPointSet;
    private float walkPointInterval;
    private float nextPatrolTime;
    public float walkPointRange;

    public float attackInterval;
    bool alreadyAttacked;

    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    public enum State
    {
        None,
        Idle,
        Walk,
        Attack
    }
    public State state;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        walkPointInterval = Random.Range(2,10);
        nextPatrolTime = Time.time+walkPointInterval;
    }

    private void Update()
    {
        velocity = agent.velocity;
        StateController();
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) Patrol();
        if (playerInSightRange && !playerInAttackRange) Chase();
        if (playerInSightRange && playerInAttackRange) Attack();
        Debug.Log(state);
    }

    void StateController()
    {
        state = State.None;
        if(velocity.x !=0  || velocity.z!=0 ) //if adding run later - && velocity.x<=speed && velocity.z<=speed
        {
            state=State.Walk;
        } else if (velocity.x ==0 && velocity.z ==0 && !alreadyAttacked)
        {
            state=State.Idle;
        } else if (alreadyAttacked)
        {
            state=State.Attack;
        }
    }

    private void Patrol()
    {
        if (nextPatrolTime <= Time.time)
        {
            if (!walkPointSet) SearchWalkPoint();
            if (walkPointSet) agent.SetDestination(walkPoint);

            Vector3 distanceToWalkPoint = transform.position - walkPoint;
            if (distanceToWalkPoint.magnitude < 1f) walkPointSet = false;
            nextPatrolTime=Time.time+walkPointInterval;
        }

    }
    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);
        walkPoint = new Vector3(transform.position.x+randomX, transform.position.y, transform.position.z+randomZ);
        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)) walkPointSet = true;
    }
    private void Chase()
    {
        agent.SetDestination(player.position);
    }
    private void Attack()
    {
        agent.SetDestination(transform.position);
        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            Debug.Log("Atak");
            alreadyAttacked=true;
            Invoke(nameof(AttackReset),attackInterval);
        }
        
    }

    private void AttackReset()
    {
        alreadyAttacked=false;
    }
    
    
}
