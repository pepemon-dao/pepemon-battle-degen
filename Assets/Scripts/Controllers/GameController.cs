using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System;
using System.Numerics;
using Nethereum.ABI;
using Nethereum.RLP;
using Pepemon.Battle;
using System.Threading.Tasks;
using TMPro;
using DG;
using DG.Tweening;
using Org.BouncyCastle.X509;
using System.Reflection;

// Manages the automation of the game. Each round is composed of two hands being played (offense and defense)
public class GameController : MonoBehaviour
{
    private const int PLAYER1_SEED = 69;
    private const int PLAYER2_SEED = 420;
    private const int TIEBREAK_SEED = 69420;

    //Attacker can either be PLAYER_ONE or PLAYER_TWO
    private enum Attacker
    {
        PLAYER_ONE,
        PLAYER_TWO
    }

    [TitleGroup("Component References"), SerializeField] UIController _uiController;

    [TitleGroup("Opponents"), SerializeField] Player _player1;
    [TitleGroup("Opponents"), SerializeField] Player _player2;

    [TitleGroup("Behaviour"), SerializeField] bool playAutomatically;

    [TitleGroup("Debug"), ShowInInspector, ReadOnly] Attacker _currentAttacker; // Resolve attacker in the current turn
    [TitleGroup("Debug"), ShowInInspector, ReadOnly] int _roundNumber;
    [TitleGroup("Debug"), ShowInInspector, ReadOnly] bool _gameHasFinished;
    [TitleGroup("Debug"), ShowInInspector, ReadOnly] bool _isPlayingRound;

    [BoxGroup("Pepemon Controller")] public PepemonCardController player1Controller;
    [BoxGroup("Pepemon Controller")] public PepemonCardController player2Controller;

    [TitleGroup("Scriptable objects list"), SerializeField] DataContainer CardsScriptableObjsData;
    [TitleGroup("AttackerUI"), SerializeField] List<GameObject> _attackerUIElements;
    [TitleGroup("AttackerUI"), SerializeField] TMP_Text dmgText1;
    [TitleGroup("AttackerUI"), SerializeField] TMP_Text dmgText2;
    [TitleGroup("HealthBarUI"), SerializeField] HealthSystem healthBar1;
    [TitleGroup("HealthBarUI"), SerializeField] HealthSystem healthBar2;

    [ReadOnly] private BigInteger battleSeed;

    [Header("StarterDeck")]
    public List<Card> starterDeck1;
    public List<Card> starterDeck2;

    private void Start()
    {
        PrepareDecksBeforeBattle();

        player1Controller.PopulateCard(_player1.PlayerPepemon);
        player2Controller.PopulateCard(_player2.PlayerPepemon);

        if (playAutomatically)
        {
            InitFirstRound();
            StartCoroutine(LoopGame());
        }
    }

