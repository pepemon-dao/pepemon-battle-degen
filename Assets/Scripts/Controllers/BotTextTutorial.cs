using NBitcoin;
using Org.BouncyCastle.Utilities.IO.Pem;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BotTextTutorial : MonoBehaviour
{
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject quitButton;
    [SerializeField] private TMP_Text tutorialText;
    [SerializeField] private List<string> tutorials = new List<string>();

    private bool isInTutorial = false;

    public bool IsInTutorial { get { return isInTutorial; } }

    private int currentTutorialStateIndex = 0;
    private const string TUTORIAL_STATE_KEY = "TUTORIAL_STATE_INDEX";

    public static BotTextTutorial Instance;

    public bool wasInTutorial { get; private set; } = false;

    private void Start()
    {
        Instance = this;
        currentTutorialStateIndex = PlayerPrefs.GetInt(TUTORIAL_STATE_KEY, 0);
    }

    private void Update()
    {
        if (isInTutorial)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayerPrefs.SetInt(TUTORIAL_STATE_KEY, currentTutorialStateIndex);
                Hide();
            }
        }
    }

    private void Hide()
    {
        Time.timeScale = 1f;
        isInTutorial = false;
        tutorialPanel.SetActive(false);
        quitButton.SetActive(true);
    }

    private void Show()
    {
        Time.timeScale = 0f;
        isInTutorial = true;
        wasInTutorial = true;
        tutorialText.text = tutorials[currentTutorialStateIndex - 1].ToString();
        tutorialPanel.SetActive(true);
        quitButton.SetActive(false);
    }

    public void TriggerTutorialEvent(int eventId)
    {
        if (eventId > currentTutorialStateIndex)
        {
            currentTutorialStateIndex = eventId;
            Show();
        }
    }
}
