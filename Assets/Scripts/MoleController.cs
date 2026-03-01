using UnityEngine;
using System.Collections;

public class MoleController : MonoBehaviour
{
    public GameObject moleHead;
    WhacAMoleController controller;

    public GameObject bombModel, moleModel;
    public GameObject explosionPrefab;
    public bool isStunned;

    private bool isBomb;

    private void Start()
    {
        controller = GetComponentInParent<WhacAMoleController>();
    }

    public void SetUpState(bool up)
    {
        if (up)
        {
            isBomb = Random.value < 0.25f;
            bombModel.SetActive(isBomb);
            moleModel.SetActive(!isBomb);
        }
        else
        {
            bombModel.SetActive(false);
            moleModel.SetActive(true);
        }
    }

    public IEnumerator MoveMole(bool up, float targetY)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(startPos.x, targetY, startPos.z);
        float duration = 0.25f;
        float elapsed = 0f;

        if (up)
            SetUpState(true);

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        if (!up)
            SetUpState(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isStunned && collision.gameObject.CompareTag("Destroyer"))
            StartCoroutine(GetHit());
    }

    IEnumerator GetHit()
    {
        isStunned = true;

        if (isBomb)
        {
            controller.ChangeScore(-5);
            bombModel.SetActive(false);
            GameObject explosion = Instantiate(explosionPrefab);
            Destroy(explosion, 1f);
        }
        else
        {
            controller.ChangeScore(1);
            moleHead.transform.localRotation = Quaternion.Euler(0f, 0f, -50f);
        }

        yield return new WaitForSeconds(1f);

        controller.PopMole(this, false);

        yield return new WaitForSeconds(1f);

        moleHead.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        isStunned = false;
    }
}