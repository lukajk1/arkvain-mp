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
    public int weapon1Index;
    public int weapon2Index;

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
    public void SetIntendedLoadout(HeroType h, int w1, int w2)
    {
        hero = h;
        weapon1Index = w1;
        weapon2Index = w2;
    }

    protected override void UpdateInput(ref LoadoutInput input)
    {
        // Sample the values set by the UI
        input.selectedHero = hero;
        input.selectedWeapon1 = weapon1Index;
        input.selectedWeapon2 = weapon2Index;
    }

    protected override void Simulate(LoadoutInput input, ref LoadoutData state, float delta)
    {
        state.hero = input.selectedHero;
        state.weapon1Index = input.selectedWeapon1;
        state.weapon2Index = input.selectedWeapon2;
    }

    // This is where we sync the internal state back to the public fields for easy access
    protected override void UpdateView(LoadoutData viewState, LoadoutData? verified)
    {
        hero = viewState.hero;
        weapon1Index = viewState.weapon1Index;
        weapon2Index = viewState.weapon2Index;
    }

    protected override LoadoutData GetInitialState()
    {
        return new LoadoutData { hero = HeroType.Richter, weapon1Index = 0, weapon2Index = 2 }; // Default to Crossbow/Revolver
    }

    public struct LoadoutInput : IPredictedData<LoadoutInput>
    {
        public HeroType selectedHero;
        public int selectedWeapon1;
        public int selectedWeapon2;
        public void Dispose() { }
    }

    public struct LoadoutData : IPredictedData<LoadoutData>
    {
        public HeroType hero;
        public int weapon1Index;
        public int weapon2Index;
        public void Dispose() { }
    }
}
