public class RealityRoost : MonoBehaviour
{
    // Users access subsystems through here
    // e.g. RealityRoost.Instance.HapticFloor.TriggerTile(3, 0.8f)
    public HapticFloorManager HapticFloor { get; private set; }
    public FanManager Fan { get; private set; }
    public ArUcoManager ArUco { get; private set; }
    // ... 

    private void Awake()
    {
        // Initialize in dependency order
        ArUco.Initialize(this);
        HapticFloor.Initialize(this);
        Fan.Initialize(this);
        // SpatialCalibration after ArUco is ready, etc.
    }
}