    /// <summary>
    /// Used for debugging battles to ensure its synced with the blockchain results
    /// </summary>
    private void PrepareSimulatedBattle(ulong starterDeckID)
    {
        // prevent overriding real battles
        if (BattlePrepController.battleData.battleRngSeed != 0)
        {
            return;
        }

        if (starterDeckID == 0) // bot battle in editor
        {
            // add console.log calls in the PepemonBattle.sol contract to get this seed
            BattlePrepController.battleData.battleRngSeed = BigInteger.Parse(
                "68188038832262297884772284640717549873770515354422947402145954532168121309549");
            BattlePrepController.battleData.player1BattleCard = 4;
            BattlePrepController.battleData.player1SupportCards = new OrderedDictionary<ulong, int>()
            {
                [12] = 1,
                [28] = 2
            };
            BattlePrepController.battleData.player2BattleCard = 8;
            BattlePrepController.battleData.player2SupportCards = new OrderedDictionary<ulong, int>()
            {
                [12] = 1,
                [29] = 1,
                [21] = 1
            };
        }
        else // bot battle with starter deck
        {
            ulong pepemonStarterID = (ulong)Web3Controller.instance.StarterPepemonID;

            // add console.log calls in the PepemonBattle.sol contract to get this seed
            BattlePrepController.battleData.battleRngSeed = BigInteger.Parse(
                "68188038832262297884772284640717549873770515354422947402145954532168121309549");
            BattlePrepController.battleData.player1BattleCard = pepemonStarterID;
            BattlePrepController.battleData.player1SupportCards = GetAllSupportCards(starterDeckID);

            ulong botStarterDeck = 10002;
            if (starterDeckID == botStarterDeck)
                botStarterDeck = 10001;
            ulong botStarterPepemon = 7;

            BattlePrepController.battleData.player2BattleCard = botStarterPepemon;
            BattlePrepController.battleData.player2SupportCards = GetAllSupportCards(botStarterDeck);
        }

        _player1.SetPlayerDeck(
        pepemon: CardsScriptableObjsData.GetPepemonById(BattlePrepController.battleData.player1BattleCard.ToString()),
        supportCards: CardsScriptableObjsData.GetAllCardsByIds(BattlePrepController.battleData.player1SupportCards));

        _player2.SetPlayerDeck(
            pepemon: CardsScriptableObjsData.GetPepemonById(BattlePrepController.battleData.player2BattleCard.ToString()),
            supportCards: CardsScriptableObjsData.GetAllCardsByIds(BattlePrepController.battleData.player2SupportCards));

        BattlePrepController.battleData.currentPlayerIsPlayer1 = true;

        BattlePrepController.battleData.isBotMatch = true;

        //resetting them

        if (Web3Controller.instance != null)
        {
            //Web3Controller.instance.StarterDeckID = 0;
            //Web3Controller.instance.StarterPepemonID = 0;
            BattlePrepController.battleData.battleRngSeed = 0;
        }
        
    }

    public IDictionary<ulong, int> GetAllSupportCards(ulong deckId)
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

    private void PrepareDecksBeforeBattle()
    {
        ulong starterDeckID = Web3Controller.instance?.StarterDeckID ?? 0;

        // When player is going through the tutorial
        if (starterDeckID != 0)
        {
            PrepareSimulatedBattle(starterDeckID);
            battleSeed = new System.Random().NextBigInteger();
        }
        else
        {
            // might be zero if ran from unity editor
            if (BattlePrepController.battleData.battleRngSeed != 0)
            {
                _player1.SetPlayerDeck(
                    pepemon: CardsScriptableObjsData.GetPepemonById(BattlePrepController.battleData.player1BattleCard.ToString()),
                    supportCards: CardsScriptableObjsData.GetAllCardsByIds(BattlePrepController.battleData.player1SupportCards));

                _player2.SetPlayerDeck(
                    pepemon: CardsScriptableObjsData.GetPepemonById(BattlePrepController.battleData.player2BattleCard.ToString()),
                    supportCards: CardsScriptableObjsData.GetAllCardsByIds(BattlePrepController.battleData.player2SupportCards));

                battleSeed = BattlePrepController.battleData.battleRngSeed;
            }
            else
            {
                // when ran from Unity Editor. Set battleSeed and GameController cards to simulate a specific battle
                Debug.LogWarning("Battle data not set from BattlePrepController");
                battleSeed = 1;
            }

            BattlePrepController.battleData.isBotMatch = false;
        }
        
    }

    [Button()]
    public void StartGame()
    {
        InitFirstRound();
    }

    [Button()]
    void ResetGame()
    {
        _gameHasFinished = false;
        _currentAttacker = Attacker.PLAYER_ONE;
        _roundNumber = 0;
        _player1.Reset();
        _player2.Reset();
    }

    // Each player shuffles their deck and draws cards equal to pepemon intelligence 
    void InitFirstRound()
    {
        _roundNumber = 0; // must start as 0
        _player1.Initialize();
        _player2.Initialize();
        _uiController.InitialiseGame(_player1, _player2);

        //set health bars
        healthBar1.SetHealth(_player1.CurrentHP);
        healthBar2.SetHealth(_player2.CurrentHP);
    }

    IEnumerator LoopGame()
    {
        yield return new WaitForSeconds(2f);
        while (!_gameHasFinished)
        {
            yield return new WaitUntil(() => !_isPlayingRound);
            yield return StartCoroutine(StartRound());
        }
    }

