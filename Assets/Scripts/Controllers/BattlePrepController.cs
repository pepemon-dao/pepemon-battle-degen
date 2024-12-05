using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Pepemon.Battle;
using Sirenix.OdinInspector;
using Thirdweb;
using UnityEngine;
using UnityEngine.UI;

public class BattlePrepController : MonoBehaviour
{
    [TitleGroup("Component References"), SerializeField] GameObject _searchForOpponentButton;
    [TitleGroup("Component References"), SerializeField] GameObject _exitButton;
    [TitleGroup("Component References"), SerializeField] GameObject _deckList;

    [ReadOnly] private PepemonMatchmaker.PepemonLeagues selectedLeague;
    [ReadOnly] private ulong selectedDeck;
    [ReadOnly] private CancellationTokenSource _cancellationTokenSource;

    // Used on GameController on the battle scene
    public static BattleData battleData { get; private set; } = new BattleData();



    [Header("StarterDeck")]
    public List<Card> starterDeck1;
    public List<Card> starterDeck2;

    void Start()
    {
        _searchForOpponentButton.GetComponent<Button>().onClick.AddListener(OnSearchForOpponentButtonClick);
        _exitButton.GetComponent<Button>().onClick.AddListener(onExitButtonClick);
        _deckList.GetComponent<DeckListLoader>().onSelectDeck.AddListener(OnDeckSelected);
    }

    // set by LeagueSelection buttons
    public void SetLeague(int league)
    {
        selectedLeague = (PepemonMatchmaker.PepemonLeagues)league;
    }

    public void OnDeckSelected(ulong deckId, bool isStarterDeck)
    {
        selectedDeck = deckId;
        if (isStarterDeck)
        {
            Web3Controller.instance.StarterDeckID = deckId;
        }
    }
    
    public void OnPepemonSelected(int pepemonID)
    {
        Web3Controller.instance.StarterPepemonID = pepemonID;
    }

    private async void OnSearchForOpponentButtonClick()
    {
        // SetApprovalForAll for CardDeck
        await EnsureDeckTransferApproved();

        var blockNumber = await Blocks.GetLatestBlockNumber();

        // filter events starting from the next block, to avoid possibly getting the last battle of the player
        var nextBlock = new BlockParameter((blockNumber + 1).ToHexBigInteger());

        // show the "Waiting for player" screen
        FindObjectOfType<MainMenuController>().ShowScreen(MainSceneScreensEnum.WaitForOpponent);

        bool failedToEnter = false;
        try
        {
            Debug.Log("Entering the matchmaker");
            if (!await PepemonMatchmaker.Enter(selectedLeague, selectedDeck))
            {
                Debug.Log("Unable to enter the matchmaker: transaction failed");
                failedToEnter = true;
            }
        }
        catch (Exception e)
        {
            Debug.Log("Unable to enter the matchmaker: " + e);
            failedToEnter = true;
        }

        var PlayerWalletAddress = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();

        // check if the current player's deck is in the matchmaker list of deck owners, if it is, then we can assume the player
        // is in the waitlist

        string deckOwner = await PepemonMatchmaker.GetDeckOwner(selectedLeague, selectedDeck);
        string player1addr = PlayerWalletAddress,
               player2addr = null;

        // If the player is in the waitlist, then its because a battle has *not* started yet, the current player's address will
        // be in the second parameter of the BattleCreated event in this case.
        // However if the battle *has* started, the player's address will be in the first parameter of the BattleCreated event
        if (deckOwner.Equals(PlayerWalletAddress, StringComparison.OrdinalIgnoreCase))
        {
            player2addr = player1addr;
            player1addr = null;
            // the Exit button must only be enabled once we are sure that the current player is in the wait list of the Matchmaker
            _exitButton.GetComponent<Button>().interactable = true;
        }
        else if (failedToEnter)
        {
            // go back to deck selection since the player could not enter nor is already waiting
            FindObjectOfType<MainMenuController>().ShowScreen(MainSceneScreensEnum.PreviousScreen);
            return;
        }

        Debug.Log("Waiting for CreatedBattle events");

        _cancellationTokenSource = new CancellationTokenSource();
        var battleEvent = await PepemonBattle.WaitForCreatedBattle(
                player1addr,
                player2addr,
                nextBlock,
                _cancellationTokenSource.Token);
        _cancellationTokenSource.Dispose();

        // initiate the battle immediatelly if the player initiated the battle
        if (battleEvent != null)
        {
            // prevent player from clicking on the Exit button if BattleCreated event was captured
            _exitButton.GetComponent<Button>().interactable = false;

            // proceed into the next scene
            await InitBattleScene((PepemonBattle.BattleCreatedEventData)battleEvent);
        }
    }

