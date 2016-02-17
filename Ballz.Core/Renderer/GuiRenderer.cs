using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ballz.Renderer
{
    public class GuiRenderer : DrawableGameComponent
    {
        Texture2D GuiTexture;
        SpriteBatch SpriteBatch;
        Ballz Game;
        IHtmlRenderer InternalRenderer;

        public GuiRenderer(Ballz game): base(game)
        {
            Game = game;

#if __MonoCS__
            throw new NotImplementedException("No Mono support for GUI rendering yet, sorry");
#else
            InternalRenderer = new Renderer.HtmlRendererWindows();
#endif
            var filepath = Path.GetFullPath(Game.Content.RootDirectory) + "/Gui/TestGui.html";
            var uri = new Uri(filepath);
            InternalRenderer.SetURL(uri.AbsoluteUri);
        }

        public override void Draw(GameTime gameTime)
        {
            if (GuiTexture.Width != Game.GraphicsDevice.Viewport.Width || GuiTexture.Height != Game.GraphicsDevice.Viewport.Height)
                UpdateViewport();

            var data = InternalRenderer.GetImage();

            if (data != null)
            {
                GuiTexture.SetData(data);
                SpriteBatch.Begin();
                SpriteBatch.Draw(GuiTexture, Vector2.Zero, Color.White);
                SpriteBatch.End();
            }
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(Game.GraphicsDevice);
            UpdateViewport();
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                InternalRenderer.Dispose();
            }

            base.Dispose(disposing);
        }

        public void UpdateViewport()
        {
            GuiTexture = new Texture2D(Game.GraphicsDevice, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height, false, SurfaceFormat.Bgra32);
            InternalRenderer.SetScreenSize(Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height);
        }
        
    }


    public interface IHtmlRenderer: IDisposable
    {
        void SetScreenSize(int width, int height);
        void SetContent(string html);
        void SetURL(string url);
        byte[] GetImage();
    }
}
