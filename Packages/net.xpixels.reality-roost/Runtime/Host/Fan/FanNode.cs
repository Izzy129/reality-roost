
namespace RealityRoost.Host.Fans
{
    public class FanNode
    {
        //IP Address of the fan node
        public readonly string NodeIp; 

        //Positional information of the fan node
        public float FanSpeed = 0;
        public float Pitch = 127; //angular position of servo 1
        public float Yaw = 127; //angular position of servo 2

        public FanNode(string nodeIp){
            NodeIp = nodeIp;
        }
    }
}
