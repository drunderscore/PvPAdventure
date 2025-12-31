using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria.Utilities;
using PvPAdventure.Core.SSC.UI;

namespace PvPAdventure.Core.SSC;

public class ServerViewer : UIState
{
    const float HeaderHeight = 52f;
    const float PanelPadding = 12f;

    internal UITextPanel<LocalizedText> ChooseYourCharacterTitlePanel;
    internal DraggableElement Container;
    internal UIGrid CharacterGrid;
    internal UICharacterCreation CharacterCreation;
    internal UIPanel CharacterCreationPanel;
    internal UICharacterNameButton NameButton;
    internal UISearchBar NameSearchBar;
    internal UITextPanel<LocalizedText> CreateButton;

    internal Player Dummy;

    int CharacterCount;
    bool CanCreate => CharacterCount < 1;

    public override void OnActivate()
    {
#if DEBUG
        Main.NewText("[DEBUG/SERVERVIEWER]: OnActivate() called!");
#endif
        RemoveAllChildren();

        ChooseYourCharacterTitlePanel = new(Language.GetText("Mods.PvPAdventure.SSC.ChooseYourCharacter"), 0.8f, true)
        {
            HAlign = 0.5f,
            BackgroundColor = new Color(73, 94, 171),
            Height = new StyleDimension(HeaderHeight, 0),
            Width = new StyleDimension(0, 1),
            Top = new StyleDimension(-PanelPadding, 0)
        };

        Container = new DraggableElement
        {
            Width = new StyleDimension(240, 0),
            Height = new StyleDimension(260 + 12, 0),
            HAlign = 0.5f,
            VAlign = 0f,
            BackgroundColor = new Color(33, 43, 79) * 0.8f,
            Top = new StyleDimension(82, 0)
        };
        Append(Container);

        Container.Append(ChooseYourCharacterTitlePanel);

        // Drag anywhere on the title panel area.
        ChooseYourCharacterTitlePanel.OnLeftMouseDown += (evt, _) => Container.BeginDrag(evt);
        ChooseYourCharacterTitlePanel.OnLeftMouseUp += (evt, _) => Container.EndDrag(evt);

        float headerInsideContainer = HeaderHeight - PanelPadding;

        Container.Append(CharacterGrid = new UIGrid
        {
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(-headerInsideContainer, 1),
            Top = new StyleDimension(headerInsideContainer, 0),
            ListPadding = 6f
        });

        Dummy = new Player();

        CharacterCreationPanel = new UIPanel
        {
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(108, 0)
        };
        CharacterCreationPanel.SetPadding(10);

        // CharacterGrid.Append(CharacterCreationPanel);

        NameButton = new UICharacterNameButton(Language.GetText("UI.PlayerNameSlot"), LocalizedText.Empty)
        {
            Width = new StyleDimension(0, 1),
            Height = new StyleDimension(40, 0)
        };
        NameButton.OnUpdate += _ =>
        {
            if (!Main.mouseLeft)
            {
                return;
            }

            switch (NameButton.IsMouseHovering)
            {
                case true when !NameSearchBar.IsWritingText:
                case false when NameSearchBar.IsWritingText:
                    {
                        NameSearchBar.ToggleTakingText();
                        break;
                    }
            }
        };
        CharacterCreationPanel.Append(NameButton);

        NameSearchBar = new UISearchBar(LocalizedText.Empty, 1)
        {
            Width = new StyleDimension(-50, 1),
            Height = new StyleDimension(40, 0),
            Left = new StyleDimension(50, 0)
        };
        NameSearchBar.OnMouseOver += (evt, _) => NameButton.MouseOver(evt);
        NameSearchBar.OnMouseOut += (evt, _) => NameButton.MouseOut(evt);
        NameSearchBar.OnContentsChanged += name => Dummy.name = name;
        CharacterCreationPanel.Append(NameSearchBar);

        CreateButton = new UITextPanel<LocalizedText>(Language.GetText("UI.Create"), 0.6f, true)
        {
            Width = new StyleDimension(0, 1),
            Top = new StyleDimension(46, 0),
            HAlign = 0.5f
        };
        CreateButton.OnMouseOver += (_, _) =>
        {
            CreateButton.BackgroundColor = new Color(73, 94, 171);
            CreateButton.BorderColor = Colors.FancyUIFatButtonMouseOver;
        };
        CreateButton.OnMouseOut += (_, _) =>
        {
            CreateButton.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            CreateButton.BorderColor = Color.Black;
        };
        CreateButton.OnLeftClick += (_, _) =>
        {
            if (!CanCreate)
            {
                Main.NewText("You can only have one SSC character.", Color.Red);
                return;
            }

            var Character = new Player();
            CharacterCreation = new UICharacterCreation(Character);

            Character.name = Dummy.name;
            Character.difficulty = PlayerDifficultyID.SoftCore;
            CharacterCreation.SetupPlayerStatsAndInventoryBasedOnDifficulty();

            Character.skinVariant = HookManager.JoinPlayer.skinVariant;
            Character.skinColor = HookManager.JoinPlayer.skinColor;
            Character.eyeColor = HookManager.JoinPlayer.eyeColor;
            Character.hair = HookManager.JoinPlayer.hair;
            Character.hairColor = HookManager.JoinPlayer.hairColor;
            Character.shirtColor = HookManager.JoinPlayer.shirtColor;
            Character.underShirtColor = HookManager.JoinPlayer.underShirtColor;
            Character.pantsColor = HookManager.JoinPlayer.pantsColor;
            Character.shoeColor = HookManager.JoinPlayer.shoeColor;

            var data = new PlayerFileData("Create.SSC", false)
            {
                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
                Player = Character
            };
            data.MarkAsServerSide();

            FileUtilities.ProtectedInvoke(() => Player.InternalSavePlayerFile(data));

            NameSearchBar.SetContents("");
            Dummy.difficulty = PlayerDifficultyID.SoftCore;
        };
        CharacterCreationPanel.Append(CreateButton);
    }

