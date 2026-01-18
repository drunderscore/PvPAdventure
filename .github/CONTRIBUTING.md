# 1. Developing & Contributing to PvPAdventure

Want to hack on PvPAdventure? Awesome! Here's what you need to get started.

**Also read:**
- [`CODE_OF_CONDUCT.md`](.github/CODE_OF_CONDUCT.md)
- [`description.txt`](description.txt)
- [Join the Discord and read the rules & guides](https://discord.gg/Kj97VV8bsx)

---

## 2. Code style and structure

### 2.1 Guidelines
- **Organize by feature, not by type**: Group related functionality by feature (e.g., `Common.Combat`, `Common.Statistics`).  
- **No godclasses**: Prefer many small `ModSystem`, `ModPlayer`, etc. over one type doing everything.
- **Reduce complexity**: Keep code readable, avoid deep inheritance and oversized classes, prefer simple composition.

### 2.2 Resources
- [tML official style guide](https://github.com/tModLoader/tModLoader/wiki/tModLoader-Style-Guide)
- [tML team advice (Discord)](https://discord.com/channels/103110554649894912/711551818194485259/1406328063205310506)
- [grugbrain.dev advice](https://grugbrain.dev/)

---

## 3. Structure

If you're modifying/expanding existing functionality, here's a quick overview of where to look:

#### `Assets` (textures used by the mod)
- `Assets.Custom` — our own textures, mainly used for UI such as spawn selector, config, etc.

#### `Common` (gameplay features)
- `Common.AdminTools` — game timer, points/team assigner, integration with DragonLens.
- `Common.Bounties` — bounty shop.
- `Common.Combat` — PvP/PvE changes, i-frames, hit/kill markers, ghost heal/LoS adjustments, etc.
- `Common.DropRates` — boss loot pool rewrites.
- `Common.GameTimer` — match state, countdown, time remaining.
- `Common.Items` — item stats, bans, shimmer transforms, prefixes.
- `Common.Npcs` — spawn rules, boss changes, hitmarker sound.
- `Common.Recipes` — crafting recipes.
- `Common.Spawnbox` — random teleport, movement rules, recall behavior.
- `Common.SpawnSelector` — adventure mirror, bed/teammate teleports, select spawn when dead, etc.
- `Common.SSC` — server sided character implementation.
- `Common.Statistics` — K/D, boss score, team points, item pickups.
- `Common.Teams` — team chat, team beds.
- `Common.UI` — player outlines, modify accessory slots, draw PvP icons, etc.

#### `Content` (items, NPC, buffs, tiles, etc)
- `Content.Items` — adventure mirror
- `Content.NPCs` — bound NPCs

#### `Core` (infrastructure)
- `Core.Assets` — loading our custom textures from `Assets`
- `Core.Config` — client & server config.
- `Core.Input` — keybinds, dash keybind.
- `Core.Net` — ping, section sync, spawn sync, packet helpers, etc.
- `Core.Utilities` — math helpers

---

## 4. Getting started

### 4.1 Prerequisites
- tModLoader
- Visual Studio (recommended), Rider, or VS Code with latest .NET SDK

### 4.2 Clone into `ModSources`
Clone the repo into tModLoader's ModSources folder:

Documents/My Games/Terraria/tModLoader/ModSources/PvPAdventure

### 4.3 Build & run

**Option A (recommended): Visual Studio**
1. Open the `.csproj`
2. Press **Start** to build and launch tModLoader with the mod

**Option B: tModLoader UI**
1. Launch tModLoader
2. **Workshop / Mods → Develop Mods**
3. **Build + Reload** PvPAdventure

> **Note:** If the tML build fails, try [Mod Reloader](https://steamcommunity.com/sharedfiles/filedetails/?id=3483722883).

---

## 5. Contributing workflow
- Keep commits small and focused.
- Use descriptive commit messages (what changed + why).
- Test changes locally; test multiplayer when relevant.
- Name branches by feature or fix (e.g., `fix/spawnbox-teleport-bug`).
- Put gameplay behavior in `Common/*` and shared infrastructure in `Core/*`.

## 6. CI

We use GitHub Actions to ensure the mod builds.

- Workflow: `.github/workflows/build.yml`
- Runs on selected branches and PRs
- Add your branch to the workflow to enable CI