    [Button()]
    void sStartGame()
    {
        StartCoroutine(StartRound());
    }

    IEnumerator StartRound()
    {
        if (_gameHasFinished)
        {
            yield return null;
        }
        if (BattlePrepController.battleData.isBotMatch)
        {
            BotTextTutorial.Instance.TriggerTutorialEvent(1);
        }
        
        //if (_roundNumber <= 1)
        //   yield return new WaitForSeconds(1.2f);
        _uiController.NewRoundDisplay();
        yield return new WaitForSeconds(1.6f);
        _uiController.HideNewRoundDisplay();
        yield return new WaitForSeconds(.3f);

        //Reset both players' hand infos to base stats
        _player1.ResetCurrentPepemonStats();
        _player2.ResetCurrentPepemonStats();

        //Refresh cards every 5 turns
        if (_roundNumber % 5 == 0)
        {
            //Need to refresh decks

            // Shuffle player1 support cards
            _player1.ShuffelCurrentDeck(PLAYER1_SEED, _roundNumber, battleSeed);

            //Reset played card count
            _player1.PlayedCardCount = 0;

            // Shuffle player2 support cards
            _player2.ShuffelCurrentDeck(PLAYER2_SEED, _roundNumber, battleSeed);

            //Reset played card count
            _player2.PlayedCardCount = 0;
        }
        else
        {
            //Don't need to refresh cards now

            // Get temp support info of previous turn's hands and calculate their effect for the new turn
            _player1.CalcSupportCardsOnTable(_player2);
            _player2.CalcSupportCardsOnTable(_player1);
        }

        Debug.Log("<b>DRAWING HANDS</b>");
        _player1.DrawNewHand();
        _player2.DrawNewHand();

        // Display hands in the UI
        _uiController.DisplayHands();

        //delay to show drawing of cards
        yield return new WaitForSeconds(1.5f); //3

        //! need to think of a better way to display the cards being played

        _isPlayingRound = true;
        Debug.Log("<b>STARTING ROUND: </b>" + _roundNumber);
        if (BattlePrepController.battleData.isBotMatch)
        {
            BotTextTutorial.Instance.TriggerTutorialEvent(2);
        }

        for (int i = 0; i < 2; i++)
        {
            if (!_gameHasFinished)
            {
                _currentAttacker = ResolveAttacker(_currentAttacker, _player1, _player2, i == 1);

                var atkPlayer = _currentAttacker == Attacker.PLAYER_ONE ? _player1 : _player2;
                var defPlayer = _currentAttacker == Attacker.PLAYER_ONE ? _player2 : _player1;

                AttackerUI(_currentAttacker);

                Debug.Log("fight currentAttacker=" + _currentAttacker);
                Debug.Log("fight totalAttackPower=" + atkPlayer.CurrentPepemonStats.atk);
                Debug.Log("fight totalDefensePower=" + atkPlayer.CurrentPepemonStats.def);

                CalcSupportCardsInHand(atkPlayer, defPlayer);

                int totalAttackPower = atkPlayer.CurrentPepemonStats.atk + CalcResistanceWeakness(atkPlayer, defPlayer);
                int totalDefensePower = defPlayer.CurrentPepemonStats.def;

                Debug.Log("fight currentAttacker=" + _currentAttacker);
                Debug.Log("fight totalAttackPower=" + totalAttackPower);
                Debug.Log("fight totalDefensePower=" + totalDefensePower);

                if (_currentAttacker == Attacker.PLAYER_ONE)
                {
                    _uiController.FlipCards(1);

                    player1Controller.ActivateCard(true);
                    player2Controller.ActivateCard(false);

                    //wait for animations showing the attacking/defending cards
                    yield return new WaitForSeconds(1.5f); //3f

                    _uiController.StartCoroutine(_uiController.DisplayTotalValues(1, totalAttackPower, totalDefensePower));

                    yield return new WaitForSeconds(1f); //1.5
                    int dmg = totalAttackPower > totalDefensePower ? (totalAttackPower - totalDefensePower) : 1;

                    _player2.CurrentHP -= dmg;
                    AttackDisplay(false, dmg);
                    healthBar2.TakeDamage(dmg);

                    yield return new WaitForSeconds(1f); //1.5

                    DisableAttackTexts();

                    _uiController.UpdateUI();
                    player1Controller.UpdateCard(_player1);
                    player2Controller.UpdateCard(_player2);

                    if (_player2.CurrentHP <= 0) BattleResut(_player1);
                }
                else
                {
                    _uiController.FlipCards(2);

                    player1Controller.ActivateCard(false);
                    player2Controller.ActivateCard(true);

                    yield return new WaitForSeconds(3f);
                    _uiController.StartCoroutine(_uiController.DisplayTotalValues(2, totalAttackPower, totalDefensePower));

                    yield return new WaitForSeconds(1f); //1.5
                    int dmg = totalAttackPower > totalDefensePower ? (totalAttackPower - totalDefensePower) : 1;

                    _player1.CurrentHP -= dmg;
                    AttackDisplay(true, dmg);
                    healthBar1.TakeDamage(dmg);

                    yield return new WaitForSeconds(1f); //1.5

                    DisableAttackTexts();

                    _uiController.UpdateUI();
                    player1Controller.UpdateCard(_player1);
                    player2Controller.UpdateCard(_player2);

                    if (_player1.CurrentHP <= 0) BattleResut(_player2);
                }
                Debug.Log("goForBattle _player1.CurrentHP=" + _player1.CurrentHP);
                Debug.Log("goForBattle _player2.CurrentHP=" + _player2.CurrentHP);

                if (BattlePrepController.battleData.isBotMatch)
                {
                    BotTextTutorial.Instance.TriggerTutorialEvent(3);
                }

                Debug.Log("waiting 2.5f");
                yield return new WaitForSeconds(1f); //1.5

                // cleanup UI
                _uiController.FlipCards(3);
                player1Controller.DeActivateCard();
                player2Controller.DeActivateCard();
                Debug.Log(" after slow");

                yield return new WaitForSeconds(1f); //1
            }
        }
        Debug.Log("<b>FINISHED ROUND: </b>" + _roundNumber);
        AttackerUI(_currentAttacker, true);
        _roundNumber++;
        _isPlayingRound = false;
    }

