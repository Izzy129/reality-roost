using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealityRoost.WorldRail
{
    public sealed class WorldRailEnvironment : MonoBehaviour
    {
        public static readonly List<WorldRailEnvironment> ActiveInstances = new List<WorldRailEnvironment>();
        public static string LastStreamingOperation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry() => ActiveInstances.Clear();

        public Transform scenery;
        public Color ambient = new Color(.35f, .4f, .45f), fog = new Color(.4f, .45f, .5f);
        public float fogDensity = .003f;
        public Material skybox;
        public Light sun;
        public int ProgressVersion { get; private set; }
        public bool Prepared { get; private set; }
        public bool Retired { get; private set; }

        readonly List<GameObject> groups = new List<GameObject>();
        GameObject[] importedRoots;
        bool useSceneLighting;
        Scene previousActiveScene;

        internal static void AttachToScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            var controller = new GameObject("Train scenery");
            SceneManager.MoveGameObjectToScene(controller, scene);
            var environment = controller.AddComponent<WorldRailEnvironment>();
            var pivot = new GameObject("Scenery");
            pivot.transform.SetParent(controller.transform, false);
            pivot.SetActive(false);
            environment.scenery = pivot.transform;
            environment.importedRoots = roots;
            environment.useSceneLighting = true;
        }

        void Awake()
        {
            ActiveInstances.Add(this);
            if (scenery) scenery.gameObject.SetActive(false);
        }

        IEnumerator Start()
        {
            if (!scenery) yield break;
            double deadline = Time.realtimeSinceStartupAsDouble + .001;
            if (importedRoots != null)
            {
                foreach (var root in importedRoots)
                {
                    if (!root || root.scene != gameObject.scene) continue;
                    // Keep the passenger's camera and listener in control.
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                        if (!camera.targetTexture) camera.enabled = false;
                    foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
                        listener.enabled = false;
                    // Netcode owns the parenting and lifetime of networked objects.
                    if (!root.GetComponentInChildren<NetworkObject>(true))
                        root.transform.SetParent(scenery, true);

                    if (Time.realtimeSinceStartupAsDouble < deadline) continue;
                    yield return null;
                    deadline = Time.realtimeSinceStartupAsDouble + .001;
                }
                importedRoots = null;
            }

            foreach (Transform child in scenery)
            {
                if (child.gameObject.activeSelf)
                {
                    groups.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                    ProgressVersion++;
                }
                if (Time.realtimeSinceStartupAsDouble < deadline) continue;
                yield return null;
                deadline = Time.realtimeSinceStartupAsDouble + .001;
            }
            scenery.gameObject.SetActive(true);
            yield return Toggle(true);
            Prepared = true;
        }

        IEnumerator Toggle(bool active)
        {
            LastStreamingOperation = (active ? "Activate " : "Deactivate ") + gameObject.scene.name;
            double deadline = Time.realtimeSinceStartupAsDouble + .001;
            int count = 0;
            foreach (var group in groups)
            {
                if (!group) continue;
                group.SetActive(active);
                ProgressVersion++;
                if (++count < 64 && Time.realtimeSinceStartupAsDouble < deadline) continue;
                yield return null;
                count = 0;
                deadline = Time.realtimeSinceStartupAsDouble + .001;
            }
        }

        public IEnumerator Retire()
        {
            Prepared = false;
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            yield return Toggle(false);
            scenery.gameObject.SetActive(false);
            Retired = true;
        }

        void OnDestroy() => ActiveInstances.Remove(this);

        public void Show(bool visible, float distance)
        {
            if (!scenery || (Application.isPlaying && !Prepared)) return;
            if (!Application.isPlaying) scenery.gameObject.SetActive(visible);
            scenery.localPosition = Vector3.back * distance;
            if (!visible) return;
            if (useSceneLighting)
            {
                if (SceneManager.GetActiveScene() != gameObject.scene)
                {
                    previousActiveScene = SceneManager.GetActiveScene();
                    SceneManager.SetActiveScene(gameObject.scene);
                }
                return;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.skybox = skybox;
            if (sun) RenderSettings.sun = sun;
        }
    }
}
