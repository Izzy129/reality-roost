using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoRoomController : MonoBehaviour
{
    public Animator dinoAnimator, labAnimator;

    public float lowerDistance = 5f;
    public float lowerDuration = 2f;

    public List<Transform> shakeObjects;
    public float shakeDuration = 1f;
    public float shakeStrength = 0.2f;

    public Rigidbody wallsAndRoof;

    void Start()
    {
        StartCoroutine(DinoAttack());
    }
    /*
    IEnumerator LowerWallsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(LowerWalls());
    }

    IEnumerator LowerWalls()
    {
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> targetPositions = new List<Vector3>();

        foreach (Transform wall in walls)
        {
            startPositions.Add(wall.position);
            targetPositions.Add(wall.position + Vector3.down * lowerDistance);
        }

        float time = 0f;

        while (time < lowerDuration)
        {
            time += Time.deltaTime;
            float t = time / lowerDuration;

            for (int i = 0; i < walls.Count; i++)
            {
                walls[i].position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
            }

            yield return null;
        }
    }*/

    IEnumerator DinoAttack()
    {
        yield return new WaitForSeconds(3f);
        dinoAnimator.SetTrigger("tackle");
        labAnimator.SetTrigger("gone");
        //wallsAndRoof.SetActive(false);
        wallsAndRoof.AddForce(transform.up * 2000f + transform.right * 2000f );
    }


    public void ShakeRoom()
    {
        StartCoroutine(ShakeCoroutine());
    }

    IEnumerator ShakeCoroutine()
    {
        float elapsed = 0f;

        Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
        foreach (Transform obj in shakeObjects)
        {
            originalPositions[obj] = obj.localPosition;
        }

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            foreach (Transform obj in shakeObjects)
            {
                Vector3 randomOffset = Random.insideUnitSphere * shakeStrength;
                obj.localPosition = originalPositions[obj] + randomOffset;
            }

            yield return null;
        }

        foreach (Transform obj in shakeObjects)
        {
            obj.localPosition = originalPositions[obj];
        }
    }
}