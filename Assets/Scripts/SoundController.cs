using UnityEngine;

public class SoundController : MonoBehaviour
{
    public float soundLevel;
    public float soundMultiplier = 1f;
    private Vector3 lastPos;
    private Vector3 velocity;

    void Awake()
    {
        lastPos = transform.position;
    }

    void FixedUpdate()
    {
        velocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        lastPos = transform.position;
        soundLevel = velocity.magnitude * soundMultiplier;
        Debug.Log(velocity);
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }
}
