using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ContentSelectionUIManager : MonoBehaviour
{
    [SerializeField] private RectTransform _contentArea;

    [SerializeField] private RuntimeImportManager _importManager;
    [SerializeField] private GameObject _uiButtonPrefab;

    [SerializeField] private GameObject _contentSelectionPanel;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _contentSelectedPanel;

    [SerializeField] private TMP_Text _selectedContentTitle;
    [SerializeField] private Image _selectedContentThumbnail;

    private readonly List<Button> _allContentButtons = new();

    private UIState _uiState = UIState.ContentSelection;
    public UIState UIState
    {
        get { return _uiState; }
        private set
        {
            _uiState = value;
            UIStateChanged();
        }
    }

    private void Awake()
    {
        _importManager.ContentFound += ImportManager_ContentFound;
        _importManager.LoadingStatusChanged += ImportManager_LoadingStatusChanged;
        _importManager.ContentInstantiated += ImportManager_ContentInstantiated;
        _importManager.ContentCleared += ImportManager_ContentCleared;
    }

    private void OnDestroy()
    {
        _importManager.ContentFound -= ImportManager_ContentFound;
        _importManager.LoadingStatusChanged -= ImportManager_LoadingStatusChanged;
        _importManager.ContentInstantiated -= ImportManager_ContentInstantiated;
        _importManager.ContentCleared -= ImportManager_ContentCleared;

        _allContentButtons.ForEach((button) => button.onClick.RemoveAllListeners());
    }

    private void ImportManager_ContentFound(List<ContentMetadata> content)
    {
        _contentArea.sizeDelta = new(Mathf.CeilToInt((float)content.Count / 2) * (_uiButtonPrefab.GetComponent<RectTransform>().rect.width + 16), _contentArea.sizeDelta.y);

        for (int i = 0; i < content.Count; i++)
        {
            var newGO = Instantiate(_uiButtonPrefab, _contentArea);
            var rect = newGO.GetComponent<RectTransform>();
            rect.anchoredPosition = new(Mathf.Floor((float)i / 2) * (rect.rect.width + 8) + 8, i % 2 == 0 ? rect.anchoredPosition.y : rect.anchoredPosition.y - rect.rect.height - 8);

            var text = newGO.GetComponentInChildren<TMP_Text>();
            text.text = content[i].name;

            var newButton = newGO.GetComponentInChildren<Button>();
            int index = i;
            newButton.onClick.AddListener(() => _importManager.CallImportContent(content[index]));
            _allContentButtons.Add(newButton);
        }

        UIState = UIState.ContentSelection;
    }

    private void ImportManager_LoadingStatusChanged(bool isLoading)
    {
        if (isLoading) UIState = UIState.Loading;
    }

    private void ImportManager_ContentInstantiated(ContentMetadata content)
    {
        _selectedContentTitle.text = content.name;
        UIState = UIState.ContentSelected;
    }

    private void ImportManager_ContentCleared()
    {
        UIState = UIState.ContentSelection;
    }

    private void UIStateChanged()
    {
        _contentSelectionPanel.SetActive(UIState == UIState.ContentSelection);
        _loadingPanel.SetActive(UIState == UIState.Loading);
        _contentSelectedPanel.SetActive(UIState == UIState.ContentSelected);
    }
}
