using System;
using System.Collections.Generic;
using System.Linq;
using Pepemon.Battle;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using static PepemonFactory;

public class DeckDisplay : MonoBehaviour
{
    [TitleGroup("Component References"), SerializeField] GameObject _supportCardPrefab;
    [TitleGroup("Component References"), SerializeField] GameObject _supportCardList;
    [TitleGroup("Component References"), SerializeField] GameObject _battleCardPrefab;
    [TitleGroup("Component References"), SerializeField] GameObject _battleCardList;
    [TitleGroup("Component References"), SerializeField] GameObject _heroCardList;
    [TitleGroup("Component References"), SerializeField] GameObject _myCardsList;
    [TitleGroup("Component References"), SerializeField] GameObject _saveDeckButton;
    [TitleGroup("Component References"), SerializeField] GameObject _saveDeckTip;
    [TitleGroup("Component References"), SerializeField] TMPro.TMP_Text offenseCardsInDeck;
    [TitleGroup("Component References"), SerializeField] TMPro.TMP_Text defenseCardsInDeck;

    private const int MAX_OFFENSE_CARDS = 10;
    private const int MIN_OFFENSE_CARDS = 0;
    private const int MAX_DEFENSE_CARDS = 10;
    private const int MIN_DEFENSE_CARDS = 0;

    private int currentDefenseCardCount = 0;
    private int currentOffenseCardCount = 0;

    private List<CardPreview> _cardPreviews = new List<CardPreview>();

    private Dictionary<ulong, CardMetadata?> metadataLookup = new Dictionary<ulong, CardMetadata?>();

    public static ulong battleCardId = 0;

    public enum CardType
    {
        Battle,
        Support
    }

    public static DeckDisplay Instance { get; private set; }

    private bool loaded = false;

    private void Awake()
    {
        Instance = this;
    }

    public ulong GetSelectedBattleCard()
    {
        return _heroCardList.GetComponentsInChildren<CardPreview>()?.Where(it => !it.GetComponent<Button>().enabled).FirstOrDefault()?.cardId ?? 0;
    }

    public void ClearBattleCardsList()
    {
        foreach (var battlecard in _battleCardList.GetComponentsInChildren<Button>())
        {
            Destroy(battlecard.gameObject);
        }
    }

    public void ClearSupportCardsList()
    {
        foreach (var battlecard in _supportCardList.GetComponentsInChildren<Button>())
        {
            Destroy(battlecard.gameObject);
        }
    }

    public void ClearMyCardsList()
    {
        List<Transform> children = new List<Transform>();

        foreach (Transform child in _myCardsList.transform)
        {
            children.Add(child);
        }
        
        foreach (Transform child in _battleCardList.transform)
        {
            children.Add(child);
        }
        
        foreach (Transform child in _supportCardList.transform)
        {
            children.Add(child);
        }
        
        foreach (Transform child in _heroCardList.transform)
        {
            children.Add(child);
        }

        foreach (Transform child in children)
        {
            Destroy(child.gameObject);
        }

        foreach (var child in _cardPreviews)
        {
            Destroy(child.gameObject);
        }

        _cardPreviews = new List<CardPreview>();

        ResetCurrentCardsInDeckNumbersOnFilterUse();
    }

    public void LoadSelectedCards(ulong selectedBattleCard, IDictionary<ulong, int> selectedSupportCards)
    {
        // Load selected battle card first
        if (selectedBattleCard != 0)
        {
            AddCard(selectedBattleCard, 1, true, isSupportCard: false);
        }

        // Load selected support cards
        foreach (var kvp in selectedSupportCards)
        {
            ulong cardId = kvp.Key;
            int quantity = kvp.Value;
            AddCard(cardId, quantity, true, isSupportCard: true);
        }
    }



    public void LoadAllBattleCards(Dictionary<ulong, int> availableCardIds, ulong selectedBattleCard)
    {
        foreach (var cardId in availableCardIds.Keys)
        {
            if (cardId != selectedBattleCard)
            {
                AddCard(cardId, availableCardIds[cardId], false, isSupportCard: false);
            }
            else
            {
                int count = availableCardIds[cardId] - 1;
                AddCard(cardId, count, false, isSupportCard: false);
            }
        }
    }

    public Dictionary<ulong, int> GetSelectedSupportCards()
    {
        var result = new Dictionary<ulong, int>();
        var supportCards = _supportCardList.GetComponentsInChildren<CardPreview>()
                     .Concat(_battleCardList.GetComponentsInChildren<CardPreview>());
        foreach (var supportCard in supportCards)
        {
            if (!supportCard.GetComponent<Button>().enabled)
            {
                result[supportCard.cardId] = result.ContainsKey(supportCard.cardId) ? result[supportCard.cardId] + 1 : 1;
            }
        }
        return result;
    }

