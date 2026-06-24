using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public Transform cameraTransform;

    private Vector3 originalPos;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        originalPos = cameraTransform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }
}