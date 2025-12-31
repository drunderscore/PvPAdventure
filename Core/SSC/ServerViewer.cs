using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PvPAdventure.Core.SSC.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace PvPAdventure.Core.SSC;

public class ServerViewer : UIState
{
    const float HeaderHeight = 52f;

    private DraggableElement Root;
    private UIPanel Container;
    private UIList CharacterList;
    private UIScrollbar Scrollbar;
    internal CreateSSCCharacterPanel CharacterCreationPanel;

    public override void OnActivate()
    {
#if DEBUG
        Main.NewText("[DEBUG] ServerViewer activated");
#endif
        RemoveAllChildren();

        Root = new DraggableElement
        {
            Width = new StyleDimension(650f, 0f),
            MaxWidth = new StyleDimension(650f, 0f),
            Top = new StyleDimension(220f - HeaderHeight, 0f),
            Height = new StyleDimension(400, 0f),
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
        Container.Top.Set(HeaderHeight - 10, 0f);
        Container.Width.Set(0f, 1f);
        Container.Height.Set(-HeaderHeight, 1f);
        Root.Append(Container);

        CharacterList = new UIList
        {
            Width = new StyleDimension(-25f, 1f),
            Height = new StyleDimension(0f, 1f),
            ListPadding = 10f,
            ManualSortMethod = (_) => { },
        };
        Container.Append(CharacterList);

        Scrollbar = new UIScrollbar();
        Scrollbar.SetView(100f, 1000f); 
        Scrollbar.Height.Set(0f, 1f);
        Scrollbar.HAlign = 1f;

        CharacterList.SetScrollbar(Scrollbar);
        Container.Append(Scrollbar);

        Root.Append(title);

        Player Dummy = new();
        CharacterCreationPanel = new CreateSSCCharacterPanel(Dummy);
        CharacterList.Add(CharacterCreationPanel);
        CharacterList.Recalculate();
    }

    public void Calc(TagCompound obj)
    {
#if DEBUG
        Main.NewText("[DEBUG/SSC] ServerViewer.Calc() invoked");
#endif
        CharacterList.Clear();

        var list = obj.Get<List<TagCompound>>(SSC.GetPID());

        foreach (var tag in list)
        {
            var item = new UIPanel
            {
                Width = new StyleDimension(0, 1),
                Height = new StyleDimension(72, 0),
            };
            item.SetPadding(10);

            item.OnMouseOver += (_, _) =>
            {
                item.BackgroundColor = new Color(73, 94, 171);
                item.BorderColor = new Color(89, 116, 213);
            };
            item.OnMouseOut += (_, _) =>
            {
                item.BackgroundColor = new Color(63, 82, 151) * 0.7f;
                item.BorderColor = new Color(89, 116, 213) * 0.7f;
            };

            CharacterList.Add(item);

            item.Append(new UIText(tag.GetString("name"))
            {
                Height = new StyleDimension(30, 0)
            });

            item.Append(new UIImage(Main.Assets.Request<Texture2D>("Images/UI/Divider"))
            {
                Width = new StyleDimension(0, 1),
                HAlign = 0.5f,
                VAlign = 0.5f,
                ScaleToFit = true
            });

            item.Append(
                new UIText(
                    new TimeSpan(tag.GetLong("play_time"))
                        .ToString(@"dd\:hh\:mm\:ss"))
                {
                    Height = new StyleDimension(15, 0),
                    HAlign = 0.5f,
                    VAlign = 1
                });

            var playButton =
                new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
                {
                    Width = new StyleDimension(32, 0),
                    Height = new StyleDimension(32, 0),
                    VAlign = 0.5f
                };

            playButton.OnLeftClick += (_, _) =>
            {
                var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
                mp.Write((byte)AdventurePacketIdentifier.SSC);
                mp.Write((byte)SSCMessageID.GoGoSSC);
                mp.Write(SSC.GetPID());
                mp.Write(tag.GetString("name"));
                mp.Send();
            };

            playButton.OnUpdate += _ =>
            {
                playButton.Width.Set(22, 0);
                playButton.Height.Set(22, 0);
                playButton.VAlign = 1f;
                playButton.Top.Set(3, 0);

                if (playButton.IsMouseHovering)
                {
                    Main.instance.MouseText(Language.GetTextValue("UI.Play"));
                }
            };

            item.Append(playButton);

            var deleteButton =
                new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
                {
                    Width = new StyleDimension(22, 0),
                    Height = new StyleDimension(22, 0),
                    HAlign = 1,
                    VAlign = 0.5f
                };

            deleteButton.OnLeftClick += (_, _) =>
            {
                if (!Main.keyState.IsKeyDown(Keys.LeftShift))
                    return;

                var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
                mp.Write((byte)AdventurePacketIdentifier.SSC);
                mp.Write((byte)SSCMessageID.EraseSSC);
                mp.Write(SSC.GetPID());
                mp.Write(tag.GetString("name"));
                mp.Send();
            };

            deleteButton.OnUpdate += _ =>
            {
                deleteButton.Width.Set(22, 0);
                deleteButton.Height.Set(22, 0);
                deleteButton.HAlign = 1f;
                deleteButton.VAlign = 1f;
                deleteButton.Top.Set(3, 0);

                if (deleteButton.IsMouseHovering)
                {
                    //Main.instance.MouseText(Language.GetTextValue("UI.Delete"));
                    Main.instance.MouseText("Shift+click to delete");
                }
            };

            item.Append(deleteButton);
        }

        CharacterList.Add(CharacterCreationPanel);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // Debug rebuild
        if (Main.keyState.IsKeyDown(Keys.I))
        {
            RemoveAllChildren();
            Activate();
        }
    }
}
