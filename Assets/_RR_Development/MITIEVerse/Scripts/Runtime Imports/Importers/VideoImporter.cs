using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

public class VideoImporter : IRuntimeImporter
{
    private readonly VideoPlayer _player;
    private readonly GameObject _screen;

    public VideoImporter(VideoPlayer player, GameObject screen) 
    {
        _player = player;
        _screen = screen;
    }

    public async Task ImportFromWeb(string uri)
    {
        _player.url = uri;
        _player.Prepare();

        while(!_player.isPrepared)
        {
            await Task.Yield();
        }
    }

    public void StartContent()
    {
        _screen.SetActive(true);
        _player.Play();
    }

    public void ClearContent()
    {
        _screen.SetActive(false);
        _player.Stop();
        _player.url = null;
    }
}