    public void LoadAllSupportCards(Dictionary<ulong, int> availableCardIds, IDictionary<ulong, int> selectedSupportCards)
    {
        // Create a temporary list to track cards that need to be removed
        var cardsToRemove = new List<ulong>();

        // Handle selected cards first
        foreach (var kvp in selectedSupportCards)
        {
            ulong cardId = kvp.Key;
            int selectedAmount = kvp.Value;

            // Deduct the selected amount from available cards
            if (availableCardIds.TryGetValue(cardId, out int availableAmount))
            {
                availableAmount -= selectedAmount;

                // If none left, mark for removal
                if (availableAmount <= 0)
                {
                    cardsToRemove.Add(cardId);
                }
                else
                {
                    availableCardIds[cardId] = availableAmount; // Update the remaining amount
                }
            }
        }

        // Remove cards marked for removal
        foreach (var cardId in cardsToRemove)
        {
            availableCardIds.Remove(cardId);
        }

        // Now load available cards
        foreach (var kvp in availableCardIds)
        {
            AddCard(kvp.Key, kvp.Value, false, true);
        }
    }


    public void SetCardEquip(bool toEquip, ulong cardId, CardType type)
    {
        switch (type)
        {
            case CardType.Battle:
                if (toEquip)
                {
                    EquipCard(cardId, false);
                }
                else
                {
                    UnEquipCard(cardId, false);
                }
                break;
            case CardType.Support:
                if (toEquip)
                {
                    EquipCard(cardId, true);
                }
                else
                {
                    UnEquipCard(cardId, true);
                }
                break;
            default:
                break;
        }
    }

    public void UpdateCardInDeckDisplay(bool firstLoad = false)
    {
        defenseCardsInDeck.text = currentDefenseCardCount.ToString() + "/" + MAX_DEFENSE_CARDS.ToString();
        offenseCardsInDeck.text = currentOffenseCardCount.ToString() + "/" + MAX_OFFENSE_CARDS.ToString();

        defenseCardsInDeck.color = InCardLimit(false) ? Color.white : Color.red;
        offenseCardsInDeck.color = InCardLimit(true) ? Color.white : Color.red;

        bool validDeck = InCardLimit(true) && InCardLimit(false) && battleCardId != 0;
        _saveDeckTip.SetActive(!validDeck);

        if (firstLoad)
        {
            loaded = true;
            _saveDeckButton.GetComponent<Button>().interactable = false; //the deck hasn't changed yet so there is nothing to save
        } else if (loaded)
        {
            _saveDeckButton.GetComponent<Button>().interactable = validDeck; //the deck has been updated
        }
    }

    public void UnloadPreviousDeck()
    {
        loaded = false;
    }

    public void UnEquipCard(ulong cardId, bool isSupportCard)
    {
        RemoveCardFromDeck(cardId, isSupportCard);
        if (!isSupportCard)
        {
            battleCardId = 0;
            HandleBattleCardZero();
        }
    }

    private void HandleBattleCardZero()
    {
        bool validDeck = InCardLimit(true) && InCardLimit(false) && battleCardId != 0;
        if (!validDeck)
        {
            _saveDeckButton.GetComponent<Button>().interactable = false;
        }
        _saveDeckTip.SetActive(!validDeck);
    }
    
    public void EquipCard(ulong cardId, bool isSupportCard)
    {
        if (!isSupportCard)
        {
            if (cardId == battleCardId)
            {
                DeselectPrevBattleCard();
                return;
            }
        }

        if (!isSupportCard)
        {
            DeselectPrevBattleCard();
            RemoveBattleCard();
            battleCardId = cardId;
            HandleBattleCardZero();
        }

        AddCardToDeck(cardId, isSupportCard);
    }

    private void DeselectPrevBattleCard()
    {
        foreach (var item in FindObjectsOfType<SelectionItem>())
        {
            var cardPreview = item.GetComponent<CardPreview>();
            bool isTheMyCardsListAndIsInteractable = cardPreview.GetComponent<Button>().enabled;
            bool isBattleCard = cardPreview.gameObject.name.Contains("Battle");
            if (cardPreview != null && isTheMyCardsListAndIsInteractable && isBattleCard) //&& cardPreview.cardId == battleCardId)
            {
                //item.SetSelected(false);
                cardPreview._checkmark.SetActive(false);
                //return; we cannot return here, because it is allowed currently to posess more pepemon cards than 1
            }
        }
    }


