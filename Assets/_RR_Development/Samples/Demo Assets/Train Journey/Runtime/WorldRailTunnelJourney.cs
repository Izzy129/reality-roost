using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityRoost.WorldRail
{
    public sealed class WorldRailTunnelJourney : NetworkBehaviour
    {
        public enum Phase { Waiting, Travel, EnterTunnel, Loading, ExitTunnel, Arriving, Complete, Fault, Preparing }
        public struct JourneyState : INetworkSerializable, IEquatable<JourneyState>
        {
            public int stage;
            public Phase phase;
            public double since;
            public void NetworkSerialize<T>(BufferSerializer<T> s) where T:IReaderWriter
            {s.SerializeValue(ref stage);s.SerializeValue(ref phase);s.SerializeValue(ref since);}
            public bool Equals(JourneyState other)=>stage==other.stage&&phase==other.phase&&since==other.since;
        }
        [Serializable]
        public class Stop
        {
#if UNITY_EDITOR
            public UnityEditor.SceneAsset scene;
#endif
            [HideInInspector] public string scenePath;
            [Min(1)] public float travelSeconds = 24;
        }

        [Tooltip("Outdoor scenes in visit order. The train starts and ends at this station.")]
        public Stop[] destinations = Array.Empty<Stop>();
        [Min(1)] public float departureTravelSeconds = 14;
        public int ReturnStage => destinations.Length + 1;
        float TravelSeconds(int stage) => stage == 0 ? departureTravelSeconds : destinations[stage - 1].travelSeconds;

        string StopName(int stage)
        {
            if (stage == 0) return "GrandStation departure";
            if (stage == ReturnStage) return "GrandStation return";
            return Path.GetFileNameWithoutExtension(destinations[stage - 1].scenePath);
        }

        public Transform stationScenery;
        public Transform tunnel;
        public Transform previousTunnel;
        public Transform roostRails;
        public bool autoStart=true;
        [Min(0)] public float departureDelay=1;
        [Min(.1f)] public float speed=6;
        [Min(1)] public float tunnelLength=36;
        [Min(1)] public float tunnelEntrySeconds=6;
        [Min(1)] public float tunnelExitSeconds=6;
        [Min(1)] public float arrivalSeconds=4;
        public float ScheduledDuration => departureDelay + departureTravelSeconds + destinations.Sum(stop => stop.travelSeconds)
            + ReturnStage * (tunnelEntrySeconds + tunnelExitSeconds) + arrivalSeconds;
        public double TotalLoadingSeconds { get; private set; }
        public double RideStartedAt { get; private set; }
        public double RideCompletedAt { get; private set; }
        public JourneyState RenderedState { get; private set; }
        public double RenderedAt { get; private set; }
        [Min(10)] public float loadTimeoutSeconds=180;
        [Tooltip("GPU upload budget during preparation. Lower values trade loading speed for VR frame time.")]
        [Range(1,4)] public int asyncUploadMilliseconds=1;
        [Tooltip("Render several frames between asynchronous environment loads.")]
        [Range(1,10)] public int loadingBreatherFrames=3;
        [TextArea] public string status="Waiting to depart";
        readonly NetworkVariable<JourneyState> state=new NetworkVariable<JourneyState>();
        JourneyState local;
        Coroutine run;
        Vector3 stationStart;
        Color sky,equator,ground,fog;
        Material skybox;
        Light stationSun;
        UnityEngine.Rendering.AmbientMode ambientMode;
        float density;
        bool fogEnabled,networkSession;
        WorldRailEnvironment[] environments=Array.Empty<WorldRailEnvironment>();
        readonly HashSet<ulong> retiredPeers=new HashSet<ulong>(),preparedPeers=new HashSet<ulong>();
        readonly Dictionary<ulong,double> peerProgressAt=new Dictionary<ulong,double>();
        readonly Dictionary<ulong,int> peerProgressVersion=new Dictionary<ulong,int>();
        double nextProgressReport;
        int retiringStage=-1,reportedRetired=-1,reportedPrepared=-1;
        bool eventDone,eventFailed;
        string expectedScene;
        SceneEventType expectedEvent;
        NetworkSceneManager subscribed;
        bool loadingBudgetApplied;
        UnityEngine.ThreadPriority originalLoadingPriority;
        int originalUploadTimeSlice;
        double Now=>NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening?NetworkManager.Singleton.ServerTime.Time:Time.unscaledTimeAsDouble;
        public JourneyState Current=>IsSpawned?state.Value:local;
        bool Networked=>NetworkManager.Singleton!=null&&NetworkManager.Singleton.IsListening;
#if UNITY_EDITOR
        internal void ResolveEditorPaths()
        {
            foreach (var stop in destinations)
                if (stop != null) stop.scenePath = UnityEditor.AssetDatabase.GetAssetPath(stop.scene);
        }
#endif
        void Awake()
        {
#if UNITY_EDITOR
            ResolveEditorPaths();
#endif
            stationStart=stationScenery.localPosition;ambientMode=RenderSettings.ambientMode;
            sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;
            fog=RenderSettings.fogColor;density=RenderSettings.fogDensity;fogEnabled=RenderSettings.fog;skybox=RenderSettings.skybox;stationSun=RenderSettings.sun;
            SceneManager.sceneLoaded += Loaded;
            SceneManager.sceneUnloaded += Unloaded;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var loaded = SceneManager.GetSceneAt(i);
                if (loaded.isLoaded && loaded != gameObject.scene) Loaded(loaded, LoadSceneMode.Additive);
            }
            RefreshEnvironments();
            if(!GetComponent<WorldRailLoadingMetrics>())gameObject.AddComponent<WorldRailLoadingMetrics>();
        }
        IEnumerator Start()
        {
            // In normal Roost boot a NetworkManager already exists. Never run a local route while it connects.
            yield return null;
            if(NetworkManager.Singleton==null&&autoStart)Begin();
        }
        public override void OnNetworkSpawn()
        {
            networkSession=true;
            Subscribe();
            if(run!=null){StopCoroutine(run);run=null;}
            if(IsServer&&autoStart)Begin();
        }
        public override void OnNetworkDespawn(){if(run!=null)StopCoroutine(run);run=null;Unsubscribe();RestoreLoadingBudget();}
        void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive || !destinations.Any(stop => stop != null && stop.scenePath == scene.path)) return;
            if (!WorldRailEnvironment.ActiveInstances.Any(env => env && env.gameObject.scene == scene))
                WorldRailEnvironment.AttachToScene(scene);
            RefreshEnvironments();
        }
        void Unloaded(Scene s)=>RefreshEnvironments();
        void RefreshEnvironments()=>environments=WorldRailEnvironment.ActiveInstances.ToArray();
        public override void OnDestroy(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneUnloaded-=Unloaded;Unsubscribe();RestoreLoadingBudget();base.OnDestroy();}
        void Unsubscribe(){if(subscribed!=null)subscribed.OnSceneEvent-=SceneEvent;subscribed=null;}
        void Subscribe()
        {
            if(Networked&&subscribed==null){subscribed=NetworkManager.Singleton.SceneManager;subscribed.OnSceneEvent+=SceneEvent;}
        }
        void ApplyLoadingBudget()
        {
            if(loadingBudgetApplied)return;
            originalLoadingPriority=Application.backgroundLoadingPriority;
            originalUploadTimeSlice=QualitySettings.asyncUploadTimeSlice;
            Application.backgroundLoadingPriority=UnityEngine.ThreadPriority.Low;
            QualitySettings.asyncUploadTimeSlice=Mathf.Clamp(asyncUploadMilliseconds,1,4);
            loadingBudgetApplied=true;
        }
        void RestoreLoadingBudget()
        {
            if(!loadingBudgetApplied)return;
            Application.backgroundLoadingPriority=originalLoadingPriority;
            QualitySettings.asyncUploadTimeSlice=originalUploadTimeSlice;
            loadingBudgetApplied=false;
        }
        public bool ValidateRoute(out string reason)
        {
            if (destinations == null || destinations.Length == 0) { reason = "Add a destination in the station Inspector."; return false; }
            if (!stationScenery || !tunnel || !roostRails) { reason = "Train journey scene references are missing."; return false; }
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Networked)
            {
                for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
                {
                    string name = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(index));
                    if (!names.Add(name)) ambiguous.Add(name);
                }
            }
            for (int i = 0; i < destinations.Length; i++)
            {
                var stop = destinations[i];
                if (stop == null || string.IsNullOrEmpty(stop.scenePath)) { reason = $"Select a scene for destination {i + 1}."; return false; }
                if (ambiguous.Contains(Path.GetFileNameWithoutExtension(stop.scenePath))) { reason = "Give this scene a unique filename for network loading: " + stop.scenePath; return false; }
                if (stop.scenePath == gameObject.scene.path) { reason = "GrandStation is already the departure and return stop."; return false; }
                if (SceneUtility.GetBuildIndexByScenePath(stop.scenePath) < 0) { reason = "Enable this destination in the build scene list: " + stop.scenePath; return false; }
            }
            reason="";return true;
        }
        [ContextMenu("Start Journey")]
        public void Begin()
        {
            if(!Application.isPlaying||run!=null||(Networked&&(!IsSpawned||!IsServer)))return;
            if(!ValidateRoute(out string reason)){status=reason;Debug.LogError("[World Rail] "+reason,this);RestoreLoadingBudget();return;}
            run=StartCoroutine(Journey());
        }
        void Set(int stage,Phase phase,double timestamp=-1)
        {
            var next=new JourneyState{stage=stage,phase=phase,since=timestamp<0?Now:timestamp};
            if(IsSpawned&&IsServer)state.Value=next;else local=next;
        }
        IEnumerator Journey()
        {
            // Leave the initial NGO scene callback before beginning the ride; no environment preloads.
            yield return null;
            RideStartedAt=Now;double cursor=RideStartedAt;
            Set(0,Phase.Waiting,cursor);cursor+=departureDelay;while(Now<cursor)yield return null;
            for(int stage=0;stage<ReturnStage;stage++)
            {
                Set(stage,Phase.Travel,cursor);cursor+=TravelSeconds(stage);while(Now<cursor)yield return null;
                Set(stage,Phase.EnterTunnel,cursor);cursor+=tunnelEntrySeconds;while(Now<cursor)yield return null;
                retiredPeers.Clear();preparedPeers.Clear();peerProgressAt.Clear();peerProgressVersion.Clear();eventFailed=false;
                Set(stage,Phase.Loading,cursor);ApplyLoadingBudget();
                yield return WaitForPeers(retiredPeers,stage,false);
                if(!eventFailed&&stage>0)yield return UnloadEnvironment(stage);
                if(!eventFailed&&stage+1<ReturnStage)yield return LoadEnvironment(stage+1);
                if(!eventFailed)yield return WaitForPeers(preparedPeers,stage,true);
                if(eventFailed){Debug.LogError("[World Rail] "+status,this);Set(stage,Phase.Fault);RestoreLoadingBudget();run=null;yield break;}
                for(int frame=0;frame<loadingBreatherFrames;frame++)yield return null;
                TotalLoadingSeconds+=Now-cursor;
                RestoreLoadingBudget();cursor=Now;
                Set(stage+1,Phase.ExitTunnel,cursor);cursor+=tunnelExitSeconds;while(Now<cursor)yield return null;
            }
            Set(ReturnStage,Phase.Arriving,cursor);cursor+=arrivalSeconds;while(Now<cursor)yield return null;
            RideCompletedAt=Now;Set(ReturnStage,Phase.Complete,cursor);status="Returned to GrandStation";
            run=null;
        }
        [ServerRpc(RequireOwnership=false)]
        void ReportReadyServerRpc(int stage,bool prepared,ServerRpcParams rpc=default)
        {
            if(Current.phase!=Phase.Loading||Current.stage!=stage)return;
            (prepared?preparedPeers:retiredPeers).Add(rpc.Receive.SenderClientId);
        }
        bool LocalReady(int stage,bool prepared)
        {
            if(prepared&&stage+1==ReturnStage||!prepared&&stage==0)return true;
            var env = EnvironmentAt(prepared ? stage + 1 : stage);
            return env&&(prepared?env.Prepared:env.Retired);
        }
        [ServerRpc(RequireOwnership=false)]
        void ReportProgressServerRpc(int stage,int version,ServerRpcParams rpc=default)
        {
            if(Current.phase!=Phase.Loading||Current.stage!=stage)return;
            ulong id=rpc.Receive.SenderClientId;
            if(!peerProgressVersion.TryGetValue(id,out int previous)||previous!=version)
            {peerProgressVersion[id]=version;peerProgressAt[id]=Now;}
        }
        IEnumerator WaitForPeers(HashSet<ulong> ready,int stage,bool prepared)
        {
            double lastLocalProgress=Now;int localVersion=-1;
            while(Networked?!NetworkManager.Singleton.ConnectedClientsIds.All(id=>ready.Contains(id)):!LocalReady(stage,prepared))
            {
                bool stalled=false;
                if(Networked)
                {
                    foreach(ulong id in NetworkManager.Singleton.ConnectedClientsIds)
                    {
                        if(ready.Contains(id))continue;
                        if(!peerProgressAt.ContainsKey(id))peerProgressAt[id]=Now;
                        if(Now-peerProgressAt[id]>loadTimeoutSeconds)stalled=true;
                    }
                }
                else
                {
                    int version=StreamingProgress();if(version!=localVersion){localVersion=version;lastLocalProgress=Now;}
                    stalled=Now-lastLocalProgress>loadTimeoutSeconds;
                }
                if(stalled){eventFailed=true;status="Scenery loading stopped making progress. Train held inside tunnel.";yield break;}
                yield return null;
            }
        }
        WorldRailEnvironment EnvironmentAt(int stage)
        {
            if (stage <= 0 || stage >= ReturnStage) return null;
            string path = destinations[stage - 1].scenePath;
            return Array.Find(environments, env => env && env.gameObject.scene.path == path);
        }

        int StreamingProgress() => environments.Where(env => env).Sum(env => env.ProgressVersion);
        void UpdateLoading(int stage)
        {
            if(retiringStage!=stage)
            {
                retiringStage=stage;
                var old=EnvironmentAt(stage);
                if(old)StartCoroutine(old.Retire());
            }
            if(Networked&&IsSpawned&&Current.phase==Phase.Loading)
            {
                if(Now>=nextProgressReport){nextProgressReport=Now+1;ReportProgressServerRpc(stage,StreamingProgress());}
                if(reportedRetired!=stage&&LocalReady(stage,false)){reportedRetired=stage;ReportReadyServerRpc(stage,false);}
                if(reportedPrepared!=stage&&LocalReady(stage,true)){reportedPrepared=stage;ReportReadyServerRpc(stage,true);}
            }
        }
        void SceneEvent(SceneEvent e)
        {
            // NGO uses LoadSceneAsync internally on every peer; lower priority here also
            // covers headset clients, which do not execute the server's Journey coroutine.
            if(e.SceneEventType==SceneEventType.Load&&e.AsyncOperation!=null&&Array.Exists(destinations, stop => stop != null && stop.scenePath == e.ScenePath))
            {ApplyLoadingBudget();e.AsyncOperation.priority=-10;}
            if(e.SceneName!=expectedScene||e.SceneEventType!=expectedEvent)return;
            eventFailed=e.ClientsThatTimedOut!=null&&e.ClientsThatTimedOut.Count>0;eventDone=true;
            if(eventFailed)status="A client timed out loading the environment. Journey held in tunnel.";
        }
        void Watch(string scene,SceneEventType type)
        {
            eventDone=false;eventFailed=false;expectedScene=Path.GetFileNameWithoutExtension(scene);expectedEvent=type;
            Subscribe();
        }
        IEnumerator WaitForEvent()
        {
            double deadline=Now+loadTimeoutSeconds;
            while(!eventDone&&Now<deadline)yield return null;
            if(!eventDone){eventFailed=true;status="Environment load timed out. Journey held in tunnel.";}
            // Do not initiate another NGO scene event from within its completion callback.
            yield return null;
        }
        IEnumerator StartNetworkSceneOperation(Func<SceneEventProgressStatus> start,string description)
        {
            double deadline=Now+loadTimeoutSeconds;
            while(Now<deadline)
            {
                double requestedAt=Time.realtimeSinceStartupAsDouble;
                var result=start();
                WorldRailLoadingMetrics.RecordRequest("NGO "+description,requestedAt);
                if(result==SceneEventProgressStatus.Started)
                {Debug.Log("[World Rail] Started network "+description,this);yield break;}
                // GrandStation may still be completing its own load when our in-scene
                // NetworkObject spawns. Busy is transient, not a failed experience.
                if(result!=SceneEventProgressStatus.SceneEventInProgress)
                {eventFailed=true;status="Could not start "+description+": "+result;yield break;}
                yield return null;
            }
            eventFailed=true;status="Timed out waiting for Netcode to start "+description;
        }
        IEnumerator LoadEnvironment(int stage)
        {
            string path=destinations[stage-1].scenePath;Watch(path,SceneEventType.LoadEventCompleted);
            if(Networked)
            {
                yield return StartNetworkSceneOperation(()=>subscribed.LoadScene(path,LoadSceneMode.Additive),"load "+expectedScene);
                if(eventFailed)yield break;
                yield return WaitForEvent();
            }
            else
            {
                double requestedAt=Time.realtimeSinceStartupAsDouble;
                var op=SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);
                WorldRailLoadingMetrics.RecordRequest("Environment "+path,requestedAt);
                if(op==null){eventFailed=true;status="Could not load "+path;yield break;}
                op.priority=-10;
                while(!op.isDone)yield return null;
            }
            RefreshEnvironments();
            var env = EnvironmentAt(stage);
            if(!env){eventFailed=true;status="Environment content is missing for "+StopName(stage);}
        }
        IEnumerator UnloadEnvironment(int stage)
        {
            var old=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(destinations[stage-1].scenePath);
            if(!old.isLoaded)yield break;
            Watch(old.name,SceneEventType.UnloadEventCompleted);
            if(Networked)
            {
                yield return StartNetworkSceneOperation(()=>subscribed.UnloadScene(old),"unload "+old.name);
                if(eventFailed){Debug.LogError("[World Rail] "+status,this);yield break;}
                yield return WaitForEvent();
            }
            else{var op=UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(old);if(op!=null)while(!op.isDone)yield return null;}
        }
        public float TunnelSwitchZ=>-tunnelLength*.5f;
        public float TunnelEntryStart=>TunnelSwitchZ+EntryDistance(tunnelEntrySeconds);
        public float TunnelExitEnd=>TunnelSwitchZ-TravelDistance(tunnelExitSeconds,speed);
        public float EntryDistance(float seconds)
        {
            float cruise=Mathf.Max(0,tunnelEntrySeconds-4),t=Mathf.Clamp(seconds,0,tunnelEntrySeconds);
            return speed*Mathf.Min(t,cruise)+ArrivalDistance(Mathf.Max(0,t-cruise),4,speed);
        }
        // Integral of smoothstep velocity. Only departure accelerates; later worlds stay at cruise.
        public static float TravelDistance(float seconds,float metersPerSecond)
        {float t=Mathf.Max(0,seconds);return metersPerSecond*(t<4?t*t*t/16-t*t*t*t/128:t-2);}
        public static float ArrivalDistance(float seconds,float duration,float metersPerSecond)
        {float t=Mathf.Clamp(seconds,0,duration),u=t/duration;return metersPerSecond*(t-duration*(u*u*u-.5f*u*u*u*u));}
        public float CruiseDistance(int stage,float seconds)=>stage==0?TravelDistance(Mathf.Min(seconds,TravelSeconds(stage)),speed):speed*Mathf.Clamp(seconds,0,TravelSeconds(stage));
        JourneyState PresentationState(double now)
        {
            var s=Current;
            if(s.since<=0)return s;
            // Coroutines and network messages can deliver a phase after its deadline.
            // Render from that deadline immediately, avoiding an endpoint pause followed by a catch-up jump.
            for(int i=0;i<20;i++)
            {
                float duration;
                switch(s.phase)
                {
                    case Phase.Waiting:duration=departureDelay;break;
                    case Phase.Travel:duration=TravelSeconds(s.stage);break;
                    case Phase.EnterTunnel:duration=tunnelEntrySeconds;break;
                    case Phase.ExitTunnel:duration=tunnelExitSeconds;break;
                    case Phase.Arriving:duration=arrivalSeconds;break;
                    default:return s;
                }
                if(now<s.since+duration)return s;
                s.since+=duration;
                switch(s.phase)
                {
                    case Phase.Waiting:s.phase=Phase.Travel;break;
                    case Phase.Travel:s.phase=Phase.EnterTunnel;break;
                    case Phase.EnterTunnel:s.phase=Phase.Loading;break;
                    case Phase.ExitTunnel:s.phase=s.stage==ReturnStage?Phase.Arriving:Phase.Travel;break;
                    case Phase.Arriving:s.phase=Phase.Complete;break;
                }
            }
            return s;
        }
        void Update()
        {
            if ((networkSession && !IsSpawned) || destinations == null || destinations.Length == 0) return;
            double now=Now;var s=PresentationState(now);float elapsed=Mathf.Max(0,(float)(now-s.since));int stage=Mathf.Clamp(s.stage,0,ReturnStage);
            RenderedState=s;RenderedAt=now;
            if(s.phase==Phase.Loading){ApplyLoadingBudget();UpdateLoading(stage);}
            else if(s.phase!=Phase.Waiting)RestoreLoadingBudget();
            bool preparationFailed=s.phase==Phase.Fault&&s.stage<0;
            bool inTunnel=s.phase==Phase.Preparing||s.phase==Phase.Waiting||s.phase==Phase.Travel||s.phase==Phase.EnterTunnel||s.phase==Phase.Loading||s.phase==Phase.ExitTunnel||s.phase==Phase.Fault;
            tunnel.gameObject.SetActive(inTunnel&&!preparationFailed);
            float tunnelZ=TunnelEntryStart;
            if(s.phase==Phase.Preparing||s.phase==Phase.Waiting)tunnelZ=TunnelEntryStart+CruiseDistance(0,departureTravelSeconds);
            if(s.phase==Phase.Travel)tunnelZ=TunnelEntryStart+CruiseDistance(stage,TravelSeconds(stage))-CruiseDistance(stage,elapsed);
            if(s.phase==Phase.EnterTunnel)tunnelZ=TunnelEntryStart-EntryDistance(elapsed);
            if(s.phase==Phase.Loading||s.phase==Phase.Fault)tunnelZ=TunnelSwitchZ;
            if(s.phase==Phase.ExitTunnel)tunnelZ=TunnelSwitchZ-TravelDistance(Mathf.Min(elapsed,tunnelExitSeconds),speed);
            tunnel.localPosition=new Vector3(0,0,tunnelZ);
            if(previousTunnel)
            {
                bool behind=stage>0&&(s.phase==Phase.Travel||s.phase==Phase.EnterTunnel||s.phase==Phase.Arriving||s.phase==Phase.Complete);
                previousTunnel.gameObject.SetActive(behind);
                float behindDistance=s.phase==Phase.Travel?CruiseDistance(stage,elapsed):s.phase==Phase.EnterTunnel?CruiseDistance(stage,TravelSeconds(stage))+EntryDistance(elapsed):s.phase==Phase.Arriving?ArrivalDistance(elapsed,arrivalSeconds,speed):speed*arrivalSeconds*.5f;
                previousTunnel.localPosition=new Vector3(0,0,TunnelExitEnd-behindDistance);
            }
            float distance=0;
            if(s.phase==Phase.Travel)distance=(stage==0?0:TravelDistance(tunnelExitSeconds,speed))+CruiseDistance(stage,elapsed);
            else if(s.phase==Phase.EnterTunnel||s.phase==Phase.Loading||s.phase==Phase.Fault)
                distance=(stage==0?0:TravelDistance(tunnelExitSeconds,speed))+CruiseDistance(stage,TravelSeconds(stage))+EntryDistance(elapsed);
            else if(s.phase==Phase.ExitTunnel)distance=TravelDistance(Mathf.Min(elapsed,tunnelExitSeconds),speed);
            if(preparationFailed)distance=0;
            bool hidden=s.phase==Phase.Loading||s.phase==Phase.Fault;
            bool stationVisible=(stage==0||stage==ReturnStage)&&!hidden;
            stationScenery.gameObject.SetActive(stationVisible);
            if(stationVisible)
            {
                float offset=-distance;
                if(stage==ReturnStage)offset=s.phase==Phase.ExitTunnel?TravelDistance(tunnelExitSeconds,speed)+speed*arrivalSeconds*.5f-distance:s.phase==Phase.Arriving?speed*arrivalSeconds*.5f-ArrivalDistance(elapsed,arrivalSeconds,speed):0;
                stationScenery.localPosition=stationStart+Vector3.forward*offset;
                RenderSettings.ambientMode=ambientMode;RenderSettings.ambientSkyColor=sky;RenderSettings.ambientEquatorColor=equator;RenderSettings.ambientGroundColor=ground;
                RenderSettings.skybox=skybox;RenderSettings.sun=stationSun;RenderSettings.fog=fogEnabled;RenderSettings.fogColor=fog;RenderSettings.fogDensity=density;
            }
            var visibleEnvironment = hidden || stationVisible ? null : EnvironmentAt(stage);
            foreach (var env in environments)
                if (env) env.Show(env == visibleEnvironment, distance);
            if((s.phase==Phase.Loading||s.phase==Phase.Fault)&&!preparationFailed)
            {
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.025f,.025f,.03f);
                RenderSettings.skybox=null;RenderSettings.sun=null;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=Color.black;RenderSettings.fogDensity=.16f;
            }
            if(s.phase!=Phase.Fault&&s.phase!=Phase.Waiting&&s.phase!=Phase.Preparing)status=StopName(stage)+" / "+s.phase;
        }
    }
}

