using AppKit;
using Foundation;

namespace sampleappxm
{
	[Register ("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate ()
		{

		}

		public override void DidFinishLaunching (NSNotification notification)
		{
			var sayer = new TestLib.Sayer ();
			NSAlert alert = new NSAlert () {
				MessageText = TestHigherLib.TopLevelEntities.SayHello (sayer).ToString ()
			};
			alert.RunModal ();
			NSApplication.SharedApplication.Terminate (this);
		}

		public override void WillTerminate (NSNotification notification)
		{
			// Insert code here to tear down your application
		}
	}
}
