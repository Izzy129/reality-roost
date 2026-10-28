using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// NetworkedInteracterable.cs handles custom user input & networks them across a multiplayer session via RPCs. 
/// This script is designed to support any component that stems from the XRBaseInteractable class (i.e XR Simple Interactable, XR Grab Interactable). https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/api/UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable.html
/// 
/// Workflow: User adds desired events to UnityEvents. During runtime, it will automatically call across the network for all connected clients. 
/// </summary>
public class NetworkedInteractable : NetworkBehaviour
{
    #region Fields
    [Header("Networked Interaction Events")]
    [SerializeField] private UnityEvent _onSelect; // Event called when an interactor begins selecting this Interactable. (XR Simple Interactable: Triggered when user's 'Hand' collider goes through collider OR Networked Grabbable: when user picks up object via grip)
    [SerializeField] private UnityEvent _onDeselect; // Event called when an interactor stops selecting this Interactable. (XR Simple Interactable: triggered when user's 'Hand' collider leaves button OR Networked Grabbable: When you release an object via grip)
    [SerializeField] private UnityEvent _onActivate; // Event called when an interactor is currently selecting this Interactable & calls the Activate input, such as from a trigger pull on a controller 

    private XRBaseInteractable _xrBaseInteractable; // Refers to the XRBaseInteractable type component on the GameObject
    #endregion

    #region MonoBehaviour
    private void Awake()
    {
        _xrBaseInteractable = GetComponent<XRBaseInteractable>();
        if (_xrBaseInteractable == null) Debug.LogError("No existing XRBaseInteractable-type component on this GameObject. Add either XRSimpleInteractor or XRGrabInteractor to your GameObject to resolve this error.");
    }

    private void OnEnable()
    {
        if (_xrBaseInteractable == null) return;
        // Delegate subscriptions
        _xrBaseInteractable.selectEntered.AddListener(OnSelect);
        _xrBaseInteractable.selectExited.AddListener(OnDeselect);
        _xrBaseInteractable.activated.AddListener(OnActivate);
    }
    private void OnDisable()
    {
        if (_xrBaseInteractable == null) return;
        // Delegate unsubscriptions
        _xrBaseInteractable.selectEntered.RemoveListener(OnSelect);
        _xrBaseInteractable.selectExited.RemoveListener(OnDeselect);
        _xrBaseInteractable.activated.RemoveListener(OnActivate);
    }
    #endregion

    #region Delegates
    /// <summary>
    /// Called when an interactor begins selecting this interactable. Calls corresponding RPC.
    /// </summary>
    /// <param name="args0"></param>
    private void OnSelect(SelectEnterEventArgs args0) 
    {
        if (!IsSpawned) return;
        // Call RPC
        SelectEntered_ServerRpc();
    }
    /// <summary>
    /// Called when an interactor stops selecting this interactable. Calls corresponding RPC.
    /// </summary>
    /// <param name="args0"></param>
    private void OnDeselect(SelectExitEventArgs args0)
    {
        if (!IsSpawned) return;
        // Call RPC
        SelectExited_ServerRpc();
    }
    /// <summary>
    /// Called when an interactor is currently selecting this interactable, and then presses the 'Activate' button input. Calls corresponding RPC.
    /// </summary>
    /// <param name="args0"></param>
    private void OnActivate(ActivateEventArgs args0)
    {
        if (!IsSpawned) return;
        // Call RPC
        Activated_ServerRpc();
    }
    #endregion

    #region Methods
    /// <summary>
    /// Called when a local client is requesting a select RPC action.
    /// </summary>
    public void RequestSelect()
    {
        Debug.Log("Request select has been called.");
        if (IsSpawned) SelectEntered_ServerRpc();
    }
    /// <summary>
    /// Called when a local client is requesting a deselect RPC action.
    /// </summary>
    public void RequestDeselect()
    {
        Debug.Log("Request Deselect has been called.");
        if (IsSpawned) SelectExited_ServerRpc();
    }
    /// <summary>
    /// Called when a local client is requesting an activate RPC action.
    /// </summary>
    public void RequestActivate()
    {
        Debug.Log("Request Deselect has been called.");
        if (IsSpawned) Activated_ServerRpc();
    }
    #endregion

    #region RPCs
    /// <summary>
    /// Server RPC that is called via the client when they request a SelectEntered event. Calls the corresponding ClientRPC to replicate to all clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SelectEntered_ServerRpc()
    {
        Debug.Log("Select Entered Server RPC sent");
        SelectEntered_ClientRpc();
    }
    /// <summary>
    /// Client RPC that broadcasts SelectEntered event to all clients.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void SelectEntered_ClientRpc()
    {
        Debug.Log("Select Entered Client RPC sent");
        _onSelect.Invoke();
    }
    /// <summary>
    /// Server RPC that is called via the client when they request a SelectExited event. Calls the corresponding ClientRPC to replicate to all clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SelectExited_ServerRpc()
    {
        Debug.Log("Select Exited Server RPC sent");
        SelectExited_ClientRpc();
    }
    /// <summary>
    /// Client RPC that broadcasts SelectExited event to all clients.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void SelectExited_ClientRpc()
    {
        Debug.Log("Select Exited Client RPC sent");
        _onDeselect.Invoke();
    }
    /// <summary>
    /// Server RPC that is called via the client when they request a Activated event. Calls the corresponding ClientRPC to replicate to all clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void Activated_ServerRpc()
    {
        Debug.Log("Activated Server RPC sent");
        Activated_ClientRpc();
    }
    /// <summary>
    /// Client RPC that broadcasts Activated event to all clients.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    private void Activated_ClientRpc()
    {
        Debug.Log("Activated Client RPC sent");
        _onActivate.Invoke();
    }
    #endregion

}
