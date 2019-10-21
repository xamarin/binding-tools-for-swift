using UIKit;

namespace tomswiftydevicetests
{
	public class Application
	{
		static string [] _args = null;
		// This is the main entry point of the application.
		static void Main (string [] args)
		{
			if (args.Length == 0) {
				_args = new string [] {
                    // set these to report results back to the a host
                    //"-host",
                    //"192.168.5.115",
                    //"-port",
                    //"44444"
                };
			} else {
				_args = args;
			}
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}

		public static string [] Args { get { return _args; } }

	}
}
