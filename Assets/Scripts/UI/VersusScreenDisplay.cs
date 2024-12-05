using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG;
using DG.Tweening;
using UnityEngine.UI;

public class VersusScreenDisplay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image imgRed;
    [SerializeField] private Image bgRed;
    [SerializeField] private Image imgBlue;
    [SerializeField] private Image bgBlue;
    [SerializeField] private TMPro.TMP_Text nameRed;
    [SerializeField] private TMPro.TMP_Text nameBlue;

    public void SetVersusScreen(Sprite red, Sprite blue, Sprite bgB, Sprite bgR, string blueN, string redN)
    {
        imgRed.sprite = red;
        imgBlue.sprite = blue;
        if (bgR != null)
            bgRed.sprite = bgR;
        if (bgB != null)
            bgBlue.sprite = bgB;
        nameRed.text = redN;
        nameBlue.text = blueN;
        StartCoroutine(Transition());
    }

    private IEnumerator Transition()
    {
        yield return new WaitForSeconds(3.25f);

        canvasGroup.DOFade(0f, 1f);
    }
}
