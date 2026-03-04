using UnityEngine;
using System.Collections;
using TMPro;

public class WhacAMoleController : MonoBehaviour
{
    public MoleController[] moles;
    public float yDif, startingY;
    public int score;
    public TextMeshPro scoreText;

    public GameObject flower, startText;
    AudioSource audioSource;
    BoxCollider boxCollider;
    public AudioClip scoreSound, explodeSound, music;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        boxCollider = GetComponent<BoxCollider>();
    }

    public void PopMole(MoleController mole, bool up)
    {
        float targetY = up ? startingY + yDif : startingY;
        StartCoroutine(mole.MoveMole(up, targetY));
    }

    IEnumerator MoleCoroutine()
    {
        score = 0;
        scoreText.text = "score: " + score.ToString();
        float endTime = Time.time + 65f;
        ToggleObjects(true);
        audioSource.PlayOneShot(music);

        while (Time.time < endTime)
        {
            foreach (var m in moles)
            {
                if (!m.isStunned)
                    PopMole(m, Random.value < 0.5f);
            }

            yield return new WaitForSeconds(Random.Range(.3f, 1.5f));
        }

        foreach (var m in moles)
        {
            PopMole(m, false);
        }

        ToggleObjects(false);
    }

    void ToggleObjects(bool gameStarting)
    {
        boxCollider.enabled = !gameStarting;
        flower.SetActive(!gameStarting);
        startText.SetActive(!gameStarting);
    }

    public void ChangeScore(int amount)
    {
        if (amount > 0)
        {
            audioSource.PlayOneShot(scoreSound);
        }
        else
        {
            audioSource.PlayOneShot(explodeSound);
        }
        score += amount;

        scoreText.text = "score: " + score.ToString();

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Destroyer"))
        {
            StartCoroutine(MoleCoroutine());
        }
    }
}