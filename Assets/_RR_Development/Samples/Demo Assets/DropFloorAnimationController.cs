using UnityEngine;

public class DropFloorAnimationController : MonoBehaviour
{
    private Animator animator;
    private GameObject finalWaypoint;
    void Start()
    {
        animator = GetComponent<Animator>();
        finalWaypoint = GameObject.Find("Waypoint (7)"); // Refactor
    }

    // Update is called once per frame
    void Update()
    {/*
        if(isOpen == true && hasReturned == true)
        {
            if(gameObject.transform.position == finalWaypoint.transform.position)
            {
                // Close door
                isOpen = false;
            }
        }*/
    }

    /// Has to be a better way for doing this.
    /// Wanted to call native method using Animator, but couldn't find one. 
    public void ToggleIsOpenParameter(bool value)
    {
        animator.SetBool("isOpen", value); 
    }
    public void ToggleHasReturnedParameter(bool value)
    {
        animator.SetBool("hasReturned", value); 
    }
}
