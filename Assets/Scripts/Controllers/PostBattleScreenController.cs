using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Scripts.Managers.Sound;
using Pepemon.Battle;
using Cysharp.Threading.Tasks.Triggers;

[RequireComponent(typeof(CanvasGroup)), RequireComponent(typeof(Animator))]
public class PostBattleScreenController : MonoBehaviour
{
    #region Enums
    protected enum ScreenState { SHOWN, HIDDEN }
    #endregion

    #region Constants
    protected const string VICTORY_TEXT = "VICTORY!";
    protected const string YOU_WIN_TEXT = "YOU WIN";

    protected const string DEFEAT_TEXT = "DEFEAT";
    protected const string YOU_LOSE_TEXT = "YOU LOSE";

    protected const string RANKING_GAIN = "Gained +#pts";
    protected const string RANKING_LOSS = "Lost -#pts";
    #endregion

    #region Editor Exposed Data
    [Title("Screen Settings")]
    [SerializeField] bool _startHidden = false;

    [SerializeField] CardPreview _pepemon;
    [SerializeField] TextReveal _victoryDefeat;
    [SerializeField] TextReveal _youWinLose;
    [SerializeField] GameObject _rewardDisplay;
    [SerializeField] GameObject _winDisplay;
    [SerializeField] GameObject _loseDisplay;
    [SerializeField] GameObject _winMsg;
    [SerializeField] GameObject _loseMsg;
    [SerializeField] GameObject _starterPackMsg;
    [SerializeField] GameObject _starterPackMsg2;
    [SerializeField] Button _btnShowMenu;
    [SerializeField] Button _btnPlayAgain;
    [SerializeField] Button _btnClaimGift;

    [Title("Screen Events")]
    private UnityEvent OnShown;
    private UnityEvent OnHidden;

    [Title("Debugging")]
    [Button(Name = "DEBUG Show lose")] private void Debug_Show_Lose() { SetResult(false); Show(); }
    [Button(Name = "DEBUG Show win")] private void Debug_Show_Win() { SetResult(true); Show(); }
    [Button(Name = "DEBUG Hide")] private void Debug_Hide() => Hide();
    #endregion

    #region Object Data
    protected ScreenState _state = ScreenState.SHOWN;

    protected bool _animating = false;
    #endregion

    #region Onboard Components
    protected CanvasGroup _canvasGroup;
    protected Animator _animator;
    #endregion

    #region EndTransitionBackToMenu
    public static bool IsGoingFromBattle = false;
    public static bool IsPlayingAgain = false;
    public static bool IsClaimingGift = false;
    #endregion

    public void SetResult(bool win)
    {
        //_victoryDefeat.SetText(win ? VICTORY_TEXT : DEFEAT_TEXT);
        //_youWinLose.SetText(win ? YOU_WIN_TEXT : YOU_LOSE_TEXT);
        _winDisplay.SetActive(win);
        _loseDisplay.SetActive(!win);

        // TODO: replace this with the actual gain/loss

        bool isBotMatch = BattlePrepController.battleData.isBotMatch;
        bool isTutorial = BotTextTutorial.Instance.wasInTutorial;
        bool claimedStarterPack = PlayerPrefs.GetInt("GotStarterPack", 0) == 1;

        if (isTutorial || claimedStarterPack)
        {
            _rewardDisplay.GetComponentInChildren<TextReveal>()
            .SetText((win ? RANKING_GAIN : RANKING_LOSS).Replace("#", "10"));
        }
        else
        {
            _rewardDisplay.SetActive(false);
            _rewardDisplay.GetComponentInChildren<TextReveal>()
            .SetText(win && !claimedStarterPack ? "Gained Starter Pack" : "");
        }

        _winMsg.GetComponent<TMPro.TMP_Text>().text = "You won! ";
        _loseMsg.GetComponent<TMPro.TMP_Text>().text = "You lost! ";

        _btnClaimGift.gameObject.SetActive(isBotMatch && !claimedStarterPack);
        /*
        bool winMsgShouldBeDisplayed = ((isBotMatch && claimedStarterPack) || !isBotMatch) && win;
        _winMsg.SetActive(winMsgShouldBeDisplayed);
        bool loseMsgShouldBeDisplayed = ((isBotMatch && claimedStarterPack) || !isBotMatch) && !win;
        _loseMsg.SetActive(loseMsgShouldBeDisplayed);
        */

        _winMsg.GetComponent<TMPro.TMP_Text>().text += (isBotMatch && !claimedStarterPack) ? "claim your starter pack" : "- Score will be updated in Leaderboards";
        //_starterPackMsg.SetActive(isBotMatch && !claimedStarterPack);
        _loseMsg.GetComponent<TMPro.TMP_Text>().text += (isBotMatch && !claimedStarterPack) ? "claim your starter pack" : "- Better luck next time";
        //_starterPackMsg2.SetActive(isBotMatch && !claimedStarterPack);
        _btnShowMenu.gameObject.SetActive(true);
        _btnPlayAgain.gameObject.SetActive((isBotMatch && claimedStarterPack) || !isBotMatch);

        ThemePlayer.Instance.PlayGameOverSong(win);
    }

