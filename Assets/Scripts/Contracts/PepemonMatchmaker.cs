using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Thirdweb;
using UnityEngine;

public class PepemonMatchmaker
{
    public enum PepemonLeagues
    {
        PvE = 0,
        PvP,
    }

    [Event("BattleFinished")]
    public class BattleFinishedEventDto : IEventDTO
    {
        [Parameter("address", "winner", 1, true)]
        public virtual string Winner { get; set; }
        [Parameter("address", "loser", 2, true)]
        public virtual string Loser { get; set; }
        [Parameter("uint256", "battleId", 3, false)]
        public virtual BigInteger BattleId { get; set; }
    }

    /// <summary>
    /// PepemonMatchmaker address
    /// </summary>
    private static string[] Addresses => Web3Controller.instance.GetChainConfig().pepemonMatchmakerAddresses;
    private static long BattleGasLimit => Web3Controller.instance.GetChainConfig().pepemonGasLimit;
    private static string Abi => Web3Controller.instance.GetChainConfig().pepemonMatchmakerAbi;
    private static Thirdweb.Contract contracts(PepemonLeagues league) => ThirdwebManager.Instance.SDK.GetContract(Addresses[(int)league], Abi);

    public static async Task<string> GetDeckOwner(PepemonLeagues league, ulong deckId)
    {
        return await contracts(league).Read<string>("deckOwner", deckId);
    }

    public static async Task<ulong> GetLeaderboardPlayersCount(PepemonLeagues league)
    {
        return await contracts(league).Read<ulong>("leaderboardPlayersCount");
    }

    public static async Task<List<(string Address, ulong Ranking)>> GetPlayersRankings(
        PepemonLeagues league, ulong count = 10, ulong offset = 0)
    {
        Thirdweb.Contract contract = contracts(league);
        var result = new RankingReturnType();
        var playersRankings = new List<(string, ulong)>();

        // contract.Read works differently in WebGL, it cant deserialize the result into RankingReturnType because its not using reflection
        // to find the class' fields, it tries to use Newtonsoft's Deserializer instead.
        // the return looks like this: {"result":[["0xfef5D85D5113828BF2a74979B4686bB80C9304F4"],["2000"]]}
        // Internally thirdweb has a generic Result class to wrap it (Result<T> where T is set in Contract.Read<T> below), so
        // the '{"result":' bit is automatically included
        if (Utils.IsWebGLBuild()) {
            var list = await contract.Read<string[][]>("getPlayersRankings", count, offset);
            if (list.Count() < 2)
            {
                Debug.LogWarning("GetPlayersRankings: Invalid format");
                return playersRankings;
            }
            try
            {
                result.rankings = list[1].Select(i => BigInteger.Parse(i)).ToList();
                result.addresses = list[0].ToList();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GetPlayersRankings: Unable to parse ranking results: {e.Message}");
            }
        } 
        else 
        {
            result = await contract.Read<RankingReturnType>("getPlayersRankings", count, offset);
        }

        if (result.addresses.Count != result.rankings.Count)
        {
            Debug.LogWarning("GetPlayersRankings: Mismatching array sizes");
        }

        for (int i = 0; i < result.rankings.Count; i++)
        {
            playersRankings.Add((result.addresses[i], ((ulong)result.rankings[i])));
        }
        return playersRankings;
    }


    public static async Task<bool> Enter(PepemonLeagues league, ulong deckId)
    {
        return (await contracts(league).Write("enter", new TransactionRequest
        {
            gasLimit = BattleGasLimit > 0 ? BattleGasLimit.ToString() : null
        },
        deckId)).receipt.status.IsOne;
    }

    public static async Task<bool> Exit(PepemonLeagues league, ulong deckId)
    {
        return (await contracts(league).Write("exit", deckId)).receipt.status.IsOne;
    }

    private class RankingReturnType
    {
        public List<string> addresses;
        public List<BigInteger> rankings;
    }
}
