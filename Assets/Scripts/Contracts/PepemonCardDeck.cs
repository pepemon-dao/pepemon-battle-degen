using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;

public class PepemonCardDeck
{
    public class SupportCardRequest
    {
        [Parameter("uint256", "supportCardId", 1)]
        public virtual BigInteger SupportCardId { get; set; }
        [Parameter("uint256", "amount", 2)]
        public virtual BigInteger Amount { get; set; }
    }

    /// <summary>
    /// PepemonCardDeck Contract Address
    /// </summary>
    private static string Address => Web3Controller.instance.GetChainConfig().pepemonCardDeckAddress;
    private static string Abi => Web3Controller.instance.GetChainConfig().pepemonCardDeckAbi;
    private static Contract contract => ThirdwebManager.Instance.SDK.GetContract(Address, Abi);

    public static async Task<ulong> GetBattleCard(ulong deckId)
    {
        var response = await contract.Read<ulong>("getBattleCardInDeck", deckId);
        return response;
    }

    public static IEnumerator GetBattleCard(ulong deckId, Action<ulong> callback)
    {
        var request = contract.Read<ulong>("getBattleCardInDeck", deckId);

        while (!request.IsCompleted)
        {
            
            yield return null;
        }

        if (request.IsFaulted)
        {
            Debug.LogError("Error fetching battle card: " + request.Exception);
            callback?.Invoke(0); // Assuming 0 is a default error state for battleCard
            yield break;
        }

        callback?.Invoke(request.Result);
    }


    public static async Task<IDictionary<ulong, int>> GetAllSupportCards(ulong deckId)
    {
        var response = await contract.Read<List<ulong>>("getAllSupportCardsInDeck", deckId);
        var result = new OrderedDictionary<ulong, int>();
        foreach (var card in response)
        {
            result[card] = result.ContainsKey(card) ? result[card] + 1 : 1;
        }
        return result;
    }

    public static IEnumerator GetAllSupportCards(ulong deckId, Action<IDictionary<ulong, int>> callback)
    {
        var request = contract.Read<List<ulong>>("getAllSupportCardsInDeck", deckId);
        var result = new OrderedDictionary<ulong, int>();

        while (!request.IsCompleted)
        {
            yield return null;
        }

        if (request.IsFaulted)
        {
            Debug.LogError("Error fetching support cards: " + request.Exception);
            callback?.Invoke(null);
            yield break;
        }

        foreach (var card in request.Result)
        {
            result[card] = result.ContainsKey(card) ? result[card] + 1 : 1;
        }

        callback?.Invoke(result);
    }


    public static async Task<List<ulong>> GetPlayerDecks(string address)
    {
        var deckCount = await contract.Read<ulong>("getDeckCount", address);
        List<ulong> deckIds = new List<ulong>();
        for (ulong i = 0; i < deckCount; i++)
        {
            deckIds.Add(await contract.Read<ulong>("playerToDecks", address, i));
        }
        return deckIds;
    }

    public static async Task<bool> GetApprovalState(string operatorAddress)
    {
        // cant use contract.ERC1155.IsApprovedForAll because it fails in WebGL
        var playerWalletAddress = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();
        if (string.IsNullOrEmpty(playerWalletAddress))
        {
            Debug.LogWarning("Unable to call 'isApprovedForAll': Player wallet was not set");
            return false;
        }
        return await contract.Read<bool>("isApprovedForAll", playerWalletAddress, operatorAddress);
    }


    public static async Task SetApprovalState(bool approved, string operatorAddress)
    {
        // cant use contract.ERC1155.SetApprovalForAll because it fails in WebGL
        await contract.Write("setApprovalForAll", operatorAddress, approved);
    }

    public static async Task MintCards()
    {
        await contract.Write("mintCards");
    }

    public static async Task CreateDeck()
    {
        // note: deck is created using the sender's address
        await contract.Write("createDeck");
    }

    public static async Task SetBattleCard(ulong deckId, ulong battleCardId)
    {
        await contract.Write("addBattleCardToDeck", deckId, battleCardId);
    }

    public static async Task RemoveBattleCard(ulong deckId)
    {
        await contract.Write("removeBattleCardFromDeck", deckId);
    }

    /// <summary>
    /// Adds support card to deck. Requires prior Approval
    /// </summary>
    public static async Task AddSupportCards(ulong deckId, params SupportCardRequest[] requests)
    {
        // TODO: Check if max support cards will be reached
        // TODO: Check if the player has the cards
        if (Utils.IsWebGLBuild())
        {
            // This fixes an annoying error "Error: invalid BigNumber value" in the webgl build
            await contract.Write("addSupportCardsToDeck", deckId, requests.Select((a) => new List<object>()
            {
                a.SupportCardId,
                a.Amount
            }));
        }
        else
        {
            await contract.Write("addSupportCardsToDeck", deckId, requests);
        }
    }

    /// <summary>
    /// Removes support card from deck. Requires prior Approval
    /// </summary>
    public static async Task RemoveSupportCards(ulong deckId, params SupportCardRequest[] requests)
    {
        // TODO: Check if the deck has requested cards before removing
        if (Utils.IsWebGLBuild())
        {
            // This fixes an annoying error "Error: invalid BigNumber value" in the webgl build
            await contract.Write("removeSupportCardsFromDeck", deckId, requests.Select((a) => new List<object>()
            {
                a.SupportCardId,
                a.Amount
            }));
        }
        else
        {
            await contract.Write("removeSupportCardsFromDeck", deckId, requests);
        }
    }
}
