using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkedModelGrab : NetworkBehaviour
{
    private void Start()
    {
        var xrGrabbable = GetComponentInChildren<XRGrabInteractable>();
        if (xrGrabbable != null)
        {
            xrGrabbable.selectEntered.AddListener(XRGrabInteractable_SelectEntered);
        }
    }

    private void XRGrabInteractable_SelectEntered(SelectEnterEventArgs args)
    {
        if (!IsOwner)
        {
            RequestOwnershipServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestOwnershipServerRpc(RpcParams rpcParams = default)
    {
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.ChangeOwnership(rpcParams.Receive.SenderClientId);
        }
    }
}
