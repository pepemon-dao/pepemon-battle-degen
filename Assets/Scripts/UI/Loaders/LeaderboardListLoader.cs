using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Thirdweb;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the loading of player rankings; Creates instances of _playerRankingPrefab controlled by PlayerRankingController
/// </summary>
public class LeaderboardListLoader : MonoBehaviour
{
    [TitleGroup("Component References"), SerializeField] PlayerRankingController _playerRankingPrefab;
    [TitleGroup("Component References"), SerializeField] GameObject _rankingList;
    [TitleGroup("Component References"), SerializeField] Text _loadingMessage;

    private const int FETCH_SIZE = 50;
    private const int TOP_PLAYERS_AMOUNT = 10;
    private bool loadingInProgress = false;

    /// <summary>
    /// Removes all elements in _playerList and loads all entries using _playerRankingPrefab.
    /// </summary>
    public async void ReloadLeaderboard(PepemonMatchmaker.PepemonLeagues league)
    {
        // prevent re-reloading things over and over again with old async calls if
        // the user decides to go back and forth very quickly between screens
        if (loadingInProgress)
            return;

        loadingInProgress = true;

        _loadingMessage.gameObject.SetActive(true);
        _loadingMessage.text = "Loading leaderboard...";

        // destroy before re-creating
        foreach (var playerRanking in _rankingList.GetComponentsInChildren<PlayerRankingController>())
        {
            Destroy(playerRanking.gameObject);
        }

        // should not happen, but if it happens then it won't crash the game
        var account = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();
        if (string.IsNullOrEmpty(account))
        {
            _loadingMessage.text = "Error: No account selected";
            return;
        }

        // load all rankings
        List<(string Address, ulong Ranking)> rankings = new();
        try
        {
            var totalPlayers = await PepemonMatchmaker.GetLeaderboardPlayersCount(league);
            for (ulong i = 0; i < totalPlayers; i+= FETCH_SIZE)
            {
                rankings.AddRange(await PepemonMatchmaker.GetPlayersRankings(league, count: FETCH_SIZE, offset: i));
            }

            rankings = rankings.OrderByDescending((i) => i.Ranking).Take(TOP_PLAYERS_AMOUNT).ToList();
        }
        catch(System.Exception e)
        {
            // Might always happen when there are no players in the leaderboard, eg.: new deployment of the Matchmaker contract.
            // Also when there are network issues
            Debug.Log($"Unable to load leaderboard: {e.Message}");
            _loadingMessage.text = "Unable to load leaderboard";
            return;
        }


        List<PlayerRankingController> refs = new List<PlayerRankingController>();
        foreach (var playerRanking in rankings)
        {
            var playerRankingInstance = Instantiate(_playerRankingPrefab);
            playerRankingInstance.transform.SetParent(_rankingList.transform, false);
            playerRankingInstance.SetInfo(playerRanking.Address, playerRanking.Ranking.ToString());
            refs.Add(playerRankingInstance);
        }

        _loadingMessage.gameObject.SetActive(false);
        loadingInProgress = false;
    }
}
