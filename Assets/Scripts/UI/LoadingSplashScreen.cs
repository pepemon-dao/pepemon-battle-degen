using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class LoadingSplashScreen : MonoBehaviour
{
    public TMP_Text loadingMainText;
    public TMP_Text loadingSubText;
    public UnityEvent finishedLoading;

    public async void StartLoading()
    {
        loadingMainText.text = "Loading card metadata...";
        await PepemonFactoryCardCache.PreloadAllMetadata(10, 10, (id) =>
        {
            loadingSubText.text = $"Loaded metadata for card {id}";
        });

        loadingMainText.text = "Loading card images...";
        await PepemonFactoryCardCache.PreloadAllImages(8, (id) =>
        {
            loadingSubText.text = $"Loaded image for card {id}";
        });

        finishedLoading?.Invoke();
    }
}
