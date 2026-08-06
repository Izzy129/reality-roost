using Unity.Netcode;
using UnityEngine;

public class NetworkAvatarCalibrator : NetworkBehaviour
{
    private AvatarCalibrator _avatarCalibrator;
    [SerializeField] private GameObject _avatar;
    public override void OnNetworkSpawn()
    {
        //_avatarCalibrator = FindFirstObjectByType<AvatarCalibrator>();
        //_avatarCalibrator.CalibrateUIButton.onClick.AddListener(() => { CalibrateButton(); });
        if (IsOwner)
        {
            Debug.Log("[RR][OnNetworkSpawn Called]");
            _avatarCalibrator = FindFirstObjectByType<AvatarCalibrator>();
            //_avatarCalibrator.CalibrateUIButton.onClick.AddListener(() => { CalibrateButton(); });
            _avatarCalibrator.CalibrateUser(_avatar, _avatar.GetComponent<Animator>());
        }
    }
    public override void OnNetworkDespawn()
    {
        if(IsOwner) _avatarCalibrator.CalibrateUIButton.onClick.RemoveAllListeners();
    }
    public void CalibrateButton()
    {
        _avatarCalibrator.CalibrateUser(_avatar, _avatar.GetComponent<Animator>());
        Debug.Log($"Calibrate method called by client ID {NetworkManager.Singleton.LocalClientId}");
    }
}
