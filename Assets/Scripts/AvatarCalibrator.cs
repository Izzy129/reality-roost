using UnityEngine;
using UnityEngine.InputSystem;

public class AvatarCalibrator : MonoBehaviour
{
    private bool _hasCalibrated = false;
    [SerializeField] private GameObject _calibrationUI; // Local calibration UI menu
    [SerializeField] private GameObject _avatar;
    private Animator _avatarAnim;

    private float _playerEyeHeight; // Real life user
    private float _avatarEyeHeight; // Avatar model height
    private float _heightScale;

    [SerializeField] private InputActionAsset _inputActions;
    private InputAction _calibrationInputButton;

    private void Start()
    {
        _avatarAnim = _avatar.GetComponent<Animator>();
        _calibrationInputButton = _inputActions.FindAction("Calibration");
        _calibrationInputButton.performed += CalibrationButtonPressed;
    }
    private void CalibrationButtonPressed(InputAction.CallbackContext obj)
    {
        _calibrationUI.SetActive(!_calibrationUI.activeInHierarchy);
    }
    /// <summary>
    /// Calibrate user so avatar fits user properly 
    /// </summary>
    public void CalibrateUser()
    {
        ResetAvatarScale(_avatar);
        MeasureUserEyeHeight();
        MeasureAvatarEyeHeight(_avatarAnim);
        CalculateHeightScale();
        ScaleAvatar(_avatar);
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
        Debug.Log("[h user eye height " + +_playerEyeHeight);
    }
    /// <summary>
    /// Get avatar height from feet to head bone
    /// </summary>
    /// <param name="animator"></param>
    public void MeasureAvatarEyeHeight(Animator animator)
    {
        // Evalulate the current status of the animator
        _avatarAnim.Update(0f);
        // Access head, feet bones
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        // Calculate player height by finding feet avg, then finding the difference
        float footY = (leftFoot.position.y + rightFoot.position.y) / 2f;
        _avatarEyeHeight = head.position.y - footY;
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
        _avatar.transform.localScale = Vector3.one * _heightScale;
    }
}