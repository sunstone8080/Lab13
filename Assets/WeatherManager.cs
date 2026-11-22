using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class WeatherManager
{
    private const string xmlApi = "https://api.openweathermap.org/data/2.5/weather?q={0}&mode=xml&appid={1}";

    private readonly string apiKey;

    private static readonly Dictionary<string, Texture2D> imageCache = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, List<Action<Texture2D>>> pending = new Dictionary<string, List<Action<Texture2D>>>();

    public WeatherManager(string apiKey)
    {
        this.apiKey = apiKey;
    }

    private IEnumerator CallAPI(string url, Action<string> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"network problem: {request.error}");
            }
            else if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"response error: {request.responseCode} (401 = API key not active yet or wrong)");
            }
            else
            {
                callback(request.downloadHandler.text);
            }
        }
    }

    public IEnumerator GetWeatherXML(string city, Action<string> callback)
    {
        string url = string.Format(xmlApi, Uri.EscapeDataString(city), apiKey);
        return CallAPI(url, callback);
    }

    public IEnumerator GetWebImage(string url, Action<Texture2D> callback)
    {
        if (imageCache.TryGetValue(url, out Texture2D cached) && cached != null)
        {
            Debug.Log("Image from CACHE: " + url);
            callback(cached);
            yield break;
        }

        if (pending.TryGetValue(url, out List<Action<Texture2D>> waiting))
        {
            waiting.Add(callback);
            yield break;
        }

        pending[url] = new List<Action<Texture2D>> { callback };
        Debug.Log("Image DOWNLOADING: " + url);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            Texture2D tex = null;
            if (request.result == UnityWebRequest.Result.Success)
            {
                tex = DownloadHandlerTexture.GetContent(request);
                imageCache[url] = tex;
            }
            else
            {
                Debug.LogError($"image download failed ({url}): {request.error}");
            }

            List<Action<Texture2D>> callbacks = pending[url];
            pending.Remove(url);
            if (tex != null)
            {
                foreach (Action<Texture2D> cb in callbacks) cb(tex);
            }
        }
    }
}
