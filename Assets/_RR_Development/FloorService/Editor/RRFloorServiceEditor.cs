using RealityRoost.Shared.HapticFloor;

using UnityEditor;

using UnityEngine;



namespace RealityRoost.Editor

{

    [CustomPropertyDrawer(typeof(FloorTiles))]

    public sealed class FloorTilesDrawer : PropertyDrawer

    {

        private static readonly string[] Labels = { "Tile 1", "Tile 2", "Tile 3", "Tile 4", "Tile 5", "Tile 6" };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)

        {

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();

            int mask = EditorGUI.MaskField(position, label, property.intValue, Labels);

            if (EditorGUI.EndChangeCheck()) property.intValue = mask & (int)FloorTiles.All;

            EditorGUI.EndProperty();

        }

    }



    [CustomEditor(typeof(RRFloorService))]

    /// <summary>
    /// Draws the Floor Service Inspector, including live tile status, diagnostics, and the Stop All control.
    /// </summary>
    public sealed class RRFloorServiceEditor : UnityEditor.Editor

    {

        private double nextRepaint;
        private bool showDiagnostics;

        private void OnEnable() { EditorApplication.update += Poll; }

        private void OnDisable()

        {

            EditorApplication.update -= Poll;

            if (!Application.isPlaying && target != null && !FloorEditorPreview.IsTracking((RRFloorService)target)) ((RRFloorService)target).ReleaseConnection();

        }

        private void Poll()

        {

            if (target == null) return;

            if (!Application.isPlaying) ((RRFloorService)target).TickConnection();

            if (EditorApplication.timeSinceStartup >= nextRepaint)

            { nextRepaint = EditorApplication.timeSinceStartup + .1; Repaint(); }

        }

        public override void OnInspectorGUI()

        {

            serializedObject.Update();


            serializedObject.ApplyModifiedProperties();

            var service = (RRFloorService)target;

            var connection = service.Connection;

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("FLOOR - LIVE SERVICE STATUS", EditorStyles.boldLabel);

            if (!service.CanAccessService)

                EditorGUILayout.HelpBox("This client sends commands through the network host. Inspect the host's Floor Service for live status.", MessageType.Info);

            bool fresh = connection.IsFresh && service.CanAccessService && service.isActiveAndEnabled;

            string state = !fresh ? "UNKNOWN / DISCONNECTED" : connection.Status.audio.ready ? "AUDIO READY" : "SERVICE ONLINE - AUDIO UNAVAILABLE";

            EditorGUILayout.HelpBox(state, fresh && connection.Status.audio.ready ? MessageType.Info : MessageType.Warning);

            if (!string.IsNullOrEmpty(connection.ConnectionError)) EditorGUILayout.LabelField(connection.ConnectionError, EditorStyles.wordWrappedLabel);

            if (fresh && !string.IsNullOrEmpty(connection.Status.audio.error)) EditorGUILayout.HelpBox(connection.Status.audio.error, MessageType.Warning);

            for (int row = 0; row < 3; row++)

            {

                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < 2; col++)

                {

                    int index = row * 2 + col;

                    FloorTileStatus status = null;

                    if (fresh) foreach (var item in connection.Status.tiles) if (item.tile_index == index) status = item;

                    bool playing = status != null && status.playing;

                    Rect box = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true));

                    EditorGUI.DrawRect(box, !fresh ? new Color(.22f, .24f, .27f) : playing ? new Color(.08f, .36f, .31f) : new Color(.16f, .20f, .24f));

                    var label = new GUIStyle(EditorStyles.whiteLabel) { wordWrap = true, padding = new RectOffset(8, 8, 6, 4) };

                    GUI.Label(box, $"TILE {index + 1}\n" + (!fresh ? "Unknown" : playing ? (status.looping ? "LOOPING" : "PLAYING") + "\n" + status.sound_id : "Idle"), label);

                    if (playing) EditorGUI.ProgressBar(new Rect(box.x + 8, box.yMax - 23, box.width - 16, 16), status.intensity, $"Gain {status.intensity:P0}");

                }

                EditorGUILayout.EndHorizontal();

            }

            EditorGUILayout.LabelField("Service playback state and gain; not a physical sensor or audio meter.", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!service.isActiveAndEnabled))
                if (GUILayout.Button("STOP ALL TILES"))
                {
                    if (Application.isPlaying) service.StopAll();
                    else FloorEditorPreview.Stop(service);
                }
            showDiagnostics = EditorGUILayout.Foldout(showDiagnostics, "Diagnostics", true);
            if (!showDiagnostics)
            {
                EditorGUILayout.LabelField(connection.CommandResult, EditorStyles.wordWrappedLabel);
                return;
            }
            serializedObject.Update();
            foreach (var field in new[] { "audioClip", "soundId", "intensity", "loop", "sequence" })
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field));
            serializedObject.ApplyModifiedProperties();

            using (new EditorGUI.DisabledScope(!service.isActiveAndEnabled || !service.CanAccessService))

            {

                EditorGUILayout.LabelField("PRESET TEST", EditorStyles.boldLabel);

                EditorGUILayout.LabelField(service.SequenceStatus, EditorStyles.wordWrappedLabel);

                if (service.Sequence != null)

                {

                    if (GUILayout.Button("Select sequence preset")) Selection.activeObject = service.Sequence;

                    using (new EditorGUI.DisabledScope(!fresh || !connection.Status.audio.ready))

                        if (GUILayout.Button("Play Sequence")) service.PlaySequence();

                    if (GUILayout.Button("Stop Sequence")) service.StopSequence();

                }

                else EditorGUILayout.HelpBox("Assign the Train Rolling preset to Sequence above.", MessageType.Info);

                EditorGUILayout.Space();

                if (service.Clip == null)

                {

                    if (GUILayout.Button("Refresh sound catalog")) { service.TickConnection(); connection.RefreshCatalog(); }

                    if (connection.Sounds.Length > 0)

                    {

                        var labels = System.Array.ConvertAll(connection.Sounds, s => s.sound_id);

                        serializedObject.Update();

                        var soundProperty = serializedObject.FindProperty("soundId");

                        int current = System.Array.IndexOf(labels, soundProperty.stringValue);

                        int selected = EditorGUILayout.Popup("Service sounds", current, labels);

                        if (selected >= 0 && selected != current) soundProperty.stringValue = labels[selected];

                        serializedObject.ApplyModifiedProperties();

                    }

                }

                else EditorGUILayout.HelpBox("The assigned AudioClip is uploaded automatically on first play. Use Decompress On Load and Preload Audio Data in its import settings.", MessageType.Info);

                serializedObject.Update();

                var tilesProperty = serializedObject.FindProperty("selectedTiles");

                var selectedTiles = (FloorTiles)tilesProperty.intValue;

                EditorGUILayout.LabelField("Select tiles to play together", EditorStyles.boldLabel);

                for (int row = 0; row < 3; row++)

                {

                    EditorGUILayout.BeginHorizontal();

                    for (int col = 0; col < 2; col++)

                    {

                        int index = row * 2 + col;

                        var flag = (FloorTiles)(1 << index);

                        bool selected = GUILayout.Toggle((selectedTiles & flag) != 0, $"Tile {index + 1}", "Button");

                        if (selected) selectedTiles |= flag;

                        else selectedTiles &= ~flag;

                    }

                    EditorGUILayout.EndHorizontal();

                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Select all")) selectedTiles = FloorTiles.All;

                if (GUILayout.Button("Clear selection")) selectedTiles = FloorTiles.None;

                EditorGUILayout.EndHorizontal();

                tilesProperty.intValue = (int)selectedTiles;

                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.HelpBox("These controls operate the real floor, including outside Play mode. Loops continue until stopped.", MessageType.Info);

                using (new EditorGUI.DisabledScope(!fresh || !connection.Status.audio.ready))

                {

                    using (new EditorGUI.DisabledScope(selectedTiles == FloorTiles.None))

                        if (GUILayout.Button("Play selected tiles")) service.Play();

                    if (GUILayout.Button("Play all tiles")) service.PlayAll();

                }

                using (new EditorGUI.DisabledScope(selectedTiles == FloorTiles.None))

                    if (GUILayout.Button("Stop selected tiles")) service.Stop();



            }

            EditorGUILayout.LabelField(connection.CommandResult, EditorStyles.wordWrappedLabel);

        }

    }

}
