using System.Collections.Generic;
using UnityEngine;

public class CardTextureLookup : MonoBehaviour
{
    public static CardTextureLookup Instance { get; private set; }

    private Dictionary<ulong, TextureData> textureLookup = new Dictionary<ulong, TextureData>();

    private void Awake()
    {
        // Ensure that there's only one instance of CardTextureLookup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public TextureData GetTextureData(ulong cardId)
    {
        if (textureLookup.TryGetValue(cardId, out var textureData))
        {
            return textureData; // Return cached TextureData
        }

        // If not found, load the texture and create sprite
        var texture = PepemonFactoryCardCache.GetImage(cardId); // Your method to load texture
        if (texture != null)
        {
            textureData = new TextureData(texture); // Create TextureData
            textureLookup[cardId] = textureData; // Cache it
        }

        return textureData; // Return TextureData (may be null if not found)
    }
}


[System.Serializable]
public class TextureData
{
    public Sprite Sprite;

    public TextureData(Texture2D texture)
    {
        Sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}

