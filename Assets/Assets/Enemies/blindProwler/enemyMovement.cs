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

        if (debugMode)
        {
            DebugMode();
        }
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

    void DebugMode()
    {
        if (agent.velocity.magnitude > 0.2f)
        {
            Vector3 startPos = transform.position;
            Vector3 movement = agent.velocity;
            int steps=10;
            float radius=0.5f;
            // Debug.DrawRay(startPos,movement,Color.magenta,0.1f);

            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 interpPos = Vector3.Lerp(startPos, startPos + movement, t);
                Vector3 interpBottom = interpPos + Vector3.down * 1.5f;
                Vector3 interpTop = interpPos + Vector3.up * 1.5f;

                Color col = Color.Lerp(Color.yellow, Color.green, t);
                DebugDrawCapsule(interpBottom, interpTop, radius, col);
            }
        }
        if (walkPointSet)
        {
            Vector3 walkPointTop = walkPoint+Vector3.up*1.5f;
            DebugDrawCapsule(walkPoint,walkPointTop,0.5f, Color.red);
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
        walkPointSet=false;
    }
    private void Attack()
    {
        agent.SetDestination(transform.position);
        transform.LookAt(player);
        walkPointSet=false;

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
    
    // Helper function to draw a capsule in the Scene view
    void DebugDrawCapsule(Vector3 start, Vector3 end, float radius, Color color)
    {
        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

            Vector3 offset1 = new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 offset2 = new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Debug.DrawLine(start + offset1, start + offset2, color);
            Debug.DrawLine(end + offset1, end + offset2, color);
            Debug.DrawLine(start + offset1, end + offset1, color);
        }
    }
}
