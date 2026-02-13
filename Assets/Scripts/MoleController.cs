using UnityEngine;
using System.Collections;

public class MoleController : MonoBehaviour
{
    public GameObject moleHead;
    WhacAMoleController controller;

    public bool isStunned;

    private void Start()
    {
        controller = GetComponentInParent<WhacAMoleController>();
    }   
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (!isStunned && collision.gameObject.CompareTag("Destroyer"))
            StartCoroutine(GetHit());
    }

    IEnumerator GetHit()
    {
        isStunned = true;
        controller.IncreaseScore();
        moleHead.transform.localRotation = Quaternion.Euler(0f, 0f, -50f);
        yield return new WaitForSeconds(1f);
        controller.PopMole(this, false); //pop down
        yield return new WaitForSeconds(1f);
        moleHead.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        isStunned = false;
    }
}