    private void RemoveCardFromDeck(ulong cardId, bool isSupportCard)
    {
        var cardPreviewToRemove = GetWhichCardToRemove(cardId, isSupportCard);

        if (cardPreviewToRemove != null)
        {
            Destroy(cardPreviewToRemove.gameObject);

            _cardPreviews.Remove(cardPreviewToRemove);

            var result = GetIfOffense(isSupportCard, cardId);
            if (result.valid)
            {
                UpdateCardInDeckCount(isSupportCard, result.isOffense, -1);
            }
            
            UpdateCardInDeckDisplay();
        }
    }

    private (bool valid, bool isOffense) GetIfOffense(bool isSupportCard, ulong cardId)
    {
        if (!isSupportCard) return (false, false);

        if (!metadataLookup.TryGetValue(cardId, out var metadata))
        {
            // Metadata not found in lookup, retrieve it
            metadata = PepemonFactoryCardCache.GetMetadata(cardId);
            if (metadata == null)
            {
                Debug.LogWarning("Card metadata not found");
                return (false, false);
            }
            // Add retrieved metadata to the lookup table
            metadataLookup[cardId] = metadata;
        }

        bool isOffense = false;
        bool isDefense = false;

        if (metadata != null && metadata.Value.description != null)
        {
            isOffense = metadata.Value.description.ToLower().Contains("attack")|| metadata.Value.description.ToLower().Contains("offense");
            isDefense = metadata.Value.description.ToLower().Contains("defense");
        }

        Card card = null;
        if (isSupportCard)
        {
            card = ScriptableDataContainerSingleton.Instance.CardsScriptableObjsData.GetCardById(cardId);
        }

        if (card != null)
        {
            isOffense = card.IsAttackingCard();
            isDefense = !isOffense;
        }
        card = ScriptableDataContainerSingleton.Instance.CardsScriptableObjsData.GetCardById(cardId);

        if (card != null)
        {
            isOffense = card.IsAttackingCard();
        }

        return (true, isOffense);
    }

    private void ResetCurrentCardsInDeckNumbersOnFilterUse()
    {
        currentDefenseCardCount = 0;
        currentOffenseCardCount = 0;
    }

    private void UpdateCardInDeckCount(bool isSupportCard, bool isOffense, int value)
    {
        if (!isSupportCard) return;

        if (isOffense)
        {
            currentOffenseCardCount += value;
        }
        else
        {
            currentDefenseCardCount += value;
        }
    }

    private CardPreview GetWhichCardToRemove(ulong cardId, bool isSupportCard)
    {
        foreach (var item in _cardPreviews)
        {
            if (item.cardId == cardId && item.isSupport == isSupportCard)
            {
                return item;
            }
        }

        return null;
    }

    private void RemoveBattleCard()
    {
        var cardPreviewToRemove = _cardPreviews.FirstOrDefault(item => !item.isSupport);

        if (cardPreviewToRemove != null)
        {
            Destroy(cardPreviewToRemove.gameObject);

            _cardPreviews.Remove(cardPreviewToRemove);
        }
    }


    private void AddCardToDeck(ulong cardId, bool isSupportCard)
    {
        if (cardId == 0)
        {
            Debug.LogWarning("Invalid card");
            return;
        }

        // Check if metadata is already in the lookup table
        if (!metadataLookup.TryGetValue(cardId, out var metadata))
        {
            // Metadata not found in lookup, retrieve it
            metadata = PepemonFactoryCardCache.GetMetadata(cardId);
            if (metadata == null)
            {
                Debug.LogWarning("Card metadata not found");
                return;
            }
            // Add retrieved metadata to the lookup table
            metadataLookup[cardId] = metadata;
        }

        bool cardIsSupportCard = metadata.Value.isSupportCard;

        // Skip if the card type does not match
        if (cardIsSupportCard != isSupportCard)
        {
            return;
        }

        // Check offense/defense status

        
        bool isOffense = false;
        bool isDefense = false;

        if (metadata != null && metadata.Value.description != null)
        {
            isOffense = metadata.Value.description.ToLower().Contains("attack") || metadata.Value.description.ToLower().Contains("offense");
            isDefense = metadata.Value.description.ToLower().Contains("defense");
        }

        if (!InCardLimit(isOffense))
        {
            return; //cannot equip card because you reached the limit
        }

        Card card = null;
        if (isSupportCard)
        {
            card = ScriptableDataContainerSingleton.Instance.CardsScriptableObjsData.GetCardById(cardId);
        }
        
        if (card != null)
        {
            isOffense = card.IsAttackingCard();
            isDefense = !isOffense;
        }

        // Validate card type
        if (isOffense && isDefense)
        {
            Debug.LogError("Not a valid card");
            return;
        }

        // Determine prefab and UI list based on card type
        GameObject prefab = isSupportCard ? _supportCardPrefab : _battleCardPrefab;
        GameObject uiList;

        if (isSupportCard)
        {
            uiList = isDefense ? _supportCardList : _battleCardList;
        }
        else
        {
            uiList = _heroCardList;
        }

        // Instantiate the card and set up the preview component
        var cardInstance = Instantiate(prefab, uiList.transform);
        var cardPreviewComponent = cardInstance.GetComponent<CardPreview>();
        cardPreviewComponent.LoadCardData(cardId, isSupportCard);
        cardPreviewComponent.GetComponent<Button>().enabled = false;

        _cardPreviews.Add(cardPreviewComponent);

        UpdateCardInDeckCount(isSupportCard, isOffense, 1);

        UpdateCardInDeckDisplay();
    }


