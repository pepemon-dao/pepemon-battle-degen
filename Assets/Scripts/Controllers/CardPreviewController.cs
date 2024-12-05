using Pepemon.Battle;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardPreviewController : MonoBehaviour
{
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private GameObject gameCard;
    [SerializeField] private GameObject pepemonCard;

    [Header("Game Card")]
    [SerializeField] private Image _cardImg;

    [Header("Pepemon Card")]
    [BoxGroup("Images"), SerializeField] private Image _backDropImage;
    [BoxGroup("Images"), SerializeField] private Image _cardContent;

    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _nameText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _hpText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _levelText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _atkText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _sAtkText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _defText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _sDefText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _spdText;
    [BoxGroup("Text"), SerializeField] private TextMeshProUGUI _intText;

    private float clickTime;
    private float doubleClickTimeThreshold = 0.2f;

    private GameObject currentObject;
    private GameObject prevObject;

    private bool isInPreview = false;

    private void Update()
    {
        if (isInPreview)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HidePreview();
                clickTime = 0f;
                return;
            }
            else
            {
                return;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time - clickTime < doubleClickTimeThreshold)
            {
                GetObject(out currentObject);
                //check if the objects doesn't changed between the clicks
                if (currentObject == prevObject)
                {
                    SetPreview(currentObject);
                }
            }
            else
            {
                GetObject(out currentObject);
                prevObject = currentObject;
            }
            clickTime = Time.time;
        }

        if (Input.GetMouseButtonDown(1))
        {
            GetObject(out currentObject);
            SetPreview(currentObject);
        }
    }

    private void HidePreview()
    {
        Time.timeScale = 1f;
        isInPreview = false;
        previewPanel.SetActive(false);
        gameCard.SetActive(false);
        pepemonCard.SetActive(false);
    }

    private void GetObject(out GameObject currentObject)
    {
        currentObject = null;

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
        pointerEventData.position = Input.mousePosition;
        List<RaycastResult> raycastResultList = new List<RaycastResult>(); 
        EventSystem.current.RaycastAll(pointerEventData, raycastResultList);
        for (int i = 0; i < raycastResultList.Count; i++)
        {
            var go = raycastResultList[i].gameObject;
            bool pepemonCard = go.GetComponentInParent<PepemonCardController>() != null;
            bool card = go.GetComponent<CardController>() != null;
            bool cardInDeck = go.GetComponentInParent<CardPreview>() != null;

            if (card)
            {
                currentObject = go;
            }
            else if (pepemonCard)
            {
                currentObject = go.transform.parent.gameObject;
            }
            else if (cardInDeck)
            {
                currentObject = go;
            }
        }
    }

    private void SetPreview(GameObject cardGo)
    {
        if (cardGo == null)
        {
            return;
        }

        isInPreview = true;
        PepemonCardController cardPepemon = cardGo.GetComponentInParent<PepemonCardController>();
        CardController card = cardGo.GetComponent<CardController>();
        CardPreview cardInDeck = cardGo.GetComponentInParent<CardPreview>();

        if (card != null)
        {
            SetCardPreview(card);
        } else if (cardPepemon != null)
        {
            SetPepemonCardPreview(cardPepemon);
        } else if (cardInDeck != null)
        {
            SetCardInDeckPreview(cardInDeck);
        }

        previewPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    private void SetPepemonCardPreview(PepemonCardController card)
    {
        PopulateCard(card.BattleCard);
        pepemonCard.SetActive(true);
    }
    
    private void SetCardInDeckPreview(CardPreview card)
    {
        _cardImg.sprite = card._cardImage.sprite;
        gameCard.SetActive(true);
    }
    
    private void SetCardPreview(CardController card)
    {
        _cardImg.sprite = card.HostedCard.CardEffectSprite;
        gameCard.SetActive(true);
    }

    public void PopulateCard(BattleCard pepemonData)
    {
        _nameText.text = pepemonData.name;
        _hpText.text = pepemonData.HealthPoints.ToString();
        _levelText.text = pepemonData.Level;
        _atkText.text = pepemonData.Attack.ToString();
        _sAtkText.text = pepemonData.SAttack.ToString();
        _defText.text = pepemonData.Defense.ToString();
        _sDefText.text = pepemonData.SDeffense.ToString();
        _spdText.text = pepemonData.Speed.ToString();
        _intText.text = pepemonData.Intelligence.ToString();

        if (pepemonData.CardContentBackdrop != null)
        {
            _backDropImage.sprite = pepemonData.CardContentBackdrop;
        }
        if (pepemonData.CardContent != null)
        {
            _cardContent.sprite = pepemonData.CardContent;
        }   
    }
}