    // Note: Same logic of the contract PepemonBattle.sol
    // This method gets the current attacker
    private Attacker ResolveAttacker(Attacker currentAttacker, Player player1, Player player2, bool isTurnOnSecondHalf)
    {
        if (!isTurnOnSecondHalf)
        {
            //Player with highest speed card goes first
            if (player1.CurrentPepemonStats.spd > player2.CurrentPepemonStats.spd)
            {
                return Attacker.PLAYER_ONE;
            }
            else if (player1.CurrentPepemonStats.spd < player2.CurrentPepemonStats.spd)
            {
                return Attacker.PLAYER_TWO;
            }
            else
            {
                //Tiebreak: intelligence
                if (player1.CurrentPepemonStats.inte > player2.CurrentPepemonStats.inte)
                {
                    return Attacker.PLAYER_ONE;
                }
                else if (player1.CurrentPepemonStats.inte < player2.CurrentPepemonStats.inte)
                {
                    return Attacker.PLAYER_TWO;
                }
                else
                {
                    //Second tiebreak: use RNG

                    // calculate random seed like in solidity
                    var abiEncode = new ABIEncode();
                    var rand = abiEncode.GetSha3ABIEncodedPacked(
                        new ABIValue("uint256", TIEBREAK_SEED),
                        new ABIValue("uint256", _roundNumber),
                        new ABIValue("uint256", battleSeed)).ToBigIntegerFromRLPDecoded();

                    return rand % 2 == 0 ? Attacker.PLAYER_ONE : Attacker.PLAYER_TWO;
                }
            }
        }
        else
        {
            return currentAttacker == Attacker.PLAYER_ONE ? Attacker.PLAYER_TWO : Attacker.PLAYER_ONE;
        }
    }

