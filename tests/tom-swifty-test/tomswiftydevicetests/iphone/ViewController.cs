using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using MonoTouch.NUnit;
using TomTest;
using Foundation;
using UIKit;

namespace tomswiftydevicetests
{
	public partial class ViewController : UIViewController
	{
		protected ViewController (IntPtr handle) : base (handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			String host = GetHost ();
			int port = GetPort ();

			TextWriter writer = null;

			try {
				writer = host != null ? (TextWriter) new TcpTextWriter (host, port) : new StringWriter ();
			} catch {
				writer = new StringWriter ();
			}

			TomTestRunner runner = new TomTestRunner (TestOutput, writer);
			runner.TestsDone += (s, e) => {
				if (writer is TcpTextWriter) {
					((TcpTextWriter) writer).Dispose ();
				}
				Console.WriteLine ("Tests done!");
				// We can't exit immediately, since all the output might not reach the desktop in that case.
				// So wait a couple of seconds, and write something in the meantime to flush buffers, etc.
				var secondsLeft = 3;
				var timer = NSTimer.CreateRepeatingTimer (TimeSpan.FromSeconds (1), (v) => {
					Console.WriteLine ($"Test run completed, will exit in {secondsLeft} seconds...");
					if (secondsLeft <= 0)
						Exit (0);
					secondsLeft--;
				});
				NSRunLoop.Main.AddTimer (timer, NSRunLoopMode.Default);
			};
			new Thread (() => {
				runner.RunAll (AppDomain.CurrentDomain.GetAssemblies ());
			}).Start ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}

		private string GetHost ()
		{
			int index = ArgIndexOf ("-host");
			return index < 0 ? null : Application.Args [index + 1];
		}

		private int GetPort ()
		{
			int index = ArgIndexOf ("-port");
			return index < 0 ? -1 : Int32.Parse (Application.Args [index + 1]);
		}

		private int ArgIndexOf (string match)
		{
			for (int i = 0; i < Application.Args.Length; i++) {
				if (Application.Args [i] == match)
					return i;
			}
			return -1;
		}

		private void GetFiles (StringBuilder sb, int depth, string directory)
		{
			string [] files = Directory.GetFiles (directory);
			string [] directories = Directory.GetDirectories (directory);
			foreach (string file in files) {
				for (int i = 0; i < depth; i++) {
					sb.Append (' ');
				}
				sb.AppendLine (Path.GetFileName (file));
			}
			foreach (string dir in directories) {
				for (int i = 0; i < depth; i++) {
					sb.Append (' ');
				}
				sb.Append (Path.GetFileName (dir)).AppendLine ("/");
				GetFiles (sb, depth + 1, Path.Combine (directory, dir));
			}
		}

		[DllImport ("__Internal", EntryPoint = "exit")]
		static extern void Exit (int status);
	}
}
