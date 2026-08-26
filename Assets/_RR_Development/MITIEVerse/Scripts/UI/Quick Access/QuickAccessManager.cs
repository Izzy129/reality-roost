using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// QuickAccessManager.cs is used to manage the user's Quick Access Menu
/// </summary>
public class QuickAccessManager : MonoBehaviour
{
    #region Fields
    [SerializeField] private GameObject _quickAccessMenu; // The UI Menu GameObject that is toggled
    [SerializeField] private InputActionReference _quickAccessIA; // The Input Action Reference that is called when the user presses the button
    #endregion
    #region MonoBehaviour
    void Start()
    {
        _quickAccessIA.action.performed += ToggleQuickAccessMenu;
    }
    private void OnDestroy()
    {
        _quickAccessIA.action.performed -= ToggleQuickAccessMenu;
    }
    #endregion
    #region Methods
    private void ToggleQuickAccessMenu(InputAction.CallbackContext obj)
    {
        Debug.Log("Quick Access Menu has been toggled!");
        _quickAccessMenu.SetActive(!_quickAccessMenu.activeSelf);
    }
    public void CloseMenu(GameObject menu)
    {
        menu.SetActive(false);
    }
    #endregion
}
