using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PvPAdventure.Content.Items;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace PvPAdventure.Content.NPCs;

internal class AdventureSantaNPC : ModNPC
{
    //public override string Texture => $"Terraria/Images/NPC_{NPCID.SantaClaus}";
    #region Defaults
    public override string Texture => $"PvPAdventure/Assets/NPC/AdventureSanta";
    public override string HeadTexture => $"PvPAdventure/Assets/NPC/AdventureSanta_Head";

    public override void SetStaticDefaults()
    {
        // Match vanilla animation frame count
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.SantaClaus];

        // Copy common town NPC static sets Santa uses
        NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[NPCID.SantaClaus];
        NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.AttackFrameCount[NPCID.SantaClaus];
        NPCID.Sets.DangerDetectRange[Type] = NPCID.Sets.DangerDetectRange[NPCID.SantaClaus];
        NPCID.Sets.AttackType[Type] = NPCID.Sets.AttackType[NPCID.SantaClaus];
        NPCID.Sets.AttackTime[Type] = NPCID.Sets.AttackTime[NPCID.SantaClaus];
        NPCID.Sets.AttackAverageChance[Type] = NPCID.Sets.AttackAverageChance[NPCID.SantaClaus];
        NPCID.Sets.HatOffsetY[Type] = NPCID.Sets.HatOffsetY[NPCID.SantaClaus];

        // Attack
        NPCID.Sets.AttackType[Type] = 1; // 0=none, 1=shoot, 2=throw, 3=magic, 4=melee
        NPCID.Sets.AttackTime[Type] = 6; // base time in ticks
        NPCID.Sets.AttackAverageChance[Type] = 1; // lower = more frequent
        NPCID.Sets.DangerDetectRange[Type] = 16*50; // how far he sees enemies (in pixels). 50 tiles.
        NPCID.Sets.AttackFrameCount[Type] = 5;

