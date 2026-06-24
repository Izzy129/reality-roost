namespace RiceEXPLab.RealityRoost.Core
{
    public abstract class RRSubsystem : MonoBehaviour
    {
        // Is this subsystem active/enabled
        public bool IsEnabled { get; private set; }
        
        // Has it completed initialization
        public bool IsReady { get; protected set; }

        // Reference back to the root RR object
        protected RealityRoost RR { get; private set; }

        // Lifecycle management
		// This method is called by RealityRoost.cs in order of subsystem launch
        internal void Initialize(RealityRoost rr)
        {
            RR = rr;
            OnInitialize();
        }

        internal void Shutdown() => OnShutdown();

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (enabled) OnEnabled();
            else OnDisabled();
        }

        // Subsystems should override these, not the Unity messages directly
        protected virtual void OnInitialize() { }
        protected virtual void OnShutdown() { }
        protected virtual void OnEnabled() { }
        protected virtual void OnDisabled() { }
    }
}