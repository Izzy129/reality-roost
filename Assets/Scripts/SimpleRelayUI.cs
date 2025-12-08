using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleRelayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RelayManager relayManager;

    [Header("UI Elements")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        // Hook up button clicks
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        // Find RelayManager if not assigned
        if (relayManager == null)
        {
            relayManager = FindObjectOfType<RelayManager>();
        }
    }

    private async void OnHostClicked()
    {
        // Disable buttons during connection
        hostButton.interactable = false;
        joinButton.interactable = false;

        statusText.text = "Starting host with Relay...";

        string joinCode = await relayManager.StartHostWithRelay();

        if (!string.IsNullOrEmpty(joinCode))
        {
            statusText.text = $"HOSTING!\n\nJoin Code:\n<size=48><b>{joinCode}</b></size>\n\n(Share this code with others)";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "Failed to host.\nCheck console for errors.";
            statusText.color = Color.red;
            hostButton.interactable = true;
            joinButton.interactable = true;
        }
    }

    private async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(code))
        {
            statusText.text = " Please enter a join code!";
            statusText.color = Color.yellow;
            return;
        }

        // Disable buttons during connection
        hostButton.interactable = false;
        joinButton.interactable = false;

        statusText.text = $"Joining with code: {code}...";

        bool success = await relayManager.JoinWithRelay(code);

        if (success)
        {
            statusText.text = "Connected to host!";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = " Failed to join.\nCheck code and try again.";
            statusText.color = Color.red;
            hostButton.interactable = true;
            joinButton.interactable = true;
        }
    }
}
