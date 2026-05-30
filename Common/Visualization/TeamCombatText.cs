using Microsoft.Xna.Framework;
using PvPAdventure.Core.Config;
using PvPAdventure.Core.Net;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace PvPAdventure.Common.Visualization;

internal enum SourceTeamCombatTextTarget : byte
{
    NPC,
    Player
}

internal readonly record struct SourceTeamCombatTextSource(int SourcePlayer, int Team, ulong DirectUntil, ulong DotUntil);

internal readonly record struct PendingSourceTeamCombatText(int Index, Rectangle Location, string DamageText, ulong Until);

internal sealed class TeamCombatText : ModSystem
{
    private static bool Enabled => ModContent.GetInstance<ServerConfig>().UseTeamCombatText;

    private const int DirectContextTicks = 45;
    private const int DotContextTicks = 900;
    private const int PendingTextTicks = 30;

    private readonly SourceTeamCombatTextSource[] npcSources = new SourceTeamCombatTextSource[Main.maxNPCs];
    private readonly SourceTeamCombatTextSource[] playerSources = new SourceTeamCombatTextSource[Main.maxPlayers];
    private readonly List<PendingSourceTeamCombatText> pendingTexts = [];

    public override void Load()
    {
        if (Main.dedServ)
            return;

        On_CombatText.NewText_Rectangle_Color_int_bool_bool += OnNewIntText;
        On_CombatText.NewText_Rectangle_Color_string_bool_bool += OnNewStringText;
    }

    public override void Unload()
    {
        if (Main.dedServ)
            return;

        On_CombatText.NewText_Rectangle_Color_int_bool_bool -= OnNewIntText;
        On_CombatText.NewText_Rectangle_Color_string_bool_bool -= OnNewStringText;
    }

    public override void PostUpdateEverything()
    {
        ClearExpiredPendingTexts();
    }

    internal void Capture(SourceTeamCombatTextTarget target, int targetIndex, int sourcePlayer)
    {
        if (!Enabled)
            return;

        if (!TryGetTeamPlayer(sourcePlayer, out Player player))
            return;

        SetSource(target, targetIndex, sourcePlayer, player.team);

        if (Main.netMode != NetmodeID.SinglePlayer)
            SendSource(target, targetIndex, sourcePlayer, player.team);
    }

    internal void HandlePacket(BinaryReader reader, int fromWho)
    {
        SourceTeamCombatTextTarget target = (SourceTeamCombatTextTarget)reader.ReadByte();
        int targetIndex = reader.ReadInt16();
        int sourcePlayer = reader.ReadByte();
        int team = reader.ReadByte();

        if (!Enabled)
            return;

        if (Main.netMode == NetmodeID.Server)
        {
            if (!TryGetTeamPlayer(sourcePlayer, out Player player))
                return;

            team = player.team;
            SetSource(target, targetIndex, sourcePlayer, team);
            SendSource(target, targetIndex, sourcePlayer, team, -1, fromWho);
            return;
        }

        SetSource(target, targetIndex, sourcePlayer, team);
    }

    private int OnNewIntText(On_CombatText.orig_NewText_Rectangle_Color_int_bool_bool orig, Rectangle location, Color color, int amount, bool dramatic, bool dot)
    {
        if (!Enabled)
            return orig(location, color, amount, dramatic, dot);

        if (TryGetSource(location, dot, out SourceTeamCombatTextSource source))
        {
            color = Main.teamColor[source.Team];
            int index = orig(location, color, amount, dramatic, dot);
            //Log.Chat("New CombatText integer: " + amount + " from " + (Terraria.Enums.Team)source.Team);
            return index;
        }

        int result = orig(location, color, amount, dramatic, dot);
        AddPendingText(result, location, amount.ToString());
        return result;
    }

    private int OnNewStringText(On_CombatText.orig_NewText_Rectangle_Color_string_bool_bool orig, Rectangle location, Color color, string text, bool dramatic, bool dot)
    {
        if (!Enabled)
            return orig(location, color, text, dramatic, dot);

        if (TryGetSource(location, dot, out SourceTeamCombatTextSource source))
        {
            color = Main.teamColor[source.Team];
            int index = orig(location, color, text, dramatic, dot);
            //Log.Chat("New CombatText string: " + text + " from " + (Terraria.Enums.Team)source.Team);
            return index;
        }

        int result = orig(location, color, text, dramatic, dot);
        AddPendingText(result, location, text);
        return result;
    }

    private void SetSource(SourceTeamCombatTextTarget target, int targetIndex, int sourcePlayer, int team)
    {
        if (!IsValidTeam(team))
            return;

        SourceTeamCombatTextSource source = new(sourcePlayer, team, Main.GameUpdateCount + DirectContextTicks, Main.GameUpdateCount + DotContextTicks);

        if (target == SourceTeamCombatTextTarget.NPC)
        {
            if (!IsValidNPC(targetIndex))
                return;

            npcSources[targetIndex] = source;
            ApplyPendingTexts(Main.npc[targetIndex].Hitbox, source);
            return;
        }

        if (!IsValidPlayer(targetIndex))
            return;

        playerSources[targetIndex] = source;
        ApplyPendingTexts(Main.player[targetIndex].Hitbox, source);
    }

