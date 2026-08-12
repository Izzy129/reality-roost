using UnityEngine;

namespace RealityRoost.Shared.Core
{
    // Base class for Host/Client subsystems (haptic floor, fans, cameras, etc)
    // Subclass implement the OnSubsystem* hooks instead of Awake/OnEnable/OnDisable directly.
    public abstract class RRSubsystem : MonoBehaviour
    {
        protected abstract string SubsystemName { get; }
        protected virtual bool LogLifecycle => true;

        private void Awake()
        {
            OnSubsystemAwake();
        }

        private void OnEnable()
        {
            OnSubsystemStart();
            if (LogLifecycle) LogInfo("Subsystem started.");
        }

        private void OnDisable()
        {
            OnSubsystemStop();
            if (LogLifecycle) LogInfo("Subsystem stopped.");
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
            Debug.Log($"[RR][DEBUG] {SubsystemName}: {message}");
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