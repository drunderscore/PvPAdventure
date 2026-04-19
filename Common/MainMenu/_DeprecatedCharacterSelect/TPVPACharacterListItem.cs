//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using ReLogic.Content;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using Terraria;
//using Terraria.Audio;
//using Terraria.GameContent;
//using Terraria.GameContent.UI.Elements;
//using Terraria.GameContent.UI.States;
//using Terraria.ID;
//using Terraria.IO;
//using Terraria.Localization;
//using Terraria.ModLoader;
//using Terraria.ModLoader.Core;
//using Terraria.ModLoader.Engine;
//using Terraria.ModLoader.UI;
//using Terraria.Social;
//using Terraria.UI;
//using Terraria.Utilities;

//namespace PvPAdventure.Common.MainMenu.ServerList;

//public class TPVPACharacterListItem : UIPanel
//{
//    private PlayerFileData _data;

//    private Asset<Texture2D> _dividerTexture;

//    private Asset<Texture2D> _innerPanelTexture;

//    private UICharacter _playerPanel;

//    private UIText _buttonLabel;

//    private UIText _deleteButtonLabel;

//    private Asset<Texture2D> _buttonCloudActiveTexture;

//    private Asset<Texture2D> _buttonCloudInactiveTexture;

//    private Asset<Texture2D> _buttonFavoriteActiveTexture;

//    private Asset<Texture2D> _buttonFavoriteInactiveTexture;

//    private Asset<Texture2D> _buttonPlayTexture;

//    private Asset<Texture2D> _buttonRenameTexture;

//    private Asset<Texture2D> _buttonDeleteTexture;

//    private UIImageButton _deleteButton;

//    private Asset<Texture2D> _errorTexture;

//    private Asset<Texture2D> _configTexture;

//    private ulong _fileSize;

//    private UIText warningLabel;

//    public PlayerFileData Data => this._data;

//    public bool IsFavorite => this._data.IsFavorite;

