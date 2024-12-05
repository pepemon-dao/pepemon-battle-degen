using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Scripts.Managers.Sound;
using Sirenix.OdinInspector;
using Thirdweb;
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
    public Button _mintDeckButton;

    public int defaultScreenId = 0;
    private int screenNavigationPosition = 0;
    private int[] screenNavigationHistory = new int[10];

    private int selectedLeagueId = 0;
    private ulong selectedDeckId = 0;

    public static bool claimedStarterDeck = false;

    private void Start()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //PostBattleScreenController.IsClaimingGift = true; //- for testing the gift mechanic with the deck manager
        // TODO: find a better way to handle re-loading the main scene

        HandleGoingBackToMenu();
        _connectWalletButton.onClick.AddListener(OnConnectWalletButtonClick);
        _mintDeckButton.onClick.AddListener(OnConnectWalletButtonClick);
        _startGameButton.onClick.AddListener(OnStartGameButtonClick);
        _manageDecksButton.onClick.AddListener(OnManageDecksButtonClick);
        _leaderboardButton.onClick.AddListener(OnLeaderboardButtonClick);
        _creditsButton.onClick.AddListener(OpenCredits);
    }

    private void HandleGoingBackToMenu()
    {
        if (PepemonFactoryCardCache.CardsIds.Count == 0)
        {
            DeInitMainScene(true);
        }
        if (PostBattleScreenController.IsPlayingAgain)
        {
            PostBattleScreenController.IsPlayingAgain = false;
            DeInitMainScene(false);

            ShowScreen(MainSceneScreensEnum.LeagueSelection);
        }
        if (PostBattleScreenController.IsGoingFromBattle)
        {
            PostBattleScreenController.IsGoingFromBattle = false;
            DeInitMainScene(false);
        }


        if (PostBattleScreenController.IsClaimingGift)
        {
            PostBattleScreenController.IsClaimingGift = false;
            
            //claim gift
            ClaimStarterDeck();
        }
    }

    private void ClaimStarterDeck()
    {
        Debug.LogError("gift claim is not yet implemented");
        PlayerPrefs.SetInt("GotStarterPack", 1);
        //claimedStarterDeck = true;
    }

    private async void DeInitMainScene(bool toLoadScreen)
    {
        // assume that when no account was selected and his scene loads, its because the game just launched

        /*
        if (Web3Controller.instance == null || Web3Controller.instance.SelectedAccountAddress == null)
        {
            _startGameButton.interactable = true;
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

        */

        // Show load screen in order to load cards into the PepemonFactoryCardCache.
        // only necessary when launching the game from the battle scene
        if (toLoadScreen)
        {
            _startGameButton.interactable = true;
            ShowScreen(defaultScreenId);
        }
        else
        {
            ShowScreen(MainSceneScreensEnum.Menu);
            _startGameButton.interactable = true;
            if (Web3Controller.instance != null && await ThirdwebManager.Instance.SDK.Wallet.GetAddress() != null)
            {
                _manageDecksButton.interactable = true;
                _leaderboardButton.interactable = true;
            }
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
        
        if (screenId == (int)MainSceneScreensEnum.LeagueSelection && !Web3Controller.instance.IsConnected)
        {
            screenId = (int)MainSceneScreensEnum.Tutorial;
        }

        for (int i = 0; i < menuScreens.Count; i++)
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
        ThemePlayer.Instance.ToggleAudio(enable);
    }

    public async void OnConnectWalletButtonClick()
    {
        if (!Web3Controller.instance.IsConnected)
        {
            await Web3Controller.instance.ConnectWallet();
        }
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

    public void UnMuteThemePlayer()
    {
        ThemePlayer.Instance.UnMute();
    }
}
