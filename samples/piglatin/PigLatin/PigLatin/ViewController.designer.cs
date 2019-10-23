// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// WARNING
//
// This file has been generated automatically by Visual Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;

namespace PigLatin
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField InputText { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextField OutputText { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton Translate { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (InputText != null) {
                InputText.Dispose ();
                InputText = null;
            }

            if (OutputText != null) {
                OutputText.Dispose ();
                OutputText = null;
            }

            if (Translate != null) {
                Translate.Dispose ();
                Translate = null;
            }
        }
    }
}

