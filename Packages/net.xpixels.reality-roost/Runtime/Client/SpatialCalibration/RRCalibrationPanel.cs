using RealityRoost.Shared.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RealityRoost.Client.SpatialCalibration
{
    // This script handles the hand menu panel for fine-tuning calibration in VR.
    // The panel itself lives in the Roost Rig prefab (a hand menu on left controller).
    public class RRCalibrationPanel : RRSubsystem
    {
        protected override string SubsystemName => "CalibUI";
        protected override bool LogLifecycle => false;

        [Header("References")]
        [Tooltip("Calibrator the readout reflects. Auto-found if left empty")]
        [SerializeField] private RRSpatialCalibrator calibrator;
        [Tooltip("Live status label at the bottom of the panel.")]
        [SerializeField] private TMP_Text status;

        [SerializeField] private InputActionAsset _inputActions;
        private InputAction _showMenuButton;
        [SerializeField] private GameObject _menuUI;

        [Header("Corner-select highlight")]
        [Tooltip("Image on the L Rail button. Colored to show which corner nudges currently move.")]
        [SerializeField] private Image leftCornerButtonImage;
        [Tooltip("Image on the R Rail button. Colored to show which corner nudges currently move.")]
        [SerializeField] private Image rightCornerButtonImage;

        [SerializeField] private Color selectedCornerColor = Color.green;
        [SerializeField] private Color unselectedCornerColor = Color.white;

        protected override void OnSubsystemStart()
        {
            if (calibrator == null)
            {
                calibrator = FindFirstObjectByType<RRSpatialCalibrator>();
            }
            if (calibrator == null)
            {
                LogError("No RRSpatialCalibrator found in scene. Calibration panel is disabled.");
                return;
            }
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                LogError("EventSystem/XRUIInputModule does not exist in Roost Rig, so calibration UI will not work. Did you modify the Roost Rig prefab?");
                return;
            }


            calibrator.OnSelectedCornerChanged += UpdateCornerButtonColors;
            UpdateCornerButtonColors(calibrator.SelectedCorner);
        }

        protected override void OnSubsystemStop()
        {
            if (calibrator != null)
            {
                calibrator.OnSelectedCornerChanged -= UpdateCornerButtonColors;
            }
        }

        // Colors the L Rail / R Rail buttons so the operator can see which corner nudges currently move
        private void UpdateCornerButtonColors(RRSpatialCalibrator.RailCorner selected)
        {
            bool leftSelected = selected == RRSpatialCalibrator.RailCorner.BackLeft;
            if (leftCornerButtonImage != null)
            {
                leftCornerButtonImage.color = leftSelected ? selectedCornerColor : unselectedCornerColor;
            }
            if (rightCornerButtonImage != null)
            {
                rightCornerButtonImage.color = leftSelected ? unselectedCornerColor : selectedCornerColor;
            }
        }

        private void Start()
        {
            _showMenuButton= _inputActions.FindAction("Calibration");
            _showMenuButton.performed += ShowMenuButtonPressed;

        }
        private void ShowMenuButtonPressed(InputAction.CallbackContext obj)
        {
            _menuUI.SetActive(!_menuUI.activeInHierarchy);

        }
        private void Update()
        {
            if (status == null || calibrator == null)
            {
                return;
            }

            CalibrationData c = calibrator.Current;
            status.text =
                $"{(calibrator.IsCalibrated ? "CALIBRATED :D" : "not calibrated :(")}\n" +
                $"yaw {c.YawDegrees:0.0}°\n" +
                $"x {c.LocalPosition.x:0.000}  z {c.LocalPosition.z:0.000}";
        }
    }
}