//    public TPVPACharacterListItem(PlayerFileData data, int snapPointIndex)
//    {
//        base.BorderColor = new Color(89, 116, 213) * 0.7f;
//        this._dividerTexture = Main.Assets.Request<Texture2D>("Images/UI/Divider");
//        this._innerPanelTexture = Main.Assets.Request<Texture2D>("Images/UI/InnerPanelBackground");
//        this._buttonCloudActiveTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonCloudActive");
//        this._buttonCloudInactiveTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonCloudInactive");
//        this._buttonFavoriteActiveTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonFavoriteActive");
//        this._buttonFavoriteInactiveTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonFavoriteInactive");
//        this._buttonPlayTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay");
//        this._buttonRenameTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonRename");
//        this._buttonDeleteTexture = Main.Assets.Request<Texture2D>("Images/UI/ButtonDelete");
//        this.InitializeTmlFields(data);
//        base.Height.Set(96f, 0f);
//        base.Width.Set(0f, 1f);
//        base.SetPadding(6f);
//        this._data = data;
//        this._playerPanel = new UICharacter(data.Player, animated: false, hasBackPanel: true, 1f, useAClone: true);
//        this._playerPanel.Left.Set(4f, 0f);
//        this._playerPanel.OnLeftDoubleClick += PlayGame;
//        base.OnLeftDoubleClick += PlayGame;
//        base.Append(this._playerPanel);
//        float num = 4f;
//        UIImageButton uIImageButton = new UIImageButton(this._buttonPlayTexture);
//        uIImageButton.VAlign = 1f;
//        uIImageButton.Left.Set(num, 0f);
//        uIImageButton.OnLeftClick += PlayGame;
//        uIImageButton.OnMouseOver += PlayMouseOver;
//        uIImageButton.OnMouseOut += ButtonMouseOut;
//        base.Append(uIImageButton);
//        num += 24f;
//        UIImageButton uIImageButton2 = new UIImageButton(this._data.IsFavorite ? this._buttonFavoriteActiveTexture : this._buttonFavoriteInactiveTexture);
//        uIImageButton2.VAlign = 1f;
//        uIImageButton2.Left.Set(num, 0f);
//        uIImageButton2.OnLeftClick += FavoriteButtonClick;
//        uIImageButton2.OnMouseOver += FavoriteMouseOver;
//        uIImageButton2.OnMouseOut += ButtonMouseOut;
//        uIImageButton2.SetVisibility(1f, this._data.IsFavorite ? 0.8f : 0.4f);
//        base.Append(uIImageButton2);
//        num += 24f;
//        if (SocialAPI.Cloud != null)
//        {
//            UIImageButton uIImageButton3 = new UIImageButton(this._data.IsCloudSave ? this._buttonCloudActiveTexture : this._buttonCloudInactiveTexture);
//            uIImageButton3.VAlign = 1f;
//            uIImageButton3.Left.Set(num, 0f);
//            uIImageButton3.OnLeftClick += CloudButtonClick;
//            uIImageButton3.OnMouseOver += CloudMouseOver;
//            uIImageButton3.OnMouseOut += ButtonMouseOut;
//            base.Append(uIImageButton3);
//            uIImageButton3.SetSnapPoint("Cloud", snapPointIndex);
//            num += 24f;
//        }
//        UIImageButton uIImageButton4 = new UIImageButton(this._buttonRenameTexture);
//        uIImageButton4.VAlign = 1f;
//        uIImageButton4.Left.Set(num, 0f);
//        uIImageButton4.OnLeftClick += RenameButtonClick;
//        uIImageButton4.OnMouseOver += RenameMouseOver;
//        uIImageButton4.OnMouseOut += ButtonMouseOut;
//        base.Append(uIImageButton4);
//        num += 24f;
//        UIImageButton uIImageButton5 = new UIImageButton(this._buttonDeleteTexture)
//        {
//            VAlign = 1f,
//            HAlign = 1f
//        };
//        if (!this._data.IsFavorite)
//        {
//            uIImageButton5.OnLeftClick += DeleteButtonClick;
//        }
//        uIImageButton5.OnMouseOver += DeleteMouseOver;
//        uIImageButton5.OnMouseOut += DeleteMouseOut;
//        this._deleteButton = uIImageButton5;
//        base.Append(uIImageButton5);
//        num += 4f;
//        this.AddTmlElements(data);
//        this._buttonLabel = new UIText("");
//        this._buttonLabel.VAlign = 1f;
//        this._buttonLabel.Left.Set(num, 0f);
//        this._buttonLabel.Top.Set(-3f, 0f);
//        base.Append(this._buttonLabel);
//        this._deleteButtonLabel = new UIText("");
//        this._deleteButtonLabel.VAlign = 1f;
//        this._deleteButtonLabel.HAlign = 1f;
//        this._deleteButtonLabel.Left.Set(-30f, 0f);
//        this._deleteButtonLabel.Top.Set(-3f, 0f);
//        base.Append(this._deleteButtonLabel);
//        uIImageButton.SetSnapPoint("Play", snapPointIndex);
//        uIImageButton2.SetSnapPoint("Favorite", snapPointIndex);
//        uIImageButton4.SetSnapPoint("Rename", snapPointIndex);
//        uIImageButton5.SetSnapPoint("Delete", snapPointIndex);
//    }

//    private void RenameMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._buttonLabel.SetText(Language.GetTextValue("UI.Rename"));
//    }

//    private void FavoriteMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        if (this._data.IsFavorite)
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.Unfavorite"));
//        }
//        else
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.Favorite"));
//        }
//    }

//    private void CloudMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        if (this._data.IsCloudSave)
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.MoveOffCloud"));
//        }
//        else if (!Steam.CheckSteamCloudStorageSufficient(this._fileSize))
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("tModLoader.CloudWarning"));
//        }
//        else
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.MoveToCloud"));
//        }
//    }

//    private void PlayMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._buttonLabel.SetText(Language.GetTextValue("UI.Play"));
//    }

//    private void DeleteMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        if (this._data.IsFavorite)
//        {
//            this._deleteButtonLabel.SetText(Language.GetTextValue("UI.CannotDeleteFavorited"));
//        }
//        else
//        {
//            this._deleteButtonLabel.SetText(Language.GetTextValue("UI.Delete"));
//        }
//    }

