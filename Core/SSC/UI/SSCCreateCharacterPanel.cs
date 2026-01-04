//using Microsoft.Xna.Framework;
//using System;
//using Terraria;
//using Terraria.GameContent.UI.Elements;
//using Terraria.GameContent.UI.States;
//using Terraria.ID;
//using Terraria.IO;
//using Terraria.Localization;
//using Terraria.UI;
//using Terraria.Utilities;

//namespace PvPAdventure.Core.SSC.UI;

//internal sealed class SSCCreateCharacterPanel : UIPanel
//{
//    readonly Player dummy;

//    UICharacterNameButton nameButton;
//    UISearchBar nameSearchBar;
//    UITextPanel<LocalizedText> createButton;

//    public SSCCreateCharacterPanel(Player dummy)
//    {
//        this.dummy = dummy;
//    }

//    public override void OnInitialize()
//    {
//        Width.Set(0f, 1f);
//        Height.Set(108f, 0f);
//        SetPadding(10f);

//        BackgroundColor = new Color(63, 82, 151) * 0.7f;
//        BorderColor = new Color(89, 116, 213) * 0.7f;

//        nameButton = new UICharacterNameButton(Language.GetText("UI.PlayerNameSlot"), LocalizedText.Empty)
//        {
//            Width = new StyleDimension(0, 1),
//            Height = new StyleDimension(40, 0)
//        };

//        nameButton.OnUpdate += _ =>
//        {
//            if (!Main.mouseLeft)
//            {
//                return;
//            }

//            switch (nameButton.IsMouseHovering)
//            {
//                case true when !nameSearchBar.IsWritingText:
//                case false when nameSearchBar.IsWritingText:
//                    nameSearchBar.ToggleTakingText();
//                    break;
//            }
//        };

//        Append(nameButton);

//        nameSearchBar = new UISearchBar(LocalizedText.Empty, 1)
//        {
//            Width = new StyleDimension(-50, 1),
//            Height = new StyleDimension(40, 0),
//            Left = new StyleDimension(50, 0)
//        };

//        nameSearchBar.OnMouseOver += (evt, _) => nameButton.MouseOver(evt);
//        nameSearchBar.OnMouseOut += (evt, _) => nameButton.MouseOut(evt);
//        nameSearchBar.OnContentsChanged += name => dummy.name = name;

//        Append(nameSearchBar);

//        createButton = new UITextPanel<LocalizedText>(Language.GetText("UI.Create"), 0.6f, true)
//        {
//            Width = new StyleDimension(0, 1),
//            Top = new StyleDimension(46, 0),
//            HAlign = 0.5f
//        };

//        createButton.OnMouseOver += (_, _) =>
//        {
//            createButton.BackgroundColor = new Color(73, 94, 171);
//            createButton.BorderColor = Colors.FancyUIFatButtonMouseOver;
//        };

//        createButton.OnMouseOut += (_, _) =>
//        {
//            createButton.BackgroundColor = new Color(63, 82, 151) * 0.8f;
//            createButton.BorderColor = Color.Black;
//        };

//        createButton.OnLeftClick += (_, _) =>
//        {
//            var character = new Player();
//            var creation = new UICharacterCreation(character);

//            character.name = dummy.name;
//            character.difficulty = PlayerDifficultyID.SoftCore;
//            creation.SetupPlayerStatsAndInventoryBasedOnDifficulty();

//            // copy visuals from joining player
//            var join = HookManager.JoinPlayer;
//            if (join != null)
//            {
//                character.skinVariant = join.skinVariant;
//                character.skinColor = join.skinColor;
//                character.eyeColor = join.eyeColor;
//                character.hair = join.hair;
//                character.hairColor = join.hairColor;
//                character.shirtColor = join.shirtColor;
//                character.underShirtColor = join.underShirtColor;
//                character.pantsColor = join.pantsColor;
//                character.shoeColor = join.shoeColor;
//            }

//            var data = new PlayerFileData("Create.SSC", false)
//            {
//                Metadata = FileMetadata.FromCurrentSettings(FileType.Player),
//                Player = character
//            };
//            data.MarkAsServerSide();

//            FileUtilities.ProtectedInvoke(() => Player.InternalSavePlayerFile(data));

//            nameSearchBar.SetContents("");
//            dummy.difficulty = PlayerDifficultyID.SoftCore;
//        };

//        Append(createButton);
//    }
//}
