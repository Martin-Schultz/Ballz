using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Ballz.Renderer
{
    public class GuiRenderer : DrawableGameComponent
    {
        SpriteBatch spriteBatch;
        Texture2D guiTexture;

        public class GuiData
        {
            public IntPtr Buffer = IntPtr.Zero;
            public int Width = 0;
            public int Height = 0;
        }

        public GuiData LastGuiData = new GuiData();

        public GuiRenderer(Ballz ballz) : base(ballz)
        {
            Enabled = true;
            Visible = true;
            DrawOrder = 10;
        }

        int Width = 1280;
        int Height = 720;

        ~GuiRenderer()
        {
            CefRuntime.Shutdown();
        }

        public void InitCef()
        {
            CefRuntime.Load();

            var cefApp = new DemoCefApp();

            CefRuntime.ExecuteProcess(new CefMainArgs(new string[0]), cefApp);

            var cefSettings = new CefSettings
            {
                // BrowserSubprocessPath = browserSubprocessPath,
                SingleProcess = false,
                WindowlessRenderingEnabled = true,
                MultiThreadedMessageLoop = true,
                LogSeverity = CefLogSeverity.Verbose,
                LogFile = "cef.log",
                BrowserSubprocessPath = "Xilium.CefGlue.Client.exe"
            };

            CefRuntime.Initialize(new CefMainArgs(new string[0]), cefSettings, cefApp);

            CefWindowInfo cefWindowInfo = CefWindowInfo.Create();
            cefWindowInfo.SetAsWindowless(IntPtr.Zero, true);

            var cefBrowserSettings = new CefBrowserSettings();
            CefClient = new DemoCefClient(this);

            var guiPath = System.IO.Path.GetFullPath("Content/Gui/index.html");

            CefBrowserHost.CreateBrowser(
                cefWindowInfo,
                CefClient,
                cefBrowserSettings,
                "file:///"+guiPath);
        }

        DemoCefClient CefClient;

        protected override void LoadContent()
        {
            guiTexture = new Texture2D(Game.GraphicsDevice, Width, Height, false, SurfaceFormat.Bgra32);
            spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            Width = Game.GraphicsDevice.Viewport.Width;
            Height = Game.GraphicsDevice.Viewport.Height;

            InitCef();
        }

        public override void Draw(GameTime gameTime)
        {
            Width = Game.GraphicsDevice.Viewport.Width;
            Height = Game.GraphicsDevice.Viewport.Height;

            if (LastGuiData.Buffer != IntPtr.Zero)
            {
                byte[] data;
                int size;
                lock (LastGuiData)
                {
                    size = LastGuiData.Width * LastGuiData.Height * 4;
                    data = new byte[size];
                    Marshal.Copy(LastGuiData.Buffer, data, 0, size);
                }

                if(LastGuiData.Width != guiTexture.Width || LastGuiData.Height != guiTexture.Height)
                {
                    guiTexture = new Texture2D(Game.GraphicsDevice, Width, Height, false, SurfaceFormat.Bgra32);
                }

                guiTexture.SetData(data, 0, size);

                spriteBatch.Begin();
                spriteBatch.Draw(guiTexture, new Vector2(0, 0), Microsoft.Xna.Framework.Color.White);
                spriteBatch.End();
            }
        }

        internal class DemoCefApp : CefApp
        {
        }

        internal class DemoCefClient : CefClient
        {
            DemoCefLoadHandler LoadHandler;
            DemoCefRenderHandler RenderHandler;
            GuiRenderer Renderer;

            public DemoCefClient(GuiRenderer renderer)
            {
                RenderHandler = new DemoCefRenderHandler(renderer);
                LoadHandler = new DemoCefLoadHandler();
                Renderer = renderer;
            }

            protected override CefRenderHandler GetRenderHandler()
            {
                return RenderHandler;
            }

            protected override CefLoadHandler GetLoadHandler()
            {
                return LoadHandler;
            }
        }

        internal class DemoCefLoadHandler : CefLoadHandler
        {
            protected override void OnLoadStart(CefBrowser browser, CefFrame frame)
            {
            }

            protected override void OnLoadEnd(CefBrowser browser, CefFrame frame, int httpStatusCode)
            {
            }
        }

        internal class DemoCefRenderHandler : CefRenderHandler
        {
            private readonly int WindowHeight;
            private readonly int WindowWidth;
            GuiRenderer Renderer;

            public DemoCefRenderHandler(GuiRenderer renderer)
            {
                Renderer = renderer;
            }

            protected override bool GetRootScreenRect(CefBrowser browser, ref CefRectangle rect)
            {
                return GetViewRect(browser, ref rect);
            }

            protected override bool GetScreenPoint(CefBrowser browser, int viewX, int viewY, ref int screenX, ref int screenY)
            {
                screenX = viewX;
                screenY = viewY;
                return true;
            }

            protected override bool GetViewRect(CefBrowser browser, ref CefRectangle rect)
            {
                rect.X = 0;
                rect.Y = 0;
                rect.Width = Renderer.Width;
                rect.Height = Renderer.Height;
                return true;
            }

            protected override bool GetScreenInfo(CefBrowser browser, CefScreenInfo screenInfo)
            {
                screenInfo.Rectangle = new CefRectangle { X = 0, Y = 0, Width = Renderer.Width, Height = Renderer.Height };
                return true;
            }

            protected override void OnPopupSize(CefBrowser browser, CefRectangle rect)
            {
            }

            protected override void OnPaint(CefBrowser browser, CefPaintElementType type, CefRectangle[] dirtyRects, IntPtr buffer, int width, int height)
            {
                lock (Renderer.LastGuiData)
                {
                    Renderer.LastGuiData.Width = width;
                    Renderer.LastGuiData.Height = height;
                    Renderer.LastGuiData.Buffer = buffer;
                }
            }

            protected override void OnCursorChange(CefBrowser browser, IntPtr cursorHandle, CefCursorType type, CefCursorInfo customCursorInfo)
            {
            }

            protected override void OnScrollOffsetChanged(CefBrowser browser, double x, double y)
            {
            }
        }
    }
}
