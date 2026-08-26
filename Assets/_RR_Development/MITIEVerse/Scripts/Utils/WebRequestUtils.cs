using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class WebRequestUtils
{
    /// <summary>
    /// Sends a get request to the provided uri and returns the download handler.
    /// </summary>
    /// <param name="uri">The uri for the GET request.</param>
    /// <returns></returns>
    public static async Task<T> SendHttpGetRequest<T>(string uri, Func<DownloadHandler, T> downloadHandler)
    {
        using UnityWebRequest request = UnityWebRequest.Get(uri);
        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Download failed: {request.error}");
            return default;
        }

        return downloadHandler(request.downloadHandler);
    }

    /// <summary>
    /// Fetches JSON from the provided uri using a GET request. Uses Newtonsoft to deserialize the requested JSON into the provided type.
    /// </summary>
    /// <typeparam name="T">The return type for deserialization. Must match the expected JSON.</typeparam>
    /// <param name="uri">The uri for the GET request.</param>
    /// <returns></returns>
    public static async Task<T> FetchJson<T>(string uri)
    {
        string json = await SendHttpGetRequest(uri, dh => dh.text);

        try
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse JSON: {e}");
            return default;
        }
    }

    /// <summary>
    /// Fetches data from the provided uri using a GET request and returns as byte array.
    /// </summary>
    /// <param name="uri">The uri for the GET request.</param>
    /// <returns></returns>
    public static async Task<byte[]> FetchData(string uri)
    {
        return await SendHttpGetRequest(uri, dh => dh.data);
    }
}