//    private void DeleteMouseOut(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._deleteButtonLabel.SetText("");
//    }

//    private void ButtonMouseOut(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._buttonLabel.SetText("");
//    }

//    private void RenameButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        SoundEngine.PlaySound(10);
//        Main.clrInput();
//        UIVirtualKeyboard uIVirtualKeyboard = new UIVirtualKeyboard(Lang.menu[45].Value, "", OnFinishedSettingName, GoBackHere, 0, allowEmpty: true);
//        uIVirtualKeyboard.SetMaxInputLength(20);
//        Main.MenuUI.SetState(uIVirtualKeyboard);
//        if (base.Parent.Parent is UIList uIList)
//        {
//            uIList.UpdateOrder();
//        }
//    }

//    private void OnFinishedSettingName(string name)
//    {
//        string newName = name.Trim();
//        Main.menuMode = 10;
//        this._data.Rename(newName);
//        Main.OpenCharacterSelectUI();
//    }

//    private void GoBackHere()
//    {
//        Main.OpenCharacterSelectUI();
//    }

//    private void CloudButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        if (this._data.IsCloudSave)
//        {
//            this._data.MoveToLocal();
//        }
//        else
//        {
//            Steam.RecalculateAvailableSteamCloudStorage();
//            if (!Steam.CheckSteamCloudStorageSufficient(this._fileSize))
//            {
//                return;
//            }
//            this._data.MoveToCloud();
//        }
//        ((UIImageButton)evt.Target).SetImage(this._data.IsCloudSave ? this._buttonCloudActiveTexture : this._buttonCloudInactiveTexture);
//        if (this._data.IsCloudSave)
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.MoveOffCloud"));
//        }
//        else
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.MoveToCloud"));
//        }
//    }

//    private void DeleteButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        for (int i = 0; i < Main.PlayerList.Count; i++)
//        {
//            if (Main.PlayerList[i] == this._data)
//            {
//                SoundEngine.PlaySound(10);
//                Main.selectedPlayer = i;
//                Main.menuMode = 5;
//                break;
//            }
//        }
//    }

//    private void PlayGame(UIMouseEvent evt, UIElement listeningElement)
//    {
//        if (listeningElement != evt.Target || _data.Player.loadStatus != 0)
//            return;

//        SoundEngine.PlaySound(SoundID.MenuOpen);
//        PlayMenuFlow.SetSelectedPlayer(_data);
//        PlayMenuFlow.OpenServerList();
//    }

//    private void FavoriteButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._data.ToggleFavorite();
//        ((UIImageButton)evt.Target).SetImage(this._data.IsFavorite ? this._buttonFavoriteActiveTexture : this._buttonFavoriteInactiveTexture);
//        ((UIImageButton)evt.Target).SetVisibility(1f, this._data.IsFavorite ? 0.8f : 0.4f);
//        if (this._data.IsFavorite)
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.Unfavorite"));
//            this._deleteButton.OnLeftClick -= DeleteButtonClick;
//        }
//        else
//        {
//            this._buttonLabel.SetText(Language.GetTextValue("UI.Favorite"));
//            this._deleteButton.OnLeftClick += DeleteButtonClick;
//        }
//        if (base.Parent.Parent is UIList uIList)
//        {
//            uIList.UpdateOrder();
//        }
//    }

//    public override int CompareTo(object obj)
//    {
//        if (obj is UICharacterListItem uICharacterListItem)
//        {
//            if (this.IsFavorite && !uICharacterListItem.IsFavorite)
//            {
//                return -1;
//            }
//            if (!this.IsFavorite && uICharacterListItem.IsFavorite)
//            {
//                return 1;
//            }
//            if (this._data.Name.CompareTo(uICharacterListItem._data.Name) != 0)
//            {
//                return this._data.Name.CompareTo(uICharacterListItem._data.Name);
//            }
//            return this._data.GetFileName().CompareTo(uICharacterListItem._data.GetFileName());
//        }
//        return base.CompareTo(obj);
//    }