    private void AddCard(ulong cardId, int count, bool isSelected, bool isSupportCard)
    {
        if (cardId == 0)
        {
            Debug.LogWarning("Invalid card");
            return;
        }
        
        // Check if metadata is already in the lookup table
        if (!metadataLookup.TryGetValue(cardId, out var metadata))
        {
            // Metadata not found in lookup, retrieve it
            metadata = PepemonFactoryCardCache.GetMetadata(cardId);
            if (metadata == null)
            {
                Debug.LogWarning("Card metadata not found");
                return;
            }

            // Add retrieved metadata to the lookup table
            metadataLookup[cardId] = metadata;
        }

        bool cardIsSupportCard = metadata.Value.isSupportCard;
        if (cardIsSupportCard != isSupportCard)
        {
            return; // Skip if card type does not match
        }

        bool isOffense = false;
        bool isDefense = false;

        if (metadata != null && metadata.Value.description != null)
        {
            isOffense = metadata.Value.description.ToLower().Contains("attack") || metadata.Value.description.ToLower().Contains("offense");
            isDefense = metadata.Value.description.ToLower().Contains("defense");
        }

        Card card = null;
        if (isSupportCard)
        {
            card = ScriptableDataContainerSingleton.Instance.CardsScriptableObjsData.GetCardById(cardId);
        }
        if (card != null)
        {
            isOffense = card.IsAttackingCard();
            isDefense = !isOffense;
        }

        if (isOffense && isDefense)
        {
            Debug.LogError("not a valid card");
            return;
        }

        GameObject prefab = isSupportCard ? _supportCardPrefab : _battleCardPrefab;
        GameObject uiList = _myCardsList;

        if (isSelected)
        {
            for (int i = 0; i < count; i++)
            {
                AddCardToDeck(cardId, isSupportCard);
            }
        }

        // Check filters and return if the card is filtered out
        if ((isDefense && !FilterController.Instance.IsFilterOn(FilterController.FilterType.Defense)) ||
            (isOffense && !FilterController.Instance.IsFilterOn(FilterController.FilterType.Offense)) ||
            (!isSupportCard && !FilterController.Instance.IsFilterOn(FilterController.FilterType.Pepemon)))
        {
            if (!FilterController.Instance.AreAllFilterOff())
            {
                return; // Filter out the card
            }
        }

        for (int i = 0; i < count; i++)
        {
            var cardInstance = Instantiate(prefab, uiList.transform);
            var cardPreviewComponent = cardInstance.GetComponent<CardPreview>();
            cardPreviewComponent.LoadCardData(cardId, isSupportCard);
            cardPreviewComponent.SetEquip(isSelected);
        }
    }

    private bool InCardLimit(bool isOffense)
    {
        if (isOffense)
        {
            return currentOffenseCardCount <= MAX_OFFENSE_CARDS && currentOffenseCardCount >= MIN_OFFENSE_CARDS;
        }
        else
        {
            return currentDefenseCardCount <= MAX_DEFENSE_CARDS && currentDefenseCardCount >= MIN_DEFENSE_CARDS;
        }
    }

    public bool DeckUpLimitReached(bool isOffense)
    {

        if (isOffense)
        {
            return currentOffenseCardCount < MAX_OFFENSE_CARDS;
        }
        else
        {
            return currentDefenseCardCount < MAX_DEFENSE_CARDS;
        }
    }

}
