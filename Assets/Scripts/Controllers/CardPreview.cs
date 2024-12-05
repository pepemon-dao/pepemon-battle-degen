using System.Collections;
using System.Collections.Generic;
using Amazon.Lambda.Model;
using Pepemon.Battle;
using Sirenix.OdinInspector;
using Thirdweb;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PepemonFactory;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// MonoBehavior for BattleCardTemplate and SupportCardTemplate
/// </summary>
public class CardPreview : MonoBehaviour
{
    [BoxGroup("Card Components"), SerializeField] public Image _cardImage;
    [BoxGroup("Card Components"), SerializeField] public TMP_Text _text;
    [BoxGroup("Card Components"), SerializeField] public GameObject _checkmark;

    [SerializeField] private GameObject defenseIcon;
    [SerializeField] private GameObject offenseIcon;

    public ulong cardId { get; private set; }
    public bool isSelected { get => transform.parent.GetComponent<SelectionGroup>().selection.Contains(GetComponent<SelectionItem>()); }

    public bool isSupport { get; private set; } = false;

    private bool isEquipped = false;

    private bool isOffense = false;

    private Dictionary<ulong, CardMetadata?> metadataLookup = new Dictionary<ulong, CardMetadata?>();

    public void ToggleSelected()
    {
        // setting SelectionItem.SetSelected directly would mess up the internal state of SelectionGroup
        transform.parent.GetComponent<SelectionGroup>().ToggleSelected(GetComponent<SelectionItem>());
        //GetComponentInParent<SelectionGroup>().ToggleSelected(GetComponent<SelectionItem>());
    }

    public void LoadCardData(ulong cardId, bool isSupport)
    {
        this.cardId = cardId;
        this.isSupport = isSupport;
        

        if (!metadataLookup.TryGetValue(cardId, out var metadata))
        {
            // Metadata not found in lookup, retrieve it
            metadata = PepemonFactoryCardCache.GetMetadata(cardId);
            if (metadata != null)
            {
                metadataLookup[cardId] = metadata;
            }
        }

        if (metadata == null)
        {
            Debug.LogWarning("Unable to locate metadata for card " + cardId);
        }

        bool isOffense = false;
        bool isDefense = false;

        if (metadata != null && metadata.Value.description != null)
        {
            isOffense = metadata.Value.description.ToLower().Contains("attack") || metadata.Value.description.ToLower().Contains("offense");
            isDefense = metadata.Value.description.ToLower().Contains("defense");
        }

        if (isSupport)
        {
            Card card = ScriptableDataContainerSingleton.Instance.CardsScriptableObjsData.GetCardById(cardId);
            if (card != null)
            {
                isOffense = card.IsAttackingCard();
                isDefense = !isOffense;
            }

            this.isOffense = isOffense;
        }
       

        if (defenseIcon != null)
        {
            defenseIcon.SetActive(isDefense);
        }
        if (offenseIcon != null)
        {
            offenseIcon.SetActive(isOffense);
        }


        // Use the singleton to get the texture
        TextureData textureData = CardTextureLookup.Instance.GetTextureData(cardId);

        if (textureData != null)
        {
            _cardImage.sprite = textureData.Sprite;
        }

        _text.text = metadata?.name ?? "Unknown Card";
    }


    public void ToggleEquipped()
    {
        if (!_checkmark.activeSelf && !DeckDisplay.Instance.DeckUpLimitReached(isOffense))
        {
            return; //cannot equip card because you reached the limit
        }

        _checkmark.SetActive(!_checkmark.activeSelf);
        isEquipped = _checkmark.activeSelf;
        SetEquipped(isEquipped);
        _checkmark.SetActive(isEquipped); //set it after too to safeguard it
    }

    public void SetEquip(bool toEquip)
    {
        _checkmark.SetActive(toEquip);
    }

    private void SetEquipped(bool toEquip)
    {
        if (isSupport)
        {
            DeckDisplay.Instance.SetCardEquip(toEquip, cardId, DeckDisplay.CardType.Support);
        }
        else
        {
            DeckDisplay.Instance.SetCardEquip(toEquip, cardId, DeckDisplay.CardType.Battle);
        }
    }
}
