// using UnityEngine;
// using UnityEngine.AI;

// public class enemyMovement : BaseEntity
// {
//     [Header("Definitions")]
//     public LayerMask whatIsGround, whatIsPlayer;

//     [Header("Agent Settings")]
//     public float speed;

//     [Header("Actions settings")]
//     public Vector3 walkPoint;
//     bool walkPointSet;
//     private float walkPointInterval;
//     private float nextPatrolTime;
//     public float walkPointRange;

//     public float attackInterval;
//     bool alreadyAttacked;

//     public float sightRange, attackRange;
//     public bool playerInSightRange, playerInAttackRange;

//     [Header("Search settings")]
//     public float investigateWaitTime = 3f;
//     bool investigating;
//     float investigateEndTime;
//     private Vector3 velocity;

//     public EntityState state;

//     private Vector3 lastPlayerPosition;
//     private void Awake()
//     {
//         player = FindAnyObjectByType<KCC>();
//         agent = GetComponent<NavMeshAgent>();
//         agent.speed = speed;
//         walkPointInterval = Random.Range(2,10);
//         nextPatrolTime = Time.time+walkPointInterval;
//     }

//     private void Update()
//     {
//         velocity = agent.velocity;
//         StateController();
//         playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
//         playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

//         if (playerInSightRange && playerInAttackRange)
//         {
//             Attack();
//         }
//         else if (playerInSightRange)
//         {
//             Chase();
//         }
//         else
//         {
//             Patrol();
//         }


//         if (debugMode)
//         {
//             DebugMode();
//         }
//     }

//     void StateController()
//     {
//         state = EntityState.None;
//         if(velocity.x !=0  || velocity.z!=0 ) //if adding run later - && velocity.x<=speed && velocity.z<=speed
//         {
//             state=EntityState.Walk;
//         } else if (alreadyAttacked)
//         {
//             state=EntityState.Attack;
//         }
//     }

//     void DebugMode()
//     {
//         if (agent.velocity.magnitude > 0.2f)
//         {
//             Vector3 startPos = transform.position;
//             Vector3 movement = agent.velocity;
//             int steps=10;
//             float radius=0.5f;
//             // Debug.DrawRay(startPos,movement,Color.magenta,0.1f);

//             for (int s = 0; s <= steps; s++)
//             {
//                 float t = s / (float)steps;
//                 Vector3 interpPos = Vector3.Lerp(startPos, startPos + movement, t);
//                 Vector3 interpBottom = interpPos + Vector3.down * 1.5f;
//                 Vector3 interpTop = interpPos + Vector3.up * 1.5f;

//                 Color col = Color.Lerp(Color.yellow, Color.green, t);
//                 DebugDrawCapsule(interpBottom, interpTop, radius, col);
//             }
//         }

//         DrawDebugTarget();
//     }
//     void DrawDebugTarget()
//     {
//         if(!debugMode) return;
        
//         Vector3 targetPos;
//         if(playerInSightRange && !playerInAttackRange)
//         {
//             targetPos=player.transform.position;
//         } else if (investigating)
//         {
//             targetPos=lastPlayerPosition;
//         } else if (walkPointSet)
//         {
//             targetPos=walkPoint;
//         } else return;
//         Vector3 targetPosTop = targetPos+Vector3.up*1.5f;
//         DebugDrawCapsule(targetPos,targetPosTop,0.5f, Color.red);
//         // if (walkPointSet)
//         // {
//         //     Vector3 walkPointTop = walkPoint+Vector3.up*1.5f;
//         //     DebugDrawCapsule(walkPoint,walkPointTop,0.5f, Color.red);
//         // } 
//     }

//     private void Patrol()
//     {

//         if (investigating)
//         {
//             TrySetDestination(lastPlayerPosition);

//             float dist = Vector3.Distance(transform.position, lastPlayerPosition);
//             if(dist<1f){
//                 agent.SetDestination(transform.position);

//                 if(investigateEndTime == 0f)
//                 {
//                     investigateEndTime= Time.time +investigateWaitTime;
//                     Debug.Log("At last known player position");
//                 }
//                 if (Time.time >= investigateEndTime)
//                 {
//                     investigating=false;
//                     investigateEndTime=0f;
//                     lastPlayerPosition=Vector3.zero;
//                     //starting patrol
//                     walkPointSet = false;
//                     walkPointInterval = Random.Range(2,10);
//                     nextPatrolTime = Time.time+walkPointInterval;
//                     Debug.Log("Return to patrolling ");
//                 }
//             }
//             return;
//         }

//         if(lastPlayerPosition != Vector3.zero)
//         {
//             investigating=true;
//             Debug.Log("Player pos lost");
//             return;
//         }

//         if (Time.time<nextPatrolTime) return;
        
//             if (!walkPointSet)
//             {
//                 // agent.SetDestination(lastPlayerPosition);
//                 SearchWalkPoint();
//             }
//             if (walkPointSet) 
//             {
//                 agent.SetDestination(walkPoint);
//                 Debug.Log("Patrol START");
//             };


//             Vector3 distanceToWalkPoint = transform.position - walkPoint;
//             if (distanceToWalkPoint.magnitude < 1f) 
//             {
//                 Debug.Log("Patrol STOP");
//                 walkPointSet = false;
//                 walkPointInterval = Random.Range(2,10);
//                 nextPatrolTime = Time.time+walkPointInterval;
//                 Debug.Log("odstep: "+walkPointInterval);
//             }
//     }
    // private void SearchWalkPoint()
    // {
    //     float randomZ = Random.Range(-walkPointRange, walkPointRange);
    //     float randomX = Random.Range(-walkPointRange, walkPointRange);
    //     walkPoint = new Vector3(transform.position.x+randomX, transform.position.y, transform.position.z+randomZ);
    //     if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)) walkPointSet = true;
//     }
//     private void Chase()
//     {
//         agent.SetDestination(player.transform.position);
//         lastPlayerPosition = player.transform.position;
//         investigating=false;
//     }
//     private void Attack()
//     {
//         agent.SetDestination(transform.position);
//         transform.LookAt(player.transform);
//         walkPointSet=false;

//         if (!alreadyAttacked)
//         {
//             Debug.Log("Atak");
//             alreadyAttacked=true;
//             Invoke(nameof(AttackReset),attackInterval);
//         }
        
//     }

//     private void AttackReset()
//     {
//         alreadyAttacked=false;
//     }

//     public void OnHearNoise(Vector3 movement)
//     {
//         lastPlayerPosition=movement;
//         investigating=true;
//     }
    
//     // Helper function to draw a capsule in the Scene view
//     void DebugDrawCapsule(Vector3 start, Vector3 end, float radius, Color color)
//     {
//         int segments = 16;
//         for (int i = 0; i < segments; i++)
//         {
//             float angle1 = (i / (float)segments) * Mathf.PI * 2;
//             float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

//             Vector3 offset1 = new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
//             Vector3 offset2 = new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

//             Debug.DrawLine(start + offset1, start + offset2, color);
//             Debug.DrawLine(end + offset1, end + offset2, color);
//             Debug.DrawLine(start + offset1, end + offset1, color);
//         }
//     }
//     void OnDrawGizmos()
//     {
//         if (!debugMode) return;

//         //Sight range
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireSphere(transform.position, sightRange);

//         //Attack range
//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(transform.position, attackRange);
//     }

// }