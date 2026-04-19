using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClickableObject : MonoBehaviour, IInteractable
{
    public Animator animator;
    public GameObject infoCanvas;
    public CatWander catWander;
    public float floatDelay = 3f;
    public float floatSpeed = 2f;

    public bool UseSnapping => false;
    public List<Transform> InteractionPositions => null;
    public Transform LookAtTarget => null;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void OnInteract()
    {
        if (catWander != null) catWander.stopped = true;
        animator.SetTrigger("playanim");
        StartCoroutine(ShowCanvasAfterAnim());
    }

    private IEnumerator ShowCanvasAfterAnim()
    {
        // Wait for transition to finish
        yield return new WaitForSeconds(0.3f);
        
        // Now we're in the easter egg state, read its length
        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log("Easter egg clip length: " + length);
        yield return new WaitForSeconds(length);

        infoCanvas.SetActive(true);
        yield return new WaitForSeconds(floatDelay);
        StartCoroutine(FloatToSky());
    }

    private IEnumerator FloatToSky()
    {
        animator.SetBool("playanim", false);
        while (true)
        {
            transform.position += Vector3.up * floatSpeed * Time.deltaTime;
            yield return null;
        }
    }
}