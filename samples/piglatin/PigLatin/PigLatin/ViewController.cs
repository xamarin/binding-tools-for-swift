// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using SwiftIgPay;
using SwiftRuntimeLibrary;
using UIKit;

namespace PigLatin {
	public partial class ViewController : UIViewController {
		protected ViewController (IntPtr handle) : base (handle)
		{
			// Note: this .ctor should not contain any initialization logic.
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			this.Translate.TouchUpInside += (sender, e) => {
				using (IgPay igpay = new SwiftIgPay.IgPay (SwiftString.FromString (InputText.Text))) {
					StringBuilder sb = new StringBuilder ();
					for (int i = 0; i < igpay.Count; i++) {
						using (SwiftString str = igpay [i]) {
							if (i > 0)
								sb.Append (" ");
							sb.Append (str.ToString ());
						}
					}
					OutputText.Text = sb.ToString ();
				}
			};
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}
	}
}
