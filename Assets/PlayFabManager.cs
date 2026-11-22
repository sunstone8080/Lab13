using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance;
    private bool loggedIn;

    void Awake() { Instance = this; }

    void Start()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request,
            result => { loggedIn = true; Debug.Log("PlayFab login success"); },
            error => Debug.LogError(error.GenerateErrorReport()));
    }

    public void LogCityChange(string city)
    {
        if (!loggedIn) return;
        var request = new WriteClientPlayerEventRequest
        {
            EventName = "city_selected",
            Body = new Dictionary<string, object> { { "city", city } }
        };
        PlayFabClientAPI.WritePlayerEvent(request,
            result => Debug.Log("Event sent: " + city),
            error => Debug.LogError(error.GenerateErrorReport()));
    }
}