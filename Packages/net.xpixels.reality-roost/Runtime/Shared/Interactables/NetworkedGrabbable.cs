using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// NetworkedGrabbable.cs transfers ownership on grab so an owner-authoritative NetworkTransform can synchronize movement. 
/// Allows clients to successfully move and deselect the Grabbable object across the network, which all clients can see.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(XRGrabInteractable), typeof(NetworkRigidbody))]
public class NetworkedGrabbable : NetworkBehaviour
{
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        _grabInteractable.selectEntered.AddListener(OnSelectEntered);
        _grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
        _grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (IsSpawned)
        {
            if (IsServer) 
                NetworkObject.ChangeOwnership(NetworkManager.LocalClientId);
            else 
                RequestOwnership_ServerRpc();
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // XRI restores the kinematic state before ownership was granted
        if (IsSpawned && !_grabInteractable.isSelected) _rigidbody.isKinematic = !IsOwner;
    }

    /// <summary>
    /// RPC that switches ownership of grabbable object to a client.
    /// </summary>
    /// <param name="rpcParams"></param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestOwnership_ServerRpc(RpcParams rpcParams = default)
    {
        if (NetworkObject.OwnerClientId != rpcParams.Receive.SenderClientId)
            NetworkObject.ChangeOwnership(rpcParams.Receive.SenderClientId);
    }
}
