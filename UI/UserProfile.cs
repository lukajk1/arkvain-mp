using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Heathen.SteamworksIntegration;

public class UserProfile : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TMP_Text userNameText;
    private bool initialized;

    void Awake()
    {
        TextUtilities.ConfigureEllipsisOverflow(userNameText);
    }

    private void Update()
    {
        if (!initialized)
        {
            if (InitializeSteamworksHeathen.SteamInitialized)
            {
                LoadUserProfile();
                initialized = true;
            }
        }
    }

    private void LoadUserProfile()
    {
        // Get local user data
        UserData localUser = UserData.Me;

        // Set username
        if (userNameText != null)
        {
            userNameText.text = localUser.Name;
        }

        // Load avatar
        if (avatarImage != null)
        {
            localUser.LoadAvatar(OnAvatarLoaded);
        }
    }

    private void OnAvatarLoaded(Texture2D avatar)
    {
        if (avatarImage != null && avatar != null)
        {
            avatarImage.texture = avatar;
        }
    }
}
