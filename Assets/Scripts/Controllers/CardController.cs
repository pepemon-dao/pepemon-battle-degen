using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
using Pepemon.Battle;

// Handles the instance of a card within a session
public class CardController : MonoBehaviour
{
    [BoxGroup("Card Components"), SerializeField] private TextMeshProUGUI _cardDisplayName;
    [BoxGroup("Card Components"), SerializeField] private TextMeshProUGUI _cardDescription;
    [BoxGroup("Card Components"), SerializeField] private Image _cardFrameImage;
    [BoxGroup("Card Components"), SerializeField] private Image _cardBackgroundImage;
    [BoxGroup("Card Components"), SerializeField] private Image _cardStatImage;
    [BoxGroup("Card Components"), SerializeField] private Image _gemImage;
    [BoxGroup("Card Components"), SerializeField] private CanvasGroup _cardGlow;
    [BoxGroup("Card Components"), SerializeField] private Image glow;

    [BoxGroup("Card Backdrops"), SerializeField] private Sprite _defenceCardFrame;
    [BoxGroup("Card Backdrops"), SerializeField] private Sprite _specialDefenceCardFrame;
    [BoxGroup("Card Backdrops"), SerializeField] private Sprite _attackCardFrame;
    [BoxGroup("Card Backdrops"), SerializeField] private Sprite _specialAttackCardFrame;

    [BoxGroup("Card Gems"), SerializeField] private Sprite _commonGem;
    [BoxGroup("Card Gems"), SerializeField] private Sprite _rareGem;
    [BoxGroup("Card Gems"), SerializeField] private Sprite _epicGem;

    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _atkBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _defBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _intBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _rAtkBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _rDefBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _spclBG;
    [BoxGroup("Card Backgrounds"), SerializeField] private Sprite _speedBG;

    [ReadOnly] public Card HostedCard;

    private Transform _targetPostion;        //the target transform in the layout group this card will lerp to
    private Vector3 _startingTargetPosition = Vector3.zero;  //the position to return to after being highlighted

    private Vector3 _targetScale;
    private Vector3 _startingScale;

    public void PopulateCard(Card card)
    {
        HostedCard = card;
        //_cardDisplayName.text = HostedCard.DisplayName;
        //_cardDescription.text = HostedCard.CardDescription;

        /*
        switch (card.Type)
        {
            case PlayCardType.Defense:
                _cardFrameImage.sprite = _defenceCardFrame;
                break;
            case PlayCardType.SpecialDefense:
                _cardFrameImage.sprite = _specialDefenceCardFrame;
                break;
            case PlayCardType.Offense:
                _cardFrameImage.sprite = _attackCardFrame;
                break;
            case PlayCardType.SpecialOffense:
                _cardFrameImage.sprite = _specialAttackCardFrame;
                break;
        }

        switch (card.Rarity)
        {
            case CardRarity.Common:
                _gemImage.sprite = _commonGem;
                break;
            case CardRarity.Rare:
                _gemImage.sprite = _rareGem;
                break;
            case CardRarity.Epic:
                _gemImage.sprite = _epicGem;
                break;
        }*/

        switch (card.Type)
        {
            case PlayCardType.Defense:
                glow.color = Color.cyan;
                break;
            case PlayCardType.SpecialDefense:
                glow.color = Color.cyan;
                break;
        }

        _cardFrameImage.sprite = card.CardEffectSprite;

        _startingScale = transform.localScale;
        _targetScale = transform.localScale;
    }

    public void SetTargetTransform(Transform _target)
    {
        _targetPostion = _target;

    }

    /// <summary>
    /// The cards move foward when they are attacking
    /// </summary>
    public void SetAttackingTransform(Vector3 to)
    {
        _startingTargetPosition = _targetPostion.position;

        _targetPostion.position += to;
        _cardGlow.DOFade(1, 2f);
        transform.SetAsLastSibling(); //make sure this card is in front of the bottom cards.

        _targetScale = _startingScale * 1.1f;

        SFXManager.Instance.SlideSFX();
    }

    /// <summary>
    /// return back to the board after being highlighted when attacking/defending
    /// </summary>
    public void ReturnToBaseTransform()
    {
        _cardGlow.DOFade(0, 1f);

        if (_startingTargetPosition != Vector3.zero)
            _targetPostion.position = _startingTargetPosition;

        _targetScale = _startingScale;
    }

    void Update()
    {
        //lerp to target position on UI

        transform.position = Vector3.Lerp(transform.position, _targetPostion.position, 5 * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, 5 * Time.deltaTime);
    }
}