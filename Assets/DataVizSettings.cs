using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DataVizSettings : MonoBehaviour
{
    [SerializeField] private Slider rotateYAxisSlider;
    [SerializeField] private Slider scaleSlider;

    private TextMeshProUGUI rotateYAxisSliderText;
    private TextMeshProUGUI scaleSliderText;

    private void Start()
    {
        rotateYAxisSliderText = rotateYAxisSlider.transform.Find("Handle Slide Area/Handle/Value Text").GetComponent<TextMeshProUGUI>();
        scaleSliderText = scaleSlider.transform.Find("Handle Slide Area/Handle/Value Text").GetComponent<TextMeshProUGUI>();
    }
    private void Update()
    {
        rotateYAxisSliderText.text = (rotateYAxisSlider.value*360).ToString("f2");
        scaleSliderText.text = scaleSlider.value.ToString("f2");
    }

    /// <summary>
    /// Rotates the Data Viz GameObject in the y axis
    /// </summary>
    /// <param name="value"></param>
    public void RotateYAxis()
    {
        var rot = gameObject.transform.rotation;
        rot.y = rotateYAxisSlider.value * 360;
        gameObject.transform.rotation = Quaternion.Euler(0, rot.y, 0);
    }
    /// <summary>
    /// Changes the Data Viz GameObject scale so user can scale up and down
    /// </summary>
    /// <param name="value"></param>
    public void UpdateScale()
    {
        var value = scaleSlider.value;
        gameObject.transform.localScale = new Vector3(value, value, value);
    }
}
