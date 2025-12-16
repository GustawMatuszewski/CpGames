using UnityEngine;
using System.Collections.Generic;

public class enemyHearing : MonoBehaviour
{
    [Header("Debug Mode")]
    public bool debugMode;
    [Header("Hearing Settings")]
    public float hearingRange = 10f;
    public float minVelocityThreshold = 5f;

    private enemyMovement movement;

    void Awake()
    {
        movement = GetComponent<enemyMovement>();
    }

    void FixedUpdate()
    {
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, hearingRange);

        List<SoundController> soundSources = new List<SoundController>();

        for (int i = 0; i < collidersInRange.Length; i++)
        {
            SoundController sc = collidersInRange[i].GetComponent<SoundController>();
            if (sc != null)
            {
                soundSources.Add(sc);
            }
        }

        if (soundSources.Count == 0) return;

        SoundController loudest = null;
        float maxSpeed = 0f;

        for (int i = 0; i < soundSources.Count; i++)
        {
            float speed = soundSources[i].GetVelocity().magnitude;
            Debug.Log("predkosc: "+speed);{}
            if (speed < minVelocityThreshold) continue;

            if (speed > maxSpeed)
            {
                maxSpeed = speed;
                loudest = soundSources[i];
            }
        }

        if (loudest != null)
            {
                Debug.Log("HEARD with speed: " + maxSpeed);
                movement.OnHearNoise(loudest.transform.position);
            }

        if (loudest == null)
        {
            Debug.Log("SILENCE");
        }

    }

    void OnDrawGizmos()
    {
        if(!debugMode) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
    }
}