//    public override void MouseOver(UIMouseEvent evt)
//    {
//        base.MouseOver(evt);
//        base.BackgroundColor = new Color(73, 94, 171);
//        base.BorderColor = new Color(89, 116, 213);
//        this._playerPanel.SetAnimated(animated: true);
//    }

//    public override void MouseOut(UIMouseEvent evt)
//    {
//        base.MouseOut(evt);
//        base.BackgroundColor = new Color(63, 82, 151) * 0.7f;
//        base.BorderColor = new Color(89, 116, 213) * 0.7f;
//        this._playerPanel.SetAnimated(animated: false);
//    }

//    private void DrawPanel(SpriteBatch spriteBatch, Vector2 position, float width)
//    {
//        spriteBatch.Draw(this._innerPanelTexture.Value, position, new Rectangle(0, 0, 8, this._innerPanelTexture.Height()), Color.White);
//        spriteBatch.Draw(this._innerPanelTexture.Value, new Vector2(position.X + 8f, position.Y), new Rectangle(8, 0, 8, this._innerPanelTexture.Height()), Color.White, 0f, Vector2.Zero, new Vector2((width - 16f) / 8f, 1f), SpriteEffects.None, 0f);
//        spriteBatch.Draw(this._innerPanelTexture.Value, new Vector2(position.X + width - 8f, position.Y), new Rectangle(16, 0, 8, this._innerPanelTexture.Height()), Color.White);
//    }

//    protected override void DrawSelf(SpriteBatch spriteBatch)
//    {
//        base.DrawSelf(spriteBatch);
//        CalculatedStyle innerDimensions = base.GetInnerDimensions();
//        CalculatedStyle dimensions = this._playerPanel.GetDimensions();
//        float num = dimensions.X + dimensions.Width;
//        Color color = Color.White;
//        string text = this._data.Name;
//        if (this._data.Player.loadStatus != 0)
//        {
//            color = Color.Gray;
//            string name = StatusID.Search.GetName(this._data.Player.loadStatus);
//            text = "(" + name + ") " + text;
//        }
//        Utils.DrawBorderString(spriteBatch, text, new Vector2(num + 6f, dimensions.Y - 2f), color);
//        spriteBatch.Draw(this._dividerTexture.Value, new Vector2(num, innerDimensions.Y + 21f), null, Color.White, 0f, Vector2.Zero, new Vector2((base.GetDimensions().X + base.GetDimensions().Width - num) / 8f, 1f), SpriteEffects.None, 0f);
//        Vector2 vector = new Vector2(num + 6f, innerDimensions.Y + 29f);
//        float num2 = 200f;
//        Vector2 vector2 = vector;
//        this.DrawPanel(spriteBatch, vector2, num2);
//        spriteBatch.Draw(TextureAssets.Heart.Value, vector2 + new Vector2(5f, 2f), Color.White);
//        vector2.X += 10f + (float)TextureAssets.Heart.Width();
//        Utils.DrawBorderString(spriteBatch, this._data.Player.statLifeMax2 + Language.GetTextValue("GameUI.PlayerLifeMax"), vector2 + new Vector2(0f, 3f), Color.White);
//        vector2.X += 65f;
//        spriteBatch.Draw(TextureAssets.Mana.Value, vector2 + new Vector2(5f, 2f), Color.White);
//        vector2.X += 10f + (float)TextureAssets.Mana.Width();
//        Utils.DrawBorderString(spriteBatch, this._data.Player.statManaMax2 + Language.GetTextValue("GameUI.PlayerManaMax"), vector2 + new Vector2(0f, 3f), Color.White);
//        vector.X += num2 + 5f;
//        Vector2 vector3 = vector;
//        float num3 = 140f;
//        if (GameCulture.FromCultureName(GameCulture.CultureName.Russian).IsActive)
//        {
//            num3 = 180f;
//        }
//        this.DrawPanel(spriteBatch, vector3, num3);
//        string text2 = "";
//        Color color2 = Color.White;
//        switch (this._data.Player.difficulty)
//        {
//            case 0:
//                text2 = Language.GetTextValue("UI.Softcore");
//                break;
//            case 1:
//                text2 = Language.GetTextValue("UI.Mediumcore");
//                color2 = Main.mcColor;
//                break;
//            case 2:
//                text2 = Language.GetTextValue("UI.Hardcore");
//                color2 = Main.hcColor;
//                break;
//            case 3:
//                text2 = Language.GetTextValue("UI.Creative");
//                color2 = Main.creativeModeColor;
//                break;
//        }
//        vector3 += new Vector2(num3 * 0.5f - FontAssets.MouseText.Value.MeasureString(text2).X * 0.5f, 3f);
//        Utils.DrawBorderString(spriteBatch, text2, vector3, color2);
//        vector.X += num3 + 5f;
//        Vector2 vector4 = vector;
//        float num4 = innerDimensions.X + innerDimensions.Width - vector4.X;
//        this.DrawPanel(spriteBatch, vector4, num4);
//        TimeSpan playTime = this._data.GetPlayTime();
//        int num5 = playTime.Days * 24 + playTime.Hours;
//        string text3 = ((num5 < 10) ? "0" : "") + num5 + playTime.ToString("\\:mm\\:ss");
//        vector4 += new Vector2(num4 * 0.5f - FontAssets.MouseText.Value.MeasureString(text3).X * 0.5f, 3f);
//        Utils.DrawBorderString(spriteBatch, text3, vector4, Color.White);
//    }

