using UnityEngine;

namespace RealityRoost.Shared.Core
{
    // Base class for Host/Client subsystems (haptic floor, fans, cameras, etc)
    // Subclass implement the OnSubsystem* hooks instead of Awake/OnEnable/OnDisable directly.
    public abstract class RRSubsystem : MonoBehaviour
    {
        protected abstract string SubsystemName { get; }

        private void Awake()
        {
            OnSubsystemAwake();
        }

        private void OnEnable()
        {
            OnSubsystemStart();
            LogInfo("Subsystem started.");
        }

        private void OnDisable()
        {
            OnSubsystemStop();
            LogInfo("Subsystem stopped.");
        }

        protected virtual void OnSubsystemAwake() { }
        protected virtual void OnSubsystemStart() { }
        protected virtual void OnSubsystemStop() { }

        protected void LogInfo(string message)
        {
            Debug.Log($"[RR][INFO] {SubsystemName}: {message}");
        }
        
        protected void LogDebug(string message)
        {
#if UNITY_EDITOR || RR_VERBOSE
            Debug.Log($"[RR][DEBUG] {SubsystemName}: {message}");
#endif
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[RR][WARN] {SubsystemName}: {message}");
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[RR][ERROR] {SubsystemName}: {message}");
        }
    }
}
