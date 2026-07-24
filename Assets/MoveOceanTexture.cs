using UnityEngine;

public class MoveOceanTexture : MonoBehaviour
{
    // Adjust these in the Inspector to control how fast the water moves
    public float speedX = 0.05f;
    public float speedY = 0.05f;

    private Material oceanMaterial;
    private Vector2 currentOffset = Vector2.zero;

    void Start()
    {
        // Automatically grabs the material attached to this object's Renderer
        oceanMaterial = GetComponent<Renderer>().material;
    }

    void Update()
    {
        // 1. Calculate the new offset based on time and speed
        currentOffset.x += speedX * Time.deltaTime;
        currentOffset.y += speedY * Time.deltaTime;

        // 2. Apply the offset to the URP Base Map
        oceanMaterial.SetTextureOffset("_BaseMap", currentOffset);
    }
}