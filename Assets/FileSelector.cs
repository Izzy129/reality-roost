
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class FileSelector : MonoBehaviour
{
    string[] filesArr = Directory.GetFiles(Application.streamingAssetsPath);
    List<string> m_DropOptions;

    TMP_Dropdown m_Dropdown;
    void Start()
    {
        m_DropOptions = GetOptions(filesArr);
        //Fetch the Dropdown GameObject
        m_Dropdown = GetComponent<TMP_Dropdown>();
        

        //Clear the old options of the Dropdown menu
        m_Dropdown.ClearOptions();
        //Add the options created in the List above
        m_Dropdown.AddOptions(m_DropOptions);
    }

    //get the option selected
    public string SelectionText()
    {
        return m_DropOptions[m_Dropdown.value];
    }

  //todo: make method that 1. excludes .meta files, 2. trims away the path at the beginning

    public List<string> GetOptions(string[] allFilesArr) 
    {
        List<string> filesList = new List<string>(allFilesArr);
        int counter = 0;
        while (counter < filesList.Count) 
        {
            if (filesList[counter].Contains(".meta")) { // remove all META files
                filesList.RemoveAt(counter);
            }
            else
            {
                string fullName = filesList[counter];
                string path = Application.streamingAssetsPath;
                int length = path.Length;
                filesList[counter] = fullName.Substring(length + 1); // keep everything after the path
                Debug.Log(filesList[counter]);
                counter++;
            }
        }
        return filesList;
    }

    public void RefreshOptions()
    {
        m_DropOptions = GetOptions(Directory.GetFiles(Application.streamingAssetsPath));
        
        //Clear the old options of the Dropdown menu
        m_Dropdown.ClearOptions();
        //Add the options created in the List above
        m_Dropdown.AddOptions(m_DropOptions);
    }
    
}