using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityRoost.Shared.Core
{
    // RR_Boot (build index 0) holds persistent networking managers
    // On Start, it loads the calibration scene on top so the player has a rig + connection UI.
    
    // The load is a plain Unity single-mode load (not NGO)
    // At boot, we aren't connected yet; each client just needs its local calibration scene loaded. 
    // NGO takes over scene management once connected


    // TODO: add network configuration and automatic netcode connection here
    public class RRBootstrap : MonoBehaviour
    {
        [Tooltip("Build Settings index of the calibration scene to load on start (RR_Boot is 0).")]
        [SerializeField] private int calibrationSceneBuildIndex = 1;

        private void Start()
        {
            int sceneCount = SceneManager.sceneCountInBuildSettings;
            if (calibrationSceneBuildIndex <= 0 || calibrationSceneBuildIndex >= sceneCount)
            {
                Debug.LogError($"[RR][ERROR] Bootstrap: calibrationSceneBuildIndex {calibrationSceneBuildIndex} " +
                               $"is invalid (build has {sceneCount} scenes, RR_Boot must be 0). Check Build Settings.");
                return;
            }
            Debug.Log($"[RR][INFO] Bootstrap: loading calibration scene (build index {calibrationSceneBuildIndex}).");
            SceneManager.LoadScene(calibrationSceneBuildIndex, LoadSceneMode.Single);
        }
    }
}