    // Note: Same logic of the contract PepemonBattle.sol
    // We calculate the effect of every card in the player's hand
    private void CalcSupportCardsInHand(Player atkPlayer, Player defPlayer)
    {
        // If this card is included in player's hand, adds an additional power equal to the total of
        // all normal offense/defense cards
        bool isPower0CardIncluded = false;

        // Total sum of normal support cards
        int totalNormalPower = 0;

        // used to check whether or not an unstackable card was already used
        var unstackableCards = new List<int>();

        // Calc attacker hand
        // Loop through every card the attacker has in his hand
        foreach (var card in atkPlayer.CurrentHand.GetCardsInHand)
        {
            var effectOne = card.effectOne;

            if (card.Type == PlayCardType.Offense)
            {
                // Card type is OFFENSE.
                // Calc effects of EffectOne
                
                (bool isTriggered, int multiplier) = CheckReqCode(atkPlayer, defPlayer, effectOne.reqCode, true);

                if (isTriggered)
                {
                    //use triggeredPower if triggered
                    int power = effectOne.triggeredPower == 0 ? effectOne.basePower : effectOne.triggeredPower;
                    atkPlayer.CurrentPepemonStats.atk += power * multiplier;
                    totalNormalPower += effectOne.triggeredPower * multiplier;
                }
                else
                {
                    //use basePower if not
                    atkPlayer.CurrentPepemonStats.atk += effectOne.basePower;
                    totalNormalPower += effectOne.basePower;
                }
                
            }
            else if (card.Type == PlayCardType.SpecialOffense)
            {
                // Card type is STRONG OFFENSE.
                //Make sure unstackable cards can't be stacked
                if (card.Unstackable)
                {
                    // Check if card is new to previous cards
                    if (unstackableCards.Contains(card.ID) ||
                        // Check if card is new to temp support info cards
                        atkPlayer.CurrentHand.GetTableSupportCards.Any(c => c.ID == card.ID))
                    {
                        // If it isn't being used for the first time - skip card
                        continue;
                    }
                    unstackableCards.Add(card.ID);
                }

                // Calc effects of EffectOne

                (bool isTriggered, int multiplier) = CheckReqCode(atkPlayer, defPlayer, effectOne.reqCode, true);
                if (isTriggered)
                {
                    if (multiplier > 1)
                    {
                        int power = effectOne.triggeredPower == 0 ? effectOne.basePower : effectOne.triggeredPower;
                        atkPlayer.CurrentPepemonStats.atk += power * multiplier;
                    }
                    else
                    {
                        if (effectOne.effectTo == EffectTo.SpecialAttack)
                        {
                            // If it's a use Special Attack instead of Attack card
                            atkPlayer.CurrentPepemonStats.atk = atkPlayer.CurrentPepemonStats.sAtk;
                            continue;
                        }
                        else if (effectOne.triggeredPower == 0)
                        {
                            // We have a card that says ATK is increased by amount
                            // Equal to the total of all offense cards in the current turn
                            isPower0CardIncluded = true;
                            continue;
                        }
                        int power = effectOne.triggeredPower == 0 ? effectOne.basePower : effectOne.triggeredPower;
                        atkPlayer.CurrentPepemonStats.atk += power;
                    }
                }
                else
                {
                    //If not triggered: use base power instead
                    atkPlayer.CurrentPepemonStats.atk += effectOne.basePower;
                    totalNormalPower += effectOne.basePower;
                }


                // If card lasts for >1 turns, Add card to table if <5 on table currently
                if (card.effectMany.power != 0 && atkPlayer.CurrentHand.GetCardsInHand.Count < 5)
                {
                    // todo: add animation to display card being added
                    atkPlayer.CurrentHand.AddCardToTable(card);
                }
            }

        }

        if (isPower0CardIncluded)
        {
            //If we have a card that says ATK is increased by amount equal to total of all offense cards
            atkPlayer.CurrentPepemonStats.atk += totalNormalPower;
        }

        // Calc defense hand
        isPower0CardIncluded = false;
        totalNormalPower = 0;
        unstackableCards.Clear();

        foreach (var card in defPlayer.CurrentHand.GetCardsInHand)
        {
            var effectOne = card.effectOne;

            if (card.Type == PlayCardType.Defense)
            {
                // Card type is DEFENSE.
                // Calc effects of EffectOne       

                (bool isTriggered, int multiplier) = CheckReqCode(atkPlayer, defPlayer, effectOne.reqCode, true);
                if (isTriggered)
                {
                    //use triggeredPower if triggered
                    int power = effectOne.triggeredPower == 0 ? effectOne.basePower : effectOne.triggeredPower;
                    defPlayer.CurrentPepemonStats.def += power * multiplier;
                    totalNormalPower += effectOne.triggeredPower * multiplier;
                }
                else
                {
                    //use basePower if not
                    defPlayer.CurrentPepemonStats.def += effectOne.basePower;
                    totalNormalPower += effectOne.basePower;
                }
            }
            else if (card.Type == PlayCardType.SpecialDefense)
            {
                // Card type is STRONG DEFENSE
                //Make sure unstackable cards can't be stacked
                if (card.Unstackable)
                {
                    // Check if card is new to previous cards
                    if (unstackableCards.Contains(card.ID) ||
                        // Check if card is new to temp support info cards
                        defPlayer.CurrentHand.GetCardsInHand.Any(c => c.ID == card.ID))
                    {
                        // If it isn't being used for the first time - skip card
                        continue;
                    }
                    unstackableCards.Add(card.ID);
                }

                // Calc effects of EffectOne
                
                (bool isTriggered, int num) = CheckReqCode(atkPlayer, defPlayer, effectOne.reqCode, true);
                if (isTriggered)
                {
                    if (num > 0)
                    {
                        defPlayer.CurrentPepemonStats.def += effectOne.triggeredPower * num;
                    }
                    else
                    {
                        if (effectOne.effectTo == EffectTo.SpecialDefense)
                        {
                            // If it's a use Special Defense instead of Defense card
                            defPlayer.CurrentPepemonStats.def = defPlayer.CurrentPepemonStats.sDef;
                            continue;
                        }
                        else if (effectOne.triggeredPower == 0)
                        {
                            // Equal to the total of all defense cards in the current turn
                            isPower0CardIncluded = true;
                            continue;
                        }
                        int power = effectOne.triggeredPower == 0 ? effectOne.basePower : effectOne.triggeredPower;
                        defPlayer.CurrentPepemonStats.def += power;
                    }
                }
                else
                {
                    //If not triggered: use base power instead
                    defPlayer.CurrentPepemonStats.def += effectOne.basePower;
                    totalNormalPower += effectOne.basePower;
                }


                // If card lasts for >1 turns, Add card to table if <5 on table currently
                if (card.effectMany.power != 0 && defPlayer.CurrentHand.GetCardsInHand.Count < 5)
                {
                    // todo: add animation to display card being added
                    defPlayer.CurrentHand.AddCardToTable(card);
                }
            }
        }

        if (isPower0CardIncluded)
        {
            //If we have a card that says DEF is increased by amount equal to total of all defense cards
            defPlayer.CurrentPepemonStats.def += totalNormalPower;
        }
    }

