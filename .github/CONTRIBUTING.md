# Developing & Contributing to PvPAdventure

This document explains how to build, run, debug, and contribute to **PvPAdventure** (a PvP battle-royale / adventure mod for tModLoader).

Please also read:

- [`CODE_OF_CONDUCT.md`](.github/CODE_OF_CONDUCT.md) (community standards)
- [`description.txt`](description.txt) (short summary of the mod’s highlights)
- The rules and guides on the Discord

---

## Repository layout and conventions

### Namespace and folder rules (project standards)

We follow these rules consistently:

- Four roots:
  - `PvPAdventure.Assets` — images and textures
  - `PvPAdventure.Core` — main mechanics/features (most code should be here)
  - `PvPAdventure.Common` — shared utilities, split by dependency surface area, asset loading
  - `PvPAdventure.Content` — declarations/assets (items/projectiles/textures/ui), organized by mechanic

- Do not organize by type (avoid `Items/Projectiles/Systems` buckets). Organize by **mechanic / feature**:
  - ✅ `Common.SpawnSelector`, `Content.SpawnSelector`
  - ❌ `Content.Systems.Weapons.Players...`

- No godclasses:
  - Avoid one ModPlayer/Global* that does everything.
  - Prefer many small `ModSystem`, `GlobalItem`, `GlobalProjectile`, `ModPlayer`, etc.

- Favor composition over deep inheritance:
  - Prefer stackable behaviors via enable flags and per-instance data fields.

### Where to put things

High-level map of core areas:

- `PvPAdventure` — root mod entry point; owns packet routing and top-level wiring
- `Common.Config` — configuration settings for both client and server
- `Core.AdminTools` — game management tools for admins
- `Core.SpawnAndSpectateSystem` — spawn picking, spectate, bed teleport, etc

Game Interface Layers (UI layers):
- `Core.Spawnbox.PvPIcons` — draws icons above players while in spawn
- `Core.Spawnbox.SpawnboxWorld` — draw a black rectangle around world spawn
- `Core.Spawnbox.SpawnboxMap` — draws a black rectangle around world spawn in the map
- `Core.SpawnAndSpectate.SpawnAndSpectateSystem` — draws the spawn/spectate UI in spawn and while dead

Match/gameplay systems (evolving):

- `System.GameManager` — match phases, timer, start/end flow **[WIP]**
- `System.RegionMananger` — manages the spawn area (e.g., 50×50 rectangle) **[WIP]**
- `System.PointsManager` — manages team scoring and K/D rules **[WIP]**
- `System.BountyManager` — manages team bounty mechanics **[WIP]**
- `System.RandomTeleportManager` — deprecated teleport system **[WIP]**

Legacy god types (scheduled for refactor):

- `AdventurePlayer` — **[WIP]**
- `AdventureNPC` — **[WIP]**
- `AdventureTile` — **[WIP]**
- `AdventureItem` — **[WIP]**

> Guiding direction: when adding new functionality, prefer feature-scoped systems/globals over growing the legacy `Adventure*` types further.

---

## Prerequisites

You’ll need:

- tModLoader (Steam install is easiest)
- .NET SDK matching the repo/CI (see solution or GitHub Actions workflow for the exact version)
- An IDE:
  - Visual Studio or Rider recommended
  - VS Code works fine for quick edits

Optional but helpful:

- Git
- A dedicated server environment for multiplayer testing
- A decompiled version of tModLoader for development

---

## Getting started

### 1) Clone into tModLoader's `ModSources`

Locate tModLoader's mod source folder.

Clone the repository into:

`Documents/My Games/Terraria/tModLoader/ModSources/`

Ensure this folder was created:

`../ModSources/PvPAdventure/`

---

### 2) Build the mod

### Option A (recommended): Build via Visual Studio

1. Open the `.csproj` file in Visual Studio
2. Press the green **Start** button to build and run the mod.

This ensures the full build and compile pipeline runs and is the preferred workflow for developers.

It also offers useful diagnostic tools and hot reload.

### Option B: Build via tModLoader UI

1. Launch tModLoader
2. Open **Workshop / Mods** → **Develop Mods**
3. Build + Reload PvPAdventure

This is the standard tML workflow and works well for users who don't have an IDE like Visual Studio.