    public async Task<IDictionary<ulong, int>> GetAllSupportCards(ulong deckId)
    {
        var result = new OrderedDictionary<ulong, int>();

        if (deckId == 10001) //using first starter deck
        {
            foreach (var card in starterDeck1)
            {
                ulong id = (ulong)card.ID;
                result[id] = result.ContainsKey(id) ? result[id] + 1 : 1;
            }
        }
        else // if not using the first then still assign a deck to it
        {
            foreach (var card in starterDeck2)
            {
                ulong id = (ulong)card.ID;
                result[id] = result.ContainsKey(id) ? result[id] + 1 : 1;
            }
        }

        return result;
    }

    private async Task InitBattleScene(PepemonBattle.BattleCreatedEventData battleEventData)
    {
        Debug.Log("Received battle event. BattleId: " + battleEventData.BattleId);
        Debug.Log("Loading battle data..");

        var reqBattleRngSeed = PepemonBattle.GetBattleRNGSeed(battleEventData.BattleId);
        var reqPlayer1BattleCard = PepemonCardDeck.GetBattleCard(battleEventData.p1DeckId);
        var reqPlayer2BattleCard = PepemonCardDeck.GetBattleCard(battleEventData.p2DeckId);

        Task<IDictionary<ulong, int>> reqPlayer1SupportCards;
        Task<IDictionary<ulong, int>> reqPlayer2SupportCards;

        if (battleEventData.p1DeckId == 10001 || battleEventData.p1DeckId == 10002) //using starter deck
        {
            reqPlayer1SupportCards = GetAllSupportCards(battleEventData.p1DeckId);
        }
        else //using own deck
        {
            reqPlayer1SupportCards = PepemonCardDeck.GetAllSupportCards(battleEventData.p1DeckId);
        }

        if (battleEventData.p2DeckId == 10001 || battleEventData.p2DeckId == 10002) //using starter deck
        {
            reqPlayer2SupportCards = GetAllSupportCards(battleEventData.p2DeckId);
        }
        else //using own deck
        {
            reqPlayer2SupportCards = PepemonCardDeck.GetAllSupportCards(battleEventData.p2DeckId);
        }

        await Task.WhenAll(reqBattleRngSeed, reqPlayer1BattleCard, reqPlayer2BattleCard, reqPlayer1SupportCards, reqPlayer2SupportCards);

        Debug.Log("Battle data loaded");

        var PlayerWalletAddress = await ThirdwebManager.Instance.SDK.Wallet.GetAddress();

        battleData.battleRngSeed = reqBattleRngSeed.Result;
        battleData.player1BattleCard = reqPlayer1BattleCard.Result;
        battleData.player2BattleCard = reqPlayer2BattleCard.Result;
        battleData.player1SupportCards = reqPlayer1SupportCards.Result;
        battleData.player2SupportCards = reqPlayer2SupportCards.Result;
        battleData.currentPlayerIsPlayer1 = battleEventData.Player1Addr
            .Equals(PlayerWalletAddress, StringComparison.OrdinalIgnoreCase);

        FindObjectOfType<MainMenuController>().ProceedToNextScene();
    }

    private async void onExitButtonClick()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            _exitButton.GetComponent<Button>().interactable = false;
            Debug.Log("Trying to exit matchmaking");
            await PepemonMatchmaker.Exit(selectedLeague, selectedDeck);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Matchmaking Exit failed: " + ex.Message);
        }

        FindObjectOfType<MainMenuController>().ShowScreen(MainSceneScreensEnum.PreviousScreen);
    }

    private async Task EnsureDeckTransferApproved()
    {
        var matchmakerAddress = Web3Controller.instance.GetChainConfig().pepemonMatchmakerAddresses[(int)selectedLeague];
        // necessary to avoid "ERC1155#safeTransferFrom: INVALID_OPERATOR"
        // TODO: place this in an "approve" button
        var approvalOk = await PepemonCardDeck.GetApprovalState(matchmakerAddress);
        if (!approvalOk)
        {
            try
            {
                await PepemonCardDeck.SetApprovalState(true, matchmakerAddress);
                approvalOk = await PepemonCardDeck.GetApprovalState(matchmakerAddress);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("SetApprovedForAll failed: " + ex.Message);
            }
        }
    }

    // container for the battle data which is used to display the battle in GameController
    public class BattleData
    {
        public bool currentPlayerIsPlayer1 { get; set; }
        public BigInteger battleRngSeed { get; set; }
        public ulong player1BattleCard { get; set; }
        public ulong player2BattleCard { get; set; }
        public IDictionary<ulong, int> player1SupportCards { get; set; }
        public IDictionary<ulong, int> player2SupportCards { get; set; }

        public bool isBotMatch = false;
    }
}
