using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using TMPro;
using Pepemon.Battle;
using DG.Tweening;

[System.Serializable]
public class PepemonTypeData
{
    public PepemonType type;
    public Sprite cardBackImage;
    public Sprite footerImage;
    public Sprite iconImage;
}

public class PepemonCardController : MonoBehaviour
{
    [BoxGroup("Images"), SerializeField] private Image _backDropImage;
    [BoxGroup("Images"), SerializeField] private Image _iconImage;
    [BoxGroup("Images"), SerializeField] private Image _footerImage;
    [BoxGroup("Images"), SerializeField] private Image _cardContentBackdrop;
    [BoxGroup("Images"), SerializeField] private Image _cardContent;
    [BoxGroup("Images"), SerializeField] private Image _cardGlow;

    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _nameText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _hpText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _levelText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _atkText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _sAtkText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _defText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _sDefText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _spdText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _intText;

    [SerializeField] private List<PepemonTypeData> typeDatas = new List<PepemonTypeData>();

    public BattleCard BattleCard { get; private set; }

    public void PopulateCard(BattleCard pepemonData)
    {
        BattleCard = pepemonData;

        _nameText.text = pepemonData.name;
        _hpText.text = pepemonData.HealthPoints.ToString();
        _levelText.text = pepemonData.Level;
        _atkText.text = pepemonData.Attack.ToString();
        _sAtkText.text = pepemonData.SAttack.ToString();
        _defText.text = pepemonData.Defense.ToString();
        _sDefText.text = pepemonData.SDeffense.ToString();
        _spdText.text = pepemonData.Speed.ToString();
        _intText.text = pepemonData.Intelligence.ToString();

        /*
        typeDatas.ForEach((tD) =>
        {
            if (tD.type == pepemonData.Type)
            {
                _backDropImage.sprite = tD.cardBackImage;
                _footerImage.sprite = tD.footerImage;
                _iconImage.sprite = tD.iconImage;
            }
        });
        */

        if (pepemonData.CardContentBackdrop != null)
        {
            //_cardContentBackdrop.sprite = pepemonData.CardContentBackdrop;
            _backDropImage.sprite = pepemonData.CardContentBackdrop;
        }
        if (pepemonData.CardContent != null)
        {
            _cardContent.sprite = pepemonData.CardContent;
        }   
    }

    public void UpdateCard(Player player)
    {
        _hpText.text = player.CurrentHP.ToString();
    }

    public void ActivateCard(bool isAttacker)
    {
        if (BotTextTutorial.Instance.IsInTutorial)
            return;

        Color color = isAttacker ? Color.red : Color.cyan;

        _cardGlow.color = color;

        _cardGlow.DOFade(0, 3f); 
    }

    public void DeActivateCard()
    {
        _cardGlow.DOFade(0, 2f);
    }
}
