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


    void Start()
    {
        StartCoroutine(MoleCoroutine());
    }

    public void PopMole(MoleController mole, bool up)
    {
        Vector3 newPos = mole.transform.position;
        newPos.y = up ? startingY + yDif : startingY;
        mole.transform.position = newPos;
    }

    IEnumerator MoleCoroutine()
    {
        while (true)
        {
            foreach (var m in moles) {
                if (!m.isStunned) PopMole(m, Random.value < 0.5f);

            }

            yield return new WaitForSeconds(Random.Range(.3f, 1.5f));
        }
    }

    public void IncreaseScore()
    {
        score++;
        if (scoreText != null)
            scoreText.text = "score: " + score.ToString();
    }

}
