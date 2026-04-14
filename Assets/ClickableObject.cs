using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ClickableObject : MonoBehaviour, IInteractable
{
    public Animator animator;
    public GameObject infoCanvas;

    // IInteractable — no snapping needed for this object
    public bool UseSnapping => false;
    public List<Transform> InteractionPositions => null;
    public Transform LookAtTarget => null;


    void Start()
    {
        // Get animator from children if not assigned directly
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        Debug.Log("Animator found: " + animator?.name);
    }
    public void OnInteract()
    {
        Debug.Log("OnInteract called!");
        animator.SetBool("playanim", true);
        Debug.Log("PlayAnim set to true, current state: " + animator.GetCurrentAnimatorStateInfo(0).IsName("Armature|EasterEgg (1)"));
        StartCoroutine(ShowCanvasAfterAnim());
    }

    private IEnumerator ShowCanvasAfterAnim()
    {
        // Wait one frame for animator to transition into the new state
        yield return null;

        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);

        // infoCanvas.SetActive(true);
        animator.SetBool("playanim", false);
    }
}