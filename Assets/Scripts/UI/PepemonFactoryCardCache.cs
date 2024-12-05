#if !UNITY_WEBGL || UNITY_EDITOR
#define ENABLE_CACHE
#endif

using NBitcoin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;
using Contracts.PepemonFactory.abi.ContractDefinition;
using Assets.Scripts.Cache;

/// <summary>
/// Downloads and caches PepemonFactory card information, including metadata 
/// and textures
/// </summary>
class PepemonFactoryCardCache
{
    private static ConcurrentDictionary<ulong, Texture2D> cardTextures = new();
    private static ConcurrentDictionary<ulong, CardMetadata> cardMetadata = new();
    private static CacheController cacheController = new();

    public static ICollection<ulong> CardsIds => cardMetadata.Keys;

    public UnityEvent<ulong> cardMetadataLoaded;
    public UnityEvent<ulong> cardImageLoaded;

    public static async Task PreloadAllMetadata(ulong parallelBatchSize, ulong batchCallSize, Action<ulong> cardLoadedCallback = null)
    {
        ulong firstCardId = 1;
        ulong lastCardId = await PepemonFactory.GetLastCardId();

        ulong concurrentRequests = 0;

        var batchLoadingTasks = new List<UniTask>();
        for (ulong batchStart = firstCardId; batchStart < lastCardId; batchStart += batchCallSize)
        {
            var batchEnd = Math.Min(batchStart + batchCallSize, lastCardId);
            batchLoadingTasks.Add(PreloadTokenMetadata(batchStart, batchEnd, cardLoadedCallback));
            concurrentRequests += 1;
            if (concurrentRequests >= parallelBatchSize)
            {
                await UniTask.WhenAll(batchLoadingTasks);
                concurrentRequests = 0;
            }
        }
        await UniTask.WhenAll(batchLoadingTasks);
    }

    public static async Task PreloadAllImages(ulong parallelBatchSize, Action<ulong> cardLoadedCallback = null)
    {
        var cardList = cardMetadata.ToList();

        for (int batchStart = 0; batchStart < cardList.Count; batchStart += (int)parallelBatchSize)
        {
            int batchEnd = Math.Min(batchStart + (int)parallelBatchSize, cardList.Count);

            List<Task> imageLoadingTasks = new List<Task>();
            for (int i = batchStart; i < batchEnd; i++)
            {
                imageLoadingTasks.Add(PreloadTokenImage(cardList[i].Key, cardLoadedCallback));
            }
            await Task.WhenAll(imageLoadingTasks);
        }
    }

    private static async UniTask PreloadTokenMetadata(ulong minId, ulong maxId, Action<ulong> cardLoadedCallback = null)
    {
        var supportCardsToFetch = new List<ulong>();
        var battleCardStats = await PepemonFactory.BatchGetBattleCardStats(minId, maxId);
        for (var i = 0; i < battleCardStats.Count; i++)
        {
            var tokenId = (ulong)i + minId;

            // when we accidentally tried to fetch a battlecard but it was a support card
            if (battleCardStats[i].Hp == 0)
            {
                supportCardsToFetch.Add(tokenId);
            }
            else
            {
                cardMetadata[tokenId] = new CardMetadata
                {
                    description = battleCardStats[i].Description,
                    image = battleCardStats[i].IpfsAddr,
                    isSupportCard = false,
                    name = battleCardStats[i].Name
                };
                cardLoadedCallback?.Invoke(tokenId);
                Debug.Log($"Loaded token metadata for id: {tokenId}");
            }
        }
        if (supportCardsToFetch.Count == 0)
        {
            return;
        }

        List<SupportCardStats> supportCardStats = new();
        try
        {
            var groups = supportCardsToFetch.GroupConsecutive().ToList();
            foreach (var group in groups)
            {
                supportCardStats.AddRange(
                    await PepemonFactory.BatchGetSupportCardStats(
                        group.First(), group.Last()
                    )
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return;
        }

        for (var i = 0; i < supportCardStats.Count; i++)
        {
            var tokenId = (ulong)i + minId;

            cardMetadata[tokenId] = new CardMetadata
            {
                description = supportCardStats[i].Description,
                image = supportCardStats[i].IpfsAddr,
                isSupportCard = true,
                name = supportCardStats[i].Name
            };
            cardLoadedCallback?.Invoke(tokenId);
            Debug.Log($"Loaded token metadata for id: {tokenId}");
        }
    }

    private static async Task PreloadTokenImage(ulong tokenId, Action<ulong> cardLoadedCallback = null)
    {
        if (!cardMetadata.ContainsKey(tokenId))
        {
            return;
        }

        CardMetadata metadata = cardMetadata[tokenId];

        var cachedImage = await cacheController.Get(metadata.image);
        if (cachedImage != null)
        {
            var texture = new Texture2D(8, 8);
            texture.LoadImage(cachedImage);
            cardTextures[tokenId] = GenerateMipmaps(texture);
            cardLoadedCallback?.Invoke(tokenId);
            return;
        }

        using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(metadata.image))
        {
            DownloadHandler handle = webRequest.downloadHandler;
            try
            {
                await webRequest.SendWebRequest();
            }
            catch (UnityWebRequestException e)
            {
                Debug.LogWarning($"Error while downloading card image {tokenId}: {e.Message}");
                cardTextures[tokenId] = new Texture2D(8, 8);
                cardLoadedCallback?.Invoke(tokenId);
                return;
            }
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning($"Error while downloading card image {tokenId}: {webRequest.error}");
                    cardTextures[tokenId] = new Texture2D(8, 8);
                    cardLoadedCallback?.Invoke(tokenId);
                    return;
            }
            Debug.Log("Download of card image successful. Card id: " + tokenId);

            var texture = DownloadHandlerTexture.GetContent(webRequest);
#if ENABLE_CACHE
            await cacheController.Set(metadata.image, texture.EncodeToPNG());
#endif
            cardTextures[tokenId] = GenerateMipmaps(texture);
            cardLoadedCallback?.Invoke(tokenId);
        }
    }

    private static Texture2D GenerateMipmaps(Texture2D source)
    {
        Texture2D output = new Texture2D(source.width, source.height, source.format, true);
        output.SetPixelData(source.GetRawTextureData<byte>(), 0);
        output.Apply(true, true);
        return output;
    }

    public static Texture2D GetImage(ulong tokenId)
    {
        return cardTextures.TryGet(tokenId);
    }

    public static CardMetadata? GetMetadata(ulong tokenId)
    {
        return cardMetadata.TryGet(tokenId);
    }
}

[Serializable]
public struct CardMetadata
{
    public string image;
    public string name;
    public string description;
    public bool isSupportCard;
}
