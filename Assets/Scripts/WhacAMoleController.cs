using UnityEngine;
using System.Collections;
using TMPro;

public class WhacAMoleController : MonoBehaviour
{
    public MoleController[] moles;
    public float yDif;
    public float startingY;
    public int score;
    public TextMeshPro scoreText;

    public GameObject flower, startText;
    AudioSource audioSource;
    BoxCollider boxCollider;
    public AudioClip scoreSound, music;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        boxCollider = GetComponent<BoxCollider>();
    }

    public void PopMole(MoleController mole, bool up)
    {
        Vector3 newPos = mole.transform.position;
        newPos.y = up ? startingY + yDif : startingY;
        mole.transform.position = newPos;
    }

    IEnumerator MoleCoroutine()
    {
        float endTime = Time.time + 60f;
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

    public void IncreaseScore()
    {
        audioSource.PlayOneShot(scoreSound);
        score++;
        if (scoreText != null)
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