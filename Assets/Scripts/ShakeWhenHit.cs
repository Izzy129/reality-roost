using UnityEngine;
using System.Collections;

public class ShakeWhenHit : MonoBehaviour
{
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 1f;
    public float frequency = 20f;

    private Quaternion originalRotation;
    private Coroutine shakeRoutine;

    void Start()
    {
        originalRotation = transform.localRotation;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Destroyer"))
        {
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);

            shakeRoutine = StartCoroutine(Shake());
        }
    }

    IEnumerator Shake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float damper = 1f - (elapsed / shakeDuration);
            float angle = Mathf.Sin(elapsed * frequency) * shakeMagnitude * damper;

            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = originalRotation;
    }
}