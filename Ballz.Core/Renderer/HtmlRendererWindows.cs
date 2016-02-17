#if !__MonoCS__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using System.Runtime.InteropServices;
using System.Drawing;

namespace Ballz.Renderer
{
    class HtmlRendererWindows : IHtmlRenderer
    {
        ChromiumWebBrowser Browser;
        public HtmlRendererWindows()
        {
            Cef.Initialize();
            Browser = new ChromiumWebBrowser("about:blank");
            Browser.FrameLoadEnd += (s, e) =>
            {
                OnInitialize?.Invoke();
                OnInitialize = null;
            };
        }

        const double dpi = 72;

        Size Size;

        Action OnInitialize;

        public byte[] GetImage()
        {
            Browser.Size = Size;
            var bmp = Browser.ScreenshotOrNull();

            if (bmp == null || bmp.Width != Size.Width || bmp.Height != Size.Height)
                return null;

            var lockedBits = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var numberOfBytes = 4 * bmp.Width * bmp.Height;
            var data = new byte[numberOfBytes];
            Marshal.Copy(lockedBits.Scan0, data, 0, numberOfBytes);
            bmp.UnlockBits(lockedBits);

            return data;
        }

        public void SetContent(string html)
        {
            if (Browser.IsBrowserInitialized)
                Browser.LoadHtml(html, "");
            else
                OnInitialize = () => Browser.LoadHtml(html, "");
        }

        public void SetURL(string url)
        {
            if (Browser.IsBrowserInitialized)
                Browser.Load(url);
            else
                OnInitialize = () => Browser.Load(url);
        }

        public void SetScreenSize(int width, int height)
        {
            Size = new Size(width, height);
            Browser.Size = Size;
        }
        
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Cef.Shutdown();
                disposedValue = true;
            }
        }

        
        ~HtmlRendererWindows() {
            Dispose(false);
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}

#endif
