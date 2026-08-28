using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RealityRoost.Shared.SceneSwitching
{
    // This script handles the operator scene-switcher hand menu. 
    public class RRSceneSwitcherMenu : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RR Scene Switcher reference. Auto-found if left empty.")]
        [SerializeField] private RRSceneSwitcher switcher;
        [Tooltip("Content transform the scene rows are instantiated under (Scroll View/Viewport/Content).")]
        [SerializeField] private Transform contentParent;
        [Tooltip("Scene row prefab (project asset made from 'Scene UI Prefab (Calib)').")]
        [SerializeField] private RRSceneSlotUI sceneSlotPrefab;
        [Tooltip("First Build Settings index to list. RR_Boot = index 0, and is never a switch target, so this defaults to 1 (RR_Calib).")]
        [SerializeField] private int firstSwitchableBuildIndex = 1;

        [SerializeField] private InputActionAsset _inputActions;
        private InputAction _showMenuButton;
        [SerializeField] private GameObject _menuUI;

        private readonly List<RRSceneSlotUI> _slots = new List<RRSceneSlotUI>();
        private int _desiredIndex = -1;

        // Build Settings index the operator has currently selected
        public int DesiredIndex => _desiredIndex;

        private void Start()
        {
            if (switcher == null)
            {
                switcher = FindFirstObjectByType<RRSceneSwitcher>();
            }

            _showMenuButton = _inputActions.FindAction("Scale Toggle"); // "Left Thumbstick In" that's already included in XRI Default Input Actions
            _showMenuButton.performed += ShowMenuButtonPressed;
            Populate();

        }
        private void ShowMenuButtonPressed(InputAction.CallbackContext obj)
        {
            _menuUI.SetActive(!_menuUI.activeInHierarchy);
        }

        // Rebuild the row list from the Build Settings scene list and select the active scene
        public void Populate()
        {
            if (switcher == null || contentParent == null || sceneSlotPrefab == null)
            {
                LogError("Missing a reference (switcher / contentParent / sceneSlotPrefab)! Cannot populate scene list.");
                return;
            }

            // remove existing dummy RRSceneSlotUI's 
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

            int count = switcher.SceneCount;
            int startIndex = Mathf.Max(0, firstSwitchableBuildIndex);
            for (int i = startIndex; i < count; i++)
            {
                RRSceneSlotUI slot = Instantiate(sceneSlotPrefab, contentParent);
                slot.Setup(i, switcher.GetSceneName(i), this);
                _slots.Add(slot);
            }

            // Start with the currently-active scene selected (fall-back clamp to startIndex)
            int active = SceneManager.GetActiveScene().buildIndex;
            if (active < startIndex)
            {
                active = startIndex;
            }
            SetDesiredIndex(active);
        }

        // Called by a row when clicked; updates every row's pill state
        public void SetDesiredIndex(int buildIndex)
        {
            _desiredIndex = buildIndex;
            foreach (RRSceneSlotUI slot in _slots)
            {
                slot.SetSelected(slot.BuildIndex == buildIndex);
            }
        }

        // "Switch Scene" button onClick
        public void OnSwitchPressed()
        {
            if (_desiredIndex < 0)
            {
                LogWarning("Switch pressed but no scene is selected.");
                return;
            }

            int active = SceneManager.GetActiveScene().buildIndex;
            if (_desiredIndex == active)
            {
                LogInfo($"Desired scene [{_desiredIndex}] is already active, not switching.");
                return;
            }
            switcher.LoadExperience(_desiredIndex);
        }

        private void LogInfo(string message) => Debug.Log($"[RR][INFO] SceneSwitchUI: {message}");
        private void LogWarning(string message) => Debug.LogWarning($"[RR][WARN] SceneSwitchUI: {message}");
        private void LogError(string message) => Debug.LogError($"[RR][ERROR] SceneSwitchUI: {message}");
    }
}
