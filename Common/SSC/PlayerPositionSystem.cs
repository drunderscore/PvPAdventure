using Microsoft.Xna.Framework;
using PvPAdventure.Core.Net;
using Steamworks;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PvPAdventure.Common.SSC;

/// <summary>
/// Saves and restores player positions per world, per character.
/// Positions are stored in the tplr (player mod data) file alongside other SSC data.
/// When a player joins a world with SSC enabled, they are teleported to their last position in that world.
/// </summary>
[Autoload(Side = ModSide.Both)]
public class PlayerPositionSystem : ModSystem
{
    /// <summary>
    /// Saves the current player's position to their SSC data.
    /// Called when the player saves their character.
    /// </summary>
    public static void SavePlayerPosition(Player player, TagCompound sscData)
    {
        if (player == null || sscData == null)
            return;

        // Store position in the SSC data tag
        sscData["posX"] = player.position.X;
        sscData["posY"] = player.position.Y;
        sscData["worldId"] = Main.worldID;
        sscData["worldName"] = Main.worldName;
    }

    /// <summary>
    /// Loads and applies the player's saved position from SSC data.
    /// Called after the player spawns in the world.
    /// </summary>
    public static bool TryLoadPlayerPosition(Player player, TagCompound sscData)
    {
        if (player == null || sscData == null)
            return false;

        // Check if position data exists for this world
        if (!sscData.ContainsKey("posX") || !sscData.ContainsKey("posY"))
            return false;

        if (!sscData.ContainsKey("worldId") || !sscData.ContainsKey("worldName"))
            return false;

        // Verify the saved position is from the same world
        int savedWorldId = sscData.GetInt("worldId");
        string savedWorldName = sscData.GetString("worldName");

        if (savedWorldId != Main.worldID || savedWorldName != Main.worldName)
        {
            // Position is from a different world, don't apply it
            Log.Debug($"SSC position mismatch: saved={savedWorldName}({savedWorldId}), current={Main.worldName}({Main.worldID})");
            return false;
        }

        float posX = sscData.GetFloat("posX");
        float posY = sscData.GetFloat("posY");

        // Clamp position to valid world bounds (with some margin for safety)
        posX = Math.Max(0, Math.Min(posX, Main.maxTilesX * 16 - player.width));
        posY = Math.Max(0, Math.Min(posY, Main.maxTilesY * 16 - player.height));

        player.position = new Vector2(posX, posY);

        Log.Chat($"SSC: Restored {player.name}'s position to ({(int)posX / 16}, {(int)posY / 16})");
        return true;
    }

    #region Helpers
    public static string FormatPlayTime(TimeSpan t)
    {
        int hours = (int)t.TotalHours;
        return $"{hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public static void PrintWelcomeMessage(Player player, TagCompound sscData, bool positionRestored)
    {
        if (player == null)
            return;

        string positionText = GetPositionText(player, sscData, positionRestored);

        Main.NewText(
            $"Welcome, {player.name}! — " +
            $"Playtime: {FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime())}" +
            positionText,
            Color.MediumPurple
        );
    }

    public static void PrintWelcomeMessage(Player player)
    {
        if (player == null)
            return;

        Main.NewText(
            $"Welcome, {player.name}! — " +
            $"Playtime: {FormatPlayTime(Main.ActivePlayerFileData.GetPlayTime())}",
            Color.MediumPurple
        );
    }

    private static string GetPositionText(Player player, TagCompound sscData, bool positionRestored)
    {
        Vector2 appliedPos = player.position;

        if (!positionRestored || sscData == null || !sscData.ContainsKey("posX") || !sscData.ContainsKey("posY"))
            return "";

        Vector2 appliedTile = new(appliedPos.X / 16f, appliedPos.Y / 16f);
        return $" — Position: ({appliedTile.X:0}, {appliedTile.Y:0})";
    }
    #endregion
}
