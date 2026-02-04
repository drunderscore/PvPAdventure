//using System;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using Terraria;
//using Terraria.GameContent;
//using Terraria.Graphics.Effects;
//using Terraria.ModLoader;

//namespace PvPAdventure.Common.MatchHistory.Projector;

// WORKING PROTOTYPE: Replaces the in-game projector with a low-res version rendered to a small RT
//public sealed class ProjectorSystem : ModSystem
//{
//    private RenderTarget2D _lowResRT;
//    private SpriteBatch _sb;
//    private int _lastLowResW;
//    private int _lastLowResH;
//    private uint _nextMissingSourceLogAt;
//    private bool _capturedAtLeastOnce;

//    public override void Load()
//    {
//        On_FilterManager.EndCapture += EndCaptureHook;
//    }

//    public override void Unload()
//    {
//        On_FilterManager.EndCapture -= EndCaptureHook;

//        if (_lowResRT != null && !_lowResRT.IsDisposed)
//        {
//            _lowResRT.Dispose();
//        }

//        _lowResRT = null;
//        _lastLowResW = 0;
//        _lastLowResH = 0;
//        _capturedAtLeastOnce = false;

//        _sb?.Dispose();
//        _sb = null;
//    }

//    private static bool IsValidRT(RenderTarget2D rt)
//    {
//        return rt != null && !rt.IsDisposed && rt.Width > 0 && rt.Height > 0;
//    }

//    private static RenderTarget2D PickSource(RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2)
//    {
//        if (IsValidRT(finalTexture))
//        {
//            return finalTexture;
//        }

//        if (IsValidRT(screenTarget1))
//        {
//            return screenTarget1;
//        }

//        if (IsValidRT(screenTarget2))
//        {
//            return screenTarget2;
//        }

//        return null;
//    }

//    private void EnsureSpriteBatch(GraphicsDevice gd)
//    {
//        if (_sb == null)
//        {
//            _sb = new SpriteBatch(gd);
//        }
//    }

//    private void EnsureLowResRT(GraphicsDevice gd, int w, int h)
//    {
//        w = Math.Max(64, w);
//        h = Math.Max(64, h);

//        if (_lowResRT != null && !_lowResRT.IsDisposed && _lastLowResW == w && _lastLowResH == h)
//        {
//            return;
//        }

//        if (_lowResRT != null && !_lowResRT.IsDisposed)
//        {
//            _lowResRT.Dispose();
//        }

//        _lowResRT = new RenderTarget2D(gd, w, h, mipMap: false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
//        _lastLowResW = w;
//        _lastLowResH = h;
//    }

//    private void EndCaptureHook(
//        On_FilterManager.orig_EndCapture orig,
//        FilterManager self,
//        RenderTarget2D finalTexture,
//        RenderTarget2D screenTarget1,
//        RenderTarget2D screenTarget2,
//        Color clearColor)
//    {
//        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);

//        if (Main.gameMenu || Main.dedServ)
//        {
//            return;
//        }

//        GraphicsDevice gd = Main.graphics?.GraphicsDevice;
//        if (gd == null)
//        {
//            return;
//        }

//        RenderTarget2D source = PickSource(finalTexture, screenTarget1, screenTarget2);
//        if (source == null)
//        {
//            uint now = Main.GameUpdateCount;
//            if (now >= _nextMissingSourceLogAt)
//            {
//                _nextMissingSourceLogAt = now + 120;
//                Log.Warn("missing capture RT (finalTexture/screenTarget1/screenTarget2)");
//            }

//            return;
//        }

//        EnsureSpriteBatch(gd);

//        int lowW = Math.Max(160, source.Width / 6);
//        int lowH = Math.Max(90, source.Height / 6);
//        EnsureLowResRT(gd, lowW, lowH);

//        RenderTargetBinding[] restoreTargets = gd.GetRenderTargets();

//        gd.SetRenderTarget(_lowResRT);
//        gd.Clear(Color.Transparent);

//        _sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
//        _sb.Draw(source, new Rectangle(0, 0, _lowResRT.Width, _lowResRT.Height), Color.White);
//        _sb.End();

//        gd.SetRenderTargets(restoreTargets);

//        _capturedAtLeastOnce = true;
//    }

//    public override void PostDrawInterface(SpriteBatch spriteBatch)
//    {
//        if (Main.gameMenu || Main.dedServ)
//        {
//            return;
//        }

//        if (!_capturedAtLeastOnce || _lowResRT == null || _lowResRT.IsDisposed)
//        {
//            return;
//        }

//        GraphicsDevice gd = Main.graphics?.GraphicsDevice;
//        if (gd == null)
//        {
//            return;
//        }

//        Player p = Main.LocalPlayer;
//        if (p == null || !p.active)
//        {
//            return;
//        }

//        EnsureSpriteBatch(gd);

//        Vector2 anchorWorld = p.Center + new Vector2(220f, -140f);
//        Vector2 anchorScreen = anchorWorld - Main.screenPosition;

//        int insetW = Math.Min(360, Main.screenWidth / 3);
//        int insetH = insetW * 9 / 16;
//        int pad = 6;

//        Rectangle outer = new Rectangle((int)anchorScreen.X, (int)anchorScreen.Y, insetW, insetH);
//        Rectangle inner = new Rectangle(outer.X + pad, outer.Y + pad, Math.Max(1, outer.Width - pad * 2), Math.Max(1, outer.Height - pad * 2));

//        if (outer.Right < 0 || outer.Bottom < 0 || outer.X > Main.screenWidth || outer.Y > Main.screenHeight)
//        {
//            return;
//        }

//        float t = (float)Main.GlobalTimeWrappedHourly;
//        Vector2 wobble = new Vector2((float)Math.Sin(t * 2.2f) * 2f, (float)Math.Cos(t * 1.7f) * 2f);

//        _sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

//        _sb.Draw(TextureAssets.MagicPixel.Value, outer, Color.Black * 0.70f);
//        _sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(outer.X - 2, outer.Y - 2, outer.Width + 4, 2), Color.White * 0.35f);
//        _sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(outer.X - 2, outer.Bottom, outer.Width + 4, 2), Color.White * 0.20f);
//        _sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(outer.X - 2, outer.Y, 2, outer.Height), Color.White * 0.20f);
//        _sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(outer.Right, outer.Y, 2, outer.Height), Color.White * 0.20f);

//        Vector2 scale = new Vector2((float)inner.Width / _lowResRT.Width, (float)inner.Height / _lowResRT.Height);
//        _sb.Draw(_lowResRT, inner.Location.ToVector2() + wobble, null, new Color(0.75f, 0.95f, 1.00f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

//        _sb.Draw(TextureAssets.MagicPixel.Value, inner, Color.White * 0.05f);

//        _sb.End();
//    }
//}
