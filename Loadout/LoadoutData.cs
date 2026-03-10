public enum HeroType
{
    Richter
}

public enum WeaponType
{
    Crossbow,
    LightningGun,
    Revolver,
    Rifle
}

[System.Serializable]
public struct LoadoutSelection
{
    public HeroType Hero;
    public WeaponType Weapon;
}
