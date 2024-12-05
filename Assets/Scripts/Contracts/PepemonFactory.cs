using Contracts.PepemonFactory.abi.ContractDefinition;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;

class PepemonFactory
{
    /// <summary>
    /// PepemonFactory address (Support cards, Battle cards)
    /// </summary>
    private static string Address => Web3Controller.instance.GetChainConfig().pepemonFactoryAddress;
    private static string Abi => Web3Controller.instance.GetChainConfig().pepemonFactoryAbi;
    private static Contract contract => ThirdwebManager.Instance.SDK.GetContract(Address, Abi);

    /// <summary>
    /// Gets the stats of multiple battle cards
    /// </summary>
    /// <returns></returns>
    public static async UniTask<List<BattleCardStats>> BatchGetBattleCardStats(ulong minId, ulong maxId)
    {
        if (Utils.IsWebGLBuild())
        {
            var response = await contract.Read<object[][]>("batchGetBattleCardStats", minId, maxId);
            return BatchGetBattleCardStatsOutputDTO.FromObject(response).ReturnValue1;
        }
        var result = await contract.ReadRaw<BatchGetBattleCardStatsOutputDTO>("batchGetBattleCardStats", minId, maxId);
        return result.ReturnValue1;
    }

    /// <summary>
    /// Gets the stats of multiple support cards
    /// </summary>
    /// <returns></returns>
    public static async UniTask<List<SupportCardStats>> BatchGetSupportCardStats(ulong minId, ulong maxId)
    {
        if (Utils.IsWebGLBuild())
        {
            var response = await contract.Read<object[][]>("batchGetSupportCardStats", minId, maxId);
            return BatchGetSupportCardStatsOutputDTO.FromObject(response).ReturnValue1;
        }
        var result = await contract.ReadRaw<BatchGetSupportCardStatsOutputDTO>("batchGetSupportCardStats", minId, maxId);
        return result.ReturnValue1;
    }

    public static async UniTask<ulong> GetLastCardId()
    {
        return await contract.Read<ulong>("getLastTokenID");
    }

    /// <summary>
    /// Returns a list of owned cards by checking the owner of a list of card IDs.
    /// </summary>
    /// <param name="address">User address</param>
    /// <param name="tokenIds">Card IDs to be checked</param>
    /// <returns>List of IDs of owned cards</returns>
    public static async Task<Dictionary<ulong, int>> GetOwnedCards(string address, List<ulong> tokenIds)
    {
        var response = await contract.Read<List<ulong>>("balanceOfBatch", Enumerable.Repeat(address, tokenIds.Count).ToList(), tokenIds);
        var ownedCards = new Dictionary<ulong, int>();
        for (int i = 0; i < (response?.Count ?? 0); i++)
            if (response[i] > 0)
                ownedCards[tokenIds[i]] = (int)response[i];

        return ownedCards;
    }

    public static IEnumerator GetOwnedCards(string address, List<ulong> tokenIds, Action<Dictionary<ulong, int>> callback)
    {
        Dictionary<ulong, int> ownedCards = new Dictionary<ulong, int>();
        var request = contract.Read<List<ulong>>("balanceOfBatch", Enumerable.Repeat(address, tokenIds.Count).ToList(), tokenIds);

        // Wait until the request completes
        while (!request.IsCompleted)
        {
            yield return null;
        }

        if (request.IsFaulted)
        {
            Debug.LogError("Error fetching owned cards: " + request.Exception);
            callback?.Invoke(null);
            yield break;
        }

        // Populate the owned cards dictionary
        for (int i = 0; i < (request.Result?.Count ?? 0); i++)
        {
            if (request.Result[i] > 0)
            {
                ownedCards[tokenIds[i]] = (int)request.Result[i];
            }
        }

        callback?.Invoke(ownedCards);
    }


    public static async Task<BigInteger> GetTokenSupply(ulong tokenId)
    {
        return await contract.Read<BigInteger>("totalSupply", tokenId);
    }

    /// <summary>
    /// Tells whether or not a contract/wallet can transfer NFTs
    /// </summary>
    /// <param name="operatorAddress">Address of the contract/wallet</param>
    /// <returns>result of IsApprovedForAll</returns>
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

    /// <summary>
    /// Approval is necessary to prevent this error in some cases: ERC1155#safeTransferFrom: INVALID_OPERATOR
    /// </summary>
    /// <param name="approved">Approval state to allow moving cards</param>
    /// <param name="operatorAddress">Contract/wallet which will be given/revoked permission to transfer the NFTs</param>
    /// <returns>Transaction hash</returns>
    public static async Task SetApprovalState(bool approved, string operatorAddress)
    {
        // cant use contract.ERC1155.SetApprovalForAll because it fails in WebGL
        await contract.Write("setApprovalForAll", operatorAddress, approved);
    }
}
