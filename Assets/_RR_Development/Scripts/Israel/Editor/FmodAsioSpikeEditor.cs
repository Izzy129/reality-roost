using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FmodAsioSpike))]
public class FmodAsioSpikeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spike = (FmodAsioSpike)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Manual Tile Control", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play mode to vibrate tiles manually.", MessageType.Info);
            return;
        }

        int tileCount = spike.NumChannels / 2;
        const int gridCols = 2; // matches dante_channel_monitor.py's 3x2 tile layout (0,1 / 2,3 / 4,5)
        int gridRows = Mathf.CeilToInt(tileCount / (float)gridCols);

        for (int row = 0; row < gridRows; row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < gridCols; col++)
            {
                int tileIndex = row * gridCols + col;
                if (tileIndex >= tileCount)
                {
                    break;
                }

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"Tile {tileIndex}", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Left"))
                {
                    spike.VibrateChannel(tileIndex * 2);
                }

                if (GUILayout.Button("Right"))
                {
                    spike.VibrateChannel(tileIndex * 2 + 1);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Stop All"))
        {
            spike.StopAll();
        }
    }
}
