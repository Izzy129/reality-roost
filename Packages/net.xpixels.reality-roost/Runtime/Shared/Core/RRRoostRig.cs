using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityRoost.Shared.Core
{
    // Handles Lifecycle for the Roost Rig prefab.
    //
    // One Roost Rig instance lives throughout the experience ("live rig"). 
    // 
    // Every scene in an experience contains a Roost Rig prefab copy ("scene rig"), so users control WHERE the play space sits in their scene/experience.
    // This includes parenting it under a moving object (e.g. a raft).
    // 
    // On scene load, the "live rig" "adopts" the "scene rig" parent and local pose.
        // "Adopt" -> moving any user-placed props from their "scene rig" on the "live rig"
        

    // Runs at very early execution order so a scene copy is deactivated before other scripts in the incoming scene can cache references to it.
    [DefaultExecutionOrder(-10000)]
    public class RRRoostRig : MonoBehaviour
    {
        static RRRoostRig s_Instance;

        // Which scene's rig placement is currently settled
        static bool s_HasPlacement;
        static int s_PlacementSceneHandle;

        bool m_NgoHooked;

        // Scene-authored objects from the current scene's rig copy. 
        readonly List<GameObject> m_AdoptedContent = new();

        void Awake()
        {
            if (s_Instance != null)
            {
                AdoptInto(s_Instance);
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            s_HasPlacement = true;
            s_PlacementSceneHandle = gameObject.scene.handle;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
                Debug.Log("[RR][INFO] RoostRig: live rig marked DontDestroyOnLoad.");
            }
            else
            {
                // First rig woke up parented: someone pressed Play directly in an experience scene.
                // No scene switching happens in that Editor flow, so persistence is not needed
                Debug.Log($"[RR][INFO] RoostRig: live rig starting parented under '{transform.parent.name}' (direct-Play flow, not persisted).");
            }

            SceneManager.sceneLoaded += OnUnitySceneLoaded;
        }

        void Start()
        {
            TryHookNgo();
            var nm = NetworkManager.Singleton;
            if (!m_NgoHooked && nm != null)
            {
                // NGO's SceneManager only exists once a session starts
                nm.OnServerStarted += TryHookNgo;
                nm.OnClientStarted += TryHookNgo;
            }
        }

        void OnDestroy()
        {
            if (s_Instance != this)
                return;
            s_Instance = null;

            SceneManager.sceneLoaded -= OnUnitySceneLoaded;
            var nm = NetworkManager.Singleton;
            if (nm != null)
            {
                nm.OnServerStarted -= TryHookNgo;
                nm.OnClientStarted -= TryHookNgo;
                if (m_NgoHooked && nm.SceneManager != null)
                    nm.SceneManager.OnSceneEvent -= OnNgoSceneEvent;
            } else
            {
                Debug.LogError("No network manager to read Ngo scene switch events from...");
            }
        }

        // ---- Adoption (runs on the scene copy, placing the live rig) ----

        void AdoptInto(RRRoostRig live)
        {
            if (s_HasPlacement && s_PlacementSceneHandle == gameObject.scene.handle)
            {
                Debug.LogError("[RR][ERROR] RoostRig: more than one RR Rig in this scene — keep exactly one RR Rig prefab per scene. Placement is ambiguous; resetting the rig to the world origin.");
                live.ResetToOrigin();
                return;
            }

            var t = live.transform;
            t.SetParent(transform.parent, false);
            t.localPosition = transform.localPosition;
            t.localRotation = transform.localRotation;
            t.localScale = Vector3.one;

            // Colocation requires 1:1 virtual:physical scale
            var lossy = t.lossyScale;
            if (!Mathf.Approximately(lossy.x, 1f) || !Mathf.Approximately(lossy.y, 1f) || !Mathf.Approximately(lossy.z, 1f))
            {
                Debug.LogWarning($"[RR][WARN] RoostRig: ancestor '{transform.parent.name}' has non-unit scale (lossy {lossy}). Forcing rig world scale back to 1, colocated play must stay 1:1 with the physical room. Remove the scaling to remove this error.");
                t.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
            }

            RehomeAdditions(transform, t, live.m_AdoptedContent);
            s_HasPlacement = true;
            s_PlacementSceneHandle = gameObject.scene.handle;
            Debug.Log($"[RR][INFO] RoostRig: adopted scene placement (parent '{(transform.parent != null ? transform.parent.name : "<scene root>")}', local position {transform.localPosition}).");
        }

        // Moves user-added objects from "scene rig" onto the "live rig."
        // Matches children by name: 
        //      same name = stock node, recurse
        //      no match = user-added content, move it over and track it in 'adopted' so it dies at the next scene switch.
        // Assumes stock rig names aren't changed!!
        static void RehomeAdditions(Transform copyNode, Transform liveNode, List<GameObject> adopted)
        {
            var claimed = new HashSet<Transform>();
            var copyChildren = new List<Transform>(copyNode.childCount);
            for (int i = 0; i < copyNode.childCount; i++)
                copyChildren.Add(copyNode.GetChild(i));

            foreach (var copyChild in copyChildren)
            {
                Transform match = null;
                for (int i = 0; i < liveNode.childCount; i++)
                {
                    var liveChild = liveNode.GetChild(i);
                    if (!claimed.Contains(liveChild) && liveChild.name == copyChild.name)
                    {
                        match = liveChild;
                        break;
                    }
                }

                if (match != null)
                {
                    claimed.Add(match);
                    RehomeAdditions(copyChild, match, adopted);
                }
                else
                {
                    ReleaseNgoParenting(copyChild);
                    copyChild.SetParent(liveNode, false);
                    adopted.Add(copyChild.gameObject);
                    Debug.Log($"[RR][INFO] RoostRig: re-homed scene object '{copyChild.name}' into the live rig under '{liveNode.name}'.");
                }
            }
        }
        static readonly List<NetworkObject> s_NetworkObjects = new();

        // Opts a subtree out of NGO's transform-parent syncing before we re-home it with our transform-parent syncing
        static void ReleaseNgoParenting(Transform root)
        {
            root.GetComponentsInChildren(true, s_NetworkObjects);

            int released = 0;
            foreach (var networkObject in s_NetworkObjects)
            {
                if (!networkObject.AutoObjectParentSync)
                    continue;
                networkObject.AutoObjectParentSync = false;
                released++;
            }

            if (released > 0)
                Debug.Log($"[RR][INFO] RoostRig: disabled AutoObjectParentSync on {released} NetworkObject(s) under '{root.name}' so it can be re-homed onto the live rig.");

            s_NetworkObjects.Clear();
        }

        void DestroyAdoptedContent()
        {
            foreach (var go in m_AdoptedContent)
            {
                if (go != null)
                    Debug.Log($"Destroying custom child \"{go.name}\" from Roost Rig");
                    Destroy(go);
            }
            m_AdoptedContent.Clear();
            Debug.Log("[RR][DEBUG] RoostRig: all custom children of Roost Rig destroyed, ready to mark DDOL!!!");
        }

        // ---- Detach / fallback (run on the live rig) ----

        void TryHookNgo()
        {
            if (m_NgoHooked)
                return;
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SceneManager == null)
                return;
            nm.SceneManager.OnSceneEvent += OnNgoSceneEvent;
            m_NgoHooked = true;
            Debug.Log("[RR][INFO] RoostRig: hooked to NGO scene events. we are now listening for scene switch events for reparenting and custom object adoption");
        }

        void OnNgoSceneEvent(SceneEvent sceneEvent)
        {
            if (sceneEvent.SceneEventType != SceneEventType.Load || sceneEvent.LoadSceneMode != LoadSceneMode.Single)
                return;
            if (s_Instance != this)
                return;

        
            // this is the last safe moment to pull the rig out of a dying scene
            // "re-homed" scene content stays behind (dies with its scene)
            DestroyAdoptedContent();
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("[RR][INFO] RoostRig: moved to DontDestroyOnLoad. Ready to switch scenes!");
        }

        void OnUnitySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || s_Instance != this)
                return;

            TryHookNgo();

            if (!s_HasPlacement || s_PlacementSceneHandle != scene.handle)
            {
                Debug.LogWarning($"[RR][WARN] RoostRig: scene '{scene.name}' contains no RR Rig — add the RR Rig prefab to the scene to choose where the play space sits. Falling back to the world origin.");
                ResetToOrigin();
                s_HasPlacement = true;
                s_PlacementSceneHandle = scene.handle;
            }
        }

        void ResetToOrigin()
        {
            DestroyAdoptedContent();
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }
    }
}
