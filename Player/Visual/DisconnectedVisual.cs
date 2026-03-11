using PurrNet;
using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the visual representation of a disconnected player body.
/// Listens to the global player list and shows an icon if the owner of this body is missing.
/// </summary>
public class DisconnectedVisual : StatelessPredictedIdentity
{
    [SerializeField] private Image _disconnectedIcon;
    
    private bool _isSubscribed;

    protected override void LateAwake()
    {
        base.LateAwake();
        
        if (predictionManager != null && predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded += OnPlayerAdded;
            predictionManager.players.onPlayerRemoved += OnPlayerRemoved;
            _isSubscribed = true;
            
            // Initial check: is our owner currently in the list?
            CheckOwnerPresence();
        }

        if (_disconnectedIcon != null) _disconnectedIcon.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        
        if (_isSubscribed && predictionManager != null && predictionManager.players != null)
        {
            predictionManager.players.onPlayerAdded -= OnPlayerAdded;
            predictionManager.players.onPlayerRemoved -= OnPlayerRemoved;
        }
    }

    private void OnPlayerAdded(PlayerID id)
    {
        if (id == owner)
        {
            SetIconState(false);
        }
    }

    private void OnPlayerRemoved(PlayerID id)
    {
        if (id == owner)
        {
            SetIconState(true);
        }
    }

    private void CheckOwnerPresence()
    {
        if (!owner.HasValue) return;

        var players = predictionManager.players.currentState.players;
        bool isPresent = false;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == owner.Value)
            {
                isPresent = true;
                break;
            }
        }

        SetIconState(!isPresent);
    }

    private void SetIconState(bool visible)
    {
        if (_disconnectedIcon != null)
        {
            _disconnectedIcon.gameObject.SetActive(visible);
        }
    }
}
