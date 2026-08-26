using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkedSlideshowState : NetworkBehaviour
{
    private int _maxSlideIndex = 0;

    public NetworkVariable<int> SlideIndex { get; private set; } = new NetworkVariable<int>(
        0,    
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<int> SlideIndexChanged;

    public override void OnNetworkSpawn()
    {
        SlideIndex.OnValueChanged += (prev, curr) => SlideIndexChanged?.Invoke(curr);
    }

    public void SetMaxSlideIndex(int maxSlideIndex)
    {
        _maxSlideIndex = maxSlideIndex;
    }

    public void RequestSlideChange(int newIndex)
    {
        if (IsServer)
        {
            ChangeSlideIndex(newIndex);
        }
        else
        {
            RequestSlideChangeServerRpc(newIndex);
        }
    }

    public void ChangeSlideIndex(int newIndex)
    {
        newIndex = Math.Clamp(newIndex, 0, _maxSlideIndex);
        SlideIndex.Value = Math.Clamp(0, newIndex, _maxSlideIndex);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestSlideChangeServerRpc(int newIndex)
    {
        ChangeSlideIndex(newIndex);
    }
}
