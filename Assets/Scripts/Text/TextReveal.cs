using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using TMPro;

public class TextReveal : MonoBehaviour
{
    #region Editor Exposed Data
    [Title("Text Settings")]
    [SerializeField] TextMeshProUGUI _tmpFore;
    [SerializeField] TextMeshProUGUI _tmpShadow;

    [Space(10)]

    [SerializeField, PropertyRange(0, 1)] float _alpha;

    [Space(10)]

    [SerializeField] private string _text;
    #endregion

    #region Private Data
    private float _threshold = 0;
    private string _curText;
    #endregion

    #region INIT
    private void Awake()
    {
        _threshold = 0.8f / _text.Length;

        _curText = "";
        _tmpFore.SetText("");

        if (_tmpShadow != null)
            _tmpShadow.SetText("");
    }
    #endregion

    public void SetText(string text)
    {
        _text = text;
        _curText = text;
    }

    #region Text Reveal Logic
    private void Update()
    {
        //_curText = _text.Substring(0, Mathf.Min(Mathf.RoundToInt(_alpha / _threshold), _text.Length));
        _tmpFore.SetText(_curText);
        
        if (_tmpShadow != null)
            _tmpShadow.SetText(_curText);
    }
    #endregion
}
