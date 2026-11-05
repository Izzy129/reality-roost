using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealityRoost.WorldRail
{
    // Scene-scoped PC VR grading. Preserve headset AA and restore the previous experience's camera/pipeline settings.
    public sealed class WorldRailLook : MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        RenderPipelineAsset originalPipeline;
        bool applied;
        struct CameraState { public Camera camera; public UniversalAdditionalCameraData data; public bool hdr,post; public LayerMask layers; }
        readonly Dictionary<Camera,CameraState> cameras=new Dictionary<Camera,CameraState>();
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            applied=true;
            originalPipeline=QualitySettings.renderPipeline;
            if(pipeline)QualitySettings.renderPipeline=pipeline;
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))Configure(camera);
            RenderPipelineManager.beginCameraRendering+=BeginCamera;
        }
        void BeginCamera(ScriptableRenderContext context,Camera camera)=>Configure(camera);
        void Configure(Camera camera)
        {
            if(!camera||camera.cameraType!=CameraType.Game||camera.CompareTag("EditorOnly")||cameras.ContainsKey(camera))return;
            if(camera.targetTexture&&!camera.stereoEnabled)return;
            var data=camera.GetUniversalAdditionalCameraData();
            if(data.renderType==CameraRenderType.Overlay)return;
            cameras.Add(camera,new CameraState{camera=camera,data=data,hdr=camera.allowHDR,post=data.renderPostProcessing,layers=data.volumeLayerMask});
            camera.allowHDR=true;data.renderPostProcessing=true;data.volumeLayerMask|=1;
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=BeginCamera;
            foreach(var state in cameras.Values)if(state.camera&&state.data)
            {state.camera.allowHDR=state.hdr;state.data.renderPostProcessing=state.post;state.data.volumeLayerMask=state.layers;}
            cameras.Clear();
            if(applied&&QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=originalPipeline;
            applied=false;
        }
    }
}
