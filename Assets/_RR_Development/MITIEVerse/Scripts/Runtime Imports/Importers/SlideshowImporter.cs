using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.IO.Compression;
using Unity.Netcode;

public class SlideshowImporter : IRuntimeImporter
{
    private readonly Button _prevButton;
    private readonly Button _nextButton;
    private readonly GameObject _screen;
    private readonly GameObject _console;
    private readonly NetworkBehaviour _networkBehaviour;
    private readonly GameObject _stateSynchronizerPrefab;
    private readonly Material _defaultMaterial;

    private List<Texture2D> _slides = new();
    private NetworkedSlideshowState _slideState;

    public SlideshowImporter(Button prevButton, Button nextButton, GameObject screen, GameObject console, NetworkBehaviour networkBehaviour, GameObject stateSynchronizerPrefab) 
    {
        _prevButton = prevButton;
        _nextButton = nextButton;
        _screen = screen;
        _console = console;
        _networkBehaviour = networkBehaviour;
        _stateSynchronizerPrefab = stateSynchronizerPrefab;
        _defaultMaterial = screen.GetComponentInChildren<MeshRenderer>().material;
    }

    public async Task ImportFromWeb(string uri)
    {
        var archiveData = await DownloadSlideArchive(uri);
        var imageData = await ExtractImageDataFromArchive(archiveData);
        foreach (var image in imageData)
        {
            _slides.Add(ExtractTextureFromImageData(image));
        }

        await SetUpStateSynchronizer();
    }

    public void StartContent()
    {
        SetScreenTexture(_slides[0]);
        SetConsoleTexture(_slides[0]);
        _screen.SetActive(true);
        _slideState.SetMaxSlideIndex(_slides.Count-1);
    }

    public void ClearContent()
    {
        _slideState.SlideIndexChanged -= SlideState_SlideIndexChanged;
        _prevButton.onClick.RemoveAllListeners();
        _nextButton.onClick.RemoveAllListeners();

        _screen.SetActive(false);
        ResetScreenTexture();
    }

    private async Task<byte[]> DownloadSlideArchive(string uri)
    {
        return await WebRequestUtils.FetchData(uri);
    }

    private async Task<List<byte[]>> ExtractImageDataFromArchive(byte[] archiveData)
    {
        List<byte[]> imageData = new List<byte[]>();

        await Task.Run(() =>
        {
            using (MemoryStream archiveStream = new MemoryStream(archiveData))
            using (ZipArchive archive = new ZipArchive(archiveStream, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string extension = Path.GetExtension(entry.Name).ToLower();
                    if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                    {
                        using (Stream entryStream = entry.Open())
                        using (MemoryStream imageStream = new MemoryStream())
                        {
                            entryStream.CopyTo(imageStream);

                            imageData.Add(imageStream.ToArray());
                        }
                    }
                }
            }
        });

        return imageData;
    }

    private Texture2D ExtractTextureFromImageData(byte[] imageData)
    {
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageData);

        return texture;
    }

    private async Task SetUpStateSynchronizer()
    {
        if(_networkBehaviour.IsServer)
        {
            var stateSynchronizerGO = GameObject.Instantiate(_stateSynchronizerPrefab);
            _slideState = stateSynchronizerGO.GetComponent<NetworkedSlideshowState>();
            _slideState.NetworkObject.Spawn(true);
        }
        else
        {
            await WaitForFindStateSynchronizer();
        }

        _slideState.SlideIndexChanged += SlideState_SlideIndexChanged;
        _prevButton.onClick.AddListener(PrevButton_OnClick);
        _nextButton.onClick.AddListener(NextButton_OnClick);
    }

    private async Task WaitForFindStateSynchronizer()
    {
        float timeout = 10f;
        float elapsed = 0f;

        while (_slideState == null && elapsed < timeout)
        {
            var synchronizer = GameObject.FindAnyObjectByType<NetworkedSlideshowState>();
            if (synchronizer != null)
            {
                _slideState = synchronizer.GetComponent<NetworkedSlideshowState>();
                break;
            }
            else
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
        }

        if (_slideState == null)
        {
            throw new Exception("Client failed to find slide state synchronizer.");
        }
    }

    private void SetScreenTexture(Texture2D texture)
    {
        MeshRenderer meshRenderer = _screen.GetComponent<MeshRenderer>();
        Material screenMat = meshRenderer.material;
        //Material material = new Material(Shader.Find("Unlit/Texture"));
        screenMat.mainTexture = texture;

        meshRenderer.material = screenMat;
    }
    private void SetConsoleTexture(Texture2D texture)
    {
        MeshRenderer meshRenderer = _console.GetComponent<MeshRenderer>();
        Material consoleMat = meshRenderer.material;
        consoleMat.mainTexture = texture;
        meshRenderer.material = consoleMat;
    }

    private void ResetScreenTexture()
    {
        _screen.GetComponent<MeshRenderer>().material = _defaultMaterial;
    }

    private void SlideState_SlideIndexChanged(int newIndex)
    {
        SetScreenTexture(_slides[newIndex]);
        SetConsoleTexture(_slides[newIndex]);
    }

    private void PrevButton_OnClick()
    {
        _slideState.RequestSlideChange(_slideState.SlideIndex.Value - 1);
    }

    private void NextButton_OnClick()
    {
        _slideState.RequestSlideChange(_slideState.SlideIndex.Value + 1);
    }
}
