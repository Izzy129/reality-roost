using UnityEngine;

public class FlowerLifetime : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;

    private void Start()


    {
        Destroy(gameObject, lifetime);
    }
}