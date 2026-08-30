using UnityEngine;

public class RigWaypointFollower : MonoBehaviour
{
    public GameObject movingObj;
    [SerializeField] private GameObject[] _waypoints;
    public float speed = .2f;
    public int count = 1;
    private float interpolationRatio = 0f;
    public bool isPaused = false;

    [SerializeField] private GameObject lowerButton, returnButton;

    void Update()
    {
        if(!isPaused) // Move to next waypoint
        {
            if (count < _waypoints.Length - 1)
            {
                interpolationRatio += Time.deltaTime * speed;
                // Lerp position
                movingObj.transform.position = Vector3.Lerp(
                    _waypoints[count].transform.position,
                    _waypoints[count + 1].transform.position,
                    interpolationRatio
                );
                // Lerp rotation
                movingObj.transform.rotation = Quaternion.Lerp(
                    _waypoints[count].transform.rotation,
                    _waypoints[count+1].transform.rotation,
                    interpolationRatio
                );
                // Reset interpolationRatio once reached destination
                if (interpolationRatio >= 1f)
                {
                    count++;
                    interpolationRatio = 0f;

                    if(_waypoints[count].name.Contains("Pause")) // We are at a waypoint where user pauses. Used typically in cases where it waits for user interaction
                    { 
                        Debug.Log("Paused"); 
                        isPaused = true;

                        if(_waypoints[count].name.Equals("Waypoint (5) - Pause")) // Refactor
                        {
                            lowerButton.SetActive(true);
                        } 
                        else if(_waypoints[count].name.Equals("Waypoint (6) - Pause")) returnButton.SetActive(true);
                    }
                }
            }
        }
    }  

    public void SetIsPaused(bool value) 
    {
        isPaused = value;
    }
}
