// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using EulerPhone;
using Foundation;
using UIKit;

namespace EulerPhoneApp {
	public partial class ViewController : UIViewController {
		protected ViewController (IntPtr handle) : base (handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (UITextField.TextFieldTextDidChangeNotification, (notification) =>
			{
				if (notification.Object == numberField) {
					var val = 0.0;
					if (Double.TryParse(this.numberField.Text, out val) && val >= 0) {
						this.sqrtValue.Text = TopLevelEntities.PrefixOperatorSquareRoot (val).ToString ();
						this.cubeRootValue.Text = TopLevelEntities.PrefixOperatorCubeRoot (val).ToString ();
					}
				}
			});
			base.ViewDidLoad ();
			eValue.Text = TopLevelEntities.LittleEpsilon.ToString ();
			piValue.Text = TopLevelEntities.Π.ToString ();
			tauValue.Text = TopLevelEntities.Tau.ToString ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}
