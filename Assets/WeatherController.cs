using System;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
    [Header("API (clear this before committing to GitHub!)")]
    [SerializeField] private string apiKey = "";

    [Header("Scene")]
    [SerializeField] private Light sun;

    [Header("Skybox materials from Fantasy Skybox FREE")]
    [SerializeField] private Material daySky;
    [SerializeField] private Material nightSky;
    [SerializeField] private Material sunnySky;
    [SerializeField] private Material sunlessSky;
    [SerializeField] private Material rainySky;
    [SerializeField] private Material snowySky;
    [SerializeField] private Material sunriseSky;
    [SerializeField] private Material sunsetSky;

    [Header("Cities (different time zones and temperatures)")]
    [SerializeField] private string[] cities = { "Orlando,us", "Reykjavik,is", "Dubai,ae", "Tokyo,jp", "Sydney,au" };

    private WeatherManager weather;
    private string status = "Loading...";

    private void Start()
    {
        weather = new WeatherManager(apiKey);
        SetCity(cities[0]);
    }

    public void SetCity(string city)
    {
        StopAllCoroutines(); 
        status = "Loading " + city + "...";
        StartCoroutine(weather.GetWeatherXML(city, OnXMLDataLoaded));

      
        if (PlayFabManager.Instance != null) PlayFabManager.Instance.LogCityChange(city);
    }

    private void OnXMLDataLoaded(string data)
    {
        Debug.Log(data);
        try
        {
            XElement root = XDocument.Parse(data).Root;
            XElement cityEl = root.Element("city");
            XElement sunEl = cityEl.Element("sun");

            string cityName = (string)cityEl.Attribute("name");
            int tzSeconds = (int?)cityEl.Element("timezone") ?? 0;
            DateTime rise = ParseUtc((string)sunEl.Attribute("rise"));
            DateTime set = ParseUtc((string)sunEl.Attribute("set"));

            float tempC = (float)root.Element("temperature").Attribute("value") - 273.15f;
            float clouds = (float)root.Element("clouds").Attribute("value");
            int code = (int)root.Element("weather").Attribute("number");
            string description = (string)root.Element("weather").Attribute("value");

            DateTime cityLocal = DateTime.UtcNow.AddSeconds(tzSeconds);
            ApplyWeather(code, clouds, rise, set, cityLocal);

            status = $"{cityName}\n{tempC:0.#} °C, {description}\nLocal time: {cityLocal:HH:mm}";
        }
        catch (Exception e)
        {
            status = "Could not parse weather data";
            Debug.LogError("XML parse error: " + e.Message);
        }
    }

    private static DateTime ParseUtc(string s)
    {
        return DateTime.Parse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

   
    private static double MinutesBetween(TimeSpan a, TimeSpan b)
    {
        double d = Math.Abs((a - b).TotalMinutes);
        return Math.Min(d, 1440.0 - d);
    }

    private void ApplyWeather(int code, float clouds, DateTime rise, DateTime set, DateTime cityLocal)
    {
       
        TimeSpan now = DateTime.UtcNow.TimeOfDay;
        TimeSpan r = rise.TimeOfDay;
        TimeSpan s = set.TimeOfDay;
        bool isDay = r < s ? (now >= r && now <= s) : (now >= r || now <= s);
        bool nearRise = MinutesBetween(now, r) < 45;
        bool nearSet = MinutesBetween(now, s) < 45;

        bool rain = code >= 200 && code < 600;  
        bool snow = code >= 600 && code < 700;

        
        Material sky;
        if (!isDay && !nearRise && !nearSet) sky = nightSky;
        else if (rain) sky = rainySky;
        else if (snow) sky = snowySky;
        else if (nearRise) sky = sunriseSky;
        else if (nearSet) sky = sunsetSky;
        else if (code == 800 || code == 801) sky = sunnySky;
        else if (code >= 700 || code >= 803) sky = sunlessSky; 
        else sky = daySky;

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();

      
        float cloudFactor = 1f - (clouds / 100f) * 0.6f;
        float intensity;
        Color color;

        if (!isDay && !nearRise && !nearSet)
        {
            intensity = 0.1f;
            color = new Color(0.4f, 0.5f, 0.9f);
        }
        else if (nearRise || nearSet)
        {
            intensity = 0.8f * cloudFactor;
            color = new Color(1f, 0.55f, 0.3f);
        }
        else
        {
            intensity = 1.2f * cloudFactor;
            color = new Color(1f, 0.96f, 0.88f);
        }

        if (rain || snow)
        {
            intensity *= 0.7f;
            color = Color.Lerp(color, Color.gray, 0.4f);
        }

        if (sun != null)
        {
            sun.intensity = intensity;
            sun.color = color;

            
            float hour = cityLocal.Hour + cityLocal.Minute / 60f;
            sun.transform.rotation = Quaternion.Euler(hour / 24f * 360f - 90f, 170f, 0f);
        }
    }

    private void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        GUI.skin.button.fontSize = 20;

        GUILayout.BeginArea(new Rect(15, 15, 360, 600));
        foreach (string city in cities)
        {
            if (GUILayout.Button(city, GUILayout.Height(40))) SetCity(city);
        }
        GUILayout.Space(10);
        GUILayout.Label(status);
        GUILayout.EndArea();
    }
}
