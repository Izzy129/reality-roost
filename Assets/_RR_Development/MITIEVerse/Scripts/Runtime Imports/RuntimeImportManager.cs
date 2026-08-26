using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using Unity.Netcode;
using System.Collections;
using UnityEngine.UI;

public class RuntimeImportManager : NetworkBehaviour
{
    #region Fields
    //Unity Fields
    [SerializeField] private string _metadataUri; //Use a proxy from dropbox for now, eventually we'll need a more refined content upload and management system.
    [SerializeField] private ContentType allowedContent;


    [ShowIfFlagged("allowedContent", (int)ContentType.Model)]
    [SerializeField] private Transform _modelSpawnPoint;
    [ShowIfFlagged("allowedContent", (int)ContentType.Model)]
    [SerializeField] private GameObject _modelSyncProxyPrefab;

    [ShowIfFlagged("allowedContent", (int)ContentType.Video)]
    [SerializeField] private VideoPlayer _videoPlayer;
    [ShowIfFlagged("allowedContent", (int)ContentType.Video)]
    [SerializeField] private GameObject _videoScreen;

    [ShowIfFlagged("allowedContent", (int)ContentType.Slideshow)]
    [SerializeField] private Button _slideshowPrevButton;
    [ShowIfFlagged("allowedContent", (int)ContentType.Slideshow)]
    [SerializeField] private Button _slideshowNextButton;
    [ShowIfFlagged("allowedContent", (int)ContentType.Slideshow)]
    [SerializeField] private GameObject _slideshowScreen;
    [ShowIfFlagged("allowedContent", (int)ContentType.Slideshow)]
    [SerializeField] private GameObject _slideshowConsole;
    [ShowIfFlagged("allowedContent", (int)ContentType.Slideshow)]
    [SerializeField] private GameObject _slideshowStateSyncPrefab;

    //Member fields
    private Dictionary<ContentType, IRuntimeImporter> _importers;
    private int _importCount;
    #endregion

    #region Properties
    public List<ContentMetadata> AvailableContent {  get; private set; }
    public ContentMetadata ActiveContent { get; private set; }
    #endregion

    #region Network Variables
    private NetworkVariable<bool> _isLoading = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> _activeContentId = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    #endregion

    #region Events
    public event Action<List<ContentMetadata>> ContentFound;
    public event Action<ContentMetadata> ContentInstantiated;
    public event Action<bool> LoadingStatusChanged;
    public event Action ContentCleared;
    #endregion

    #region Unity Messages
    private void Awake()
    {
        _importers = new Dictionary<ContentType, IRuntimeImporter>();
        if (allowedContent.HasFlag(ContentType.Model)) _importers.Add(ContentType.Model, new ModelImporter(_modelSpawnPoint, this, _modelSyncProxyPrefab));
        if (allowedContent.HasFlag(ContentType.Video)) _importers.Add(ContentType.Video, new VideoImporter(_videoPlayer, _videoScreen));
        if (allowedContent.HasFlag(ContentType.Slideshow)) _importers.Add(ContentType.Slideshow, new SlideshowImporter(_slideshowPrevButton, _slideshowNextButton, _slideshowScreen, _slideshowConsole, this, _slideshowStateSyncPrefab));
    }

