using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RealityRoost.Shared.SceneSwitching
{
    // Script that represents an entry in the scene switcher UI's list
    
    public class RRSceneSlotUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text sceneNameText;
        [SerializeField] private GameObject selectImage;
        [SerializeField] private GameObject unselectedImage;

        // Build Settings index this row represents
        public int BuildIndex { get; private set; }

        private RRSceneSwitcherMenu _menu;

        public void Setup(int buildIndex, string sceneName, RRSceneSwitcherMenu menu)
        {
            BuildIndex = buildIndex;
            _menu = menu;
            if (sceneNameText != null)
            {
                sceneNameText.text = sceneName;
            }
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnRowClicked);
            }
        }

        // Show the "Selected" pill when this row is the menu's desired index, else the "Select" pill
        public void SetSelected(bool selected)
        {
            if (selectImage != null)
            {
                selectImage.SetActive(selected);
            }
            if (unselectedImage != null)
            {
                unselectedImage.SetActive(!selected);
            }
        }

        private void OnRowClicked()
        {
            if (_menu != null)
            {
                _menu.SetDesiredIndex(BuildIndex);
            }
        }

        private void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(OnRowClicked);
            }
        }
    }
}
