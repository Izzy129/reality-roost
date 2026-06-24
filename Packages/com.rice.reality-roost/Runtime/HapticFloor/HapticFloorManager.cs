namespace RiceEXPLab.RealityRoost.HapticFloor
{
    public class HapticFloorManager : RRSubsystem
    {
        // Public API
        public void TriggerTileIndex(int tileIndex, float intensity) { }
        public void TriggerAtPosition(Vector3 worldPos, float intensity) { }
        public void StopAll() { }

        // Internal implementation
        protected override void OnInitialize()
        {
            // Connect to middleware, validate tile count, etc.
        }

        protected override void OnShutdown()
        {
            StopAll();
        }
    }
}