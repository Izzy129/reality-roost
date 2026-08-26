using UnityEngine;

public class ShowIfFlaggedAttribute : PropertyAttribute
{
    public string enumFieldName;
    public int flagValue;

    public ShowIfFlaggedAttribute(string enumFieldName, int flagValue)
    {
        this.enumFieldName = enumFieldName;
        this.flagValue = flagValue;
    }
}
