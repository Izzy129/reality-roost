using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

public class ModelImporter : IRuntimeImporter
{
    private readonly float _targetScale = 0.4f;
    private readonly Transform _spawnPoint;
    private readonly NetworkBehaviour _networkBehaviour;
    private readonly GameObject _syncProxyPrefab;

    private GameObject _instantiatedModel;
    private GameObject _syncProxy;

    public ModelImporter(Transform spawnPoint, NetworkBehaviour networkBehaviour, GameObject syncProxyPrefab)
    {
        _spawnPoint = spawnPoint;
        _networkBehaviour = networkBehaviour;
        _syncProxyPrefab = syncProxyPrefab;
    }

    public async Task ImportFromWeb(string uri)
    {
        string filename = Path.GetFileName(uri);

        var importOpt = new ImportOptions();
        importOpt.DataLoader = new UnityWebRequestLoader(uri);
        var import = new GLTFSceneImporter(filename, importOpt);
        await import.LoadSceneAsync();

        _instantiatedModel = import.CreatedObject;
        ResizeModel(_instantiatedModel);
        _instantiatedModel.SetActive(false);

        await SetupSyncProxy();
    }

    public async Task SetupSyncProxy()
    {
        if (_networkBehaviour.IsServer)
        {
            _syncProxy = GameObject.Instantiate(_syncProxyPrefab, _spawnPoint.position, _spawnPoint.rotation);

            NetworkObject netObj = _syncProxy.GetComponent<NetworkObject>();
            netObj.Spawn(true);
        }
        else
        {
            await WaitForProxyAndAttach();
        }
    }

    public void StartContent()
    {
        _instantiatedModel.transform.SetParent(_syncProxy.transform, false);
        _instantiatedModel.SetActive(true);
    }

    private async Task WaitForProxyAndAttach()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (_syncProxy == null && elapsed < timeout)
        {
            var networkModels = GameObject.FindObjectsByType<NetworkedModelGrab>(FindObjectsSortMode.None);
            if (networkModels.Length > 0 && networkModels[0].gameObject.transform.childCount == 0)
            {
                _syncProxy = networkModels[0].gameObject;
                break;
            }
            

            if (_syncProxy == null)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
        }

        if (_syncProxy == null)
        {
            throw new Exception("Client failed to find sync proxy.");
        }
    }

    public void ClearContent()
    {
        if (_instantiatedModel != null)
        {
            GameObject.Destroy(_instantiatedModel);
            _instantiatedModel = null;
        }

        if (_syncProxy != null)
        {
            if (_networkBehaviour.IsServer)
            {
                NetworkObject netObj = _syncProxy.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
            }

            GameObject.Destroy(_syncProxy);
            _syncProxy = null;
        }
    }

    private void ResizeModel(GameObject go)
    {
        var meshes = go.GetComponentsInChildren<MeshRenderer>();

        Bounds combinedBounds = meshes[0].bounds;
        for (int i = 1; i < meshes.Length; i++)
        {
            combinedBounds.Encapsulate(meshes[i].bounds);
        }

        float maxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        float scaleFactor = _targetScale / maxDimension;

        go.transform.localScale = Vector3.one * scaleFactor;
    }
}