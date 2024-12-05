using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Nethereum.RPC.Eth.DTOs;
using System.Linq;
using System.Numerics;
using Thirdweb;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

class PepemonBattle
{
    // used for getting events in native platforms (non-webgl)
    [Event("BattleCreated")]
    public class BattleCreatedEventDto : IEventDTO
    {
        [Parameter("address", "player1Addr", 1, true)]
        public virtual string Player1Addr { get; set; }
        [Parameter("address", "player2Addr", 2, true)]
        public virtual string Player2Addr { get; set; }
        [Parameter("uint256", "battleId", 3, false)]
        public virtual BigInteger BattleId { get; set; }
        [Parameter("uint256", "p1DeckId", 4, false)]
        public virtual BigInteger Player1Deck { get; set; }
        [Parameter("uint256", "p2DeckId", 5, false)]
        public virtual BigInteger Player2Deck { get; set; }
    }

    private static string Address => Web3Controller.instance.GetChainConfig().pepemonBattleAddress;
    // The latest ABI can be taken from block explorer page at the bottom:
    // https://testnet.ftmscan.com/address/0xBe973123CF4ECC840fbA3aE14DACC6aD80Aaa2A9#code
    private static string Abi => Web3Controller.instance.GetChainConfig().pepemonBattleAbi;
    private static Thirdweb.Contract contract => ThirdwebManager.Instance.SDK.GetContract(Address, Abi);

    /// <summary>
    /// Retrieves the random seed generated during a new Battle.
    /// The "ulong" type cannot be used as a return type because it cannot handle the generated seed's size
    /// </summary>
    /// <param name="battleId">ID of the battle</param>
    /// <returns>The uint256 seed if the battle exists, 0 if the battle does not exists</returns>
    public static async Task<BigInteger> GetBattleRNGSeed(ulong battleId)
    {
        return await contract.Read<BigInteger>("battleIdRNGSeed", battleId);
    }

    /// <summary>
    /// Tries to get events based off two player's addresses, if the event is not found, keeps polling for events
    /// and checking them until an event is found or cancellationToken is triggered
    /// </summary>
    /// <param name="playerAddr1">Address of the 1st player, ignored when Null</param>
    /// <param name="playerAddr2">Address of the 2nd player, ignored when Null</param>
    /// <param name="from">Filter from a specific block</param>
    /// <param name="cancellationToken">Stops waiting for the event</param>
    /// <returns>Data of the BattleCreated event if an event was found, null if cancellationToken was triggered</returns>
    public static async Task<BattleCreatedEventData?> WaitForCreatedBattle(
        string playerAddr1,
        string playerAddr2,
        BlockParameter from,
        CancellationToken cancellationToken)
    {
        if (Utils.IsWebGLBuild())
        {
            var filter = new Dictionary<string, object>
            {
                ["player1Addr"] = playerAddr1,
                ["player2Addr"] = playerAddr2,
            };
            var logs = await ThirdwebExtensions.GetEventsAsync<BattleCreatedEventData>(
                contract, 
                "BattleCreated", 
                filter, 
                (int)from.BlockNumber.Value, 
                token: cancellationToken
            );
            if (logs.Count == 0)
            {
                return null;
            }
            return logs.LastOrDefault().data;
        }
        else
        {
            var eventLogs = await new BattleCreatedEventDto()
                .GetEventABI()
                .CreateFilterInput(Address, playerAddr1, playerAddr2, from, null)
                .WaitForEventAsync<BattleCreatedEventDto>(contract, token: cancellationToken);

            var lastEvent = eventLogs.LastOrDefault()?.Event;
            if (lastEvent == null)
            {
                return null;
            }

            return new BattleCreatedEventData()
            {
                BattleId = (ulong)lastEvent.BattleId,
                Player1Addr = lastEvent.Player1Addr,
                Player2Addr = lastEvent.Player2Addr,
                p1DeckId = (ulong)lastEvent.Player1Deck,
                p2DeckId = (ulong)lastEvent.Player2Deck,
            };
        }
    }

    // These fields must match the event fields from the contract in order to work in WebGL
    [Serializable]
    public struct BattleCreatedEventData
    {
        public string Player1Addr;
        public string Player2Addr;
        public ulong BattleId;
        public ulong p1DeckId;
        public ulong p2DeckId;
    }
}
