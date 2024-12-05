using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// MonoBehaviour for Screen_4_ManageDecks
/// </summary>
public class ScreenManageDecks : MonoBehaviour
{
    [TitleGroup("Component References"), SerializeField] public GameObject _editDeckListLoader;
    [TitleGroup("Component References"), SerializeField] public GameObject _screenEditDeck;

    private void Start()
    {
        _editDeckListLoader.GetComponent<DeckListLoader>().onEditDeck.AddListener(SelectEditDeck);
    }

    public void SelectEditDeck(ulong deckId)
    {
        FindObjectOfType<MainMenuController>().ShowScreen(MainSceneScreensEnum.EditDeck);
        _screenEditDeck.GetComponent<ScreenEditDeck>().LoadAllCards(deckId, FilterController.Instance.currentFilter);
    }

    public void ReloadAllDecks()
    {
        _editDeckListLoader.GetComponent<DeckListLoader>().ReloadAllDecks();
    }
}
