using PurrNet;
using PurrNet.Prediction;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A predicted identity that tracks a single player's loadout selection.
/// One instance of this is spawned for every player and owned by them.
/// </summary>
public class PlayerLoadoutState : PredictedIdentity<PlayerLoadoutState.LoadoutInput, PlayerLoadoutState.LoadoutData>
{
    private static readonly Dictionary<PlayerID, PlayerLoadoutState> _allLoadouts = new();

    public static PlayerLoadoutState GetLoadout(PlayerID player)
    {
        _allLoadouts.TryGetValue(player, out var loadout);
        return loadout;
    }

    [Header("Current Selection")]
    public HeroType hero;
    public int weaponIndex;

    protected override void LateAwake()
    {
        base.LateAwake();
        if (owner.HasValue)
        {
            _allLoadouts[owner.Value] = this;
            Debug.Log($"[PlayerLoadoutState] Registered loadout for player {owner.Value}");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (owner.HasValue)
        {
            _allLoadouts.Remove(owner.Value);
        }
    }

    /// <summary>
    /// Called by the UI (LoadoutManager) to update the local player's intent.
    /// </summary>
    public void SetIntendedLoadout(HeroType h, int w)
    {
        hero = h;
        weaponIndex = w;
    }

    protected override void UpdateInput(ref LoadoutInput input)
    {
        // Sample the values set by the UI
        input.selectedHero = hero;
        input.selectedWeapon = weaponIndex;
    }

    protected override void Simulate(LoadoutInput input, ref LoadoutData state, float delta)
    {
        state.hero = input.selectedHero;
        state.weaponIndex = input.selectedWeapon;
    }

    // This is where we sync the internal state back to the public fields for easy access
    protected override void UpdateView(LoadoutData viewState, LoadoutData? verified)
    {
        hero = viewState.hero;
        weaponIndex = viewState.weaponIndex;
    }

    protected override LoadoutData GetInitialState()
    {
        return new LoadoutData { hero = HeroType.Richter, weaponIndex = 1 }; // Default to Player/Deagle
    }

    public struct LoadoutInput : IPredictedData<LoadoutInput>
    {
        public HeroType selectedHero;
        public int selectedWeapon;
        public void Dispose() { }
    }

    public struct LoadoutData : IPredictedData<LoadoutData>
    {
        public HeroType hero;
        public int weaponIndex;
        public void Dispose() { }
    }
}
