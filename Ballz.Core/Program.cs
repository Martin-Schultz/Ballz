﻿using System;

namespace Ballz
{
#if MONOMAC
   using MonoMac.AppKit;
   using MonoMac.Foundation;
   #endif
    
    internal static class Program
    {
        private static Ballz game;

        internal static void RunGame()
        {
            game = Ballz.The();
            game.Run();
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
#if !MONOMAC
        [STAThread]
#endif
        private static void Main(string[] args)
        {
#if MONOMAC
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main(args);
			}
         #else
            RunGame();
#endif
        }
    }

#if MONOMAC
	class AppDelegate : NSApplicationDelegate
	{
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs a) =>  {
				if (a.Name.StartsWith("MonoMac")) {
					return typeof(MonoMac.AppKit.AppKitFramework).Assembly;
				}
				return null;
			};
			Program.RunGame();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}  
	#endif
}