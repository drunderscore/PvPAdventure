using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.SSC.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PvPAdventure.Core.SSC;

public class ServerViewer : UIState
{
    private const float HeaderHeight = 52f;

    private DraggableElement Root;
    private UIPanel Container;
    private UIList CharacterList;
    private UIScrollbar Scrollbar;
    internal CreateSSCCharacterPanel CharacterCreationPanel;

    private int lastBuildHash;

    public override void OnActivate()
    {
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(530f, 0f),
            Top = new StyleDimension(220f - HeaderHeight, 0f),
            Height = new StyleDimension(400f, 0f),
            HAlign = 0.5f
        };
        Append(Root);

        var title = new UITextPanel<LocalizedText>(Language.GetText("Mods.PvPAdventure.SSC.SelectPlayer"), 0.8f, large: true)
        {
            HAlign = 0.5f,
            Height = new StyleDimension(HeaderHeight, 0f),
            BackgroundColor = new Color(73, 94, 171),
        };
        title.SetPadding(15f);

        title.OnLeftMouseDown += (evt, _) => Root.BeginDrag(evt);
        title.OnLeftMouseUp += (evt, _) => Root.EndDrag(evt);

        Container = new UIPanel
        {
            BackgroundColor = new Color(33, 43, 79) * 0.8f
        };
        Container.Top.Set(HeaderHeight - 0f, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-HeaderHeight, 1f);
        Root.Append(Container);

        CharacterList = new UIList
        {
            Width = new StyleDimension(-25f, 1f),
            Height = new StyleDimension(0f, 1f),
            ListPadding = 10f,
            ManualSortMethod = _ => { },
        };
        Container.Append(CharacterList);

        Scrollbar = new UIScrollbar();
        Scrollbar.SetView(100f, 1000f);
        Scrollbar.Height.Set(0f, 1f);
        Scrollbar.HAlign = 1f;

        CharacterList.SetScrollbar(Scrollbar);
        Container.Append(Scrollbar);

        Player dummy = new Player();
        CharacterCreationPanel = new CreateSSCCharacterPanel(dummy);

        CharacterList.Clear();
        CharacterList.Add(CharacterCreationPanel);
        CharacterList.Recalculate();

        lastBuildHash = 0;

