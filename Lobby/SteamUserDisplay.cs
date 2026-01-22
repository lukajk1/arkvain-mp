#if STEAMWORKS_NET
using Steamworks;
#endif
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SteamUserDisplay : MonoBehaviour
{
#if STEAMWORKS_NET
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TextMeshProUGUI displayName;

    private Callback<AvatarImageLoaded_t> _avatarImageLoadedCallback;
    private CSteamID _currentSteamId;

    private void Awake()
    {
        Debug.Log("[STEAM AVATAR] SteamAvatarDisplay Awake called");
    }

    private void OnEnable()
    {
        Debug.Log("[STEAM AVATAR] SteamAvatarDisplay OnEnable called");
        if (IsSteamAvailable())
        {
            Debug.Log("[STEAM AVATAR] Steam is available, creating callback");
            _avatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
        }
        else
        {
            Debug.LogWarning("[STEAM AVATAR] Steam is NOT available in OnEnable");
        }
    }

    private void Start()
    {
        Debug.Log("[STEAM AVATAR] Start called, loading local user's avatar");
        LoadLocalAvatar();
    }

    private void OnDisable()
    {
        _avatarImageLoadedCallback?.Dispose();
    }

    /// <summary>
    /// Load and display the avatar for the given Steam ID
    /// </summary>
    /// <param name="steamId">Steam ID as string</param>
    public void LoadAvatar(string steamId)
    {
        Debug.Log($"[STEAM AVATAR] LoadAvatar called with Steam ID: {steamId}");

        if (!IsSteamAvailable())
        {
            Debug.LogWarning("[STEAM AVATAR] Steam is not available.");
            return;
        }

        if (!ulong.TryParse(steamId, out ulong id))
        {
            Debug.LogError($"[STEAM AVATAR] Failed to parse Steam ID: {steamId}");
            return;
        }

        _currentSteamId = new CSteamID(id);
        Debug.Log($"[STEAM AVATAR] Parsed Steam ID successfully, loading avatar...");
        UpdateDisplayName(_currentSteamId);
        LoadAvatarForSteamId(_currentSteamId);
    }

    /// <summary>
    /// Load and display the avatar for the local Steam user
    /// </summary>
    public void LoadLocalAvatar()
    {
        if (!IsSteamAvailable())
        {
            Debug.LogWarning("[STEAM AVATAR] Steam is not available.");
            return;
        }

        _currentSteamId = SteamUser.GetSteamID();
        UpdateDisplayName(_currentSteamId);
        LoadAvatarForSteamId(_currentSteamId);
    }

    private void LoadAvatarForSteamId(CSteamID steamId)
    {
        var avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);

        if (avatarHandle == -1)
        {
            Debug.Log($"[STEAM AVATAR] Avatar not cached yet for {steamId}, waiting for callback...");
            return;
        }

        LoadAvatarFromHandle(avatarHandle);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        if (callback.m_steamID != _currentSteamId)
            return;

        if (callback.m_iImage == -1)
        {
            Debug.LogWarning($"[STEAM AVATAR] Failed to load avatar for {_currentSteamId}");
            return;
        }

        LoadAvatarFromHandle(callback.m_iImage);
    }

    private void LoadAvatarFromHandle(int avatarHandle)
    {
        if (!SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height))
        {
            Debug.LogError("[STEAM AVATAR] Failed to get image size.");
            return;
        }

        byte[] imageBuffer = new byte[width * height * 4]; // RGBA

        if (!SteamUtils.GetImageRGBA(avatarHandle, imageBuffer, imageBuffer.Length))
        {
            Debug.LogError("[STEAM AVATAR] Failed to get image RGBA data.");
            return;
        }

        Texture2D avatar = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        avatar.LoadRawTextureData(imageBuffer);
        FlipTextureVertically(avatar);
        avatar.Apply();

        if (avatarImage != null)
        {
            avatarImage.texture = avatar;
            Debug.Log($"[STEAM AVATAR] Avatar loaded successfully for {_currentSteamId}");
        }
        else
        {
            Debug.LogError("[STEAM AVATAR] RawImage component is not assigned!");
        }
    }

    private void FlipTextureVertically(Texture2D texture)
    {
        var pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var topPixel = pixels[y * width + x];
                var bottomPixel = pixels[(height - 1 - y) * width + x];

                pixels[y * width + x] = bottomPixel;
                pixels[(height - 1 - y) * width + x] = topPixel;
            }
        }

        texture.SetPixels(pixels);
    }

    private void UpdateDisplayName(CSteamID steamId)
    {
        if (displayName != null)
        {
            string name = SteamFriends.GetFriendPersonaName(steamId);
            displayName.text = name;
            Debug.Log($"[STEAM AVATAR] Display name set to: {name}");
        }
        else
        {
            Debug.LogWarning("[STEAM AVATAR] DisplayName TextMeshProUGUI is not assigned!");
        }
    }

    private bool IsSteamAvailable()
    {
        try
        {
            InteropHelp.TestIfAvailableClient();
            return true;
        }
        catch
        {
            return false;
        }
    }
#else
    private void Awake()
    {
        Debug.LogWarning("[STEAM AVATAR] Steamworks.NET is not available. Avatar display disabled.");
    }
#endif
}
