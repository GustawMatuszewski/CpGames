using UnityEngine;
using System.Collections;

public class CatWander : MonoBehaviour
{
    public Transform[] waypoints;
    public float moveSpeed = 1.5f;
    public float waitTime = 2f;
    public Animator animator;

    int currentWaypoint = 0;
    bool waiting = false;
    [HideInInspector] public bool stopped = false;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

void Update()
{
    if (waiting || stopped || waypoints.Length == 0)
    {
        animator.SetBool("walking", false);
        return;
    }

    animator.SetBool("walking", true);

    Transform target = waypoints[currentWaypoint];
    transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
    transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

    if (Vector3.Distance(transform.position, target.position) < 0.1f)
        StartCoroutine(Wait());
}

    IEnumerator Wait()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        waiting = false;
    }
}