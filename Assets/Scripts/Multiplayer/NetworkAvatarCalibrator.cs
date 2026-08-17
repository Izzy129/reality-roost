using Unity.Netcode;
using UnityEngine;
using RootMotion.FinalIK;

public class NetworkAvatarCalibrator : NetworkBehaviour
{
    public AvatarCalibrator _avatarCalibrator;
    [SerializeField] private GameObject _avatar;
    private float userScale;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
              Debug.Log("[RR][OnNetworkSpawn Called], User " + NetworkManager.Singleton.LocalClientId);
            _avatarCalibrator = FindFirstObjectByType<AvatarCalibrator>();
               Debug.Log("[RR][scale before]" + _avatar.transform.localScale.y);
            _avatarCalibrator.CalibrateUser(_avatar, _avatar.GetComponent<Animator>());
            //_avatarCalibrator.CalibrateUIButton.onClick.AddListener(() => { CalibrateButton(); });
            userScale = _avatar.transform.localScale.y; //float scale = _avatar.transform.localScale.y;
            CalibrateServerRpc(userScale);
            TestRpc(); // add check so if first client in session, no need to run
            Debug.Log("[RR][End of OnNetworkSpawn]");
        }
    }

    [Rpc(SendTo.Server)]
    public void CalibrateServerRpc(float scale)
    {
        Debug.Log($"[CALIBRATION] Server received scale {scale} " +
        $"for player {OwnerClientId}");
        CalibrateClientRpc(scale);
    }
    [Rpc(SendTo.Everyone)]
    public void CalibrateClientRpc(float scale)
    {
        Debug.Log(
        $"[CALIBRATION] Client {NetworkManager.Singleton.LocalClientId} " +
        $"received scale {scale} for Owner {OwnerClientId}"
        );

        _avatar.transform.localScale = Vector3.one * scale; // avatarCalib call instead?
        _avatar.GetComponent<VRIK>().enabled = true;
    }

    [Rpc(SendTo.Owner)]
    public void TestRpc() // Late-joining clients
    {
        Debug.Log("[Entered TestRpc]");
        // get each avatar, apply scale and VRIK on
        for(int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
        {
            if(NetworkManager.Singleton.ConnectedClientsIds[i] == NetworkManager.Singleton.LocalClientId) return;

            var test = NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<NetworkAvatarCalibrator>();
            test._avatar.transform.localScale = Vector3.one * test.userScale;
            test._avatar.GetComponent<VRIK>().enabled = true;    
        }
    }

    /*
    public override void OnNetworkDespawn()
    {
        if(IsOwner) _avatarCalibrator.CalibrateUIButton.onClick.RemoveAllListeners();
    }
    public void CalibrateButton()
    {
        _avatarCalibrator.CalibrateUser(_avatar, _avatar.GetComponent<Animator>());
        Debug.Log($"Calibrate method called by client ID {NetworkManager.Singleton.LocalClientId}");
    }*/
}