//    private void InitializeTmlFields(PlayerFileData data)
//    {
//        this._errorTexture = UICommon.ButtonErrorTexture;
//        this._configTexture = UICommon.ButtonConfigTexture;
//        this._fileSize = (ulong)FileUtilities.GetFileSize(data.Path, data.IsCloudSave);
//    }

//    private void AddTmlElements(PlayerFileData data)
//    {
//        this.warningLabel = new UIText("")
//        {
//            VAlign = 0f,
//            HAlign = 1f
//        };
//        float topRightButtonsLeftPixels = 0f;
//        this.warningLabel.Top.Set(3f, 0f);
//        base.Append(this.warningLabel);
//        StringBuilder shortSB;
//        if (data.Player.usedMods != null)
//        {
//            string[] currentModNames = Terraria.ModLoader.ModLoader.Mods.Select((Mod m) => m.Name).ToArray();
//            List<string> missingMods = data.Player.usedMods.Except(currentModNames).Select(ModOrganizer.GetDisplayNameCleanFromLocalModsOrDefaultToModName).ToList();
//            List<string> newMods = currentModNames.Except(new string[1] { "ModLoader" }).Except(data.Player.usedMods).Select(ModOrganizer.GetDisplayNameCleanFromLocalModsOrDefaultToModName)
//                .ToList();
//            bool checkModPack = Path.GetFileNameWithoutExtension(ModOrganizer.ModPackActive) != data.Player.modPack;
//            if (checkModPack || missingMods.Count > 0 || newMods.Count > 0)
//            {
//                UIImageButton modListWarning = new UIImageButton(this._errorTexture)
//                {
//                    VAlign = 0f,
//                    HAlign = 1f,
//                    Top = new StyleDimension(-2f, 0f),
//                    Left = new StyleDimension(topRightButtonsLeftPixels, 0f)
//                };
//                topRightButtonsLeftPixels -= 24f;
//                StringBuilder fullSB = new StringBuilder(Language.GetTextValue("tModLoader.ModsDifferentSinceLastPlay"));
//                shortSB = new StringBuilder();
//                if (checkModPack)
//                {
//                    string pack = data.Player.modPack;
//                    if (string.IsNullOrEmpty(pack))
//                    {
//                        pack = "None";
//                    }
//                    shortSB.Append(Separator() + Language.GetTextValue("tModLoader.ModPackMismatch", pack));
//                    fullSB.Append("\n" + Language.GetTextValue("tModLoader.ModPackMismatch", pack));
//                }
//                if (missingMods.Count > 0)
//                {
//                    shortSB.Append(Separator() + ((missingMods.Count > 1) ? Language.GetTextValue("tModLoader.MissingXMods", missingMods.Count) : Language.GetTextValue("tModLoader.Missing1Mod")));
//                    fullSB.Append("\n" + Language.GetTextValue("tModLoader.MissingModsListing", string.Join("\n", missingMods.Select((string x) => "- " + x))));
//                }
//                if (newMods.Count > 0)
//                {
//                    shortSB.Append(Separator() + ((newMods.Count > 1) ? Language.GetTextValue("tModLoader.NewXMods", newMods.Count) : Language.GetTextValue("tModLoader.New1Mod")));
//                    fullSB.Append("\n" + Language.GetTextValue("tModLoader.NewModsListing", string.Join("\n", newMods.Select((string x) => "- " + x))));
//                }
//                if (shortSB.Length != 0)
//                {
//                    shortSB.Append('.');
//                }
//                string warning = shortSB.ToString();
//                string fullWarning = fullSB.ToString();
//                modListWarning.OnMouseOver += delegate
//                {
//                    this.warningLabel.SetText(warning);
//                };
//                modListWarning.OnMouseOut += delegate
//                {
//                    this.warningLabel.SetText("");
//                };
//                modListWarning.OnLeftClick += delegate
//                {
//                    Interface.infoMessage.Show(fullWarning, 888, Main._characterSelectMenu);
//                };
//                base.Append(modListWarning);
//            }
//        }
//        if (data.customDataFail != null)
//        {
//            UIImageButton errorButton = new UIImageButton(this._errorTexture)
//            {
//                VAlign = 0f,
//                HAlign = 1f,
//                Top = new StyleDimension(-2f, 0f),
//                Left = new StyleDimension(topRightButtonsLeftPixels, 0f)
//            };
//            topRightButtonsLeftPixels -= 24f;
//            errorButton.OnLeftClick += ErrorButtonClick;
//            errorButton.OnMouseOver += ErrorMouseOver;
//            errorButton.OnMouseOut += delegate
//            {
//                this.warningLabel.SetText("");
//            };
//            base.Append(errorButton);
//        }
//        if (data.Player.ModSaveErrors.Any())
//        {
//            UIImageButton errorButton2 = new UIImageButton(this._errorTexture)
//            {
//                VAlign = 0f,
//                HAlign = 1f,
//                Top = new StyleDimension(-2f, 0f),
//                Left = new StyleDimension(topRightButtonsLeftPixels, 0f)
//            };
//            topRightButtonsLeftPixels -= 24f;
//            errorButton2.OnLeftClick += SaveErrorButtonClick;
//            errorButton2.OnMouseOver += SaveErrorMouseOver;
//            errorButton2.OnMouseOut += delegate
//            {
//                this.warningLabel.SetText("");
//            };
//            base.Append(errorButton2);
//        }
//        this.warningLabel.Left.Set(topRightButtonsLeftPixels - 6f, 0f);
//        string Separator()
//        {
//            if (shortSB.Length == 0)
//            {
//                return null;
//            }
//            return "; ";
//        }
//    }

//    private void ErrorMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this.warningLabel.SetText(this._data.customDataFail.modName + " Error");
//    }

//    private void SaveErrorMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this.warningLabel.SetText(Language.GetTextValue("tModLoader.ViewSaveErrorMessage"));
//    }

//    private void ConfigMouseOver(UIMouseEvent evt, UIElement listeningElement)
//    {
//        this._buttonLabel.SetText("Edit Player Config");
//    }

//    private void ErrorButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        Interface.infoMessage.Show(Language.GetTextValue("tModLoader.PlayerCustomDataFail") + "\n\n" + this._data.customDataFail.InnerException, 888, Main._characterSelectMenu);
//    }

//    private void SaveErrorButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//        string message = Utils.CreateSaveErrorMessage("tModLoader.PlayerCustomDataSaveFail", this._data.Player.ModSaveErrors, doubleNewline: true).ToString();
//        Interface.infoMessage.Show(message, 888, Main._characterSelectMenu);
//    }

//    private void ConfigButtonClick(UIMouseEvent evt, UIElement listeningElement)
//    {
//    }
//}