    public void Calc(TagCompound obj)
    {
#if DEBUG
        Main.NewText("[DEBUG/SSC] ServerViewer.Calc() invoked");
#endif
        CharacterGrid.Clear();

        var list = obj.Get<List<TagCompound>>(SSC.GetPID());
        CharacterCount = list.Count;

        foreach (var tag in list)
        {
            var item = new UIPanel
            {
                Width = new StyleDimension(0, 1),
                Height = new StyleDimension(72, 0),
                PaddingTop = 8
            };
            item.SetPadding(10);

            item.OnMouseOver += (_, _) =>
            {
                item.BackgroundColor = new Color(73, 94, 171);
                item.BorderColor = Color.Yellow;
            };
            item.OnMouseOut += (_, _) =>
            {
                item.BackgroundColor = new Color(63, 82, 151) * 0.7f;
                item.BorderColor = Color.Black;
            };

            CharacterGrid.Add(item);

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

            item.Append(new UIText(new TimeSpan(tag.GetLong("play_time")).ToString(@"dd\:hh\:mm\:ss"))
            {
                Height = new StyleDimension(15, 0),
                HAlign = 0.5f,
                VAlign = 1
            });

            var itemPlayButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay"))
            {
                Width = new StyleDimension(32, 0),
                Height = new StyleDimension(32, 0),
                VAlign = 0.5f,
                Left = new StyleDimension(4, 0)
            };
            itemPlayButton.OnLeftClick += (_, _) =>
            {
                var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
                mp.Write((byte)AdventurePacketIdentifier.SSC);
                mp.Write((byte)SSCMessageID.GoGoSSC);
                mp.Write(SSC.GetPID());
                mp.Write(tag.GetString("name"));
                mp.Send();
            };
            itemPlayButton.OnUpdate += _ =>
            {
                itemPlayButton.Width.Set(22, 0);
                itemPlayButton.Height.Set(22, 0);
                itemPlayButton.VAlign = 1f;
                itemPlayButton.Left.Set(0, 0);
                itemPlayButton.Top.Set(3, 0);

                if (itemPlayButton.IsMouseHovering)
                {
                    Main.instance.MouseText(Language.GetTextValue("UI.Play"));
                }
            };
            item.Append(itemPlayButton);

            var itemDeleteButton = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete"))
            {
                Width = new StyleDimension(22, 0),
                Height = new StyleDimension(22, 0),
                HAlign = 1,
                VAlign = 0.5f,
                Left = new StyleDimension(-4, 0)
            };
            itemDeleteButton.OnLeftClick += (_, _) =>
            {
                var mp = ModContent.GetInstance<PvPAdventure>().GetPacket();
                mp.Write((byte)AdventurePacketIdentifier.SSC);
                mp.Write((byte)SSCMessageID.EraseSSC);
                mp.Write(SSC.GetPID());
                mp.Write(tag.Get<string>("name"));
                mp.Send();
            };
            itemDeleteButton.OnUpdate += _ =>
            {
                itemDeleteButton.Width.Set(22, 0);
                itemDeleteButton.Height.Set(22, 0);
                itemDeleteButton.HAlign = 1f;
                itemDeleteButton.VAlign = 1f;
                itemDeleteButton.Left.Set(0, 0);
                itemDeleteButton.Top.Set(3, 0);

                if (itemDeleteButton.IsMouseHovering)
                {
                    Main.instance.MouseText(Language.GetTextValue("UI.Delete"));
                }
            };
            item.Append(itemDeleteButton);
        }

        // IMPORTANT: UIGrid placement works only with Add().
        CharacterGrid.Add(CharacterCreationPanel);
    }
}
