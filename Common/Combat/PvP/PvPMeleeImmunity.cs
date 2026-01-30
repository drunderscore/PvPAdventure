using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Combat.PvP;

/// <summary>
/// - Prevents melee PvP from hitting the same target more than once per swing.
/// </summary>
internal sealed class PvPMeleeImmunity : ModPlayer
{
    private int _currentMeleeUseId;
    private int _lastItemAnimation;

    private readonly Dictionary<(int attackerWho, int useId), int> _meleeImmuneBySwing = [];

    // Buffers to avoid modifying the dictionary while enumerating it.
    private readonly List<(int attackerWho, int useId)> _keyBuffer = [];
    private readonly List<(int attackerWho, int useId)> _expiredBuffer = [];

    public override bool CanHitPvp(Item item, Player target)
    {
        UpdateMeleeUseId();

        var targetImmunity = target.GetModPlayer<PvPMeleeImmunity>();

        if (targetImmunity._meleeImmuneBySwing.TryGetValue((Player.whoAmI, _currentMeleeUseId), out var remainingFrames))
        {
            if (remainingFrames > 0)
                return false;
        }

        int immunityFrames = Player.itemAnimation + 2;
        targetImmunity._meleeImmuneBySwing[(Player.whoAmI, _currentMeleeUseId)] = immunityFrames;

        return true;
    }

    public override void PreUpdate()
    {
        UpdateMeleeUseId();
        TickMeleeSwingImmunity();
    }

    private void UpdateMeleeUseId()
    {
        int anim = Player.itemAnimation;

        if (anim > 0 && _lastItemAnimation == 0)
            _currentMeleeUseId++;

        _lastItemAnimation = anim;
    }

    private void TickMeleeSwingImmunity()
    {
        if (_meleeImmuneBySwing.Count == 0)
            return;

        _keyBuffer.Clear();
        foreach (var key in _meleeImmuneBySwing.Keys)
            _keyBuffer.Add(key);

        _expiredBuffer.Clear();

        for (int i = 0; i < _keyBuffer.Count; i++)
        {
            var key = _keyBuffer[i];

            int t = _meleeImmuneBySwing[key] - 1;

            if (t <= 0)
            {
                _expiredBuffer.Add(key);
                continue;
            }

            _meleeImmuneBySwing[key] = t;
        }

        for (int i = 0; i < _expiredBuffer.Count; i++)
            _meleeImmuneBySwing.Remove(_expiredBuffer[i]);
    }
}
