using UnityEngine;
using System.Collections.Generic;

public class BirdSpawner : MonoBehaviour
{
    public List<GameObject> birdPrefabs;
    public float spawnRadius = 30f;
    public float minHeight = 25f;
    public float maxHeight = 50f;
    public float speed = 10f;
    public float lifetime = 20f;
    public float spawnInterval = 5f;
    public float centerOffset = 10f;

    class BirdInstance
    {
        public Transform transform;
        public Vector3 direction;
        public float timeAlive;
    }

    List<BirdInstance> birds = new List<BirdInstance>();

    void Start()
    {
        InvokeRepeating(nameof(SpawnBirdGroup), 0f, spawnInterval);
    }

    void SpawnBirdGroup()
    {
        if (birdPrefabs == null || birdPrefabs.Count == 0) return;

        var prefab = birdPrefabs[Random.Range(0, birdPrefabs.Count)];

        Vector2 circle = Random.insideUnitCircle.normalized;
        Vector3 edge = new Vector3(circle.x, 0f, circle.y) * spawnRadius;

        float height = Random.Range(minHeight, maxHeight);
        Vector3 spawnPos = transform.position + edge + Vector3.up * height;

        var obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        Vector3 offset = new Vector3(
            Random.Range(-centerOffset, centerOffset),
            0f,
            Random.Range(-centerOffset, centerOffset)
        );

        Vector3 target = transform.position + offset;
        Vector3 dir = (target - spawnPos);
        dir.y = 0f;
        dir.Normalize();

        birds.Add(new BirdInstance
        {
            transform = obj.transform,
            direction = dir,
            timeAlive = 0f
        });
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = birds.Count - 1; i >= 0; i--)
        {
            var b = birds[i];

            if (b.transform == null)
            {
                birds.RemoveAt(i);
                continue;
            }

            b.transform.position += b.direction * speed * dt;
            b.transform.rotation = Quaternion.LookRotation(b.direction, Vector3.up);

            b.timeAlive += dt;
            if (b.timeAlive >= lifetime)
            {
                Destroy(b.transform.gameObject);
                birds.RemoveAt(i);
            }
        }
    }
}