        //NPCID.Sets.ActsLikeTownNPC[Type] = true; // town-like behavior without full town NPC UI (incl. happiness)
    }

    public override void SetDefaults()
    {
        // Copies stats, aiStyle, townNPC flags, sounds, etc.
        NPC.CloneDefaults(NPCID.SantaClaus);

        // Ensures the AI/animation logic follows Santa
        AIType = NPCID.Pirate;
        AnimationType = NPCID.SantaClaus;

        // Ensure shop is an option
        //NPC.townNPC = true;
        NPC.friendly = true;

        // Attack
        NPC.damage = 0;
    }

    // Only for debugging purposes; prevent spawning in normal gameplay
    public override bool CanTownNPCSpawn(int numTownNPCs) => false;

    public override List<string> SetNPCNameList()
    {
        return ["Adventure Santa"];
    }
    #endregion

    #region NPC dialogue
    public override string GetChat()
    {
        WeightedRandom<string> chat = new();

        // playful / PvP-adventure lines
        chat.Add("Ho ho ho! Nothing like a good duel to warm the spirit.");
        chat.Add("PvP builds character… and rivalries.");
        chat.Add("I’ve seen heroes fall faster than snow in a Blizzard.");

        // seasoned adventurer wisdom
        chat.Add("Every great adventure starts with poor preparation.");
        chat.Add("You learn more from defeat than from loot.");
        chat.Add("A good team wins fights. A great team survives them.");
        chat.Add("Terraria rewards the bold… and punishes the careless.");

        // Santa-flavored flavor text
        chat.Add("Even Santa needs good gear in this world.");
        chat.Add("Naughty or nice? In PvP, everyone bleeds the same.");
        chat.Add("Ho ho ho… that build could use some work.");

        return chat;
    }
    #endregion

    #region Shop
    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
        {
            shopName = "Shop";
        }
        else // Special greeting
        {
            //SoundEngine.PlaySound(SoundID.NPCHit10);

            // Emote
            //EmoteBubble.NewBubble(EmoteID.RPSWinRock, new WorldUIAnchor(NPC), 120); // 120 ticks = 2 seconds

            // Set up the greeting animation with findframe
            greetingTimer = 36;

        }
    }
    public override void AddShops()
    {
        var shop = new NPCShop(Type);

        //shop.Add(ModContent.ItemType<AdventureBag>());
        shop.Add(ModContent.ItemType<AdventureMirror>());
        shop.Register();
    }
    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = "Shop";
        button2 = "Greeting";
    }
    #endregion

    #region Greeting
    private int greetingTimer;
    private const int GreetingDurationTicks = 60;

    // Greeting animation: frames 21-24
    private const int GreetingStartFrame = 21;
    private const int GreetingFrameCount = 4; // 21,22,23,24

    // How many ticks each frame should last.
    // 120 ticks total / 4 frames = 30 ticks per frame
    private const int GreetingTicksPerFrame = GreetingDurationTicks / GreetingFrameCount;

    private int _attackLockTimer;

    public override void AI()
    {
        if (_attackLockTimer > 0)
            _attackLockTimer--;

        // If we are attacking, don't allow greeting animation to override frames
        if (_attackLockTimer > 0 && greetingTimer > 0)
            greetingTimer = 0;

        if (greetingTimer > 0)
            greetingTimer--;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (greetingTimer <= 0 || _attackLockTimer > 0)
            return true;

        Texture2D texture = TextureAssets.Npc[Type].Value;

        int frameCount = Main.npcFrameCount[Type];
        if (frameCount <= 0)
        {
            return true;
        }

        int frameHeight = texture.Height / frameCount;

        // greeting settings
         const int GreetingDurationTicks = 60;

        // Greeting animation: frames 21-24
        const int GreetingStartFrame = 21;
        const int GreetingFrameCount = 4; // 21,22,23,24

        int elapsed = GreetingDurationTicks - greetingTimer;

        int step = elapsed / GreetingTicksPerFrame; // 0..3
        if (step < 0)
        {
            step = 0;
        }
        else if (step >= GreetingFrameCount)
        {
            step = GreetingFrameCount - 1;
        }

        int frameIndex = GreetingStartFrame + step;

        // Safety clamp
        if (frameIndex < 0)
        {
            frameIndex = 0;
        }
        else if (frameIndex >= frameCount)
        {
            frameIndex = frameCount - 1;
        }

        var frame = new Rectangle(0, frameIndex * frameHeight, texture.Width, frameHeight);

        Vector2 origin = frame.Size() * 0.5f;

        Vector2 drawPos = NPC.Center - screenPos;
        drawPos.Y += NPC.gfxOffY;
        drawPos.Y -= 2;

        SpriteEffects effects = NPC.direction == -1
        //SpriteEffects effects = NPC.spriteDirection == 1
            ? SpriteEffects.None
            : SpriteEffects.FlipHorizontally;

        Main.EntitySpriteDraw(
            texture,
            drawPos,
            frame,
            drawColor,
            NPC.rotation,
            origin,
            NPC.scale,
            effects
        );

        return false;
    }
    #endregion

    #region Attack

    // two default methods for attack
    public override void TownNPCAttackStrength(ref int damage, ref float knockback)
    {
        // The amount of base damage the attack will do.
        // This is NOT the same as NPC.damage (that is for contact damage).
        // Remember, the damage will increase as more bosses are defeated.
        damage = 20;
        // The amount of knockback the attack will deal.
        // This value does not scale like damage does.
        knockback = 4f;
    }

    public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
    {
        // How long, in ticks, the Town NPC must wait before they can attack again.
        // The actual length will be: cooldown <= length < (cooldown + randExtraCooldown)
        cooldown = 1;
        randExtraCooldown = 0;
    }

    // Shooting specific attack

    public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
    {
        // Shooting
        projType = ProjectileID.Bullet; // Set the type of projectile the Town NPC will attack with.
        attackDelay = 1; // This is the amount of time, in ticks, before the projectile will actually be spawned after the attack animation has started.

        // If the world is Hardmode, change the projectile to something else.
        if (Main.hardMode)
        {
            projType = ProjectileID.CursedBullet;
        }
    }

    public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
    {
        // Shooting
        multiplier = 16f; // multiplier is similar to shootSpeed. It determines how fast the projectile will move.
        randomOffset = 0.1f; // This will affect the speed of the projectile (which also affects how accurate it will be).
    }

    public override void DrawTownAttackGun(ref Texture2D item, ref Rectangle itemFrame, ref float scale, ref int horizontalHoldoutOffset)
    {
        // Only used for shooting attacks.

        // Here is an example on how we would change which weapon is displayed. Omit this part if only want one weapon.
        // In Pre-Hardmode, display the first gun.
        if (!Main.hardMode)
        {
            // This hook takes a Texture2D instead of an int for the item. That means the weapon our Town NPC uses doesn't need to be an existing item.
            // But, that also means we need to load the texture ourselves. Luckily, GetItemDrawFrame() can do the work for us.
            // The first parameter is what you set as the item.
            // Then, there are two "out" parameters. We can use those out parameters.
            Main.GetItemDrawFrame(ItemID.FlintlockPistol, out Texture2D itemTexture, out Rectangle itemRectangle);

            // Set the item texture to the item texture.
            item = itemTexture;

            // This is the source rectangle for the texture that will be drawn.
            // In this case, it is just the entire bounds of the texture because it has only one frame.
            // You could change this if your texture has multiple frames to be animated.
            itemFrame = itemRectangle;

            scale = 1f; // How large the item is drawn.
            horizontalHoldoutOffset = -1; // How close it is drawn to the Town NPC. Adjust this if the item isn't in the Town NPC's hand.

            return; // Return early so the Hardmode code doesn't run.
        }

        // If the world is in Hardmode, change the item to something else.
        Main.GetItemDrawFrame(ItemID.VenusMagnum, out Texture2D itemTexture2, out Rectangle itemRectangle2);
        item = itemTexture2;
        itemFrame = itemRectangle2;
        scale = 10f;
        horizontalHoldoutOffset = -1;
    }
    #endregion
}
