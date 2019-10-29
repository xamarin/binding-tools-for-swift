// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

using UIKit;

namespace sampleappios
{
	public partial class ViewController : UIViewController
	{
		protected ViewController (IntPtr handle) : base (handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidAppear (bool animated)
		{
			base.ViewDidAppear (animated);

			var sayer = new TestLib.Sayer ();
			UIAlertController alert = new UIAlertController () {
				Message = TestHigherLib.TopLevelEntities.SayHello (sayer).ToString ()
			};
			alert.AddAction (UIAlertAction.Create ("OK", UIAlertActionStyle.Default, null));
			PresentViewController (alert, true, null);
		}
	}
}
