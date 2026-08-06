using RootMotion.FinalIK;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AvatarCalibrator : MonoBehaviour
{
    [SerializeField] private CalibrationMode _calibrationMode;
    [SerializeField] private GameObject _avatar;
    private Animator _avatarAnim;

    private float _playerEyeHeight; // Real life user
    private float _avatarEyeHeight; // Avatar model height
    private float _heightScale;

    [SerializeField] private GameObject _calibrationUI; // Local calibration UI menu
    public Button CalibrateUIButton; // Calibration UI Button
    [SerializeField] private InputActionAsset _inputActions;
    private InputAction _calibrationInputButton;

    private float _leftFootOffset, _rightFootOffset;
    private GameObject _leftAnkleTarget, _rightAnkleTarget;

    private void Start()
    {
        _avatarAnim = _avatar.GetComponent<Animator>();
        CalibrationState.Value = _calibrationMode;

        CalibrateUIButton = _calibrationUI.GetComponentInChildren<Button>();
        _calibrationInputButton = _inputActions.FindAction("Calibration");
        _calibrationInputButton.performed += CalibrationButtonPressed;
        //CalibrateUIButton.onClick.AddListener(delegate { CalibrateUser(_avatar, _avatarAnim); }); // Local OnClick() call -- removed for now
    }
    private void CalibrationButtonPressed(InputAction.CallbackContext obj)
    {
        _calibrationUI.SetActive(!_calibrationUI.activeInHierarchy);
    }
    /// <summary>
    /// Toggles VRIK before and after avatar calibration
    /// </summary>
    /// <param name="avatar"></param>
    private void ToggleVRIK(GameObject avatar)
    {
        var state = avatar.GetComponent<VRIK>().enabled;
        state = !state;
        avatar.GetComponent<VRIK>().enabled = state;
        UnityEngine.Debug.Log("[VRIK State] " + state);
    }
    /// <summary>
    /// Calibrates avatar to user properly 
    /// </summary>
    public void CalibrateUser(GameObject avatar, Animator animator)
    {
        switch(_calibrationMode)
        {
            case CalibrationMode.ThreePoint:
                Calibrate3PT(avatar, animator);
                break;
            case CalibrationMode.FullBody:
                _leftAnkleTarget = GameObject.Find("Left Ankle Target");
                _rightAnkleTarget = GameObject.Find("Right Ankle Target");
                CalibrateFBT(avatar, animator, _leftAnkleTarget, _rightAnkleTarget);
                break;
        }             
    }
    /// <summary>
    /// Calibrates 3 Point avatar to user. 
    /// 3PT: Tracking user head, controllers.
    /// </summary>
    /// <param name="avatar"></param>
    /// <param name="animator"></param>
    public void Calibrate3PT(GameObject avatar, Animator animator)
    {
        if (avatar.GetComponent<VRIK>().enabled) ToggleVRIK(avatar);
        ResetAvatarScale(avatar);
        MeasureUserEyeHeight();
        MeasureAvatarEyeHeight(animator);
        CalculateHeightScale();
        ScaleAvatar(avatar);
        ToggleVRIK(avatar);
    }
    /// <summary>
    /// Calibrates FBT avatar to user
    /// </summary>
    /// <param name="avatar"></param>
    /// <param name="animator"></param>
    /// <param name="leftAnkleTracker"></param>
    /// <param name="rightAnkleTracker"></param>
    public void CalibrateFBT(GameObject avatar, Animator animator, GameObject leftAnkleTracker, GameObject rightAnkleTracker)
    {
        if (avatar.GetComponent<VRIK>().enabled) ToggleVRIK(avatar);
        ResetAvatarScale(avatar);
        MeasureUserEyeHeight();
        MeasureAvatarEyeHeight(animator);
        CalculateHeightScale();
        ScaleAvatar(avatar);
        ToggleVRIK(avatar);
        CalculateFeetOffset(animator, _leftAnkleTarget, _rightAnkleTarget);
    }
    /// <summary>
    /// Resets avatar scale if user re-calibrates multiple times during gameplay
    /// </summary>
    /// <param name="avatar"></param>
    private void ResetAvatarScale(GameObject avatar)
    {
        avatar.transform.localScale = Vector3.one;
    }
    /// <summary>
    /// Get user height from XR Origin
    /// </summary>
    public void MeasureUserEyeHeight()
    {
        _playerEyeHeight = Camera.main.transform.localPosition.y;
    }
    /// <summary>
    /// Get avatar height from feet to head bone
    /// </summary>
    /// <param name="animator"></param>
    public void MeasureAvatarEyeHeight(Animator animator)
    {
        // Evalulate the current status of the animator
        animator.Update(0f);
        // Access eye, feet bones
        Transform leftEye = animator.GetBoneTransform(HumanBodyBones.LeftEye);
        Transform rightEye = animator.GetBoneTransform(HumanBodyBones.RightEye);
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftToes); // Foot
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightToes);
        // Calculate player height by finding avg, then finding the difference
        float eyeY = (leftEye.position.y + rightEye.position.y) / 2f;
        float footY = (leftFoot.position.y + rightFoot.position.y) / 2f;
        _avatarEyeHeight = eyeY - footY;
    }
    /// <summary>
    /// Calculate scale from user height and avatar eye height
    /// </summary>
    private void CalculateHeightScale()
    {
        _heightScale = _playerEyeHeight / _avatarEyeHeight;
    }
    /// <summary>
    /// Scale avatar from 1*HeightScale
    /// </summary>
    /// <param name="avatarRoot"></param>
    private void ScaleAvatar(GameObject avatar)
    {
        avatar.transform.localScale = Vector3.one * _heightScale;
    }
    /// <summary>
    /// Calculates offset from user's ankle tracker to avatar's foot bone. Ensures avatar's feet are grounded. 
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="leftAnkleTracker"></param>
    /// <param name="rightAnkleTracker"></param>
    private void CalculateFeetOffset(Animator animator, GameObject leftAnkleTracker, GameObject rightAnkleTracker)
    {
        // Calculate left foot offset
        var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        _leftFootOffset = leftFoot.position.y - leftAnkleTracker.transform.position.y;
        leftAnkleTracker.transform.localPosition = new Vector3(0, _leftFootOffset,0);
        // Calculate right foot offset
        var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightToes);
        _rightFootOffset = rightFoot.position.y - rightAnkleTracker.transform.position.y;
        rightAnkleTracker.transform.localPosition = new Vector3(0, _rightFootOffset, 0);
    }
}