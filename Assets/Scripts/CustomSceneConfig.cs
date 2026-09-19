using UnityEngine;

/// <summary>
/// Used if you want to tweak specific player settings per custom scene
/// </summary>
public class CustomSceneConfig : MonoBehaviour
{
    Camera mainCamera;

    // Variables Users can change
    [SerializeField] float cameraMinClippingPlane;
    [SerializeField] float cameraMaxClippingPlane;

    // Initial values
    float init_cameraMinClippingPlane;
    float init_cameraMaxClippingPlane;

    void Start()
    {
        mainCamera = Camera.main;

        init_cameraMinClippingPlane = mainCamera.nearClipPlane;
        init_cameraMaxClippingPlane = mainCamera.farClipPlane;

        if(cameraMinClippingPlane != 0) mainCamera.nearClipPlane = cameraMinClippingPlane;
        if(cameraMaxClippingPlane != 0) mainCamera.farClipPlane = cameraMaxClippingPlane;
    }
    private void OnDestroy()
    {
        // Reset values back to default
        mainCamera.nearClipPlane = init_cameraMinClippingPlane;
        mainCamera.farClipPlane= init_cameraMaxClippingPlane;
    }
}