    public void LoadPepemonDisplay(ulong cardId)
    {
        _pepemon.LoadCardData(cardId, false);
    }

    public void OnBtnPlayAgainClick()
    {
        bool isBotMatch = BattlePrepController.battleData.isBotMatch;
        if (isBotMatch)
        {
            ThemePlayer.Instance.SkipEndGameMusic();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            IsPlayingAgain = true;
            OnBtnShowMenuClick();
        }
        
    }

    public void OnBtnShowMenuClick()
    {
        // go back to previous scene
        IsGoingFromBattle = true;

        //reset the bot battle values
        Web3Controller.instance.StarterDeckID = 0;
        Web3Controller.instance.StarterPepemonID = 0;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex - 1);
    }
    
    public void OnBtnClaimGiftClick()
    {
        // go back to previous scene
        IsGoingFromBattle = true;
        IsClaimingGift = true;

        if (!Web3Controller.instance.IsConnected)
        {
            Web3Controller.instance.ConnectWallet();
        }

        InvokeRepeating(nameof(CheckIfWalletConnected), 0.5f, 0.3f);
    }

    private void CheckIfWalletConnected()
    {
        if (Web3Controller.instance.IsConnected)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex - 1);
        }
    }

    #region INIT
    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        _state = _startHidden ? ScreenState.HIDDEN : ScreenState.SHOWN;
        _btnShowMenu.onClick.AddListener(OnBtnShowMenuClick);
        _btnPlayAgain.onClick.AddListener(OnBtnPlayAgainClick);

        _btnClaimGift.onClick.AddListener(OnBtnClaimGiftClick);
    }

    private void OnValidate()
    {
        var cg = GetComponent<CanvasGroup>();
        if (_startHidden)
        {
            cg.alpha = 0;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }
        else
        {
            cg.alpha = 1;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
    }
    #endregion

    #region State Management Logic
    protected async virtual void UpdateState(ScreenState newState)
    {
        if (_animating) return;
        if (newState == _state) return;
        _state = newState;
        _animating = true;

        switch (_state)
        {
            case ScreenState.SHOWN:
                await Show_Task();
                OnShown?.Invoke();
                break;
            case ScreenState.HIDDEN:
                await Hide_Task();
                OnHidden?.Invoke();
                break;
        }

        _animating = false;
    }

    public virtual void Show() => UpdateState(ScreenState.SHOWN);
    public virtual void Hide() => UpdateState(ScreenState.HIDDEN);
    #endregion

    #region Show/Hide Logic
    protected async virtual Task Show_Task()
    {
        _animator.SetTrigger("OnShow");

        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Show Complete"))
            await Task.Delay(100);
    }
    protected async virtual Task Hide_Task()
    {
        _animator.SetTrigger("OnHide");

        while (!_animator.GetCurrentAnimatorStateInfo(0).IsName("Hide Complete"))
            await Task.Delay(100);
    }
    #endregion
}
