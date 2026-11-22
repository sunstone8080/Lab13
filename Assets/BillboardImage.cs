using System.Collections;
using UnityEngine;

public class BillboardImage : MonoBehaviour
{
    [SerializeField] private string imageUrl =
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/15/Cat_August_2010-4.jpg/2560px-Cat_August_2010-4.jpg";

    private static readonly WeatherManager manager = new WeatherManager("");

    private IEnumerator Start()
    {
        Renderer rend = GetComponent<Renderer>();
        yield return manager.GetWebImage(imageUrl, tex =>
        {
            if (rend != null) rend.material.mainTexture = tex;
        });
    }
}