#if UNITY_EDITOR
namespace RealityRoost.WorldRail
{
    // Keep the route in the station scene. Nothing is generated for a destination.
    [UnityEditor.CustomEditor(typeof(WorldRailTunnelJourney))]
    internal sealed class WorldRailRouteInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            using (new UnityEditor.EditorGUI.DisabledScope(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode))
            {
                DrawDefaultInspector();
            }
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

            serializedObject.Update();
            var list = serializedObject.FindProperty("destinations");
            var paths = new HashSet<string>();
            for (int i = 0; i < list.arraySize; i++)
            {
                var stop = list.GetArrayElementAtIndex(i);
                var asset = stop.FindPropertyRelative("scene").objectReferenceValue;
                string path = asset ? UnityEditor.AssetDatabase.GetAssetPath(asset) : "";
                stop.FindPropertyRelative("scenePath").stringValue = path;
                if (path.Length > 0) paths.Add(path);
            }
            serializedObject.ApplyModifiedProperties();
            var global = IncludeDestinations(UnityEditor.EditorBuildSettings.globalScenes, paths);
            if (global != null) UnityEditor.EditorBuildSettings.globalScenes = global;
            var active = IncludeDestinations(UnityEditor.EditorBuildSettings.scenes, paths);
            if (active != null) UnityEditor.EditorBuildSettings.scenes = active;
        }

        static UnityEditor.EditorBuildSettingsScene[] IncludeDestinations(UnityEditor.EditorBuildSettingsScene[] scenes, HashSet<string> paths)
        {
            var missing = new HashSet<string>(paths);
            var result = scenes.ToList();
            bool changed = false;
            for (int i = 0; i < result.Count; i++)
            {
                if (!missing.Remove(result[i].path) || result[i].enabled) continue;
                result[i] = new UnityEditor.EditorBuildSettingsScene(result[i].path, true);
                changed = true;
            }
            foreach (string path in missing)
                result.Add(new UnityEditor.EditorBuildSettingsScene(path, true));
            return changed || missing.Count > 0 ? result.ToArray() : null;
        }
    }

    // Build copies must use the current scene path even after a destination asset is renamed.
    internal sealed class WorldRailRouteBuild : UnityEditor.Build.IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, UnityEditor.Build.Reporting.BuildReport report)
        {
            if (report == null) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var journey in root.GetComponentsInChildren<WorldRailTunnelJourney>(true))
                    journey.ResolveEditorPaths();
        }
    }
}
#endif
