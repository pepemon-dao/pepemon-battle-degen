using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Unity.Rpc;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    public string creditsURL;
    public Web3Controller web3;
    public List<GameObject> menuScreens;

    public GameObject _selectDeckListLoader;

    public ScreenManageDecks _screenManageDecks;
    public ScreenLeaderboard _screenLeaderboard;

    public Button _connectWalletButton;
    public Button _startGameButton;
    public Button _manageDecksButton;
    public Button _leaderboardButton;
    public Button _creditsButton;

    public int defaultScreenId = 0;
    private int screenNavigationPosition = 0;
    private int[] screenNavigationHistory = new int[10];

    private int selectedLeagueId = 0;
    private ulong selectedDeckId = 0;

    private void Start()
    {
        // TODO: find a better way to handle re-loading the main scene
        DeInitMainScene();
        _connectWalletButton.onClick.AddListener(OnConnectWalletButtonClick);
        _startGameButton.onClick.AddListener(OnStartGameButtonClick);
        _manageDecksButton.onClick.AddListener(OnManageDecksButtonClick);
        _leaderboardButton.onClick.AddListener(OnLeaderboardButtonClick);
        _creditsButton.onClick.AddListener(OpenCredits);
    }

    private void DeInitMainScene()
    {
        // assume that when no account was selected and his scene loads, its because the game just launched
        if (Web3Controller.instance == null || Web3Controller.instance.SelectedAccountAddress == null)
        {
            ShowScreen(defaultScreenId);
        }
        // assume that when an account was already selected, this scene was loaded after a battle that just ended
        else
        {
            ShowScreen(MainSceneScreensEnum.Menu);
            _startGameButton.interactable = true;
            _manageDecksButton.interactable = true;
            _leaderboardButton.interactable = true;
        }
    }

    // TODO: use this method for all ShowScreen calls
    public void ShowScreen(MainSceneScreensEnum screen) => ShowScreen((int) screen);

    public void ShowScreen(int screenId)
    {
        if (screenId < 0)
        {
            int nextPosition = screenNavigationPosition + screenId;
            screenId = screenNavigationHistory[nextPosition % screenNavigationHistory.Length];
            screenNavigationPosition = (nextPosition - 1) % screenNavigationHistory.Length;
        }

        for(int i = 0; i < menuScreens.Count; i++)
        {
            menuScreens[i].SetActive(i == screenId);
        }

        screenNavigationPosition = (screenNavigationPosition + 1) % screenNavigationHistory.Length;
        screenNavigationHistory[screenNavigationPosition] = screenId;
    }

    public void SelectLeague(int leagueId)
    {
        selectedLeagueId = leagueId;
    }

    public void StartGame()
    {
        // Matchmaking
        // 

        Debug.Log($"Start matchmaking with league: {selectedLeagueId} and deck {selectedDeckId}");
    }

    public void ProceedToNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    public void OpenCredits()
    {
        Application.OpenURL(creditsURL);
    }

    public void ToggleAudio(bool enable)
    {

    }

    public void OnConnectWalletButtonClick()
    {
        Web3Controller.instance.ConnectWallet();
    }

    public void OnStartGameButtonClick()
    {
        ShowScreen(MainSceneScreensEnum.LeagueSelection);
    }

    public void OnManageDecksButtonClick()
    {
        _screenManageDecks.ReloadAllDecks();
        ShowScreen(MainSceneScreensEnum.ManageDecks);
    }

    public void OnLeaderboardButtonClick()
    {
        _screenLeaderboard.ReloadDefaultLeaderboard();
        ShowScreen(MainSceneScreensEnum.Leaderboard);
    }
}
