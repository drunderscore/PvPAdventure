namespace PvPAdventure.Common.Skins;

public readonly record struct SkinIdentity(string Prototype, string Name)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Prototype) && !string.IsNullOrWhiteSpace(Name);
}