// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace PigLatin
{
	[Register ("MainViewController")]
	partial class MainViewController
	{
		[Outlet]
		UIKit.UITextField InputText { get; set; }

		[Outlet]
		UIKit.UITextField OuputText { get; set; }

		[Outlet]
		UIKit.UIButton XlateButton { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (XlateButton != null) {
				XlateButton.Dispose ();
				XlateButton = null;
			}

			if (InputText != null) {
				InputText.Dispose ();
				InputText = null;
			}

			if (OuputText != null) {
				OuputText.Dispose ();
				OuputText = null;
			}
		}
	}
}
