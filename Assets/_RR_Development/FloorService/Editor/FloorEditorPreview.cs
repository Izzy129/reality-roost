using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RealityRoost.Shared.HapticFloor;

namespace RealityRoost.Editor
{
    /// <summary>
    /// Runs floor playback previews from the Inspector so users can test physical tiles outside Play Mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class FloorEditorPreview
    {
        private sealed class Preview
        {
            public RRFloorPlayer player;
            public bool stopping;
            public double stopAt;
        }
        private static readonly Dictionary<RRFloorService, Preview> previews = new Dictionary<RRFloorService, Preview>();
        private static Scene calibrationPreview;
        private static RRFloorService calibrationService;
        private const string CalibrationGuid = "a8830dedd9d12e74eaa329c5c4a1d7c7";

        public static RRFloorService ServiceFor(RRFloorPlayer player) =>
            player.floorService != null ? player.floorService : calibrationService;

        private static RRFloorService OpenCalibrationService()
        {
            var path = AssetDatabase.GUIDToAssetPath(CalibrationGuid);
            if (string.IsNullOrEmpty(path)) return null;
            var scene = SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded)
            {
                if (!calibrationPreview.IsValid()) calibrationPreview = EditorSceneManager.OpenPreviewScene(path);
                scene = calibrationPreview;
            }
            foreach (var root in scene.GetRootGameObjects())
            {
                var service = root.GetComponentInChildren<RRFloorService>();
                if (service != null && service.isActiveAndEnabled) return calibrationService = service;
            }
            return null;
        }

        static FloorEditorPreview()
        {
            EditorApplication.update += Tick;
            AssemblyReloadEvents.beforeAssemblyReload += Release;
            EditorApplication.quitting += Release;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingEditMode) Release();
            };
        }

        public static bool IsTracking(RRFloorService service) => service != null && previews.ContainsKey(service);

        public static void Play(RRFloorPlayer player)
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { player.ValidateEffect(); }
            catch (System.Exception e) { Debug.LogWarning("[RR] Floor preview: " + e.Message, player); return; }
            var service = ServiceFor(player);
            if (service == null) service = OpenCalibrationService();
            if (service == null || !service.isActiveAndEnabled)
            {
                Debug.LogWarning("[RR] Floor preview: No enabled Floor Service found in RR_Calib.", player);
                return;
            }
            previews[service] = new Preview { player = player };
            if (player.source == RRFloorPlayer.Source.Preset) service.PlayPreset(player.preset);
            else service.PlayClip(player.audioClip, "", player.tiles, player.intensity, player.loop);
        }

        public static void Stop(RRFloorService service)
        {
            if (service == null || Application.isPlaying) return;
            if (!previews.TryGetValue(service, out var preview)) previews[service] = preview = new Preview();
            preview.stopping = true;
            preview.stopAt = EditorApplication.timeSinceStartup;
            service.StopAll();
        }

        private static void Tick()
        {
            if (Application.isPlaying) return;
            foreach (var pair in new List<KeyValuePair<RRFloorService, Preview>>(previews))
            {
                var service = pair.Key;
                var preview = pair.Value;
                if (service == null) { previews.Remove(service); continue; }
                if (!preview.stopping && (preview.player == null || !preview.player.isActiveAndEnabled || ServiceFor(preview.player) != service)) Stop(service);
                service.TickConnection();
                if (preview.stopping && (!service.Connection.IsBusy || EditorApplication.timeSinceStartup - preview.stopAt > 20))
                {
                    service.ReleaseConnection();
                    previews.Remove(service);
                }
            }
        }

        private static void Release()
        {
            foreach (var service in new List<RRFloorService>(previews.Keys))
            {
                if (service == null) continue;
                service.StopAll();
                service.TickConnection();
                service.ReleaseConnection();
            }
            previews.Clear();
            if (calibrationService != null) calibrationService.ReleaseConnection();
            calibrationService = null;
            if (calibrationPreview.IsValid()) EditorSceneManager.ClosePreviewScene(calibrationPreview);
            calibrationPreview = default;
        }
    }
}