    // Note: Same logic of "resistanceWeaknessCal" in PepemonBattle.sol
    private int CalcResistanceWeakness(Player atkPlayer, Player defPlayer)
    {
        if (atkPlayer.PlayerPepemon.Type == defPlayer.PlayerPepemon.Weakness)
        {
            return 2;
        }
        else if (atkPlayer.PlayerPepemon.Type == defPlayer.PlayerPepemon.Resistence)
        {
            return -2;
        }
        return 0;
    }

    /// <summary>
    /// Checks if the requirements are satisfied for a certain code
    /// Note: Same logic of the contract PepemonBattle.sol
    /// </summary>
    /// <returns>bool telling if a requirement was satisfied, and a multiplier for the card's attack power</returns>
    private static Tuple<bool, int> CheckReqCode(Player atkPlayer, Player defPlayer, int reqCode, bool isAttacker)
    {
        bool isTriggered = false;
        int multiplier = 1;
        switch (reqCode)
        {
            case 0:
                // No requirement
                isTriggered = true;
                break;
            case 1:
                // Intelligence of offense pepemon <= 5.
                isTriggered = atkPlayer.CurrentPepemonStats.inte <= 5;
                break;
            case 2:
                // Number of defense cards of defense pepemon is 0.
                isTriggered = defPlayer.CurrentHand.AllDefenseCards.Count == 0;
                break;
            case 3:
                // Each +2 offense cards of offense pepemon.
                multiplier = atkPlayer.CurrentHand.AllOffenseCards.Count(
                    card => card.effectOne.basePower == 2 && card.effectOne.effectTo == EffectTo.Attack);
                isTriggered = multiplier > 0;
                break;
            case 4:
                // Each +3 offense cards of offense pepemon.
                multiplier = atkPlayer.CurrentHand.AllOffenseCards.Count(
                    card => card.effectOne.basePower == 3 && card.effectOne.effectTo == EffectTo.Attack);
                isTriggered = multiplier > 0;
                break;
            case 5:
                // Each offense card of offense pepemon.
                multiplier = atkPlayer.CurrentHand.AllOffenseCards.Count;
                isTriggered = multiplier > 0;
                break;
            case 6:
                // Each +3 defense card of defense pepemon.
                multiplier = defPlayer.CurrentHand.AllOffenseCards.Count(
                    card => card.effectOne.basePower == 3 && card.effectOne.effectTo == EffectTo.Defense);
                isTriggered = multiplier > 0;
                break;
            case 7:
                // Each +3 defense card of defense pepemon.
                multiplier = defPlayer.CurrentHand.AllOffenseCards.Count(
                    card => card.effectOne.basePower == 4 && card.effectOne.effectTo == EffectTo.Defense);
                isTriggered = multiplier > 0;
                break;
            case 8:
                // Intelligence of defense pepemon <= 5.
                isTriggered = (defPlayer.CurrentPepemonStats.inte <= 5);
                break;
            case 9:
                // Intelligence of defense pepemon >= 7.
                isTriggered = (defPlayer.CurrentPepemonStats.inte >= 7);
                break;
            case 10:
                // Offense pepemon is using strong attack
                isTriggered = atkPlayer.CurrentHand.AllSpecialOffenseCards.Count > 0;
                break;
            case 11:
                // The current HP is less than 50% of max HP.
                var player = defPlayer;
                if (isAttacker)
                {
                    player = atkPlayer;
                }
                isTriggered = player.CurrentHP * 2 <= player.PlayerPepemon.HealthPoints;
                break;
            default:
                break;
        }
        return new(isTriggered, multiplier);
    }

