using System;
using System.IO;
using RealityRoost.Shared.Core;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace RealityRoost.Shared.SceneSwitching
{
    // Server-authoritative scene switcher for Reality Roost. Lives in Shared so the
    // Roost Rig prefab can reference it without the Host assembly's RR_HOST constraint.
    // LoadExperience() still hard-fails on anything that is not the NGO host.
    // Call LoadExperience(buildIndex) to switch scenes.
    // Netcode replicates the load to every connected client in sync.
    // Each client's RRSpatialCalibrator re-applies its own persisted calibration on load.
    //
    // Scenes come from the Build Settings list: index 0 is the RR_Boot scene, index 1 is the RR_Calb scene, 2..n are RR experience scenes
    public class RRSceneSwitcher : RRSubsystem
    {
        protected override string SubsystemName => "SceneSwitch";

        protected override bool LogLifecycle => false;

        // Fired when a scene load begins or when every client has finished loading. 
        // TODO: currently unused; future operator dashboard subscribes and forwards these events over WebSocket/HTTP
        public event Action<string> OnSceneLoadStarted;
        public event Action<string> OnSceneLoadComplete;

        private bool _subscribed;

        // Number of scenes in Build Settings
        public int SceneCount => SceneManager.sceneCountInBuildSettings;

        private static NetworkManager Nm => NetworkManager.Singleton;

        // Helper function to get Scene Name from Build Index
        public string GetSceneName(int buildIndex)
        {
            if (buildIndex < 0 || buildIndex >= SceneCount)
            {
                return string.Empty;
            }
            string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            return Path.GetFileNameWithoutExtension(path);
        }

        protected override void OnSubsystemStart()
        {
            if (Nm == null)
            {
                LogWarning("No NetworkManager in scene - scene switching is disabled until one exists.");
                return;
            }

            Nm.OnServerStarted += TrySubscribe;
            Nm.OnServerStopped += HandleStopped;
            // If already running when this component starts, subscribe now
            if (Nm.IsListening)
            {
                TrySubscribe();
            }
        }

        protected override void OnSubsystemStop()
        {
            if (Nm != null)
            {
                Nm.OnServerStarted -= TrySubscribe;
                Nm.OnServerStopped -= HandleStopped;
            }
            Unsubscribe();
        }

        // ---- Public API (operator UI hooks) ----

        // Load an experience by its Build Settings index (single mode replaces the current scene)
        public void LoadExperience(int buildIndex)
        {
            if (!Nm.IsListening)
            {
                LogError("LoadExperience called without NGO server - must be connected to NGO server");
                return;
            }
            if (!Nm.IsServer || !Nm.IsHost)
            {
                LogError("LoadExperience called without scene authority - must be NGO host and connected.");
                return;
            }
            if (buildIndex < 0 || buildIndex >= SceneCount)
            {
                LogError($"LoadExperience index {buildIndex} out of range (0..{SceneCount - 1}).");
                return;
            }

            string sceneName = GetSceneName(buildIndex);
            SceneEventProgressStatus status = Nm.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                LogError($"LoadScene('{sceneName}') failed to start, NGO error: {status}.");
                return;
            }
            LogInfo($"Loading experience [{buildIndex}] '{sceneName}' (single mode).");
        }

        // ---- Internals ----

        private void TrySubscribe()
        {
            if (_subscribed || Nm == null || Nm.SceneManager == null)
            {
                return;
            }
            Nm.SceneManager.OnSceneEvent += HandleSceneEvent;
            _subscribed = true;
        }

        private void HandleStopped(bool _) => Unsubscribe();

        private void Unsubscribe()
        {
            if (_subscribed && Nm != null && Nm.SceneManager != null)
            {
                Nm.SceneManager.OnSceneEvent -= HandleSceneEvent;
            }
            _subscribed = false;
        }

        private void HandleSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    OnSceneLoadStarted?.Invoke(sceneEvent.SceneName);
                    break;
                case SceneEventType.LoadEventCompleted:
                    LogInfo($"Scene '{sceneEvent.SceneName}' finished loading on all clients.");
                    OnSceneLoadComplete?.Invoke(sceneEvent.SceneName);
                    break;
            }
        }
    }
}