        Root.Append(title);
    }

    public void Calc(TagCompound obj)
    {
        if (CharacterList == null || CharacterCreationPanel == null)
        {
            return;
        }

        if (obj == null)
        {
            EnsureCreationPanelOnly();
            return;
        }

        string pid = SSC.GetPID();
        if (!obj.ContainsKey(pid))
        {
            EnsureCreationPanelOnly();
            return;
        }

        List<TagCompound> list = obj.Get<List<TagCompound>>(pid);

        int buildHash = 17;
        buildHash = (buildHash * 31) ^ list.Count;

        for (int i = 0; i < list.Count; i++)
        {
            TagCompound tag = list[i];

            string name = "";
            long playTime = 0;

            if (tag.ContainsKey("name"))
            {
                name = tag.GetString("name");
            }

            if (tag.ContainsKey("play_time"))
            {
                playTime = tag.GetLong("play_time");
            }

            buildHash = (buildHash * 31) ^ (name?.GetHashCode() ?? 0);
            buildHash = (buildHash * 31) ^ playTime.GetHashCode();
        }

        if (buildHash == lastBuildHash && CharacterList.Count > 0)
        {
            return;
        }

        lastBuildHash = buildHash;

        CharacterList.Clear();

        for (int i = 0; i < list.Count; i++)
        {
            TagCompound tag = list[i];

            string name = tag.GetString("name");
            long playTime = tag.GetLong("play_time");

            Player playerForPreview = BuildPreviewPlayer(tag);

            var item = new SSCCharacterListItem(
                playerForPreview,
                name,
                playTime,
                i,
                OnPlayClicked,
                OnDeleteClicked);

            CharacterList.Add(item);
        }

        CharacterList.Add(CharacterCreationPanel);
        CharacterList.Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Main.keyState.IsKeyDown(Keys.I))
        {
            RemoveAllChildren();
            Activate();
        }
    }

    private void EnsureCreationPanelOnly()
    {
        if (CharacterList.Count == 1 && CharacterList._items[0] == CharacterCreationPanel)
        {
            return;
        }

        CharacterList.Clear();
        CharacterList.Add(CharacterCreationPanel);
        CharacterList.Recalculate();
    }

    private static Player BuildPreviewPlayer(TagCompound tag)
    {
        // This will show defaults unless you also send the below fields from NetSend.
        // Recommended keys to include per character:
        // - difficulty, lifeMax, life, manaMax, mana
        // - skinVariant, hair, male
        // - skinColor, eyeColor, hairColor, shirtColor, underShirtColor, pantsColor, shoeColor
        var p = new Player();

        if (tag.ContainsKey("name"))
        {
            p.name = tag.GetString("name");
        }

        p.difficulty = PlayerDifficultyID.SoftCore;
        if (tag.ContainsKey("difficulty"))
        {
            p.difficulty = (byte)tag.GetInt("difficulty");
        }

        int lifeMax = 100;
        int life = 100;
        int manaMax = 20;
        int mana = 20;

        if (tag.ContainsKey("lifeMax"))
        {
            lifeMax = tag.GetInt("lifeMax");
        }

        if (tag.ContainsKey("life"))
        {
            life = tag.GetInt("life");
        }
        else
        {
            life = lifeMax;
        }

        if (tag.ContainsKey("manaMax"))
        {
            manaMax = tag.GetInt("manaMax");
        }

        if (tag.ContainsKey("mana"))
        {
            mana = tag.GetInt("mana");
        }
        else
        {
            mana = manaMax;
        }

        p.statLifeMax2 = lifeMax;
        p.statLife = life;
        p.statManaMax2 = manaMax;
        p.statMana = mana;

        if (tag.ContainsKey("male"))
        {
            p.Male = tag.GetBool("male");
        }

        if (tag.ContainsKey("skinVariant"))
        {
            p.skinVariant = tag.GetInt("skinVariant");
        }

        if (tag.ContainsKey("hair"))
        {
            p.hair = tag.GetInt("hair");
        }

        if (tag.ContainsKey("skinColor"))
        {
            p.skinColor = tag.Get<Color>("skinColor");
        }

        if (tag.ContainsKey("eyeColor"))
        {
            p.eyeColor = tag.Get<Color>("eyeColor");
        }

        if (tag.ContainsKey("hairColor"))
        {
            p.hairColor = tag.Get<Color>("hairColor");
        }

        if (tag.ContainsKey("shirtColor"))
        {
            p.shirtColor = tag.Get<Color>("shirtColor");
        }

        if (tag.ContainsKey("underShirtColor"))
        {
            p.underShirtColor = tag.Get<Color>("underShirtColor");
        }

        if (tag.ContainsKey("pantsColor"))
        {
            p.pantsColor = tag.Get<Color>("pantsColor");
        }

        if (tag.ContainsKey("shoeColor"))
        {
            p.shoeColor = tag.Get<Color>("shoeColor");
        }

        return p;
    }

    private static void OnPlayClicked(string characterName)
    {
        var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
        mp.Write((byte)AdventurePacketIdentifier.SSC);
        mp.Write((byte)SSCMessageID.GoGoSSC);
        mp.Write(SSC.GetPID());
        mp.Write(characterName);
        mp.Send();
    }

    private static void OnDeleteClicked(string characterName)
    {
        var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
        mp.Write((byte)AdventurePacketIdentifier.SSC);
        mp.Write((byte)SSCMessageID.EraseSSC);
        mp.Write(SSC.GetPID());
        mp.Write(characterName);
        mp.Send();
    }
}