    private bool TryGetSource(Rectangle location, bool dot, out SourceTeamCombatTextSource source)
    {
        source = default;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            if (!IsValidNPC(i) || !Main.npc[i].Hitbox.Intersects(location))
                continue;

            SourceTeamCombatTextSource candidate = npcSources[i];

            if (!IsActiveSource(candidate, dot))
                continue;

            int distance = (int)Vector2.DistanceSquared(Main.npc[i].Hitbox.Center.ToVector2(), location.Center.ToVector2());

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            source = candidate;
        }

        for (int i = 0; i < Main.maxPlayers; i++)
        {
            if (!IsValidPlayer(i) || !Main.player[i].Hitbox.Intersects(location))
                continue;

            SourceTeamCombatTextSource candidate = playerSources[i];

            if (!IsActiveSource(candidate, dot))
                continue;

            int distance = (int)Vector2.DistanceSquared(Main.player[i].Hitbox.Center.ToVector2(), location.Center.ToVector2());

            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            source = candidate;
        }

        return bestDistance != int.MaxValue;
    }

    private void ApplyPendingTexts(Rectangle targetHitbox, SourceTeamCombatTextSource source)
    {
        for (int i = pendingTexts.Count - 1; i >= 0; i--)
        {
            PendingSourceTeamCombatText pending = pendingTexts[i];

            if (Main.GameUpdateCount > pending.Until || pending.Index < 0 || pending.Index >= Main.maxCombatText || !Main.combatText[pending.Index].active)
            {
                pendingTexts.RemoveAt(i);
                continue;
            }

            if (!targetHitbox.Intersects(pending.Location))
                continue;

            Main.combatText[pending.Index].color = Main.teamColor[source.Team];
            Log.Chat("Applied pending combat text: " + pending.DamageText + " from " + (Terraria.Enums.Team)source.Team);
            pendingTexts.RemoveAt(i);
        }
    }

    private void AddPendingText(int index, Rectangle location, string damageText)
    {
        if (index < 0 || index >= Main.maxCombatText)
            return;

        pendingTexts.Add(new PendingSourceTeamCombatText(index, location, damageText, Main.GameUpdateCount + PendingTextTicks));
    }

    private void ClearExpiredPendingTexts()
    {
        for (int i = pendingTexts.Count - 1; i >= 0; i--)
        {
            PendingSourceTeamCombatText pending = pendingTexts[i];

            if (Main.GameUpdateCount <= pending.Until && pending.Index >= 0 && pending.Index < Main.maxCombatText && Main.combatText[pending.Index].active)
                continue;

            pendingTexts.RemoveAt(i);
        }
    }

    private void SendSource(SourceTeamCombatTextTarget target, int targetIndex, int sourcePlayer, int team, int toClient = -1, int ignoreClient = -1)
    {
        ModPacket packet = Mod.GetPacket();
        packet.Write((byte)AdventurePacketIdentifier.TeamCombatText);
        packet.Write((byte)target);
        packet.Write((short)targetIndex);
        packet.Write((byte)sourcePlayer);
        packet.Write((byte)team);
        packet.Send(toClient, ignoreClient);
    }

    private static bool IsActiveSource(SourceTeamCombatTextSource source, bool dot)
    {
        if (!IsValidTeam(source.Team))
            return false;

        return Main.GameUpdateCount <= (dot ? source.DotUntil : source.DirectUntil);
    }

    private static bool TryGetTeamPlayer(int playerIndex, out Player player)
    {
        player = null;

        if (!IsValidPlayer(playerIndex))
            return false;

        player = Main.player[playerIndex];
        return IsValidTeam(player.team);
    }

    private static bool IsValidNPC(int npcIndex)
    {
        return npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active;
    }

    private static bool IsValidPlayer(int playerIndex)
    {
        return playerIndex >= 0 && playerIndex < Main.maxPlayers && Main.player[playerIndex].active;
    }

    private static bool IsValidTeam(int team)
    {
        return team > 0 && team < Main.teamColor.Length;
    }
}

// Capture projectile hits and send them to the TeamCombatText system.
internal sealed class SourceTeamCombatTextProjectile : GlobalProjectile
{
    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        ModContent.GetInstance<TeamCombatText>().Capture(SourceTeamCombatTextTarget.NPC, target.whoAmI, projectile.owner);
    }

    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
    {
        ModContent.GetInstance<TeamCombatText>().Capture(SourceTeamCombatTextTarget.Player, target.whoAmI, projectile.owner);
    }
}

// Capture item hits and send them to the TeamCombatText system.
internal sealed class SourceTeamCombatTextItem : GlobalItem
{
    public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
    {
        ModContent.GetInstance<TeamCombatText>().Capture(SourceTeamCombatTextTarget.NPC, target.whoAmI, player.whoAmI);
    }

    public override void ModifyHitPvp(Item item, Player player, Player target, ref Player.HurtModifiers modifiers)
    {
        ModContent.GetInstance<TeamCombatText>().Capture(SourceTeamCombatTextTarget.Player, target.whoAmI, player.whoAmI);
    }
}