// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace EulerPhoneApp
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel cubeRootValue { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel eValue { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField numberField { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel piValue { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel sqrtValue { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel tauValue { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (cubeRootValue != null) {
                cubeRootValue.Dispose ();
                cubeRootValue = null;
            }

            if (eValue != null) {
                eValue.Dispose ();
                eValue = null;
            }

            if (numberField != null) {
                numberField.Dispose ();
                numberField = null;
            }

            if (piValue != null) {
                piValue.Dispose ();
                piValue = null;
            }

            if (sqrtValue != null) {
                sqrtValue.Dispose ();
                sqrtValue = null;
            }

            if (tauValue != null) {
                tauValue.Dispose ();
                tauValue = null;
            }
        }
    }
}