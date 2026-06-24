using UnityEngine;

public class StegaWalkTemp : MonoBehaviour
{
    public float speed = 2f;

    private void Start()
    {
        GetComponent<Animator>().SetFloat("walkanimspeed", speed);
    }
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
