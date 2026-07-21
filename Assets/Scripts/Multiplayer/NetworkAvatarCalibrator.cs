using Unity.Netcode;
using UnityEngine;

public class NetworkAvatarCalibrator : NetworkBehaviour
{
    private AvatarCalibrator _avatarCalibrator;
    [SerializeField] private GameObject _avatar;
    public override void OnNetworkSpawn()
    {
        if(IsOwner)
        {
            _avatarCalibrator = FindFirstObjectByType<AvatarCalibrator>();
            _avatarCalibrator.CalibrateUser(_avatar, _avatar.GetComponent<Animator>());
        }
    }
}
