//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Threading.Tasks;
//using Terraria;
//using Terraria.Audio;
//using Terraria.GameContent.UI.Elements;
//using Terraria.ID;
//using Terraria.ModLoader.UI;
//using Terraria.UI;

//namespace PvPAdventure.Common.MainMenu.MatchHistory.LegacyMatchHistory.UI;

//// Button that lets you download a TPVPA game as a .pvpdem
//// Shows a green success text for 1.5 seconds and then reverts
//// TODO: Opens the user's file OS saving system? Or just saves to tModLoader/PvPAdventure/Replays
//public sealed class UIOpenButton : UITextPanel<string>
//{
//    //private bool _clickedOffsetActive;

//    //private readonly Func<string> getDownloadedLabel;
//    //private readonly float baseTextScale;
//    private readonly Action _onClick;

//    public UIOpenButton(Action onClick, Func<string> getDownloadedLabel, float textScale = 0.7f, bool large = true)
//    : base("Open", textScale, large)
//    {
//        this._onClick = onClick;
//        //this.getDownloadedLabel = getDownloadedLabel;
//        //baseTextScale = textScale;

//        Width = new StyleDimension(-10f, 0.5f);
//        Height = new StyleDimension(50f, 0f);

//        PaddingLeft = 10f;
//        PaddingRight = 10f;
//        PaddingTop = 10f;
//        PaddingBottom = 10f;

//        BackgroundColor = UICommon.DefaultUIBlueMouseOver;
//        BorderColor = Color.Black;

//        bool playedTick = false;
//        bool feedbackActive = false;
//        int feedbackNonce = 0;

//        OnMouseOver += (_, __) =>
//        {
//            if (feedbackActive)
//                return;

//            BackgroundColor = UICommon.DefaultUIBlue;
//            BorderColor = Colors.FancyUIFatButtonMouseOver;

//            if (!playedTick)
//            {
//                SoundEngine.PlaySound(SoundID.MenuTick);
//                playedTick = true;
//            }
//        };

//        OnMouseOut += (_, __) =>
//        {
//            if (feedbackActive)
//                return;

//            BackgroundColor = UICommon.DefaultUIBlueMouseOver;
//            BorderColor = Color.Black;
//            playedTick = false;
//        };

//        OnLeftClick += (_, __) =>
//        {
//            try
//            {
//                _onClick?.Invoke();
//            }
//            catch (Exception e)
//            {
//                Log.Error($"Open button click failed: {e}");
//            }

//            string text = "Opening...";

//            int nonce = ++feedbackNonce;
//            feedbackActive = true;
//            //_clickedOffsetActive = true;

//            SetText(text);
//            BackgroundColor = new Color(40, 130, 50) * 0.9f;
//            BorderColor = Color.Black;

//            Task.Run(async () =>
//            {
//                await Task.Delay(1000);

//                Main.QueueMainThreadAction(() =>
//                {
//                    if (nonce != feedbackNonce)
//                        return;

//                    feedbackActive = false;
//                    //_clickedOffsetActive = false;

//                    SetText("Open");
//                    BackgroundColor = UICommon.DefaultUIBlueMouseOver;
//                    BorderColor = Color.Black;
//                });
//            });
//        };
//    }

//    protected override void DrawSelf(SpriteBatch spriteBatch)
//    {
//        base.DrawSelf(spriteBatch);
//        return;

//        //string text = Text;
//        //var oldText = _text;
//        //Vector2 oldSize = _textSize;

//        //_text = "";
//        //_textSize = Vector2.Zero;

//        //base.DrawSelf(spriteBatch);

//        //_text = oldText;
//        //_textSize = oldSize;

//        //DrawTextCentered(spriteBatch, text);
//    }

//    //private void DrawTextCentered(SpriteBatch spriteBatch, string text)
//    //{
//    //    CalculatedStyle inner = GetInnerDimensions();
//    //    Vector2 pos = inner.Position();

//    //    pos.X += (inner.Width - _textSize.X) * TextHAlign;
//    //    pos.Y += (inner.Height - _textSize.Y) * 0.5f-8;

//    //    if (_clickedOffsetActive)
//    //        pos.Y += 2f;

//    //    pos.X = (int)pos.X;
//    //    pos.Y = (int)pos.Y;

//    //    if (HideContents)
//    //    {
//    //        _asterisks ??= "";
//    //        if (_asterisks.Length != text.Length)
//    //            _asterisks = new string('*', text.Length);
//    //        text = _asterisks;
//    //    }

//    //    if (_isLarge)
//    //        Utils.DrawBorderStringBig(spriteBatch, text, pos, _color, _textScale);
//    //    else
//    //        Utils.DrawBorderString(spriteBatch, text, pos, _color, _textScale);
//    //}

//    public void RefreshLabel()
//    {
//        //SetFittedText("Open");
//        SetText("Open");
//        return;

//        //string label = getDownloadedLabel?.Invoke() ?? "";

//        //string s = "Download";
//        //if (!string.IsNullOrWhiteSpace(label))
//        //s += " " + label;

//        //SetFittedText(s);
//    }
//    //private void SetFittedText(string s)
//    //{
//    //    Recalculate();

//    //    float w = GetInnerDimensions().Width;

//    //    var font = _isLarge ? FontAssets.DeathText.Value : FontAssets.MouseText.Value;
//    //    float px = ChatManager.GetStringSize(font, s, Vector2.One).X;

//    //    float fit = baseTextScale;
//    //    if (px > 0f && w > 0f)
//    //        fit = Math.Min(baseTextScale, (w - 4f) / px);

//    //    SetText(s, fit, _isLarge);

//    //    MinWidth = new StyleDimension(0f, 0f);
//    //    MinHeight = new StyleDimension(0f, 0f);
//    //}

//    #region Open file
//    public static void OpenMatchFile(
//    string folder,
//    DateTime start,
//    Func<DateTime, string> getDeterministicPath)
//    {
//        string path = Path.GetFullPath(getDeterministicPath(start));

//        if (!File.Exists(path))
//        {
//            string? resolved = ResolveExistingMatchPath(folder, start);
//            if (resolved == null)
//                return;

//            path = resolved;
//        }

//        try
//        {
//            Process.Start(new ProcessStartInfo
//            {
//                FileName = path,
//                UseShellExecute = true
//            });
//        }
//        catch
//        {
//        }
//    }

//    private static string? ResolveExistingMatchPath(string folder, DateTime start)
//    {
//        if (!Directory.Exists(folder))
//            return null;

//        string? p;

//        p = PickNewest(folder, start);
//        if (p != null)
//            return p;

//        p = PickNewest(folder, start.ToUniversalTime());
//        if (p != null)
//            return p;

//        p = PickNewest(folder, start.ToLocalTime());
//        if (p != null)
//            return p;

//        return null;
//    }

//    private static string? PickNewest(string folder, DateTime t)
//    {
//        string prefix = t.ToString("yyyy-MM-dd_HH;mm");
//        string[] files = Directory.GetFiles(folder, prefix + "*.nbt", SearchOption.TopDirectoryOnly);

//        if (files.Length == 0)
//            return null;

//        string best = files[0];
//        DateTime bestWrite = File.GetLastWriteTimeUtc(best);

//        for (int i = 1; i < files.Length; i++)
//        {
//            DateTime w = File.GetLastWriteTimeUtc(files[i]);
//            if (w > bestWrite)
//            {
//                bestWrite = w;
//                best = files[i];
//            }
//        }

//        return best;
//    }
//    #endregion
//}