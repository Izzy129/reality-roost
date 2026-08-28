using RealityRoost.Shared.HapticFloor;
using UnityEngine;
using Newtonsoft.Json.Bson;


#if UNITY_EDITOR
using System;
using UnityEditor;
#endif

namespace RealityRoost.Client.HapticFloor
{
    // Drop this on any GameObject that should be able to rumble the tile underneath it.
    // Drag an AudioClip from a Resources/ folder into audioClip and call Play()/Stop().
    public class RRHapticEmitter : MonoBehaviour
    {
        // AudioClip to play to the tile
        [Tooltip("Drag the AudioClip you want to play to the tile underneath this GameObject.")]
        [SerializeField] private AudioClip audioClip;
        // AudioClip resource path used for syncing audio between client and host
        [SerializeField, HideInInspector] private string audioClipResourcePath;

        [SerializeField, Range(0f, 1f)] private float intensity = 1f;
        [Tooltip("If on, the clip repeats until Stop() is called. If off, it plays once and stops itself.")]
        [SerializeField] private bool loop = false;

        private int _activeTileIndex = -1;

        [ContextMenu("Test Play")]
        public void Play()
        {
            Play(intensity);
        }

        public void Play(float playIntensity)
        { 
            _activeTileIndex = HapticFloorUtils.PositionToTileIndex(transform.position);
            Play(playIntensity, _activeTileIndex);
        }
        public void Play(float playIntensity, int tileIndex)
        {
            if (HapticFloorClient.Instance == null)
            {
                Debug.LogError("[RR][ERROR] RRHapticEmitter: No HapticFloorClient.Instance in the scene.");
                return;
            }

            if (string.IsNullOrEmpty(audioClipResourcePath))
            {
                Debug.LogError("[RR][ERROR] RRHapticEmitter: audioClip is not set or is not inside a Resources folder.");
                return;
            }

            HapticFloorClient.Instance.PlayClip(tileIndex, audioClipResourcePath, playIntensity, loop);
        }
        

        [ContextMenu("Test Stop")]
        public void Stop()
        {
            if (HapticFloorClient.Instance == null || _activeTileIndex < 0)
            {
                return;
            }

            HapticFloorClient.Instance.StopRumble(_activeTileIndex);
            _activeTileIndex = -1;
        }
        public void Stop(int tileIndex)
        {
            if (HapticFloorClient.Instance == null || tileIndex < 0 || tileIndex > 5)
            {
                return;
            }
            HapticFloorClient.Instance.StopRumble(tileIndex);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (audioClip == null)
            {
                audioClipResourcePath = null;
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(audioClip);
            const string resourcesFolder = "/Resources/";
            int resourcesIndex = assetPath.IndexOf(resourcesFolder, StringComparison.Ordinal);

            if (resourcesIndex < 0)
            {
                Debug.LogError($"[RR][ERROR] RRHapticEmitter: '{audioClip.name}' must be inside a Resources folder to be used as an audio clip.");
                audioClipResourcePath = null;
                return;
            }

            string relativePath = assetPath.Substring(resourcesIndex + resourcesFolder.Length);
            audioClipResourcePath = System.IO.Path.ChangeExtension(relativePath, null);
        }
#endif
    }
}
