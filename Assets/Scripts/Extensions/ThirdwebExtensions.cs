using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System;
using Thirdweb;
using Nethereum.Contracts;
using Cysharp.Threading.Tasks;
using System.Linq;

public static class ThirdwebExtensions
{
    public const int EVENT_POLLING_INTERVAL_MS = 2000;

    public static async Task<List<ContractEvent<T>>> GetEventsAsync<T>(
        Thirdweb.Contract contract,
        string eventName,
        Dictionary<string, object> filter, 
        int fromBLock,
        int millisecondsToWait = EVENT_POLLING_INTERVAL_MS,
        CancellationToken token = default
    ) {
        if (!Utils.IsWebGLBuild())
        {
            throw new UnityException("Method cannot be used in non-webgl builds");
        }
        Debug.Log($"Waiting event {eventName} with WebGL");
        List<ContractEvent<T>> logs;
        do
        {
            logs = await contract.Events.Get<T>(eventName, new EventQueryOptions
            {
                fromBlock = fromBLock,
                filters = filter
            });
            if (logs.Count == 0)
            {
                try
                {
                    await UniTask.Delay(millisecondsToWait, cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log($"Stopped waiting event {eventName}");
                    break;
                }
            }
        } while (logs.Count == 0);

        return logs;
        
    }

    public static async Task<List<EventLog<TEventDTO>>> GetEventsAsync<TEventDTO>(
        this NewFilterInput eventFilter, 
        Thirdweb.Contract contract
    )
        where TEventDTO : IEventDTO, new()
    {
        var web3 = Utils.GetWeb3();
        var transferEventHandler = web3.Eth.GetEvent<TEventDTO>(contract.Address);

        Debug.Log($"Getting events: {typeof(TEventDTO).Name} " +
            $"with {eventFilter.Topics.Length} filter params " +
            $"from blocks {eventFilter.FromBlock.BlockNumber} - {eventFilter.ToBlock.BlockNumber}");

        List<EventLog<TEventDTO>> eventLogs = await transferEventHandler.GetAllChangesAsync(eventFilter);

        Debug.Log("Events received: " + eventLogs.Count);
        return eventLogs;
    }

    public static async Task<List<EventLog<TEventDTO>>> WaitForEventAsync<TEventDTO>(
        this NewFilterInput eventFilter,
        Thirdweb.Contract contract,
        int millisecondsToWait = EVENT_POLLING_INTERVAL_MS,
        CancellationToken token = default
    )
    where TEventDTO : IEventDTO, new()
    {
        Debug.Log($"Waiting for event: {typeof(TEventDTO).Name} " +
            $"with {eventFilter.Topics.Length} topics " +
            $"from block {eventFilter.FromBlock.BlockNumber}");

        var web3 = Utils.GetWeb3();
        var transferEventHandler = web3.Eth.GetEvent<TEventDTO>(contract.Address);

        List<EventLog<TEventDTO>> eventLogs;
        do
        {
            eventLogs = await transferEventHandler.GetAllChangesAsync(eventFilter);

            if (eventLogs.Count == 0)
            {
                try
                {
                    await UniTask.Delay(millisecondsToWait, cancellationToken: token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Stopped waiting event " + typeof(TEventDTO).Name);
                    break;
                }
            }
        }
        while (eventLogs.Count == 0);

        Debug.Log("Events received: " + eventLogs.Count);

        return eventLogs;   
    }
}