    // Used to display the current round. Note that current round must begin at 0 because its how it works in the blockchain
    public int GetRoundNumber() => _roundNumber + 1;


    void BattleResut(Player winner)
    {
        var player1won = ulong.Parse(winner.PlayerPepemon.ID) == BattlePrepController.battleData.player1BattleCard;
        // when player1won=false and currentPlayerIsPlayer1=false, currentPlayerWon=true
        // because player2 won and current player is Player2
        var currentPlayerWon = player1won == BattlePrepController.battleData.currentPlayerIsPlayer1;
        _uiController.DisplayBattleResult(winner, currentPlayerWon);
        _gameHasFinished = true;
    }

    private void AttackerUI(Attacker attacker, bool disable = false)
    {
        bool isPlayer1Attacker = attacker == Attacker.PLAYER_ONE;

        _attackerUIElements[0].SetActive(isPlayer1Attacker && !disable);
        _attackerUIElements[1].SetActive(!isPlayer1Attacker && !disable);
        _attackerUIElements[2].SetActive(!isPlayer1Attacker && !disable);
        _attackerUIElements[3].SetActive(isPlayer1Attacker && !disable);
    }

    private void AttackDisplay(bool isPlayer1, int dmg)
    {
        if (isPlayer1)
        {
            dmgText1.text = "-" + dmg.ToString();
            dmgText1.DOFade(1f, 1.5f);
        }
        else
        {
            dmgText2.text = "-" + dmg.ToString();
            dmgText2.DOFade(1f, 1.5f);
        }

        SFXManager.Instance.HitSFX();
    }

    private void DisableAttackTexts()
    {
        dmgText1.DOFade(0f, 1f);
        dmgText2.DOFade(0f, 1f);
    }
}
