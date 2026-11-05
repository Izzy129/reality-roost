using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace RealityRoost.WorldRail
{
    // Runs on each client. Measures application frame gaps, not compositor motion-to-photon latency.
    [DisallowMultipleComponent, DefaultExecutionOrder(10000)]
    public sealed class WorldRailLoadingMetrics : MonoBehaviour
    {
        [Serializable] public sealed class LoadingWindow
        {
            public int stage, samples, framesOverBudget;
            public bool refreshRateReported, xrRunning, gpuTimingReported;
            public float refreshRateHz;
            public double frameBudgetMs, durationSeconds, maxFrameGapMs, maxAppGpuMs, maxSynchronousRequestMs;
            public string worstOperation, slowestRequest;
        }
        [Serializable] public sealed class Report
        {
            public string utc, platform, graphicsDevice, unityVersion, outcome;
            public bool editor;
            public int uploadBufferMB;
            public List<LoadingWindow> windows = new List<LoadingWindow>(4);
        }
        readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>(2);
        WorldRailTunnelJourney journey;
        XRDisplaySubsystem display;
        Report report;
        LoadingWindow active;
        double lastFrame, started;
        string previousOperation, outputPath;
        bool saved;
        static WorldRailLoadingMetrics instance;

        public static void RecordRequest(string operation, double began)
        {
            if (!instance || instance.active == null) return;
            double milliseconds = (Time.realtimeSinceStartupAsDouble - began) * 1000;
            if (milliseconds > instance.active.maxSynchronousRequestMs)
            { instance.active.maxSynchronousRequestMs = milliseconds; instance.active.slowestRequest = operation; }
        }

        void Awake()
        {
            journey = GetComponent<WorldRailTunnelJourney>();
            instance = this;
            report = new Report { utc = DateTime.UtcNow.ToString("O"), platform = Application.platform.ToString(),
                graphicsDevice = SystemInfo.graphicsDeviceName, unityVersion = Application.unityVersion,
                editor = Application.isEditor, uploadBufferMB = QualitySettings.asyncUploadBufferSize };
#if UNITY_EDITOR
            outputPath = Path.GetFullPath("Library/WorldRailLoading-client.json");
#else
            outputPath = Path.Combine(Application.persistentDataPath, "WorldRailLoading", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".json");
#endif
        }
        void LateUpdate()
        {
            if (!journey || saved) return;
            double now = Time.realtimeSinceStartupAsDouble;
            var state = journey.RenderedState;
            if (active != null)
            {
                double gap = (now - lastFrame) * 1000;
                active.samples++;
                if (gap > active.frameBudgetMs) active.framesOverBudget++;
                if (gap > active.maxFrameGapMs) { active.maxFrameGapMs = gap; active.worstOperation = "Before: " + previousOperation + "; after: " + WorldRailEnvironment.LastStreamingOperation; }
                if (display != null && display.running && display.TryGetAppGPUTimeLastFrame(out float gpuSeconds))
                { active.gpuTimingReported = true; active.maxAppGpuMs = Math.Max(active.maxAppGpuMs, gpuSeconds * 1000); }
                active.durationSeconds = now - started;
                if (state.phase != WorldRailTunnelJourney.Phase.Loading || state.stage != active.stage) active = null;
            }
            if (state.phase == WorldRailTunnelJourney.Phase.Loading && active == null)
            {
                SubsystemManager.GetSubsystems(displays);
                display = displays.Find(d => d.running);
                float hz = 0;
                bool reported = display != null && display.TryGetDisplayRefreshRate(out hz) && hz > 0;
                active = new LoadingWindow { stage = state.stage, xrRunning = display != null,
                    refreshRateReported = reported, refreshRateHz = reported ? hz : 0,
                    frameBudgetMs = reported ? 1000.0 / hz : 1000.0 / 90 };
                report.windows.Add(active);
                started = now;
            }
            lastFrame = now;
            previousOperation = WorldRailEnvironment.LastStreamingOperation;
            if (state.phase == WorldRailTunnelJourney.Phase.Complete || state.phase == WorldRailTunnelJourney.Phase.Fault)
                Save(state.phase.ToString());
        }
        void OnDestroy() { if (report != null) Save("Interrupted"); if(instance == this) instance = null; }
        async void Save(string outcome)
        {
            if (saved || report.windows.Count == 0) return;
            saved = true;
            report.outcome = outcome;
            string json = JsonUtility.ToJson(report, true), path = outputPath;
            // No per-frame logging or disk writes during loading. The file write runs off the Unity thread.
            try { await Task.Run(() => { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, json); }); }
            catch (Exception e) { Debug.LogWarning("World Rail loading report could not be saved: " + e.Message); }
        }
    }
}
