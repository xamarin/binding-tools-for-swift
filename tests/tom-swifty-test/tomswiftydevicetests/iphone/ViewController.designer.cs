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

namespace tomswiftydevicetests
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UITextView TestOutput { get; set; }

        void ReleaseDesignerOutlets ()
        {
            if (TestOutput != null) {
                TestOutput.Dispose ();
                TestOutput = null;
            }
        }
    }
}
