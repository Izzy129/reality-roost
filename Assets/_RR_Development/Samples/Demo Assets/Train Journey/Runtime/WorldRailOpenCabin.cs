using UnityEngine;

namespace RealityRoost.WorldRail
{
    public sealed class WorldRailOpenCabin : MonoBehaviour
    {
        public WorldRailTunnelJourney journey;
        public Transform[] retractingParts;
        Vector3[] positions,scales;
        public float Openness { get; private set; }
        void Awake()
        {
            positions=new Vector3[retractingParts.Length];scales=new Vector3[retractingParts.Length];
            for(int i=0;i<positions.Length;i++){positions[i]=retractingParts[i].localPosition;scales[i]=retractingParts[i].localScale;}
        }
        void LateUpdate()
        {
            var s=journey.RenderedState;
            // Stay open through every world and tunnel, including the final tunnel exit.
            // Close only once the train has reached the station arrival phase.
            bool outdoor=(s.stage>0&&s.stage<journey.ReturnStage)||(s.stage==journey.ReturnStage&&s.phase==WorldRailTunnelJourney.Phase.ExitTunnel);
            Openness=Mathf.MoveTowards(Openness,outdoor?1:0,Time.unscaledDeltaTime/2);
            float t=Mathf.SmoothStep(0,1,Openness);
            for(int i=0;i<positions.Length;i++)
            {
                var part=retractingParts[i];part.gameObject.SetActive(t<.9999f);
                if(part.name.Contains("canopy")||positions[i].y>2.2f)
                {
                    // Retract overhead pieces toward the front above head height, never through the user.
                    part.localPosition=Vector3.Lerp(positions[i],new Vector3(positions[i].x,positions[i].y,1.42f),t);
                    part.localScale=new Vector3(scales[i].x,scales[i].y,scales[i].z*(1-t));
                }
                else
                {
                    part.localPosition=Vector3.Lerp(positions[i],new Vector3(positions[i].x,-.22f,positions[i].z),t);
                    part.localScale=new Vector3(scales[i].x,scales[i].y*(1-t),scales[i].z);
                }
            }
        }
    }
}
