using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    [SerializeField] private string url = "https://pepemon.world";

    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}
