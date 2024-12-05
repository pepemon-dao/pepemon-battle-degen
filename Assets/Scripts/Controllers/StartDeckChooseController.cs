using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartDeckChooseController : MonoBehaviour
{
    [SerializeField] private Image currentPepemonImg;
    [SerializeField] private TMP_Text pepemonName;
    [SerializeField] private TMP_Text descDisplay;
    [SerializeField] private Button defaultSetting;

    private void Start()
    {
        defaultSetting.onClick.Invoke();
    }

    public void SetImg(Sprite sprite)
    {
        currentPepemonImg.sprite = sprite;
    }

    public void SetName(string name)
    {
        pepemonName.text = name;
    }

    public void SetDesc(string desc)
    {
        descDisplay.text = desc;
    }
}