    public override void OnNetworkSpawn()
    {
        _isLoading.OnValueChanged += IsLoading_ValueChanged;
        _activeContentId.OnValueChanged += ActiveContentId_ValueChanged;

        //For late joiners
        if (!IsServer && _activeContentId.Value != -1)
        {
            ContentMetadata metadata = AvailableContent?.Find(c => c.id == _activeContentId.Value);
            if (metadata != null) 
            {
                ImportContent(metadata);
            } 
            else
            {
                StartCoroutine(WaitForMetadataThenImport(_activeContentId.Value));
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        _isLoading.OnValueChanged -= IsLoading_ValueChanged;
        _activeContentId.OnValueChanged -= ActiveContentId_ValueChanged;

        if (ActiveContent != null)
        {
            ClearContent();
        }
    }

    private async void Start()
    {
        if (IsServer) _isLoading.Value = true;

        AvailableContent = (await WebRequestUtils.FetchJson<List<ContentMetadata>>(_metadataUri))
            .Where(c => !c.items.Any(item => !allowedContent.HasFlag(item.type)))
            .ToList();
        Debug.Log("Runtime import metadata found. Count: " + AvailableContent.Count);
        ContentFound?.Invoke(AvailableContent);

        if (IsServer) _isLoading.Value = false;
    }
    #endregion

    #region Methods
    public void CallImportContent(ContentMetadata metadata)
    {
        if (!IsServer)
        {
            RequestImportServerRpc(metadata.id);
        } 
        else
        {
            ServerBeginImport(metadata);
        }
    }

    private void ServerBeginImport(ContentMetadata metadata)
    {
        _importCount = 0;
        _isLoading.Value = true;
        _activeContentId.Value = metadata.id;
        ImportContent(metadata);
    }

    private void ImportContentById(int id)
    {
        if (AvailableContent == null) { 
            Debug.LogError("Available content not loaded");
            return;
        }

        ContentMetadata metadata = AvailableContent.Find(c => c.id == id);
        if (metadata != null)
        {
            ImportContent(metadata);
        }
        else
        {
            Debug.LogError($"Content with id {id} not found.");
        }
    }

    private async void ImportContent(ContentMetadata metadata)
    {
        try
        {
            await Task.WhenAll(metadata.items.Select(item => 
                _importers[item.type].ImportFromWeb(item.url)
            ));

            if (IsServer) 
            { 
                _importCount++;
                while (_importCount < NetworkManager.Singleton.ConnectedClientsList.Count) {
                    await Task.Yield();
                }

                ServerStartContent(metadata);
            }
            else
            {
                NotifyImportCompleteServerRpc();
            }
        } 
        catch (Exception e)
        {
            Debug.LogError($"Failed to import {metadata.name}: {e.Message}");
            if (IsServer) _activeContentId.Value = -1;
        }

        if (IsServer) _isLoading.Value = false;
    }

    private void ServerStartContent(ContentMetadata metadata)
    {
        if (!IsServer) throw new Exception("Non-server called ServerStartContent");

        StartContentClientRpc(metadata.id);
        StartContent(metadata);
    }

    private void StartContent(ContentMetadata metadata)
    {
        metadata.items.ForEach(item => _importers[item.type].StartContent());

        ContentInstantiated?.Invoke(metadata);
        ActiveContent = metadata;
    }

    public void CallClearContent() //Called from Selected UI "Return" Button
    {
        if (!IsServer)
        {
            RequestClearContentServerRpc();
        } 
        else
        {
            NetworkedClearContent();
        }
    }

    private void NetworkedClearContent()
    {
        if (ActiveContent == null)
        {
            Debug.LogError("ActiveContent is null despite clear call");
            return;
        }

        int clearedContentId = ActiveContent.id;

        ClearContent();
        ContentCleared?.Invoke();

        ClearContentClientRpc(clearedContentId);

        _activeContentId.Value = -1;
    }

    private void ClearContent()
    {
        foreach (var item in ActiveContent.items)
        {
            _importers[item.type].ClearContent();
        }

        ActiveContent = null;
    }
    #endregion

    #region Delegates
    private void IsLoading_ValueChanged(bool previousValue, bool newValue)
    {
        LoadingStatusChanged?.Invoke(newValue);
    }

    private void ActiveContentId_ValueChanged(int previousValue, int newValue)
    {
        if (!IsServer && newValue != -1)
        {
            if (AvailableContent == null)
            {
                StartCoroutine(WaitForMetadataThenImport(newValue));
            }
            else
            {
                ImportContentById(newValue);
            }
        }
    }
    #endregion

    #region Coroutines
    private IEnumerator WaitForMetadataThenImport(int id)
    {
        while (AvailableContent == null)
        {
            yield return new WaitForEndOfFrame();
        }

        ImportContentById(id);
    }
    #endregion

    #region RPCs
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestImportServerRpc(int id, RpcParams rpcParams = default)
    {
        if (_isLoading.Value || _activeContentId.Value != -1)
        {
            Debug.LogWarning("Cannot import: content is already loading or active");
            return;
        }

        ContentMetadata metadata = AvailableContent.Find(c => c.id == id);
        if (metadata != null)
        {
            ServerBeginImport(metadata);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void NotifyImportCompleteServerRpc()
    {
        _importCount++;
    }

    [Rpc(SendTo.NotServer)]
    private void StartContentClientRpc(int id)
    {
        StartContent(AvailableContent?.Find(c => c.id == id));
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestClearContentServerRpc(RpcParams rpcParams = default)
    {
        if (_isLoading.Value || _activeContentId.Value == -1)
        {
            Debug.LogWarning("Cannot clear: content is loading or already cleared");
            return;
        }

        NetworkedClearContent();
    }

    [Rpc(SendTo.NotServer)]
    private void ClearContentClientRpc(int id)
    {
        if (ActiveContent?.id == id)
        {
            ClearContent();
            ContentCleared?.Invoke();
        }
    }
    #endregion
}
