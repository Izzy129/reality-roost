using Unity.Netcode;
using UnityEngine;

public class SimplePlayerController : NetworkBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (IsOwner)
        {
            GetComponent<Renderer>().material.color = Color.green;
            Debug.Log("You are the green player");

        } else
        {
            GetComponent<Renderer>().material.color = Color.red;
            Debug.Log("You are the red player");
        }
    }

    // Update is called once per frame
    private void Update()
    {


        float speed = 20f;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(h, 0, v) * speed * Time.deltaTime);
        


    }
}
