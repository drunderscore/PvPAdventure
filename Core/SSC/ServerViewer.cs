using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.SSC.UI;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.IO;
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
    internal SSCCreateCharacterPanel CharacterCreationPanel;

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

        var title = new UITextPanel<LocalizedText>(Language.GetText("Mods.PvPAdventure.SSC.SelectPlayer"), 0.6f, large: true)
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
        Container.Top.Set(HeaderHeight - 3f, 0f);
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
        CharacterCreationPanel = new SSCCreateCharacterPanel(dummy);

        CharacterList.Clear();
        CharacterList.Add(CharacterCreationPanel);
        CharacterList.Recalculate();

        lastBuildHash = 0;

        Root.Append(title);

        var system = ModContent.GetInstance<ServerSystem>();
        if (system.LastCharacterList != null)
        {
            Calc(system.LastCharacterList);
        }
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

        Log.Chat("Calc() called");
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

#if DEBUG
        if (Main.keyState.IsKeyDown(Keys.I))
        {
            RemoveAllChildren();
            Activate();
        }
#endif
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
        var p = new Player();

        if (tag.ContainsKey("plr"))
        {
            byte[] data = tag.GetByteArray("plr");

            var fd = new PlayerFileData("Preview", false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
            };

            Player.LoadPlayerFromStream(fd, data, null);

            // Set life
            if (tag.ContainsKey("lifeMax"))
                fd.Player.statLifeMax2 = tag.GetInt("lifeMax");
            else
                fd.Player.statLifeMax2 = 100;

            // Set mana
            if (tag.ContainsKey("manaMax"))
                fd.Player.statManaMax2 = tag.GetInt("manaMax");
            else
                fd.Player.statManaMax2 = 20;

            return fd.Player;
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