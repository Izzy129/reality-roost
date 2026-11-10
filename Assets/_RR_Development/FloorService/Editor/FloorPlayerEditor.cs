using UnityEditor;
using UnityEngine;
using RealityRoost.Shared.HapticFloor;

namespace RealityRoost.Editor
{
    /// <summary>
    /// Draws the Floor Player Inspector where users choose a clip or preset and decide when it plays.
    /// </summary>
    [CustomEditor(typeof(RRFloorPlayer))]
    public sealed class RRFloorPlayerEditor : UnityEditor.Editor
    {
        private bool showNotifications;
        private bool showEventHelp;
        private bool showScriptHelp;
        private RRFloorService inspectedService;
        private double nextRepaint;

        private void OnEnable() { EditorApplication.update += Poll; }
        private void OnDisable()
        {
            EditorApplication.update -= Poll;
            ReleaseInspectedService();
        }
        private void ReleaseInspectedService()
        {
            if (!Application.isPlaying && inspectedService != null && !FloorEditorPreview.IsTracking(inspectedService))
                inspectedService.ReleaseConnection();
            inspectedService = null;
        }
        private void Poll()
        {
            if (target == null) return;
            var player = (RRFloorPlayer)target;
            var service = Application.isPlaying ? player.floorService : FloorEditorPreview.ServiceFor(player);
            if (inspectedService != service)
            {
                ReleaseInspectedService();
                inspectedService = service;
            }
            if (!Application.isPlaying && inspectedService != null) inspectedService.TickConnection();
            if (EditorApplication.timeSinceStartup >= nextRepaint)
            { nextRepaint = EditorApplication.timeSinceStartup + .1; Repaint(); }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var source = serializedObject.FindProperty("source");
            EditorGUILayout.PropertyField(source, new GUIContent("What To Play"));
            if (source.enumValueIndex == 1)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("preset"));
            }
            else
                foreach (var field in new[] { "audioClip", "tiles", "intensity", "loop" })
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            EditorGUILayout.Space();
            var playOnStart = serializedObject.FindProperty("playOnStart");
            playOnStart.boolValue = EditorGUILayout.Popup(
                new GUIContent("When To Play", "On Start plays once when ready. When Triggered waits for your experience to call BeginPlay()."),
                playOnStart.boolValue ? 1 : 0,
                new[] { "When Triggered", "On Start" }) == 1;
            if (!playOnStart.boolValue)
            {
                EditorGUILayout.LabelField("Start this effect from an event or a script.", EditorStyles.wordWrappedMiniLabel);
                showEventHelp = EditorGUILayout.Foldout(showEventHelp, "Play from an event", true);
                if (showEventHelp)
                {
                    EditorGUILayout.HelpBox(
                        "1. Find the event that should play your preset, such as a Timeline Signal Receiver reaction.\n\n" +
                        "2. Click + to add a listener.\n\n" +
                        "3. From the Hierarchy, drag the GameObject with your Floor Player into the listener’s object field.\n\n" +
                        "4. Choose RRFloorPlayer → BeginPlay ().", MessageType.None);
                }
                showScriptHelp = EditorGUILayout.Foldout(showScriptHelp, "Play from a script", true);
                if (showScriptHelp)
                {
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                        if (GUILayout.Button("Create Effect Script")) FloorEffectScriptCreator.Create();
                    EditorGUILayout.HelpBox(
                        "1. Click Create Effect Script and name your script.\n\n" +
                        "2. Add the new script to a GameObject in your scene.\n\n" +
                        "3. On the new component, drag the GameObject with your Floor Player into the Floor Player field.\n\n" +
                        "4. In the script, call BeginEffect() when your preset should start. Call EndEffect() to stop all tiles.", MessageType.None);
                }
            }
            EditorGUILayout.Space();
            showNotifications = EditorGUILayout.Foldout(showNotifications, "Playback Notifications (Optional)", true);
            if (showNotifications)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onPlay"), new GUIContent("After Play Request"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onStop"), new GUIContent("After Stop Request"));
            }
            serializedObject.ApplyModifiedProperties();
            var player = (RRFloorPlayer)target;
            if (player.IsWaitingToStart)
            {
                EditorGUILayout.HelpBox("Waiting for the host and floor audio. Check Floor Service's live status.", MessageType.Info);
                Repaint();
            }
            bool valid = true;
            try { player.ValidateEffect(); }
            catch (System.Exception e) { valid = false; EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
            bool editing = !Application.isPlaying;
            var service = editing ? FloorEditorPreview.ServiceFor(player) : player.floorService;
            if (service != null)
            {
                var connection = service.Connection;
                EditorGUILayout.LabelField("Floor", connection.IsFresh && connection.Status.audio.ready ? "Ready" : "Unavailable / waiting");
                EditorGUILayout.LabelField(connection.CommandResult, EditorStyles.wordWrappedLabel);
                if (!string.IsNullOrEmpty(connection.ConnectionError))
                    EditorGUILayout.LabelField(connection.ConnectionError, EditorStyles.wordWrappedLabel);
            }
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!valid || (service != null && !service.isActiveAndEnabled) || !player.isActiveAndEnabled ||
                (editing ? EditorApplication.isPlayingOrWillChangePlaymode : !player.CanPlay)))
            {
                if (GUILayout.Button(editing ? "Preview Effect" : "Begin Play"))
                {
                    if (editing) FloorEditorPreview.Play(player);
                    else player.BeginPlay();
                }
            }
            using (new EditorGUI.DisabledScope(service == null || !service.isActiveAndEnabled))
                if (GUILayout.Button("Stop All Tiles"))
                {
                    if (editing) FloorEditorPreview.Stop(service);
                    else player.EndPlay();
                }
            EditorGUILayout.EndHorizontal();
            if (editing)
                EditorGUILayout.LabelField("Preview plays on the physical floor. Use Stop All Tiles before entering Play mode.", EditorStyles.wordWrappedMiniLabel);
        }
    }

}
