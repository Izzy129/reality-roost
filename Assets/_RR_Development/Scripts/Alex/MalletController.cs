using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MalletController : MonoBehaviour
{
    public float maxDistance = 5f;

    private Vector3 startPos; 

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, startPos) > maxDistance)
        {
            StartCoroutine(ResetMallet());
        }
    }

    public IEnumerator ResetMallet()
    {
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.enabled = false;

        yield return new WaitForSeconds(0.2f);

        transform.position = startPos;

        grabInteractable.enabled = true;
    }
}