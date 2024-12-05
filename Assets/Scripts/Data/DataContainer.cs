using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pepemon.Battle
{
    [CreateAssetMenu(menuName = "Containers/Cards data container")]
    public class DataContainer : ScriptableObject
    {
        // should contain all pepemons/cards scriptable objects currently in the game
        public List<BattleCard> Pepemons;
        public List<Card> Cards;

        // returns all cards (repeating them when necessary)
        public IEnumerable<Card> GetAllCardsByIds(IDictionary<ulong, int> cards)
        {
            foreach (var supportCard in GetCardsTypesByIds(cards.Keys.ToList()))
            {
                if (supportCard != null)
                {
                    if (!cards.TryGetValue((ulong)supportCard.ID, out var copies))
                    {
                        Debug.LogWarning("Card ID not found: " + supportCard.ID);
                        continue;
                    }
                    foreach (var card in Enumerable.Repeat(supportCard, copies))
                    {
                        yield return card;
                    }
                }
            }
        }

        public Card GetCardById(ulong cardId)
        {
            // Call GetCardsTypesByIds but only pass the single cardId in a list
            var card = GetCardsTypesByIds(new List<ulong> { cardId }).FirstOrDefault();

            if (card == null)
            {
                Debug.LogWarning("Card ID not found: " + cardId);
            }

            return card;
        }

        public IEnumerable<Card> GetCardsTypesByIds(List<ulong> ids)
        {
            foreach (var id in ids)
            {
                yield return Cards.FirstOrDefault(card => (ulong)card.ID == id);
            }
        }

        public BattleCard GetPepemonById(string id)
        {
            return Pepemons.FirstOrDefault(pepemon => pepemon.ID == id);
        }
    }
}
