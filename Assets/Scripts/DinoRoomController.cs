using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DinoRoomController : MonoBehaviour
{
    public Animator dinoAnimator, labAnimator;

    public List<Transform> shakeObjects;

    public float shakeDuration = 1f;
    public float maxShakeStrength = 0.6f;
    public float minShakeStrength = 0.05f;

    public float startX = 50f;
    public float endX = 15f;

    public GameObject raptor;
    public RaptorSoundEffects raptorSoundEffects;
    public AudioSource dinoAudioSource;
    public AudioClip dinoFootstepClip;

    bool inIntro = true;
    bool isDinoRunning = true;
    public float raptorSpeed = 2f;

    public GameObject alarm;

    float stepTimer = 0f;
    float nextStepTime;

    bool runStarted = false;

    void Start()
    {
        nextStepTime = Random.Range(.5f, .6f);
        StartCoroutine(DinoRun());
    }

    private void Update()
    {
        if (inIntro && raptor.transform.localPosition.x < endX)
        {
            isDinoRunning = false;
            inIntro = false;
        }

        if (isDinoRunning)
            raptor.transform.Translate(Vector3.forward * -raptorSpeed * Time.deltaTime);

        if (!runStarted || !isDinoRunning)
            return;

        stepTimer += Time.deltaTime;

        if (stepTimer >= nextStepTime)
        {
            stepTimer = 0f;
            nextStepTime = Random.Range(.5f, .6f);

            dinoAudioSource.PlayOneShot(dinoFootstepClip);

            float strength = GetShakeStrength();
            ShakeRoom(shakeDuration, strength);
        }
    }

    IEnumerator DinoRun()
    {
        yield return new WaitForSeconds(3f);
        alarm.SetActive(true);

        runStarted = true;

        while (isDinoRunning)
        {
            yield return null;
        }

        StartCoroutine(DinoAttack());
    }

    IEnumerator DinoAttack()
    {
        raptorSoundEffects.Growl();
        yield return new WaitForSeconds(Random.Range(2f, 4f));

        raptorSoundEffects.Roar();
        yield return new WaitForSeconds(Random.Range(2f, 4f));

        dinoAnimator.SetTrigger("tackle");
        yield return new WaitForSeconds(.2f);

        labAnimator.SetTrigger("gone");
        ShakeRoom(shakeDuration, GetShakeStrength());
        raptorSoundEffects.Call();

        while (true)
        { 

            yield return new WaitForSeconds(Random.Range(2f, 5f));

            dinoAnimator.SetTrigger("sniff");
            yield return new WaitForSeconds(.5f);
            raptorSoundEffects.Sniff();

            yield return new WaitForSeconds(Random.Range(2f, 5f));

            dinoAnimator.SetTrigger("bite");
            yield return new WaitForSeconds(.5f);
            raptorSoundEffects.Bark();


            //temp
            StartCoroutine(DinoHit());
            yield return new WaitForSeconds(1f);


        }
    }

    float GetShakeStrength()
    {
        float currentX = raptor.transform.localPosition.x;
        float t = Mathf.InverseLerp(startX, endX, currentX);
        return Mathf.Lerp(minShakeStrength, maxShakeStrength, t);
    }

    public void ShakeRoom(float duration, float strength)
    {
        StartCoroutine(ShakeCoroutine(duration, strength));
    }

    IEnumerator ShakeCoroutine(float duration, float strength)
    {
        float elapsed = 0f;

        Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
        foreach (Transform obj in shakeObjects)
        {
            originalPositions[obj] = obj.localPosition;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            foreach (Transform obj in shakeObjects)
            {
                Vector3 randomOffset = Random.insideUnitSphere * strength;
                obj.localPosition = originalPositions[obj] + randomOffset;
            }

            yield return null;
        }

        foreach (Transform obj in shakeObjects)
        {
            obj.localPosition = originalPositions[obj];
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("hurtDino"))
        {
            StartCoroutine(DinoHit());
        }
    }

    IEnumerator DinoHit()
    {
        dinoAnimator.SetTrigger("hit");
        yield return new WaitForSeconds(1f);
        raptor.transform.Rotate(0f, 180f, 0f);
        isDinoRunning = true;
    }
}