using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DrawingUIManager : NetworkBehaviour
{
    [SerializeField] private Button _eraseAllButton;
    void Start()
    {
        // Toggle off Erase All Button if user is not the host
        if(!IsHost)
        {
            _eraseAllButton.gameObject.SetActive(false);
        }
    }
}
