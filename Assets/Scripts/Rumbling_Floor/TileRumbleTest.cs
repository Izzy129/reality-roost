using UnityEngine;
using System.Collections;

public class TileRumbleTest : MonoBehaviour
{
    [Header("Rumble Settings")]
    [SerializeField][Range(0f, 1f)] private float intensity = 0.8f;
    [SerializeField] private float duration = 5f;

    [Header("Info")]
    [SerializeField] private bool isRumbling = false;

    [ContextMenu("Test Rumble (5 seconds)")]
    public void TestRumble()
    {
        if (isRumbling)
        {
            Debug.LogWarning("[TileRumbleTest] Already rumbling!");
            return;
        }

        StartCoroutine(RumbleCoroutine());
    }

    private IEnumerator RumbleCoroutine()
    {
        isRumbling = true;
        float elapsed = 0f;

        Debug.Log($"[TileRumbleTest] Starting rumble for {duration} seconds at intensity {intensity}");

        while (elapsed < duration)
        {
            // Call TileMapper to rumble at this GameObject's position
            TileMapper.Instance.Rumble(this.transform, intensity);

            elapsed += Time.deltaTime;
            yield return null; // Wait one frame
        }

        isRumbling = false;
        Debug.Log("[TileRumbleTest] Rumble complete!");
    }
}